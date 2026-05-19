using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateGuest : MonoBehaviour
{
    private const float SendInterval = 0.05f;
    private const float InputHeartbeatInterval = 0.5f;
    private const float InputChangeThreshold = 0.001f;

    private readonly Dictionary<int, OnlineEntityReplica> _enemyReplicas = new Dictionary<int, OnlineEntityReplica>();
    private readonly Dictionary<int, OnlineEntityReplica> _projectileReplicas = new Dictionary<int, OnlineEntityReplica>();

    private Material _meleeMat;
    private Material _rangedMat;
    private Material _projMat;
    private GameWebSocketClient _ws;
    private bool _matchCompleted;
    private OnlineMatchInputMessage _lastSentInput;
    private float _nextInputHeartbeatAt;

    private void Start()
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        GameStatsTracker.StartMatch();

        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            _meleeMat = bootstrap.meleeEnemyMaterial;
            _rangedMat = bootstrap.rangedEnemyMaterial;
            _projMat = bootstrap.enemyProjectileMaterial;
        }

        StartCoroutine(ConnectThenRun());
    }

    private IEnumerator ConnectThenRun()
    {
        string wsUrl = BuildWsUrl();
        _ws = new GameWebSocketClient();

        Task connectTask = _ws.ConnectAsync(wsUrl);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsFaulted || !_ws.IsConnected)
        {
            Debug.LogWarning("GameStateGuest: WebSocket connect failed - " + connectTask.Exception?.GetBaseException()?.Message);
            yield break;
        }

        yield return Send("{\"type\":\"register\",\"role\":\"guest\"}");
        StartCoroutine(ReceiveLoop());
        StartCoroutine(SendLoop());
    }

    private IEnumerator ReceiveLoop()
    {
        while (_ws.IsConnected)
        {
            string msg;
            while (_ws.TryReceive(out msg))
                HandleHostMessage(msg);

            yield return null;
        }
    }

    private IEnumerator SendLoop()
    {
        while (_ws.IsConnected)
        {
            OnlineMatchInputMessage input = BuildInput();
            if (ShouldSendInput(input))
            {
                yield return Send(JsonUtility.ToJson(input));
                _lastSentInput = input;
                _nextInputHeartbeatAt = Time.time + InputHeartbeatInterval;
            }

            yield return new WaitForSeconds(SendInterval);
        }
    }

    private OnlineMatchInputMessage BuildInput()
    {
        PlayerController player = PlayerController.main;
        Vector2 move = player != null && player.enabled ? player.LastMoveInput : Vector2.zero;
        Vector2 aim = player != null ? player.GetAimDirectionForNetwork() : Vector2.down;

        return new OnlineMatchInputMessage
        {
            moveX = move.x,
            moveY = move.y,
            aimX = aim.x,
            aimY = aim.y,
            attackSeq = player != null ? player.NetworkAttackSequence : 0,
            chargeSeq = player != null ? player.NetworkChargeSequence : 0,
            burstSeq = player != null ? player.NetworkBurstSequence : 0,
            consumableSeq = player != null ? player.NetworkConsumableSequence : 0,
            weaponDamage = Mathf.Max(1, PlayerLoadout.WeaponDamage),
            weaponType = PlayerLoadout.CurrentWeaponKind.ToString(),
            weaponColor = PlayerLoadout.WeaponColorHex,
            skinId = PlayerLoadout.EquippedSkinId,
            skinColor = PlayerSkinVisuals.GetEquippedSkinColorHex(),
            maxHp = Mathf.Max(1f, PlayerLoadout.MaxHP),
            consumableQuantity = Mathf.Max(0, PlayerLoadout.ConsumableQuantity),
            consumableHealAmount = Mathf.Max(0f, PlayerLoadout.ConsumableHealAmount),
            consumableCooldown = Mathf.Max(0f, PlayerLoadout.ConsumableCooldown),
            consumableIsSpeedBoost = PlayerLoadout.ConsumableIsSpeedBoost,
            speedBoostDuration = Mathf.Max(0f, PlayerLoadout.SpeedBoostDuration),
            speedBoostMultiplier = Mathf.Max(1f, PlayerLoadout.SpeedBoostMultiplier)
        };
    }

    private bool ShouldSendInput(OnlineMatchInputMessage input)
    {
        if (_lastSentInput == null)
            return true;

        if (Time.time >= _nextInputHeartbeatAt)
            return true;

        if (Mathf.Abs(input.moveX - _lastSentInput.moveX) > InputChangeThreshold) return true;
        if (Mathf.Abs(input.moveY - _lastSentInput.moveY) > InputChangeThreshold) return true;
        if (Mathf.Abs(input.aimX - _lastSentInput.aimX) > InputChangeThreshold) return true;
        if (Mathf.Abs(input.aimY - _lastSentInput.aimY) > InputChangeThreshold) return true;
        if (input.attackSeq != _lastSentInput.attackSeq) return true;
        if (input.chargeSeq != _lastSentInput.chargeSeq) return true;
        if (input.burstSeq != _lastSentInput.burstSeq) return true;
        if (input.consumableSeq != _lastSentInput.consumableSeq) return true;
        if (!string.Equals(input.weaponType, _lastSentInput.weaponType, StringComparison.Ordinal)) return true;
        if (!string.Equals(input.weaponColor, _lastSentInput.weaponColor, StringComparison.Ordinal)) return true;
        if (input.skinId != _lastSentInput.skinId) return true;
        if (!string.Equals(input.skinColor, _lastSentInput.skinColor, StringComparison.Ordinal)) return true;

        return false;
    }

    private void HandleHostMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        if (!json.Contains("\"type\":\"state\""))
            return;

        try
        {
            OnlineMatchStateMessage state = JsonUtility.FromJson<OnlineMatchStateMessage>(json);
            if (state == null) return;
            ApplyState(state);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GameStateGuest: failed to parse host state: " + ex.Message);
        }
    }

    private void ApplyState(OnlineMatchStateMessage state)
    {
        EnemySpawner.SetNetworkElapsedTime(state.elapsedSeconds);
        ApplyPlayerStates(state.players, state.matchEnded);
        ApplyEnemyState(state.enemies);
        ApplyProjectileState(state.projectiles);

        if (state.matchEnded && !_matchCompleted)
        {
            _matchCompleted = true;
            DisableLocalPlayer();
            GameStatsTracker.CompleteNetworkMatch(
                state.meleeKills,
                state.rangedKills,
                Mathf.Max(0, state.elapsedSeconds));
        }
    }

    private void ApplyPlayerStates(OnlinePlayerState[] players, bool matchEnded)
    {
        if (players == null) return;

        foreach (OnlinePlayerState player in players)
        {
            if (player == null) continue;

            if (player.id == 0)
                ApplyRemoteHostPlayer(player);
            else if (player.id == 1)
                ApplyLocalGuestPlayer(player, matchEnded);
        }
    }

    private void ApplyRemoteHostPlayer(OnlinePlayerState state)
    {
        if (OnlinePlayerSync.Instance == null) return;

        if (!state.alive)
        {
            OnlinePlayerSync.Instance.ClearRemotePlayer();
            return;
        }

        OnlinePlayerSync.Instance.SetRemoteState(
            new Vector3(state.x, state.y, 0f),
            new Vector3(state.vx, state.vy, 0f),
            state.skinId,
            state.skinColor);
    }

    private void ApplyLocalGuestPlayer(OnlinePlayerState state, bool matchEnded)
    {
        PlayerController player = PlayerController.main;
        if (player == null) return;

        Health health = player.GetComponent<Health>();
        if (health != null)
        {
            health.maxHp = Mathf.Max(state.maxHp, state.hp);
            health.hp = Mathf.Clamp(state.hp, 0f, Mathf.Max(health.maxHp, 0.01f));
        }

        Vector3 authoritative = new Vector3(state.x, state.y, player.transform.position.z);
        float distance = Vector3.Distance(player.transform.position, authoritative);
        player.transform.position = distance > 2f
            ? authoritative
            : Vector3.Lerp(player.transform.position, authoritative, 0.18f);

        SetLocalPlayerVisibleAndControllable(state.alive && !matchEnded);
    }

    private void SetLocalPlayerVisibleAndControllable(bool alive)
    {
        PlayerController player = PlayerController.main;
        if (player == null) return;

        player.enabled = alive;

        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = alive;

        Collider2D collider = player.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = alive;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null && !alive) body.linearVelocity = Vector2.zero;
    }

    private void DisableLocalPlayer()
    {
        SetLocalPlayerVisibleAndControllable(false);
    }

    private void ApplyEnemyState(OnlineEnemyState[] enemies)
    {
        var activeIds = new HashSet<int>();

        if (enemies != null)
        {
            foreach (OnlineEnemyState enemy in enemies)
            {
                if (enemy == null || enemy.hp <= 0f) continue;
                activeIds.Add(enemy.id);

                if (_enemyReplicas.TryGetValue(enemy.id, out OnlineEntityReplica replica) && replica != null)
                {
                    UpdateEnemyReplica(replica, enemy);
                }
                else
                {
                    _enemyReplicas[enemy.id] = SpawnEnemyReplica(enemy);
                }
            }
        }

        RemoveInactive(_enemyReplicas, activeIds);
    }

    private OnlineEntityReplica SpawnEnemyReplica(OnlineEnemyState enemy)
    {
        bool isRanged = enemy.type == 1;
        bool isGiant = enemy.type == 2;
        GameObject go = new GameObject(isGiant ? "EnemyReplicaGiant" : isRanged ? "EnemyReplicaRanged" : "EnemyReplicaMelee");
        go.transform.position = new Vector3(enemy.x, enemy.y, 0f);
        go.transform.localScale = new Vector3(Mathf.Max(0.2f, enemy.size), Mathf.Max(0.2f, enemy.size), 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SimpleSprite.Square;
        sr.sortingOrder = 5;
        if (isGiant)
        {
            sr.color = new Color(0.45f, 0.2f, 0.75f, 1f);
            if (_meleeMat != null) { sr.sharedMaterial = _meleeMat; sr.color = Color.white; }
        }
        else if (isRanged)
        {
            sr.color = new Color(1f, 0.65f, 0.2f, 1f);
            if (_rangedMat != null) { sr.sharedMaterial = _rangedMat; sr.color = Color.white; }
        }
        else
        {
            sr.color = new Color(0.25f, 1f, 0.25f, 1f);
            if (_meleeMat != null) { sr.sharedMaterial = _meleeMat; sr.color = Color.white; }
        }

        Health health = go.AddComponent<Health>();
        health.maxHp = Mathf.Max(0.1f, enemy.maxHp);
        health.hp = Mathf.Clamp(enemy.hp, 0.01f, health.maxHp);

        OnlineEntityReplica replica = go.AddComponent<OnlineEntityReplica>();
        replica.SnapTo(go.transform.position);
        return replica;
    }

    private void UpdateEnemyReplica(OnlineEntityReplica replica, OnlineEnemyState enemy)
    {
        replica.transform.localScale = new Vector3(Mathf.Max(0.2f, enemy.size), Mathf.Max(0.2f, enemy.size), 1f);
        replica.SetTarget(
            new Vector3(enemy.x, enemy.y, 0f),
            new Vector3(enemy.vx, enemy.vy, 0f),
            14f);

        Health health = replica.GetComponent<Health>();
        if (health != null)
        {
            health.maxHp = Mathf.Max(0.1f, enemy.maxHp);
            health.hp = Mathf.Clamp(enemy.hp, 0.01f, health.maxHp);
        }
    }

    private void ApplyProjectileState(OnlineProjectileState[] projectiles)
    {
        var activeIds = new HashSet<int>();

        if (projectiles != null)
        {
            foreach (OnlineProjectileState projectile in projectiles)
            {
                if (projectile == null || projectile.life <= 0f) continue;
                if (projectile.fromPlayer && projectile.ownerId == 1) continue;
                activeIds.Add(projectile.id);

                if (_projectileReplicas.TryGetValue(projectile.id, out OnlineEntityReplica replica) && replica != null)
                {
                    ApplyProjectileReplicaShape(replica.transform, projectile);
                    replica.SetTarget(
                        new Vector3(projectile.x, projectile.y, 0f),
                        new Vector3(projectile.vx, projectile.vy, 0f),
                        20f);
                }
                else
                {
                    _projectileReplicas[projectile.id] = SpawnProjectileReplica(projectile);
                }
            }
        }

        RemoveInactive(_projectileReplicas, activeIds);
    }

    private OnlineEntityReplica SpawnProjectileReplica(OnlineProjectileState projectile)
    {
        GameObject go = new GameObject(projectile.fromPlayer ? "PlayerProjectileReplica" : "ProjectileReplica");
        go.transform.position = new Vector3(projectile.x, projectile.y, 0f);
        ApplyProjectileReplicaShape(go.transform, projectile);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SimpleSprite.Square;
        sr.color = projectile.fromPlayer || projectile.isHitbox
            ? PlayerLoadout.ParseWeaponColor(projectile.color, Color.white)
            : new Color(1f, 0.55f, 0.15f, 1f);
        sr.sortingOrder = 9;
        if (!projectile.fromPlayer && _projMat != null) { sr.sharedMaterial = _projMat; sr.color = Color.white; }

        OnlineEntityReplica replica = go.AddComponent<OnlineEntityReplica>();
        replica.SnapTo(go.transform.position);
        return replica;
    }

    private void ApplyProjectileReplicaShape(Transform target, OnlineProjectileState projectile)
    {
        if (target == null) return;

        if (projectile.isHitbox)
        {
            float scaleX = Mathf.Max(0.05f, projectile.scaleX);
            float scaleY = Mathf.Max(0.05f, projectile.scaleY);
            target.localScale = new Vector3(scaleX, scaleY, 1f);
            target.rotation = Quaternion.Euler(0f, 0f, projectile.rotationZ);
            return;
        }

        float size = projectile.fromPlayer && projectile.size > 0f ? projectile.size : 0.25f;
        target.localScale = new Vector3(size, size, 1f);
    }

    private void RemoveInactive(Dictionary<int, OnlineEntityReplica> replicas, HashSet<int> activeIds)
    {
        var dead = new List<int>();
        foreach (var kv in replicas)
        {
            if (!activeIds.Contains(kv.Key) || kv.Value == null)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
                dead.Add(kv.Key);
            }
        }

        foreach (int id in dead)
            replicas.Remove(id);
    }

    private IEnumerator Send(string json)
    {
        if (_ws == null || !_ws.IsConnected) yield break;
        Task t = _ws.SendAsync(json);
        yield return new WaitUntil(() => t.IsCompleted);
    }

    private static string BuildWsUrl()
    {
        string baseUrl = GameStatsTracker.ApiBaseUrl.TrimEnd('/');
        string wsUrl = baseUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        wsUrl += "/game-ws?room=" + Mathf.Max(1, MultiplayerState.OnlineRoomNumber);
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            wsUrl += "&token=" + Uri.EscapeDataString(AuthSession.AccessToken);
        return wsUrl;
    }

    private void OnDestroy()
    {
        _ws?.Dispose();
    }
}

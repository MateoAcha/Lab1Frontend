using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateHost : MonoBehaviour
{
    private const float SyncInterval = 1f / 15f;
    private const float AttackVisualMinLife = 0.22f;

    private GameWebSocketClient _ws;
    private PlayerController _remotePlayer;
    private readonly Dictionary<int, AttackVisualSnapshot> _attackVisuals = new Dictionary<int, AttackVisualSnapshot>();
    private int _lastAttackSeq;
    private int _lastChargeSeq;
    private int _lastBurstSeq;
    private int _lastConsumableSeq;
    private int _nextEntityId = 1;
    private int _tick;
    private bool _sawRunActive;

    private void Start()
    {
        HitBox.Spawned += HandleHitBoxSpawned;
        _remotePlayer = FindRemotePlayer();
        StartCoroutine(ConnectThenSync());
    }

    private IEnumerator ConnectThenSync()
    {
        string wsUrl = BuildWsUrl();
        _ws = new GameWebSocketClient();

        Task connectTask = _ws.ConnectAsync(wsUrl);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsFaulted || !_ws.IsConnected)
        {
            Debug.LogWarning("GameStateHost: WebSocket connect failed - " + connectTask.Exception?.GetBaseException()?.Message);
            yield break;
        }

        yield return Send("{\"type\":\"register\",\"role\":\"host\"}");
        StartCoroutine(SyncLoop());
    }

    private IEnumerator SyncLoop()
    {
        while (_ws.IsConnected)
        {
            string msg;
            while (_ws.TryReceive(out msg))
                HandleGuestMessage(msg);

            if (GameStatsTracker.IsRunActive)
                _sawRunActive = true;

            yield return Send(JsonUtility.ToJson(BuildState()));
            yield return new WaitForSeconds(SyncInterval);
        }
    }

    private void HandleGuestMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"input\""))
        {
            return;
        }

        try
        {
            OnlineMatchInputMessage input = JsonUtility.FromJson<OnlineMatchInputMessage>(json);
            if (input == null) return;
            ApplyGuestInput(input);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GameStateHost: failed to parse guest input: " + ex.Message);
        }
    }

    private void ApplyGuestInput(OnlineMatchInputMessage input)
    {
        if (_remotePlayer == null)
            _remotePlayer = FindRemotePlayer();
        if (_remotePlayer == null)
            return;

        _remotePlayer.ConfigureNetworkLoadout(
            input.weaponDamage,
            input.weaponType,
            input.weaponColor,
            input.skinId,
            input.skinColor,
            input.maxHp,
            input.consumableQuantity,
            input.consumableHealAmount,
            input.consumableCooldown,
            input.consumableIsSpeedBoost,
            input.speedBoostDuration,
            input.speedBoostMultiplier);

        bool attackDown = input.attackSeq > 0 && input.attackSeq != _lastAttackSeq;
        bool chargeDown = input.chargeSeq > 0 && input.chargeSeq != _lastChargeSeq;
        bool burstDown = input.burstSeq > 0 && input.burstSeq != _lastBurstSeq;
        bool consumableDown = input.consumableSeq > 0 && input.consumableSeq != _lastConsumableSeq;

        _lastAttackSeq = input.attackSeq;
        _lastChargeSeq = input.chargeSeq;
        _lastBurstSeq = input.burstSeq;
        _lastConsumableSeq = input.consumableSeq;

        _remotePlayer.ApplyExternalInput(
            new Vector2(input.moveX, input.moveY),
            new Vector2(input.aimX, input.aimY),
            attackDown,
            chargeDown,
            burstDown,
            consumableDown);
    }

    private OnlineMatchStateMessage BuildState()
    {
        bool ended = _sawRunActive && !GameStatsTracker.IsRunActive;
        int meleeKills = ended ? GameStatsTracker.LastRunMeleeKills : GameStatsTracker.CurrentMeleeKills;
        int rangedKills = ended ? GameStatsTracker.LastRunRangedKills : GameStatsTracker.CurrentRangedKills;
        int seconds = ended ? GameStatsTracker.LastRunTimeSeconds : GameStatsTracker.CurrentRunTimeSeconds;

        return new OnlineMatchStateMessage
        {
            tick = ++_tick,
            matchEnded = ended,
            meleeKills = meleeKills,
            rangedKills = rangedKills,
            elapsedSeconds = seconds,
            players = BuildPlayers(),
            enemies = BuildEnemies(),
            projectiles = BuildProjectiles()
        };
    }

    private OnlinePlayerState[] BuildPlayers()
    {
        if (_remotePlayer == null)
            _remotePlayer = FindRemotePlayer();

        return new[]
        {
            BuildPlayerState(0, PlayerController.main),
            BuildPlayerState(1, _remotePlayer)
        };
    }

    private OnlinePlayerState BuildPlayerState(int id, PlayerController player)
    {
        Health health = player != null ? player.GetComponent<Health>() : null;
        bool alive = player != null && health != null && health.hp > 0f;
        Vector3 pos = player != null ? player.transform.position : Vector3.zero;
        Rigidbody2D body = player != null ? player.GetComponent<Rigidbody2D>() : null;
        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;

        return new OnlinePlayerState
        {
            id = id,
            x = pos.x,
            y = pos.y,
            vx = velocity.x,
            vy = velocity.y,
            hp = health != null ? health.hp : 0f,
            maxHp = health != null ? Mathf.Max(health.maxHp, health.hp) : 0f,
            alive = alive,
            skinId = GetPlayerSkinId(id, player),
            skinColor = GetPlayerSkinColor(id, player)
        };
    }

    private int GetPlayerSkinId(int id, PlayerController player)
    {
        if (id == 0)
            return PlayerLoadout.EquippedSkinId;
        return player != null ? player.NetworkSkinId : 0;
    }

    private string GetPlayerSkinColor(int id, PlayerController player)
    {
        if (id == 0)
            return PlayerSkinVisuals.GetEquippedSkinColorHex();
        return player != null ? player.NetworkSkinColor : "#4DBFFF";
    }

    private OnlineEnemyState[] BuildEnemies()
    {
        var enemies = new List<OnlineEnemyState>();

        for (int i = OnlineNetworkRegistry.MeleeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = OnlineNetworkRegistry.MeleeEnemies[i];
            if (enemy == null)
            {
                OnlineNetworkRegistry.MeleeEnemies.RemoveAt(i);
                continue;
            }
            AddEnemyState(enemies, enemy.gameObject, 0);
        }

        for (int i = OnlineNetworkRegistry.RangedEnemies.Count - 1; i >= 0; i--)
        {
            RangedEnemyController enemy = OnlineNetworkRegistry.RangedEnemies[i];
            if (enemy == null)
            {
                OnlineNetworkRegistry.RangedEnemies.RemoveAt(i);
                continue;
            }
            AddEnemyState(enemies, enemy.gameObject, 1);
        }

        for (int i = OnlineNetworkRegistry.GiantEnemies.Count - 1; i >= 0; i--)
        {
            GiantEnemyController enemy = OnlineNetworkRegistry.GiantEnemies[i];
            if (enemy == null)
            {
                OnlineNetworkRegistry.GiantEnemies.RemoveAt(i);
                continue;
            }
            AddEnemyState(enemies, enemy.gameObject, 2);
        }

        return enemies.ToArray();
    }

    private void AddEnemyState(List<OnlineEnemyState> enemies, GameObject enemy, int type)
    {
        if (enemy == null) return;

        Health health = enemy.GetComponent<Health>();
        if (health != null && health.hp <= 0f) return;
        Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;

        enemies.Add(new OnlineEnemyState
        {
            id = GetOrAssignId(enemy),
            type = type,
            x = enemy.transform.position.x,
            y = enemy.transform.position.y,
            vx = velocity.x,
            vy = velocity.y,
            hp = health != null ? health.hp : 1f,
            maxHp = health != null ? Mathf.Max(health.maxHp, health.hp) : 1f,
            size = enemy.transform.localScale.x
        });
    }

    private OnlineProjectileState[] BuildProjectiles()
    {
        var projectiles = new List<OnlineProjectileState>();

        for (int i = OnlineNetworkRegistry.Projectiles.Count - 1; i >= 0; i--)
        {
            EnemyProjectile projectile = OnlineNetworkRegistry.Projectiles[i];
            if (projectile == null)
            {
                OnlineNetworkRegistry.Projectiles.RemoveAt(i);
                continue;
            }

            projectiles.Add(new OnlineProjectileState
            {
                id = GetOrAssignId(projectile.gameObject),
                fromPlayer = false,
                x = projectile.transform.position.x,
                y = projectile.transform.position.y,
                vx = projectile.direction.x * projectile.speed,
                vy = projectile.direction.y * projectile.speed,
                life = projectile.RemainingLife
            });
        }

        for (int i = OnlineNetworkRegistry.PlayerProjectiles.Count - 1; i >= 0; i--)
        {
            PlayerProjectile projectile = OnlineNetworkRegistry.PlayerProjectiles[i];
            if (projectile == null)
            {
                OnlineNetworkRegistry.PlayerProjectiles.RemoveAt(i);
                continue;
            }

            projectiles.Add(new OnlineProjectileState
            {
                id = GetOrAssignId(projectile.gameObject),
                fromPlayer = true,
                ownerId = projectile.ownerPlayerIndex,
                color = "#" + ColorUtility.ToHtmlStringRGB(projectile.projectileColor),
                size = Mathf.Max(0.05f, projectile.transform.localScale.x),
                x = projectile.transform.position.x,
                y = projectile.transform.position.y,
                vx = projectile.direction.x * projectile.speed,
                vy = projectile.direction.y * projectile.speed,
                life = projectile.RemainingLife
            });
        }

        AddPlayerHitBoxStates(projectiles);

        return projectiles.ToArray();
    }

    private void AddPlayerHitBoxStates(List<OnlineProjectileState> projectiles)
    {
        var activeIds = new HashSet<int>();

        for (int i = OnlineNetworkRegistry.PlayerHitBoxes.Count - 1; i >= 0; i--)
        {
            HitBox hitBox = OnlineNetworkRegistry.PlayerHitBoxes[i];
            if (hitBox == null)
            {
                OnlineNetworkRegistry.PlayerHitBoxes.RemoveAt(i);
                continue;
            }

            int id = GetOrAssignId(hitBox.gameObject);
            activeIds.Add(id);
            float expireAt = Time.time + hitBox.RemainingLife;
            if (_attackVisuals.TryGetValue(id, out AttackVisualSnapshot existing))
                expireAt = Mathf.Max(expireAt, existing.expireAt);
            else
                expireAt = Time.time + Mathf.Max(AttackVisualMinLife, hitBox.RemainingLife);

            AttackVisualSnapshot snapshot = BuildAttackVisualSnapshot(id, hitBox, expireAt);
            _attackVisuals[id] = snapshot;
            projectiles.Add(snapshot.ToState(Time.time));
        }

        var expired = new List<int>();
        foreach (var kv in _attackVisuals)
        {
            if (activeIds.Contains(kv.Key))
                continue;
            if (Time.time > kv.Value.expireAt)
            {
                expired.Add(kv.Key);
                continue;
            }

            projectiles.Add(kv.Value.ToState(Time.time));
        }

        foreach (int id in expired)
            _attackVisuals.Remove(id);
    }

    private void HandleHitBoxSpawned(HitBox hitBox)
    {
        if (hitBox == null || hitBox.ownerPlayerIndex < 0)
            return;

        int id = GetOrAssignId(hitBox.gameObject);
        _attackVisuals[id] = BuildAttackVisualSnapshot(
            id,
            hitBox,
            Time.time + Mathf.Max(AttackVisualMinLife, hitBox.life));
    }

    private AttackVisualSnapshot BuildAttackVisualSnapshot(int id, HitBox hitBox, float expireAt)
    {
        Transform t = hitBox.transform;
        return new AttackVisualSnapshot
        {
            id = id,
            ownerId = hitBox.ownerPlayerIndex,
            color = "#" + ColorUtility.ToHtmlStringRGBA(hitBox.visualColor),
            x = t.position.x,
            y = t.position.y,
            rotationZ = t.eulerAngles.z,
            scaleX = Mathf.Max(0.05f, t.localScale.x),
            scaleY = Mathf.Max(0.05f, t.localScale.y),
            expireAt = expireAt
        };
    }

    private int GetOrAssignId(GameObject obj)
    {
        NetworkEntityId id = obj.GetComponent<NetworkEntityId>();
        if (id == null)
            id = obj.AddComponent<NetworkEntityId>();

        if (id.Id <= 0)
            id.Id = _nextEntityId++;

        return id.Id;
    }

    private PlayerController FindRemotePlayer()
    {
        return MultiplayerState.GetPlayerByIndex(1);
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
        HitBox.Spawned -= HandleHitBoxSpawned;
        _ws?.Dispose();
    }

    private class AttackVisualSnapshot
    {
        public int id;
        public int ownerId;
        public string color;
        public float x;
        public float y;
        public float rotationZ;
        public float scaleX;
        public float scaleY;
        public float expireAt;

        public OnlineProjectileState ToState(float now)
        {
            return new OnlineProjectileState
            {
                id = id,
                fromPlayer = true,
                ownerId = ownerId,
                isHitbox = true,
                color = color,
                size = Mathf.Max(scaleX, scaleY),
                scaleX = scaleX,
                scaleY = scaleY,
                rotationZ = rotationZ,
                x = x,
                y = y,
                vx = 0f,
                vy = 0f,
                life = Mathf.Max(0.01f, expireAt - now)
            };
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStateGuest : MonoBehaviour
{
    private const float SendInterval = 0.05f;
    private const float InputHeartbeatInterval = 0.5f;
    private const float InputChangeThreshold = 0.001f;

    private readonly Dictionary<int, OnlineEntityReplica> _enemyReplicas = new Dictionary<int, OnlineEntityReplica>();
    private readonly Dictionary<int, OnlineEntityReplica> _projectileReplicas = new Dictionary<int, OnlineEntityReplica>();
    private readonly Dictionary<int, GameObject> _itemDropReplicas = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, GameObject> _abilityReplicas = new Dictionary<int, GameObject>();
    private int _pendingPickupId = -1;
    private int _appliedMapIndex = -1;

    private Material _meleeMat;
    private Material _rangedMat;
    private Material _projMat;
    private Sprite[] _enemyAttackOrbSprites;
    private Texture2D _enemyAttackOrbTexture;
    private int _enemyAttackOrbFrameCount = 3;
    private float _enemyAttackOrbFps = 10f;
    private GameWebSocketClient _ws;
    private bool _matchCompleted;
    private bool _hostPaused;
    private GameObject _hostPauseNotice;
    private OnlineMatchInputMessage _lastSentInput;
    private float _nextInputHeartbeatAt;

    private void Start()
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        GameAudio.StopMusic(); // will start again once first map state arrives

        GameStatsTracker.StartMatch();

        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            _meleeMat = bootstrap.meleeEnemyMaterial;
            _rangedMat = bootstrap.rangedEnemyMaterial;
            _projMat = bootstrap.enemyProjectileMaterial;
            _enemyAttackOrbSprites = bootstrap.enemyAttackOrbSprites;
            _enemyAttackOrbTexture = bootstrap.enemyAttackOrbTexture;
            _enemyAttackOrbFrameCount = bootstrap.enemyAttackOrbFrameCount;
            _enemyAttackOrbFps = bootstrap.enemyAttackOrbFps;
        }

        BuildHostPauseNotice();
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

        if (!_matchCompleted)
            HandleHostLeft();
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

        OnlineMatchInputMessage input = new OnlineMatchInputMessage
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
            weaponItemId = PlayerLoadout.EquippedWeaponItemId,
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

        FillSkillInput(input, SkillWeaponBranch.SwordSpear, SkillSlotKind.Active);
        FillSkillInput(input, SkillWeaponBranch.SwordSpear, SkillSlotKind.Passive);
        FillSkillInput(input, SkillWeaponBranch.Ranged, SkillSlotKind.Active);
        FillSkillInput(input, SkillWeaponBranch.Ranged, SkillSlotKind.Passive);
        if (_pendingPickupId >= 0)
        {
            input.pickedUpItemId = _pendingPickupId;
            _pendingPickupId = -1;
        }
        return input;
    }

    private static void FillSkillInput(OnlineMatchInputMessage input, SkillWeaponBranch branch, SkillSlotKind slotKind)
    {
        if (input == null)
        {
            return;
        }

        PlayerSkillDefinition skill = PlayerSkillLoadout.GetEquipped(branch, slotKind);
        string skillId = skill != null ? skill.id : "";
        int level = skill != null ? PlayerSkillLoadout.GetSkillLevel(skill.id) : 0;

        if (branch == SkillWeaponBranch.SwordSpear && slotKind == SkillSlotKind.Active)
        {
            input.swordSpearActiveSkillId = skillId;
            input.swordSpearActiveSkillLevel = level;
        }
        else if (branch == SkillWeaponBranch.SwordSpear && slotKind == SkillSlotKind.Passive)
        {
            input.swordSpearPassiveSkillId = skillId;
            input.swordSpearPassiveSkillLevel = level;
        }
        else if (branch == SkillWeaponBranch.Ranged && slotKind == SkillSlotKind.Active)
        {
            input.rangedActiveSkillId = skillId;
            input.rangedActiveSkillLevel = level;
        }
        else if (branch == SkillWeaponBranch.Ranged && slotKind == SkillSlotKind.Passive)
        {
            input.rangedPassiveSkillId = skillId;
            input.rangedPassiveSkillLevel = level;
        }
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
        if (input.weaponItemId != _lastSentInput.weaponItemId) return true;
        if (!string.Equals(input.weaponType, _lastSentInput.weaponType, StringComparison.Ordinal)) return true;
        if (!string.Equals(input.weaponColor, _lastSentInput.weaponColor, StringComparison.Ordinal)) return true;
        if (input.skinId != _lastSentInput.skinId) return true;
        if (!string.Equals(input.skinColor, _lastSentInput.skinColor, StringComparison.Ordinal)) return true;
        if (!string.Equals(input.swordSpearActiveSkillId, _lastSentInput.swordSpearActiveSkillId, StringComparison.Ordinal)) return true;
        if (input.swordSpearActiveSkillLevel != _lastSentInput.swordSpearActiveSkillLevel) return true;
        if (!string.Equals(input.swordSpearPassiveSkillId, _lastSentInput.swordSpearPassiveSkillId, StringComparison.Ordinal)) return true;
        if (input.swordSpearPassiveSkillLevel != _lastSentInput.swordSpearPassiveSkillLevel) return true;
        if (!string.Equals(input.rangedActiveSkillId, _lastSentInput.rangedActiveSkillId, StringComparison.Ordinal)) return true;
        if (input.rangedActiveSkillLevel != _lastSentInput.rangedActiveSkillLevel) return true;
        if (!string.Equals(input.rangedPassiveSkillId, _lastSentInput.rangedPassiveSkillId, StringComparison.Ordinal)) return true;
        if (input.rangedPassiveSkillLevel != _lastSentInput.rangedPassiveSkillLevel) return true;

        return false;
    }

    private void HandleHostMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        if (json.Contains("\"host_left\""))
        {
            HandleHostLeft();
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
        ApplyMapState(state.mapIndex);
        ApplyExitState(state);
        ApplyPlayerStates(state.players, state.matchEnded);
        ApplyHostPauseState(state.pausedByHost, state.matchEnded);
        ApplyMatchEndingState(state.matchEnding, state.matchEnded);
        ApplyEnemyState(state.enemies);
        ApplyProjectileState(state.projectiles);
        ApplyItemDropState(state.itemDrops);
        ApplyAbilityEffects(state.abilities);

        if (state.matchEnded && !_matchCompleted)
        {
            _matchCompleted = true;
            if (state.matchFinished)
            {
                Time.timeScale = 0f;
            }
            DisableLocalPlayer();
            GameStatsTracker.CompleteNetworkMatch(
                state.meleeKills,
                state.rangedKills,
                state.giantKills,
                Mathf.Max(0, state.elapsedSeconds),
                state.matchFinished);
        }
    }

    private void ApplyExitState(OnlineMatchStateMessage state)
    {
        if (state == null)
        {
            return;
        }

        if (state.exits != null && state.exits.Length > 0)
        {
            MatchExit.SetSyncedStates(state.exits);
            return;
        }

        MatchExit.SetSyncedPosition(new Vector2(state.exitX, state.exitY), state.exitActive);
    }

    private void ApplyMapState(int mapIndex)
    {
        if (mapIndex < 0)
        {
            return;
        }

        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            bootstrap.ApplyMapSelection(mapIndex, true);
        }
        else
        {
            GameMapSelection.Select(mapIndex);
        }

        if (mapIndex != _appliedMapIndex)
        {
            _appliedMapIndex = mapIndex;
            GameAudio.EnsureMatchMusic(mapIndex);
        }
    }

    private void ApplyMatchEndingState(bool matchEnding, bool matchEnded)
    {
        if (!matchEnding || matchEnded)
        {
            return;
        }

        Time.timeScale = 0f;

        PlayerController player = PlayerController.main;
        if (player == null)
        {
            return;
        }

        player.enabled = false;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
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
            state.skinColor,
            state.hp,
            state.maxHp,
            state.attackSeq,
            state.weaponType,
            state.weaponColor);
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

    private void ApplyHostPauseState(bool paused, bool matchEnded)
    {
        _hostPaused = paused && !matchEnded;
        if (_hostPauseNotice != null)
            _hostPauseNotice.SetActive(_hostPaused);

        PlayerController player = PlayerController.main;
        if (player == null || matchEnded) return;

        Health health = player.GetComponent<Health>();
        bool alive = health == null || health.hp > 0f;
        if (!alive) return;

        player.enabled = !_hostPaused;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null && _hostPaused)
            body.linearVelocity = Vector2.zero;
    }

    private void DisableLocalPlayer()
    {
        SetLocalPlayerVisibleAndControllable(false);
    }

    private void HandleHostLeft()
    {
        if (_matchCompleted) return;
        _matchCompleted = true;
        Time.timeScale = 1f;
        MultiplayerState.RequestReturnToOnlineMenu();
        SceneManager.LoadScene("Menu");
    }

    private void BuildHostPauseNotice()
    {
        GameObject canvasObj = new GameObject("HostPauseNoticeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        _hostPauseNotice = new GameObject("HostPauseNotice");
        _hostPauseNotice.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = _hostPauseNotice.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(520f, 56f);
        rect.anchoredPosition = new Vector2(0f, -28f);

        TextMeshProUGUI text = _hostPauseNotice.AddComponent<TextMeshProUGUI>();
        text.text = "Host paused the game";
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.92f, 0.45f, 1f);
        text.raycastTarget = false;

        _hostPauseNotice.SetActive(false);
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
        bool isGiant  = enemy.type == 2;
        bool isMelee  = !isRanged && !isGiant;

        GameObject go = new GameObject(isGiant ? "EnemyReplicaGiant" : isRanged ? "EnemyReplicaRanged" : "EnemyReplicaMelee");
        go.transform.position   = new Vector3(enemy.x, enemy.y, 0f);
        go.transform.localScale = new Vector3(Mathf.Max(0.2f, enemy.size), Mathf.Max(0.2f, enemy.size), 1f);

        // All animated enemy types use a child sprite so spriteScale adjusts
        // visual size without affecting the parent's hitbox.
        GameObject srTarget = new GameObject("Sprite");
        srTarget.transform.SetParent(go.transform);
        srTarget.transform.localPosition = Vector3.zero;
        srTarget.transform.localScale    = Vector3.one;

        SpriteRenderer sr = srTarget.AddComponent<SpriteRenderer>();
        sr.sprite       = SimpleSprite.Square;
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
            sr.color = Color.white;
            if (_meleeMat != null) sr.sharedMaterial = _meleeMat;
        }

        Health health  = go.AddComponent<Health>();
        health.maxHp   = Mathf.Max(0.1f, enemy.maxHp);
        health.hp      = Mathf.Clamp(enemy.hp, 0.01f, health.maxHp);

        if (isMelee)
            srTarget.AddComponent<MeleeEnemyAnimator>();
        else if (isRanged)
            srTarget.AddComponent<RangedEnemyAnimator>();
        else if (isGiant)
            srTarget.AddComponent<GiantEnemyAnimator>();

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
                if (projectile.fromPlayer && projectile.ownerId == 1 && !projectile.isMinion) continue;
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
        GameObject go = new GameObject(projectile.isMinion ? "MinionReplica" : projectile.fromPlayer ? "PlayerProjectileReplica" : "ProjectileReplica");
        go.transform.position = new Vector3(projectile.x, projectile.y, 0f);
        ApplyProjectileReplicaShape(go.transform, projectile);

        if (projectile.isMinion)
        {
            SpriteRenderer msr = go.AddComponent<SpriteRenderer>();
            msr.sprite = SimpleSprite.Circle;
            msr.color = PlayerLoadout.ParseWeaponColor(projectile.color, new Color(0.8f, 0.6f, 1f, 1f));
            msr.sortingOrder = 9;
            OnlineEntityReplica mr = go.AddComponent<OnlineEntityReplica>();
            mr.SnapTo(go.transform.position);
            return mr;
        }

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite[] enemyFrames = projectile.fromPlayer || projectile.isHitbox
            ? null
            : EnemyProjectileAnimator.ResolveFrames(_enemyAttackOrbSprites, _enemyAttackOrbTexture, _enemyAttackOrbFrameCount);
        bool hasEnemyOrbSprite = enemyFrames != null && enemyFrames.Length > 0 && enemyFrames[0] != null;
        sr.sprite = hasEnemyOrbSprite ? enemyFrames[0] : SimpleSprite.Square;
        sr.color = hasEnemyOrbSprite
            ? Color.white
            : (projectile.fromPlayer || projectile.isHitbox
                ? PlayerLoadout.ParseWeaponColor(projectile.color, Color.white)
                : new Color(1f, 0.55f, 0.15f, 1f));
        sr.sortingOrder = 9;
        if (!projectile.fromPlayer && _projMat != null) { sr.sharedMaterial = _projMat; sr.color = Color.white; }
        if (hasEnemyOrbSprite && enemyFrames.Length > 1)
        {
            EnemyProjectileAnimator animator = go.AddComponent<EnemyProjectileAnimator>();
            animator.frames = enemyFrames;
            animator.fps = _enemyAttackOrbFps;
        }

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

        float size = projectile.size > 0f ? projectile.size : 0.25f;
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

    private void ApplyItemDropState(OnlineItemDropState[] drops)
    {
        var activeIds = new HashSet<int>();
        if (drops != null)
        {
            foreach (OnlineItemDropState drop in drops)
            {
                if (drop == null) continue;
                activeIds.Add(drop.id);
                if (!_itemDropReplicas.TryGetValue(drop.id, out GameObject go) || go == null)
                {
                    go = new GameObject("ItemDropReplica");
                    go.transform.position = new Vector3(drop.x, drop.y, 0f);
                    go.transform.localScale = Vector3.one * 0.9f;
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SimpleSprite.Circle;
                    sr.color = new Color(1f, 0.85f, 0.2f, 1f);
                    sr.sortingOrder = 4;
                    CircleCollider2D col = go.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.55f;
                    _itemDropReplicas[drop.id] = go;
                }
                else
                {
                    go.transform.position = new Vector3(drop.x, drop.y, 0f);
                }
            }
        }

        var deadDrops = new List<int>();
        foreach (var kv in _itemDropReplicas)
        {
            if (!activeIds.Contains(kv.Key) || kv.Value == null)
            {
                if (kv.Value != null) Destroy(kv.Value);
                deadDrops.Add(kv.Key);
            }
        }
        foreach (int id in deadDrops) _itemDropReplicas.Remove(id);

        CheckItemPickup();
    }

    private void CheckItemPickup()
    {
        if (_pendingPickupId >= 0 || _itemDropReplicas.Count == 0) return;
        PlayerController player = PlayerController.main;
        if (player == null) return;
        Vector2 playerPos = player.transform.position;
        foreach (var kv in _itemDropReplicas)
        {
            if (kv.Value == null) continue;
            float dist = Vector2.Distance(playerPos, (Vector2)kv.Value.transform.position);
            if (dist < 0.8f)
            {
                _pendingPickupId = kv.Key;
                GameStatsTracker.RegisterMaterialCollected(FindCurrentMapMaterialDrop());
                GameAudio.PlayItemPickup();
                Destroy(kv.Value);
                _itemDropReplicas.Remove(kv.Key);
                break;
            }
        }
    }

    private static MapMaterialDefinition FindCurrentMapMaterialDrop()
    {
        GameBootstrap bootstrap = UnityEngine.Object.FindObjectOfType<GameBootstrap>();
        return bootstrap != null ? bootstrap.GetSelectedMapMaterialDrop() : null;
    }

    private void ApplyAbilityEffects(OnlineAbilityState[] abilities)
    {
        var seen = new HashSet<int>();
        if (abilities != null)
        {
            foreach (OnlineAbilityState s in abilities)
            {
                if (s == null) continue;
                seen.Add(s.id);
                if (!_abilityReplicas.ContainsKey(s.id))
                    _abilityReplicas[s.id] = SpawnAbilityReplica(s);
            }
        }

        var toRemove = new List<int>();
        foreach (var kv in _abilityReplicas)
        {
            if (!seen.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }
        foreach (int id in toRemove)
        {
            if (_abilityReplicas[id] != null)
                UnityEngine.Object.Destroy(_abilityReplicas[id]);
            _abilityReplicas.Remove(id);
        }
    }

    private GameObject SpawnAbilityReplica(OnlineAbilityState s)
    {
        Color col = new Color(s.cr / 255f, s.cg / 255f, s.cb / 255f, 0.6f);
        Vector3 pos = new Vector3(s.x, s.y, 0f);

        switch (s.type)
        {
            case 0: // ExpansionBurst - expanding circle
            {
                var go = new GameObject("BurstReplica");
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * Mathf.Max(0.05f, s.scale);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SimpleSprite.Circle;
                sr.color = new Color(col.r, col.g, col.b, 0.45f);
                sr.sortingOrder = 11;
                var rep = go.AddComponent<AbilityReplica>();
                rep.targetScale = s.maxScale;
                rep.dieAt = Time.time + s.remaining;
                return go;
            }
            case 1: // GravityBomb - moving ball
            {
                var go = new GameObject("GravityBombReplica");
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * 0.8f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SimpleSprite.Circle;
                sr.color = col;
                sr.sortingOrder = 14;
                var rep = go.AddComponent<AbilityReplica>();
                rep.velocity = new Vector2(s.vx, s.vy);
                rep.dieAt = Time.time + s.remaining;
                return go;
            }
            case 2: // GravityWell - pulsing circle
            {
                var go = new GameObject("GravityWellReplica");
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * Mathf.Max(0.5f, s.scale);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SimpleSprite.Circle;
                sr.color = new Color(col.r, col.g, col.b, 0.22f);
                sr.sortingOrder = 6;
                var rep = go.AddComponent<AbilityReplica>();
                rep.pulse = true;
                rep.dieAt = Time.time + s.remaining;
                return go;
            }
            case 3: // ThrownWeapon - moving rectangle
            {
                var go = new GameObject("ThrownWeaponReplica");
                go.transform.position = pos;
                float ws = Mathf.Max(0.1f, s.scale);
                go.transform.localScale = new Vector3(ws, ws * 0.3f, 1f);
                float angle = Mathf.Atan2(s.vy, s.vx) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SimpleSprite.Square;
                sr.color = col;
                sr.sortingOrder = 12;
                var rep = go.AddComponent<AbilityReplica>();
                rep.velocity = new Vector2(s.vx, s.vy);
                rep.rotateSpeed = 860f;
                rep.dieAt = Time.time + s.remaining;
                return go;
            }
            default:
                return null;
        }
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

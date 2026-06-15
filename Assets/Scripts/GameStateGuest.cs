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
    private readonly Dictionary<int, OnlineEntityReplica> _effectReplicas = new Dictionary<int, OnlineEntityReplica>();
    private readonly Dictionary<int, float> _effectReplicaExpiresAt = new Dictionary<int, float>();
    private readonly Dictionary<int, DroppedMaterialPickup> _pickupReplicas = new Dictionary<int, DroppedMaterialPickup>();
    private readonly HashSet<int> _locallyCollectedPickupIds = new HashSet<int>();

    private Material _meleeMat;
    private Material _rangedMat;
    private Material _projMat;
    private Sprite[] _enemyAttackOrbSprites;
    private Texture2D _enemyAttackOrbTexture;
    private int _enemyAttackOrbFrameCount = 3;
    private float _enemyAttackOrbFps = 10f;
    private GameBootstrap _bootstrap;
    private GameWebSocketClient _ws;
    private bool _matchCompleted;
    private bool _hostPaused;
    private bool _sentReady;
    private bool _matchStarted;
    private bool _appliedMatchEndingVisual;
    private GameObject _hostPauseNotice;
    private OnlineMatchInputMessage _lastSentInput;
    private float _nextInputHeartbeatAt;
    private int _pickupCollectSeq;
    private int _pendingPickupCollectId;

    private void Start()
    {
        OnlineMatchStartGate.Show("Syncing online match...");
        GameAudio.StopMusic();

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        GameStatsTracker.StartMatch();

        _bootstrap = FindObjectOfType<GameBootstrap>();
        if (_bootstrap != null)
        {
            _meleeMat = _bootstrap.meleeEnemyMaterial;
            _rangedMat = _bootstrap.rangedEnemyMaterial;
            _projMat = _bootstrap.enemyProjectileMaterial;
            _enemyAttackOrbSprites = _bootstrap.enemyAttackOrbSprites;
            _enemyAttackOrbTexture = _bootstrap.enemyAttackOrbTexture;
            _enemyAttackOrbFrameCount = _bootstrap.enemyAttackOrbFrameCount;
            _enemyAttackOrbFps = _bootstrap.enemyAttackOrbFps;
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
            OnlineMatchStartGate.SetMessage("Could not connect to host.");
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
            pickupSeq = _pickupCollectSeq,
            pickupId = _pendingPickupCollectId,
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
        if (input.pickupSeq != _lastSentInput.pickupSeq) return true;
        if (input.pickupId != _lastSentInput.pickupId) return true;
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
        EnemySpawner.SetNetworkElapsedTime(state.matchStarted ? state.elapsedSeconds : 0f);
        ApplyMapState(state.mapIndex);
        ApplyStartState(state);
        ApplyExitState(state);
        ApplyPlayerStates(state.players, state.matchEnded, state.matchStarted);
        ApplyHostPauseState(state.pausedByHost, state.matchEnded, state.matchStarted);
        ApplyMatchEndingState(state.matchEnding, state.matchEnded, state.endingPlayerId);
        ApplyEnemyState(state.enemies);
        ApplyProjectileState(state.projectiles);
        ApplyEffectState(state.effects);
        ApplyMaterialPickupState(state.pickups);

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

    private void ApplyStartState(OnlineMatchStateMessage state)
    {
        if (state == null)
        {
            return;
        }

        if (!_sentReady && state.mapIndex >= 0)
        {
            _sentReady = true;
            OnlineMatchStartGate.SetMessage("Waiting for host...");
            OnlineMatchInputMessage readyInput = BuildInput();
            readyInput.ready = true;
            readyInput.readyMapIndex = state.mapIndex;
            StartCoroutine(Send(JsonUtility.ToJson(readyInput)));
        }

        if (state.matchStarted)
        {
            if (!_matchStarted)
            {
                _matchStarted = true;
                OnlineMatchStartGate.Hide();
            }

            GameAudio.EnsureMatchMusic(state.mapIndex);
            return;
        }

        OnlineMatchStartGate.Show(_sentReady ? "Waiting for host..." : "Syncing online match...");
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

    }

    private void ApplyMatchEndingState(bool matchEnding, bool matchEnded, int endingPlayerId)
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

        if (!_appliedMatchEndingVisual)
        {
            _appliedMatchEndingVisual = true;
            Transform target = ResolveEndingPlayerTransform(endingPlayerId);
            if (target != null)
                StartCoroutine(WarpSyncedPlayerVisual(target));
        }
    }

    private Transform ResolveEndingPlayerTransform(int endingPlayerId)
    {
        if (endingPlayerId == 1 && PlayerController.main != null)
            return PlayerController.main.transform;

        if (endingPlayerId == 0 && RemotePlayerGhost.Instance != null)
            return RemotePlayerGhost.Instance.transform;

        PlayerController player = PlayerController.main;
        if (player == null)
            return RemotePlayerGhost.Instance != null ? RemotePlayerGhost.Instance.transform : null;

        Vector2 exitPosition = MatchExit.Position;
        Transform local = player.transform;
        Transform remote = RemotePlayerGhost.Instance != null ? RemotePlayerGhost.Instance.transform : null;
        if (remote == null)
            return local;

        float localDistance = ((Vector2)local.position - exitPosition).sqrMagnitude;
        float remoteDistance = ((Vector2)remote.position - exitPosition).sqrMagnitude;
        return localDistance <= remoteDistance ? local : remote;
    }

    private IEnumerator WarpSyncedPlayerVisual(Transform target)
    {
        Vector3 startScale = target.localScale;
        Vector3 startPosition = target.position;
        const float duration = 0.7f;
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            float width = Mathf.Lerp(1f, 0.08f, eased);
            float height = Mathf.Lerp(1f, 2.4f, eased);
            target.localScale = new Vector3(startScale.x * width, startScale.y * height, startScale.z);
            target.position = startPosition + Vector3.up * Mathf.Lerp(0f, 1.1f, eased);
            SetRenderersAlpha(target, Mathf.Lerp(1f, 0f, eased));
            yield return null;
        }

        if (target != null)
            target.gameObject.SetActive(false);
    }

    private static void SetRenderersAlpha(Transform target, float alpha)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Color color = renderers[i].color;
            color.a = alpha;
            renderers[i].color = color;
        }
    }

    private void ApplyPlayerStates(OnlinePlayerState[] players, bool matchEnded, bool matchStarted)
    {
        if (players == null) return;

        foreach (OnlinePlayerState player in players)
        {
            if (player == null) continue;

            if (player.id == 0)
                ApplyRemoteHostPlayer(player);
            else if (player.id == 1)
                ApplyLocalGuestPlayer(player, matchEnded, matchStarted);
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
            state.weaponItemId,
            state.weaponType,
            state.weaponColor);
    }

    private void ApplyLocalGuestPlayer(OnlinePlayerState state, bool matchEnded, bool matchStarted)
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

        SetLocalPlayerVisibleAndControllable(matchStarted && state.alive && !matchEnded);
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

    private void ApplyHostPauseState(bool paused, bool matchEnded, bool matchStarted)
    {
        _hostPaused = paused && !matchEnded;
        if (_hostPauseNotice != null)
            _hostPauseNotice.SetActive(_hostPaused);

        PlayerController player = PlayerController.main;
        if (player == null || matchEnded) return;

        if (!matchStarted)
        {
            player.enabled = false;
            Rigidbody2D waitingBody = player.GetComponent<Rigidbody2D>();
            if (waitingBody != null)
                waitingBody.linearVelocity = Vector2.zero;
            return;
        }

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

    public void RequestMaterialPickupCollect(int pickupId)
    {
        if (pickupId <= 0)
            return;

        _locallyCollectedPickupIds.Add(pickupId);
        _pendingPickupCollectId = pickupId;
        _pickupCollectSeq++;
    }

    private void HandleHostLeft()
    {
        if (_matchCompleted) return;
        _matchCompleted = true;
        OnlineMatchStartGate.Reset();
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
                    ApplyProjectileReplicaVisual(replica.gameObject, projectile);
                    Vector3 position = new Vector3(projectile.x, projectile.y, 0f);
                    if (projectile.isHitbox && projectile.swordSwing)
                    {
                        ConfigureSwordSwingReplica(replica.gameObject, projectile);
                    }
                    else if (projectile.isHitbox)
                    {
                        replica.SnapTo(position);
                    }
                    else
                    {
                        replica.SetTarget(
                            position,
                            new Vector3(projectile.vx, projectile.vy, 0f),
                            20f);
                    }
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
        ApplyProjectileReplicaVisual(go, projectile);
        if (hasEnemyOrbSprite && enemyFrames.Length > 1)
        {
            EnemyProjectileAnimator animator = go.AddComponent<EnemyProjectileAnimator>();
            animator.frames = enemyFrames;
            animator.fps = _enemyAttackOrbFps;
        }

        OnlineEntityReplica replica = go.AddComponent<OnlineEntityReplica>();
        if (projectile.isHitbox && projectile.swordSwing)
        {
            replica.enabled = false;
            ConfigureSwordSwingReplica(go, projectile);
        }
        else
        {
            replica.SnapTo(go.transform.position);
        }
        return replica;
    }

    private void ApplyProjectileReplicaVisual(GameObject go, OnlineProjectileState projectile)
    {
        if (go == null || projectile == null)
            return;

        SpriteRenderer rootRenderer = go.GetComponent<SpriteRenderer>();
        Transform weaponVisual = go.transform.Find("WeaponVisual");

        if (!projectile.isHitbox || !projectile.hasWeaponVisual)
        {
            if (rootRenderer != null)
                rootRenderer.enabled = true;
            if (weaponVisual != null)
                weaponVisual.gameObject.SetActive(false);
            return;
        }

        WeaponKind weaponKind = PlayerLoadout.ParseWeaponKind(projectile.weaponType);
        Sprite weaponSprite = OnlineWeaponVisuals.ResolveAttackSprite(
            weaponKind,
            projectile.weaponItemId,
            _bootstrap != null ? _bootstrap : FindObjectOfType<GameBootstrap>());
        if (weaponSprite == null)
        {
            if (rootRenderer != null)
                rootRenderer.enabled = true;
            if (weaponVisual != null)
                weaponVisual.gameObject.SetActive(false);
            return;
        }

        if (rootRenderer != null)
            rootRenderer.enabled = false;

        if (weaponVisual == null)
        {
            GameObject visualObj = new GameObject("WeaponVisual");
            visualObj.transform.SetParent(go.transform, false);
            weaponVisual = visualObj.transform;
        }

        weaponVisual.gameObject.SetActive(true);
        weaponVisual.localPosition = new Vector3(projectile.visualOffsetX, projectile.visualOffsetY, 0f);
        weaponVisual.localRotation = Quaternion.Euler(0f, 0f, projectile.visualRotationZ);
        weaponVisual.localScale = new Vector3(
            Mathf.Approximately(projectile.visualScaleX, 0f) ? 1f : projectile.visualScaleX,
            Mathf.Approximately(projectile.visualScaleY, 0f) ? 1f : projectile.visualScaleY,
            1f);

        SpriteRenderer weaponRenderer = weaponVisual.GetComponent<SpriteRenderer>();
        if (weaponRenderer == null)
            weaponRenderer = weaponVisual.gameObject.AddComponent<SpriteRenderer>();
        weaponRenderer.sprite = weaponSprite;
        weaponRenderer.color = Color.white;
        weaponRenderer.sortingOrder = 10;
    }

    private void ApplyProjectileReplicaShape(Transform target, OnlineProjectileState projectile)
    {
        if (target == null) return;

        if (projectile.isHitbox)
        {
            float scaleX = Mathf.Max(0.05f, projectile.scaleX);
            float scaleY = Mathf.Max(0.05f, projectile.scaleY);
            target.localScale = new Vector3(scaleX, scaleY, 1f);
            if (!projectile.swordSwing)
                target.rotation = Quaternion.Euler(0f, 0f, projectile.rotationZ);
            return;
        }

        float size = projectile.size > 0f ? projectile.size : 0.25f;
        target.localScale = new Vector3(size, size, 1f);
    }

    private void ConfigureSwordSwingReplica(GameObject go, OnlineProjectileState projectile)
    {
        if (go == null || projectile == null || !projectile.swordSwing)
            return;

        OnlineEntityReplica replica = go.GetComponent<OnlineEntityReplica>();
        if (replica != null)
            replica.enabled = false;

        OnlineSwordSwingReplica swing = go.GetComponent<OnlineSwordSwingReplica>();
        Transform owner = ResolveProjectileOwner(projectile.ownerId);
        if (swing == null)
        {
            swing = go.AddComponent<OnlineSwordSwingReplica>();
            Vector2 direction = new Vector2(projectile.swingDirectionX, projectile.swingDirectionY);
            if (direction.sqrMagnitude < 0.001f)
            {
                Vector3 fallbackDirection = Quaternion.Euler(0f, 0f, projectile.rotationZ) * Vector3.right;
                direction = new Vector2(fallbackDirection.x, fallbackDirection.y);
            }
            swing.Begin(
                owner,
                direction,
                projectile.swingDistance > 0f ? projectile.swingDistance : Mathf.Max(0.1f, projectile.size),
                projectile.swingDuration > 0f ? projectile.swingDuration : Mathf.Max(0.08f, projectile.life),
                projectile.swingArcDegrees > 0f ? projectile.swingArcDegrees : 110f);
            return;
        }

        swing.RefreshOwner(owner);
    }

    private Transform ResolveProjectileOwner(int ownerId)
    {
        if (ownerId == 0 && RemotePlayerGhost.Instance != null)
            return RemotePlayerGhost.Instance.transform;

        PlayerController[] players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].playerIndex == ownerId)
                return players[i].transform;
        }

        return RemotePlayerGhost.Instance != null ? RemotePlayerGhost.Instance.transform : null;
    }

    private void ApplyEffectState(OnlineEffectState[] effects)
    {
        var activeIds = new HashSet<int>();

        if (effects != null)
        {
            foreach (OnlineEffectState effect in effects)
            {
                if (effect == null || effect.life <= 0f) continue;
                if (effect.ownerId == 1) continue;

                activeIds.Add(effect.id);
                if (effect.type == OnlineEffectType.FireTrail)
                    _effectReplicaExpiresAt[effect.id] = Time.time + Mathf.Max(0.05f, effect.life);
                if (_effectReplicas.TryGetValue(effect.id, out OnlineEntityReplica replica) && replica != null)
                {
                    ApplyEffectReplica(replica.gameObject, effect);
                }
                else
                {
                    _effectReplicas[effect.id] = SpawnEffectReplica(effect);
                }
            }
        }

        RemoveInactiveEffects(activeIds);
    }

    private void ApplyMaterialPickupState(OnlineMaterialPickupState[] pickups)
    {
        var activeIds = new HashSet<int>();
        var stateIds = new HashSet<int>();

        if (pickups != null)
        {
            foreach (OnlineMaterialPickupState pickup in pickups)
            {
                if (pickup == null || pickup.id <= 0)
                    continue;

                stateIds.Add(pickup.id);
                if (_locallyCollectedPickupIds.Contains(pickup.id))
                    continue;

                activeIds.Add(pickup.id);
                if (_pickupReplicas.TryGetValue(pickup.id, out DroppedMaterialPickup replica) && replica != null)
                {
                    replica.ApplyNetworkState(pickup);
                }
                else
                {
                    _pickupReplicas[pickup.id] = DroppedMaterialPickup.SpawnNetworkReplica(pickup);
                }
            }
        }

        RemoveInactivePickups(activeIds);
        ClearConfirmedLocalPickupCollections(stateIds);
    }

    private OnlineEntityReplica SpawnEffectReplica(OnlineEffectState effect)
    {
        GameObject go = new GameObject(GetEffectReplicaName(effect.type));
        go.transform.position = new Vector3(effect.x, effect.y, 0f);
        BuildEffectVisual(go, effect);

        OnlineEntityReplica replica = go.AddComponent<OnlineEntityReplica>();
        replica.SnapTo(go.transform.position);
        ApplyEffectReplica(go, effect);
        return replica;
    }

    private void ApplyEffectReplica(GameObject go, OnlineEffectState effect)
    {
        if (go == null || effect == null)
            return;

        if (!UsesLocalEffectRotation(effect))
            go.transform.rotation = Quaternion.Euler(0f, 0f, effect.rotationZ);
        Vector3 effectScale = new Vector3(
            Mathf.Approximately(effect.scaleX, 0f) ? 1f : effect.scaleX,
            Mathf.Approximately(effect.scaleY, 0f) ? 1f : effect.scaleY,
            1f);
        if (UsesSmoothedEffectScale(effect.type))
            SmoothScale(go.transform, effectScale, 24f);
        else
            go.transform.localScale = effectScale;

        OnlineEntityReplica replica = go.GetComponent<OnlineEntityReplica>();
        Vector3 position = new Vector3(effect.x, effect.y, 0f);
        if (IsFastEffect(effect.type))
        {
            if (replica != null) replica.SnapTo(position);
            else go.transform.position = position;
        }
        else if (replica != null)
        {
            replica.SetTarget(position, new Vector3(effect.vx, effect.vy, 0f), 18f);
        }
        else
        {
            go.transform.position = position;
        }

        UpdateEffectVisual(go, effect);
    }

    private void BuildEffectVisual(GameObject go, OnlineEffectState effect)
    {
        if (go == null || effect == null)
            return;

        if (effect.type == OnlineEffectType.GravityBombProjectile)
        {
            EnsureGravityBombVisual(go);
            return;
        }

        if (effect.type == OnlineEffectType.PlayerMinion)
        {
            EnsureMinionVisual(go);
            return;
        }

        if (effect.type == OnlineEffectType.TemporaryWall)
        {
            EnsureTemporaryWallVisual(go, effect);
            return;
        }

        if (effect.type == OnlineEffectType.PlayerDecoy)
        {
            EnsurePlayerDecoyVisual(go, effect);
            return;
        }

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<SpriteRenderer>();

        renderer.sprite = (effect.type == OnlineEffectType.RangedAbilityProjectile && effect.explosive)
            ? SimpleSprite.Square
            : SimpleSprite.Circle;
        renderer.sortingOrder = GetEffectSortingOrder(effect.type);
    }

    private void UpdateEffectVisual(GameObject go, OnlineEffectState effect)
    {
        if (effect.type == OnlineEffectType.ThrownWeapon)
        {
            ApplyThrownWeaponReplicaVisual(go, effect);
            return;
        }

        if (effect.type == OnlineEffectType.GravityBombProjectile)
        {
            ApplyGravityBombReplicaVisual(go, effect);
            return;
        }

        if (effect.type == OnlineEffectType.PlayerMinion)
        {
            UpdateMinionReplicaVisual(go, effect);
            return;
        }

        if (effect.type == OnlineEffectType.TemporaryWall)
        {
            UpdateTemporaryWallReplicaVisual(go, effect);
            return;
        }

        if (effect.type == OnlineEffectType.PlayerDecoy)
        {
            UpdatePlayerDecoyReplicaVisual(go, effect);
            return;
        }

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        renderer.color = PlayerLoadout.ParseWeaponColor(effect.color, Color.white);
        if (effect.type == OnlineEffectType.ExpansionBurst ||
            effect.type == OnlineEffectType.GravityWell ||
            effect.type == OnlineEffectType.FireTrail)
        {
            Color color = renderer.color;
            color.a = Mathf.Clamp01(color.a);
            renderer.color = color;
        }

        if (effect.type == OnlineEffectType.FireTrail)
        {
            OnlineFireTrailReplica fireTrail = go.GetComponent<OnlineFireTrailReplica>();
            if (fireTrail == null)
                fireTrail = go.AddComponent<OnlineFireTrailReplica>();
            fireTrail.Configure(renderer, renderer.color, effect.life);
        }
    }

    private void ApplyThrownWeaponReplicaVisual(GameObject go, OnlineEffectState effect)
    {
        ConfigureSpin(go, effect.boomerang, 860f);

        SpriteRenderer rootRenderer = go.GetComponent<SpriteRenderer>();
        Transform weaponVisual = go.transform.Find("WeaponVisual");

        WeaponKind weaponKind = PlayerLoadout.ParseWeaponKind(effect.weaponType);
        Sprite weaponSprite = OnlineWeaponVisuals.ResolveAttackSprite(
            weaponKind,
            effect.weaponItemId,
            _bootstrap != null ? _bootstrap : FindObjectOfType<GameBootstrap>());

        if (weaponSprite == null)
        {
            if (rootRenderer == null)
                rootRenderer = go.AddComponent<SpriteRenderer>();
            rootRenderer.enabled = true;
            rootRenderer.sprite = SimpleSprite.Square;
            rootRenderer.color = PlayerLoadout.ParseWeaponColor(effect.color, Color.white);
            rootRenderer.sortingOrder = 12;
            if (weaponVisual != null)
                weaponVisual.gameObject.SetActive(false);
            return;
        }

        if (rootRenderer != null)
            rootRenderer.enabled = false;

        if (weaponVisual == null)
        {
            GameObject visualObj = new GameObject("WeaponVisual");
            visualObj.transform.SetParent(go.transform, false);
            weaponVisual = visualObj.transform;
        }

        weaponVisual.gameObject.SetActive(true);
        weaponVisual.localPosition = new Vector3(effect.visualOffsetX, effect.visualOffsetY, 0f);
        weaponVisual.localRotation = Quaternion.Euler(0f, 0f, effect.visualRotationZ);
        weaponVisual.localScale = new Vector3(
            Mathf.Approximately(effect.visualScaleX, 0f) ? 1f : effect.visualScaleX,
            Mathf.Approximately(effect.visualScaleY, 0f) ? 1f : effect.visualScaleY,
            1f);

        SpriteRenderer weaponRenderer = weaponVisual.GetComponent<SpriteRenderer>();
        if (weaponRenderer == null)
            weaponRenderer = weaponVisual.gameObject.AddComponent<SpriteRenderer>();
        weaponRenderer.sprite = weaponSprite;
        weaponRenderer.color = Color.white;
        weaponRenderer.sortingOrder = 12;
    }

    private void EnsureGravityBombVisual(GameObject go)
    {
        if (go.transform.Find("Shadow") == null)
        {
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = SimpleSprite.Circle;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.24f);
            shadowRenderer.sortingOrder = 8;
        }

        if (go.transform.Find("BombVisual") == null)
        {
            GameObject visual = new GameObject("BombVisual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSprite.Circle;
            renderer.sortingOrder = 14;
        }
    }

    private void ApplyGravityBombReplicaVisual(GameObject go, OnlineEffectState effect)
    {
        EnsureGravityBombVisual(go);

        Transform shadow = go.transform.Find("Shadow");
        if (shadow != null)
        {
            Vector3 shadowScale = new Vector3(
                Mathf.Approximately(effect.shadowScaleX, 0f) ? 1f : effect.shadowScaleX,
                Mathf.Approximately(effect.shadowScaleY, 0f) ? 0.35f : effect.shadowScaleY,
                1f);
            SmoothLocalTransform(shadow, Vector3.zero, shadowScale, 22f);
        }

        Transform visual = go.transform.Find("BombVisual");
        if (visual != null)
        {
            Vector3 visualPosition = new Vector3(0f, effect.visualOffsetY, 0f);
            Vector3 visualScale = new Vector3(
                Mathf.Approximately(effect.visualScaleX, 0f) ? 1f : effect.visualScaleX,
                Mathf.Approximately(effect.visualScaleY, 0f) ? 1f : effect.visualScaleY,
                1f);
            SmoothLocalTransform(visual, visualPosition, visualScale, 22f);
            ConfigureSpin(visual.gameObject, true, 540f);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = PlayerLoadout.ParseWeaponColor(effect.color, Color.white);
        }
    }

    private void EnsureMinionVisual(GameObject go)
    {
        Transform sprite = go.transform.Find("Sprite");
        if (sprite != null)
            return;

        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(go.transform, false);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = Vector3.one;

        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = new Color(1f, 0.88f, 0.12f, 1f);
        renderer.sortingOrder = 6;

        PlayerMinionAnimator animator = spriteObj.AddComponent<PlayerMinionAnimator>();
        if (_bootstrap != null)
        {
            animator.moveSprites = _bootstrap.minionMoveSprites;
            animator.moveSheet = _bootstrap.minionMoveTexture;
            animator.resourceSheetName = _bootstrap.minionMoveResource;
            animator.fps = Mathf.Max(0.01f, _bootstrap.minionMoveFps);
            animator.spriteScale = Mathf.Max(0.01f, _bootstrap.minionSpriteScale);
        }
    }

    private void UpdateMinionReplicaVisual(GameObject go, OnlineEffectState effect)
    {
        EnsureMinionVisual(go);
        Transform sprite = go.transform.Find("Sprite");
        if (sprite == null)
            return;

        SpriteRenderer renderer = sprite.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.enabled = true;
    }

    private void EnsureTemporaryWallVisual(GameObject go, OnlineEffectState effect)
    {
        if (go.transform.Find("GuardWallCore") != null)
            return;

        float wallLength = Mathf.Max(0.01f, Mathf.Abs(effect.scaleX));
        float wallThickness = Mathf.Max(0.01f, Mathf.Abs(effect.scaleY));
        float capDiameter = wallThickness * 2.7f;
        float capScaleX = capDiameter / wallLength;

        CreateReplicaGlowPiece(go.transform, "GuardWallOuterGlow", Vector2.zero, new Vector2(1.12f, 3.4f), new Color(0.30f, 0.82f, 1f, 0.22f), 7);
        CreateReplicaGlowPiece(go.transform, "GuardWallInnerGlow", Vector2.zero, new Vector2(0.98f, 2.1f), new Color(0.84f, 1f, 1f, 0.28f), 8);
        CreateReplicaGlowPiece(go.transform, "GuardWallCore", Vector2.zero, new Vector2(0.92f, 0.78f), new Color(0.58f, 0.94f, 1f, 0.62f), 9);
        CreateReplicaGlowPiece(go.transform, "GuardWallEnergySpine", Vector2.zero, new Vector2(0.86f, 0.18f), new Color(0.94f, 1f, 1f, 0.72f), 10);
        CreateReplicaGlowPiece(go.transform, "GuardWallLeftBloom", new Vector2(-0.5f, 0f), new Vector2(capScaleX, 2.7f), new Color(0.72f, 0.96f, 1f, 0.52f), 9, SimpleSprite.Circle);
        CreateReplicaGlowPiece(go.transform, "GuardWallRightBloom", new Vector2(0.5f, 0f), new Vector2(capScaleX, 2.7f), new Color(0.72f, 0.96f, 1f, 0.52f), 9, SimpleSprite.Circle);
    }

    private void UpdateTemporaryWallReplicaVisual(GameObject go, OnlineEffectState effect)
    {
        EnsureTemporaryWallVisual(go, effect);
    }

    private void EnsurePlayerDecoyVisual(GameObject go, OnlineEffectState effect)
    {
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<SpriteRenderer>();

        renderer.sortingOrder = 4;
        PlayerSkinVisuals.Apply(renderer, effect.skinId, effect.skinColor, null);
        Color color = PlayerLoadout.ParseWeaponColor(effect.color, Color.white);
        color.a = Mathf.Clamp01(color.a);
        renderer.color = color;
    }

    private void UpdatePlayerDecoyReplicaVisual(GameObject go, OnlineEffectState effect)
    {
        EnsurePlayerDecoyVisual(go, effect);
    }

    private void CreateReplicaGlowPiece(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 scale,
        Color color,
        int sortingOrder,
        Sprite sprite = null)
    {
        GameObject piece = new GameObject(name);
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = new Vector3(position.x, position.y, 0f);
        piece.transform.localRotation = Quaternion.identity;
        piece.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : SimpleSprite.Square;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static void ConfigureSpin(GameObject go, bool active, float zDegreesPerSecond)
    {
        if (go == null)
            return;

        OnlineReplicaSpin spin = go.GetComponent<OnlineReplicaSpin>();
        if (!active)
        {
            if (spin != null)
                spin.zDegreesPerSecond = 0f;
            return;
        }

        if (spin == null)
            spin = go.AddComponent<OnlineReplicaSpin>();
        spin.zDegreesPerSecond = zDegreesPerSecond;
    }

    private static void SmoothLocalTransform(Transform target, Vector3 localPosition, Vector3 localScale, float lerpSpeed)
    {
        if (target == null)
            return;

        OnlineLocalTransformSmoother smoother = target.GetComponent<OnlineLocalTransformSmoother>();
        if (smoother == null)
            smoother = target.gameObject.AddComponent<OnlineLocalTransformSmoother>();
        smoother.SetTarget(localPosition, localScale, lerpSpeed);
    }

    private static void SmoothScale(Transform target, Vector3 scale, float lerpSpeed)
    {
        if (target == null)
            return;

        OnlineScaleSmoother smoother = target.GetComponent<OnlineScaleSmoother>();
        if (smoother == null)
            smoother = target.gameObject.AddComponent<OnlineScaleSmoother>();
        smoother.SetTarget(scale, lerpSpeed);
    }

    private static bool IsFastEffect(int effectType)
    {
        return effectType == OnlineEffectType.ExpansionBurst ||
            effectType == OnlineEffectType.GravityWell ||
            effectType == OnlineEffectType.FireTrail ||
            effectType == OnlineEffectType.TemporaryWall ||
            effectType == OnlineEffectType.PlayerDecoy;
    }

    private static bool UsesLocalEffectRotation(OnlineEffectState effect)
    {
        return effect != null &&
            effect.type == OnlineEffectType.ThrownWeapon &&
            effect.boomerang;
    }

    private static bool UsesSmoothedEffectScale(int effectType)
    {
        return effectType == OnlineEffectType.ExpansionBurst ||
            effectType == OnlineEffectType.GravityWell;
    }

    private static string GetEffectReplicaName(int effectType)
    {
        return effectType switch
        {
            OnlineEffectType.ThrownWeapon => "ThrownWeaponReplica",
            OnlineEffectType.RangedAbilityProjectile => "RangedAbilityProjectileReplica",
            OnlineEffectType.ExpansionBurst => "ExpansionBurstReplica",
            OnlineEffectType.GravityBombProjectile => "GravityBombReplica",
            OnlineEffectType.GravityWell => "GravityWellReplica",
            OnlineEffectType.PlayerMinion => "PlayerMinionReplica",
            OnlineEffectType.FireTrail => "FireTrailReplica",
            OnlineEffectType.TemporaryWall => "TemporaryWallReplica",
            OnlineEffectType.PlayerDecoy => "PlayerDecoyReplica",
            _ => "OnlineEffectReplica"
        };
    }

    private static int GetEffectSortingOrder(int effectType)
    {
        return effectType switch
        {
            OnlineEffectType.RangedAbilityProjectile => 12,
            OnlineEffectType.ExpansionBurst => 13,
            OnlineEffectType.GravityWell => 6,
            OnlineEffectType.FireTrail => 5,
            OnlineEffectType.TemporaryWall => 9,
            OnlineEffectType.PlayerDecoy => 4,
            _ => 9
        };
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

    private void RemoveInactiveEffects(HashSet<int> activeIds)
    {
        var dead = new List<int>();
        foreach (var kv in _effectReplicas)
        {
            bool active = activeIds.Contains(kv.Key);
            bool keepUntilExpiry = false;
            if (!active && _effectReplicaExpiresAt.TryGetValue(kv.Key, out float expireAt))
                keepUntilExpiry = Time.time < expireAt;

            if ((active || keepUntilExpiry) && kv.Value != null)
                continue;

            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
            dead.Add(kv.Key);
        }

        foreach (int id in dead)
        {
            _effectReplicas.Remove(id);
            _effectReplicaExpiresAt.Remove(id);
        }
    }

    private void RemoveInactivePickups(HashSet<int> activeIds)
    {
        var dead = new List<int>();
        foreach (var kv in _pickupReplicas)
        {
            if (activeIds.Contains(kv.Key) && kv.Value != null)
                continue;

            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
            dead.Add(kv.Key);
        }

        foreach (int id in dead)
            _pickupReplicas.Remove(id);
    }

    private void ClearConfirmedLocalPickupCollections(HashSet<int> stateIds)
    {
        var confirmed = new List<int>();
        foreach (int id in _locallyCollectedPickupIds)
        {
            if (!stateIds.Contains(id))
                confirmed.Add(id);
        }

        foreach (int id in confirmed)
            _locallyCollectedPickupIds.Remove(id);
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
        if (OnlineMatchStartGate.IsWaiting)
            OnlineMatchStartGate.Reset();
        _ws?.Dispose();
    }
}

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
    private bool _guestReady;
    private bool _matchStarted;

    private void Start()
    {
        OnlineMatchStartGate.Show("Waiting for guest...");
        GameAudio.StopMusic();
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
            OnlineMatchStartGate.SetMessage("Could not connect to match server.");
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
            yield return new WaitForSecondsRealtime(SyncInterval);
        }
    }

    private void HandleGuestMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        if (!json.Contains("\"input\""))
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
            input.speedBoostMultiplier,
            input.swordSpearActiveSkillId,
            input.swordSpearActiveSkillLevel,
            input.swordSpearPassiveSkillId,
            input.swordSpearPassiveSkillLevel,
            input.rangedActiveSkillId,
            input.rangedActiveSkillLevel,
            input.rangedPassiveSkillId,
            input.rangedPassiveSkillLevel,
            input.weaponItemId);

        if (input.ready && input.readyMapIndex == GameMapSelection.SelectedMapIndex)
            MarkGuestReady();

        if (!_matchStarted)
        {
            _remotePlayer.ApplyExternalInput(Vector2.zero, Vector2.down, false, false, false, false);
            return;
        }

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

    private void MarkGuestReady()
    {
        if (_guestReady)
            return;

        _guestReady = true;
        _matchStarted = true;
        OnlineMatchStartGate.Hide();
        GameAudio.EnsureMatchMusic(GameMapSelection.SelectedMapIndex);
    }

    private OnlineMatchStateMessage BuildState()
    {
        bool ended = _sawRunActive && !GameStatsTracker.IsRunActive;
        int meleeKills = ended ? GameStatsTracker.LastRunMeleeKills : GameStatsTracker.CurrentMeleeKills;
        int rangedKills = ended ? GameStatsTracker.LastRunRangedKills : GameStatsTracker.CurrentRangedKills;
        int giantKills = ended ? GameStatsTracker.LastRunGiantKills : GameStatsTracker.CurrentGiantKills;
        int seconds = ended ? GameStatsTracker.LastRunTimeSeconds : GameStatsTracker.CurrentRunTimeSeconds;

        return new OnlineMatchStateMessage
        {
            tick = ++_tick,
            matchStarted = _matchStarted,
            matchEnded = ended,
            matchEnding = MatchExit.IsEnding,
            matchFinished = ended && GameStatsTracker.LastRunWasFinished,
            pausedByHost = PauseMenu.IsPaused,
            endingPlayerId = MatchExit.EndingPlayerId,
            meleeKills = meleeKills,
            rangedKills = rangedKills,
            giantKills = giantKills,
            elapsedSeconds = seconds,
            mapIndex = GameMapSelection.SelectedMapIndex,
            exitActive = MatchExit.HasExit,
            exitX = MatchExit.Position.x,
            exitY = MatchExit.Position.y,
            exits = MatchExit.GetActiveStates(),
            players = BuildPlayers(),
            enemies = BuildEnemies(),
            projectiles = BuildProjectiles(),
            effects = BuildEffects()
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
            skinColor = GetPlayerSkinColor(id, player),
            attackSeq = GetPlayerAttackSequence(id, player),
            weaponItemId = GetPlayerWeaponItemId(id, player),
            weaponType = GetPlayerWeaponType(id, player),
            weaponColor = GetPlayerWeaponColor(id, player)
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
        return player != null ? player.NetworkSkinColor : "#FFFFFF";
    }

    private int GetPlayerAttackSequence(int id, PlayerController player)
    {
        if (id == 1)
            return _lastAttackSeq;
        return player != null ? player.NetworkAttackSequence : 0;
    }

    private int GetPlayerWeaponItemId(int id, PlayerController player)
    {
        if (id == 0)
            return PlayerLoadout.EquippedWeaponItemId;
        return player != null ? player.NetworkWeaponItemId : 0;
    }

    private string GetPlayerWeaponType(int id, PlayerController player)
    {
        if (id == 0)
            return PlayerLoadout.CurrentWeaponKind.ToString();
        return player != null ? player.NetworkWeaponType : "Spear";
    }

    private string GetPlayerWeaponColor(int id, PlayerController player)
    {
        if (id == 0)
            return PlayerLoadout.WeaponColorHex;
        return player != null ? player.NetworkWeaponColor : "#FFFFFF";
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
                size = Mathf.Max(0.05f, projectile.transform.localScale.x),
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

    private OnlineEffectState[] BuildEffects()
    {
        var effects = new List<OnlineEffectState>();

        AddThrownWeaponEffects(effects);
        AddRangedAbilityProjectileEffects(effects);
        AddExpansionBurstEffects(effects);
        AddGravityBombEffects(effects);
        AddGravityWellEffects(effects);
        AddPlayerMinionEffects(effects);
        AddFireTrailEffects(effects);
        AddTemporaryWallEffects(effects);
        AddPlayerDecoyEffects(effects);

        return effects.ToArray();
    }

    private void AddThrownWeaponEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.ThrownWeapons.Count - 1; i >= 0; i--)
        {
            PlayerThrownWeapon weapon = OnlineNetworkRegistry.ThrownWeapons[i];
            if (weapon == null)
            {
                OnlineNetworkRegistry.ThrownWeapons.RemoveAt(i);
                continue;
            }

            Transform visual = FindWeaponVisual(weapon.transform);
            Vector2 velocity = weapon.CurrentVelocity;
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(weapon.gameObject),
                type = OnlineEffectType.ThrownWeapon,
                ownerId = weapon.ownerPlayerIndex,
                x = weapon.transform.position.x,
                y = weapon.transform.position.y,
                vx = velocity.x,
                vy = velocity.y,
                rotationZ = weapon.transform.eulerAngles.z,
                scaleX = Mathf.Max(0.05f, weapon.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, weapon.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(weapon.weaponColor),
                life = weapon.RemainingLife,
                boomerang = weapon.boomerang,
                weaponItemId = weapon.weaponItemId,
                weaponType = string.IsNullOrWhiteSpace(weapon.weaponType) ? (weapon.boomerang ? "Sword" : "Spear") : weapon.weaponType,
                hasWeaponVisual = visual != null,
                visualOffsetX = visual != null ? visual.localPosition.x : 0f,
                visualOffsetY = visual != null ? visual.localPosition.y : 0f,
                visualScaleX = visual != null ? visual.localScale.x : 1f,
                visualScaleY = visual != null ? visual.localScale.y : 1f,
                visualRotationZ = visual != null ? visual.localEulerAngles.z : 0f
            });
        }
    }

    private void AddRangedAbilityProjectileEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.RangedAbilityProjectiles.Count - 1; i >= 0; i--)
        {
            RangedAbilityProjectile projectile = OnlineNetworkRegistry.RangedAbilityProjectiles[i];
            if (projectile == null)
            {
                OnlineNetworkRegistry.RangedAbilityProjectiles.RemoveAt(i);
                continue;
            }

            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(projectile.gameObject),
                type = OnlineEffectType.RangedAbilityProjectile,
                ownerId = projectile.ownerPlayerIndex,
                x = projectile.transform.position.x,
                y = projectile.transform.position.y,
                vx = projectile.direction.x * projectile.speed,
                vy = projectile.direction.y * projectile.speed,
                rotationZ = projectile.transform.eulerAngles.z,
                scaleX = Mathf.Max(0.05f, projectile.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, projectile.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(projectile.projectileColor),
                life = projectile.RemainingLife,
                explosive = projectile.explodesOnImpact
            });
        }
    }

    private void AddExpansionBurstEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.ExpansionBursts.Count - 1; i >= 0; i--)
        {
            ExpansionBurst burst = OnlineNetworkRegistry.ExpansionBursts[i];
            if (burst == null)
            {
                OnlineNetworkRegistry.ExpansionBursts.RemoveAt(i);
                continue;
            }

            SpriteRenderer renderer = burst.GetComponent<SpriteRenderer>();
            Color color = renderer != null ? renderer.color : new Color(0.5f, 0.9f, 1f, 0.25f);
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(burst.gameObject),
                type = OnlineEffectType.ExpansionBurst,
                ownerId = burst.ownerPlayerIndex,
                x = burst.transform.position.x,
                y = burst.transform.position.y,
                rotationZ = burst.transform.eulerAngles.z,
                scaleX = Mathf.Max(0.05f, burst.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, burst.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(color),
                life = Mathf.Max(0.08f, burst.RemainingLife)
            });
        }
    }

    private void AddGravityBombEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.GravityBombs.Count - 1; i >= 0; i--)
        {
            GravityBombProjectile bomb = OnlineNetworkRegistry.GravityBombs[i];
            if (bomb == null)
            {
                OnlineNetworkRegistry.GravityBombs.RemoveAt(i);
                continue;
            }

            Vector3 visualScale = bomb.VisualLocalScale;
            Vector3 shadowScale = bomb.ShadowLocalScale;
            Vector2 velocity = bomb.CurrentVelocity;
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(bomb.gameObject),
                type = OnlineEffectType.GravityBombProjectile,
                ownerId = bomb.ownerPlayerIndex,
                x = bomb.transform.position.x,
                y = bomb.transform.position.y,
                vx = velocity.x,
                vy = velocity.y,
                rotationZ = bomb.transform.eulerAngles.z,
                scaleX = 1f,
                scaleY = 1f,
                color = "#" + ColorUtility.ToHtmlStringRGBA(bomb.bombColor),
                life = bomb.RemainingLife,
                visualOffsetY = bomb.VisualLocalY,
                visualScaleX = visualScale.x,
                visualScaleY = visualScale.y,
                shadowScaleX = shadowScale.x,
                shadowScaleY = shadowScale.y
            });
        }
    }

    private void AddGravityWellEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.GravityWells.Count - 1; i >= 0; i--)
        {
            GravityWell well = OnlineNetworkRegistry.GravityWells[i];
            if (well == null)
            {
                OnlineNetworkRegistry.GravityWells.RemoveAt(i);
                continue;
            }

            SpriteRenderer renderer = well.GetComponent<SpriteRenderer>();
            Color color = renderer != null ? renderer.color : well.color;
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(well.gameObject),
                type = OnlineEffectType.GravityWell,
                ownerId = well.ownerPlayerIndex,
                x = well.transform.position.x,
                y = well.transform.position.y,
                scaleX = Mathf.Max(0.05f, well.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, well.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(color),
                life = well.RemainingLife
            });
        }
    }

    private void AddPlayerMinionEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.PlayerMinions.Count - 1; i >= 0; i--)
        {
            PlayerMinion minion = OnlineNetworkRegistry.PlayerMinions[i];
            if (minion == null)
            {
                OnlineNetworkRegistry.PlayerMinions.RemoveAt(i);
                continue;
            }

            Vector2 velocity = minion.CurrentVelocity;
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(minion.gameObject),
                type = OnlineEffectType.PlayerMinion,
                ownerId = minion.ownerPlayerIndex,
                x = minion.transform.position.x,
                y = minion.transform.position.y,
                vx = velocity.x,
                vy = velocity.y,
                rotationZ = minion.transform.eulerAngles.z,
                scaleX = Mathf.Max(0.05f, minion.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, minion.transform.localScale.y),
                color = "#FFE01FFF",
                life = minion.RemainingLife
            });
        }
    }

    private void AddFireTrailEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.FireTrails.Count - 1; i >= 0; i--)
        {
            FireTrailSegment fireTrail = OnlineNetworkRegistry.FireTrails[i];
            if (fireTrail == null)
            {
                OnlineNetworkRegistry.FireTrails.RemoveAt(i);
                continue;
            }

            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(fireTrail.gameObject),
                type = OnlineEffectType.FireTrail,
                ownerId = fireTrail.ownerPlayerIndex,
                x = fireTrail.transform.position.x,
                y = fireTrail.transform.position.y,
                scaleX = Mathf.Max(0.05f, fireTrail.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, fireTrail.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(fireTrail.fireColor),
                life = fireTrail.RemainingLife
            });
        }
    }

    private void AddTemporaryWallEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.TemporaryWalls.Count - 1; i >= 0; i--)
        {
            TemporaryWall wall = OnlineNetworkRegistry.TemporaryWalls[i];
            if (wall == null)
            {
                OnlineNetworkRegistry.TemporaryWalls.RemoveAt(i);
                continue;
            }

            SpriteRenderer renderer = wall.GetComponentInChildren<SpriteRenderer>();
            Color color = renderer != null ? renderer.color : new Color(0.58f, 0.94f, 1f, 0.62f);
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(wall.gameObject),
                type = OnlineEffectType.TemporaryWall,
                ownerId = wall.ownerPlayerIndex,
                x = wall.transform.position.x,
                y = wall.transform.position.y,
                rotationZ = wall.transform.eulerAngles.z,
                scaleX = Mathf.Max(0.05f, wall.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, wall.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(color),
                life = wall.RemainingLife
            });
        }
    }

    private void AddPlayerDecoyEffects(List<OnlineEffectState> effects)
    {
        for (int i = OnlineNetworkRegistry.PlayerDecoys.Count - 1; i >= 0; i--)
        {
            PlayerDecoy decoy = OnlineNetworkRegistry.PlayerDecoys[i];
            if (decoy == null)
            {
                OnlineNetworkRegistry.PlayerDecoys.RemoveAt(i);
                continue;
            }

            SpriteRenderer renderer = decoy.GetComponent<SpriteRenderer>();
            Color color = renderer != null ? renderer.color : Color.white;
            PlayerController owner = MultiplayerState.GetPlayerByIndex(decoy.ownerPlayerIndex);
            effects.Add(new OnlineEffectState
            {
                id = GetOrAssignId(decoy.gameObject),
                type = OnlineEffectType.PlayerDecoy,
                ownerId = decoy.ownerPlayerIndex,
                x = decoy.transform.position.x,
                y = decoy.transform.position.y,
                rotationZ = decoy.transform.eulerAngles.z,
                scaleX = Mathf.Max(0.05f, decoy.transform.localScale.x),
                scaleY = Mathf.Max(0.05f, decoy.transform.localScale.y),
                color = "#" + ColorUtility.ToHtmlStringRGBA(color),
                life = decoy.RemainingLife,
                skinId = owner != null ? owner.NetworkSkinId : 0,
                skinColor = owner != null ? owner.NetworkSkinColor : "#FFFFFF"
            });
        }
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
        Transform weaponVisual = FindWeaponVisual(t);
        PlayerController owner = MultiplayerState.GetPlayerByIndex(hitBox.ownerPlayerIndex);
        return new AttackVisualSnapshot
        {
            id = id,
            ownerId = hitBox.ownerPlayerIndex,
            color = "#" + ColorUtility.ToHtmlStringRGBA(hitBox.visualColor),
            weaponType = owner != null ? owner.NetworkWeaponType : "Spear",
            weaponItemId = owner != null ? owner.NetworkWeaponItemId : 0,
            hasWeaponVisual = weaponVisual != null,
            visualOffset = weaponVisual != null ? (Vector2)weaponVisual.localPosition : Vector2.zero,
            visualScale = weaponVisual != null ? (Vector2)weaponVisual.localScale : Vector2.one,
            visualRotationZ = weaponVisual != null ? weaponVisual.localEulerAngles.z : 0f,
            x = t.position.x,
            y = t.position.y,
            rotationZ = t.eulerAngles.z,
            scaleX = Mathf.Max(0.05f, t.localScale.x),
            scaleY = Mathf.Max(0.05f, t.localScale.y),
            expireAt = expireAt
        };
    }

    private Transform FindWeaponVisual(Transform root)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.GetComponent<SpriteRenderer>() != null && child.name == "WeaponVisual")
                return child;
        }

        return null;
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
        if (OnlineMatchStartGate.IsWaiting)
            OnlineMatchStartGate.Reset();
        _ws?.Dispose();
    }

    private class AttackVisualSnapshot
    {
        public int id;
        public int ownerId;
        public string color;
        public string weaponType;
        public int weaponItemId;
        public bool hasWeaponVisual;
        public Vector2 visualOffset;
        public Vector2 visualScale = Vector2.one;
        public float visualRotationZ;
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
                weaponType = weaponType,
                weaponItemId = weaponItemId,
                hasWeaponVisual = hasWeaponVisual,
                size = Mathf.Max(scaleX, scaleY),
                scaleX = scaleX,
                scaleY = scaleY,
                rotationZ = rotationZ,
                visualOffsetX = visualOffset.x,
                visualOffsetY = visualOffset.y,
                visualScaleX = visualScale.x,
                visualScaleY = visualScale.y,
                visualRotationZ = visualRotationZ,
                x = x,
                y = y,
                vx = 0f,
                vy = 0f,
                life = Mathf.Max(0.01f, expireAt - now)
            };
        }
    }
}

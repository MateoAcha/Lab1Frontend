using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateHost : MonoBehaviour
{
    private const float SyncInterval = 0.05f;

    private GameWebSocketClient _ws;
    private PlayerController _remotePlayer;
    private int _lastAttackSeq;
    private int _lastChargeSeq;
    private int _lastBurstSeq;
    private int _lastConsumableSeq;
    private int _nextEntityId = 1;
    private int _tick;
    private bool _sawRunActive;

    private void Start()
    {
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
            rocks = BuildRocks(),
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

        return new OnlinePlayerState
        {
            id = id,
            x = pos.x,
            y = pos.y,
            hp = health != null ? health.hp : 0f,
            maxHp = health != null ? Mathf.Max(health.maxHp, health.hp) : 0f,
            alive = alive
        };
    }

    private OnlineRockState[] BuildRocks()
    {
        GameObject rocksRoot = GameObject.Find("RuntimeRocks");
        if (rocksRoot == null)
        {
            return new OnlineRockState[0];
        }

        var rocks = new List<OnlineRockState>(rocksRoot.transform.childCount);
        foreach (Transform t in rocksRoot.transform)
        {
            rocks.Add(new OnlineRockState
            {
                x = t.position.x,
                y = t.position.y,
                size = t.localScale.x
            });
        }

        return rocks.ToArray();
    }

    private OnlineEnemyState[] BuildEnemies()
    {
        var enemies = new List<OnlineEnemyState>();

        foreach (EnemyController enemy in FindObjectsOfType<EnemyController>())
        {
            AddEnemyState(enemies, enemy.gameObject, 0);
        }

        foreach (RangedEnemyController enemy in FindObjectsOfType<RangedEnemyController>())
        {
            AddEnemyState(enemies, enemy.gameObject, 1);
        }

        return enemies.ToArray();
    }

    private void AddEnemyState(List<OnlineEnemyState> enemies, GameObject enemy, int type)
    {
        if (enemy == null) return;

        Health health = enemy.GetComponent<Health>();
        if (health != null && health.hp <= 0f) return;

        enemies.Add(new OnlineEnemyState
        {
            id = GetOrAssignId(enemy),
            type = type,
            x = enemy.transform.position.x,
            y = enemy.transform.position.y,
            hp = health != null ? health.hp : 1f,
            maxHp = health != null ? Mathf.Max(health.maxHp, health.hp) : 1f,
            size = enemy.transform.localScale.x
        });
    }

    private OnlineProjectileState[] BuildProjectiles()
    {
        var projectiles = new List<OnlineProjectileState>();

        foreach (EnemyProjectile projectile in FindObjectsOfType<EnemyProjectile>())
        {
            if (projectile == null) continue;
            projectiles.Add(new OnlineProjectileState
            {
                id = GetOrAssignId(projectile.gameObject),
                x = projectile.transform.position.x,
                y = projectile.transform.position.y,
                vx = projectile.direction.x * projectile.speed,
                vy = projectile.direction.y * projectile.speed,
                life = projectile.RemainingLife
            });
        }

        return projectiles.ToArray();
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
        foreach (PlayerController player in FindObjectsOfType<PlayerController>())
        {
            if (player != null && player.playerIndex == 1)
                return player;
        }

        return null;
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
        wsUrl += "/game-ws";
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            wsUrl += "?token=" + Uri.EscapeDataString(AuthSession.AccessToken);
        return wsUrl;
    }

    private void OnDestroy()
    {
        _ws?.Dispose();
    }
}

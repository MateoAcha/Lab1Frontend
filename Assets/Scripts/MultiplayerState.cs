using System.Collections.Generic;
using UnityEngine;

public static class MultiplayerState
{
    public static bool IsMultiplayer { get; private set; }
    public static bool IsOnline      { get; private set; }
    public static bool IsHost        { get; private set; }
    public static int OnlineRoomNumber { get; private set; } = 1;
    private static bool _returnToOnlineMenu;

    private static readonly List<PlayerController> _players = new List<PlayerController>();
    private static readonly List<Transform> _enemyTargets = new List<Transform>();

    public static void SetMultiplayer(bool value) { IsMultiplayer = value; }
    public static void SetOnline(bool value)      { IsOnline = value; }
    public static void SetHost(bool value)        { IsHost = value; }
    public static void SetOnlineRoomNumber(int value) { OnlineRoomNumber = Mathf.Max(1, value); }
    public static void RequestReturnToOnlineMenu() { _returnToOnlineMenu = true; }
    public static bool ConsumeReturnToOnlineMenu()
    {
        bool value = _returnToOnlineMenu;
        _returnToOnlineMenu = false;
        return value;
    }

    public static void RegisterPlayer(PlayerController player)
    {
        if (player != null && !_players.Contains(player))
            _players.Add(player);
    }

    public static void UnregisterPlayer(PlayerController player)
    {
        _players.Remove(player);
    }

    public static void RegisterEnemyTarget(Transform target)
    {
        if (target != null && !_enemyTargets.Contains(target))
            _enemyTargets.Add(target);
    }

    public static void UnregisterEnemyTarget(Transform target)
    {
        _enemyTargets.Remove(target);
    }

    public static Transform GetOtherPlayer(Transform exclude)
    {
        foreach (PlayerController p in _players)
        {
            if (p != null && p.transform != exclude)
                return p.transform;
        }
        return null;
    }

    public static PlayerController GetPlayerByIndex(int playerIndex)
    {
        foreach (PlayerController p in _players)
        {
            if (p != null && p.playerIndex == playerIndex)
                return p;
        }
        return null;
    }

    public static Transform GetNearestPlayer(Vector3 position)
    {
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        for (int i = _enemyTargets.Count - 1; i >= 0; i--)
        {
            Transform target = _enemyTargets[i];
            if (target == null)
            {
                _enemyTargets.RemoveAt(i);
                continue;
            }

            float sqDist = (target.position - position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = target;
            }
        }

        foreach (PlayerController p in _players)
        {
            if (p == null) continue;
            if (!p.EnemiesCanSee) continue;
            float sqDist = (p.transform.position - position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = p.transform;
            }
        }

        // In online mode, also consider the remote player's ghost position
        // so host-side enemies chase whichever player (local or remote) is closest.
        if (IsOnline && RemotePlayerGhost.Instance != null
            && OnlinePlayerSync.Instance != null && OnlinePlayerSync.Instance.HasRemotePlayer)
        {
            float sqDist = (RemotePlayerGhost.Instance.transform.position - position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = RemotePlayerGhost.Instance.transform;
            }
        }

        if (nearest == null && PlayerController.main != null && PlayerController.main.EnemiesCanSee)
            nearest = PlayerController.main.transform;

        return nearest;
    }

    public static void RegisterPlayerDeath(PlayerController player)
    {
        UnregisterPlayer(player);

        bool anyAlive = false;
        foreach (PlayerController p in _players)
        {
            if (p != null) { anyAlive = true; break; }
        }

        if (!anyAlive)
            GameStatsTracker.RegisterPlayerDied();
    }

    public static void Reset()
    {
        _players.Clear();
        IsMultiplayer = false;
        IsOnline = false;
        IsHost = false;
        OnlineRoomNumber = 1;
        _enemyTargets.Clear();
    }
}

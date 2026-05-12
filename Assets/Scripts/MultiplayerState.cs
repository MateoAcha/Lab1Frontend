using System.Collections.Generic;
using UnityEngine;

public static class MultiplayerState
{
    public static bool IsMultiplayer { get; private set; }
    public static bool IsOnline      { get; private set; }
    public static bool IsHost        { get; private set; }

    private static readonly List<PlayerController> _players = new List<PlayerController>();

    public static void SetMultiplayer(bool value) { IsMultiplayer = value; }
    public static void SetOnline(bool value)      { IsOnline = value; }
    public static void SetHost(bool value)        { IsHost = value; }

    public static void RegisterPlayer(PlayerController player)
    {
        if (player != null && !_players.Contains(player))
            _players.Add(player);
    }

    public static void UnregisterPlayer(PlayerController player)
    {
        _players.Remove(player);
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

    public static Transform GetNearestPlayer(Vector3 position)
    {
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        foreach (PlayerController p in _players)
        {
            if (p == null) continue;
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

        if (nearest == null && PlayerController.main != null)
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
    }
}

using UnityEngine;

public static class PvpDamageUtility
{
    public static bool TryGetOpposingPlayer(Collider2D other, int ownerPlayerIndex, out PlayerController player)
    {
        player = other != null ? other.GetComponent<PlayerController>() : null;
        if (!MultiplayerState.IsPvP || ownerPlayerIndex < 0 || player == null)
        {
            return false;
        }

        return player.playerIndex != ownerPlayerIndex;
    }

    public static bool TryDamageOpposingPlayer(Collider2D other, int ownerPlayerIndex, float damage)
    {
        PlayerController player;
        if (!TryGetOpposingPlayer(other, ownerPlayerIndex, out player))
        {
            return false;
        }

        Health health = other.GetComponent<Health>();
        if (health == null)
        {
            return false;
        }

        health.Hit(damage);
        return true;
    }

    public static bool IsOwnedByPlayer(Collider2D other, int ownerPlayerIndex)
    {
        if (other == null || ownerPlayerIndex < 0)
        {
            return false;
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            return player.playerIndex == ownerPlayerIndex;
        }

        TemporaryWall wall = other.GetComponent<TemporaryWall>();
        if (wall != null)
        {
            return wall.ownerPlayerIndex == ownerPlayerIndex;
        }

        PlayerDecoy decoy = other.GetComponent<PlayerDecoy>();
        if (decoy != null)
        {
            return decoy.ownerPlayerIndex == ownerPlayerIndex;
        }

        PlayerMinion minion = other.GetComponent<PlayerMinion>();
        return minion != null && minion.ownerPlayerIndex == ownerPlayerIndex;
    }
}

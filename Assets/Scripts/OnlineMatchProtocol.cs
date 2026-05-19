using System;

[Serializable]
public class OnlineMatchInputMessage
{
    public string type = "input";
    public float moveX;
    public float moveY;
    public float aimX;
    public float aimY = -1f;
    public int attackSeq;
    public int chargeSeq;
    public int burstSeq;
    public int consumableSeq;
    public int weaponDamage = 1;
    public string weaponType = "Spear";
    public string weaponColor = "#FFFFFF";
    public int skinId;
    public string skinColor = "#4DBFFF";
    public float maxHp = 10f;
    public int consumableQuantity;
    public float consumableHealAmount;
    public float consumableCooldown;
    public bool consumableIsSpeedBoost;
    public float speedBoostDuration = 3f;
    public float speedBoostMultiplier = 2f;
}

[Serializable]
public class OnlineMatchStateMessage
{
    public string type = "state";
    public int tick;
    public bool matchEnded;
    public int meleeKills;
    public int rangedKills;
    public int elapsedSeconds;
    public OnlinePlayerState[] players;
    public OnlineEnemyState[] enemies;
    public OnlineProjectileState[] projectiles;
}

[Serializable]
public class OnlinePlayerState
{
    public int id;
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float hp;
    public float maxHp;
    public bool alive;
    public int skinId;
    public string skinColor = "#4DBFFF";
}

[Serializable]
public class OnlineEnemyState
{
    public int id;
    public int type;
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float hp;
    public float maxHp;
    public float size;
}

[Serializable]
public class OnlineProjectileState
{
    public int id;
    public bool fromPlayer;
    public int ownerId;
    public bool isHitbox;
    public string color;
    public float size;
    public float scaleX;
    public float scaleY;
    public float rotationZ;
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float life;
}

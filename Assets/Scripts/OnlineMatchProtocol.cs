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
    public int weaponItemId;
    public string weaponType = "Spear";
    public string weaponColor = "#FFFFFF";
    public int skinId;
    public string skinColor = "#FFFFFF";
    public float maxHp = 10f;
    public int consumableQuantity;
    public float consumableHealAmount;
    public float consumableCooldown;
    public bool consumableIsSpeedBoost;
    public float speedBoostDuration = 3f;
    public float speedBoostMultiplier = 2f;
    public string swordSpearActiveSkillId = "";
    public int swordSpearActiveSkillLevel;
    public string swordSpearPassiveSkillId = "";
    public int swordSpearPassiveSkillLevel;
    public string rangedActiveSkillId = "";
    public int rangedActiveSkillLevel;
    public string rangedPassiveSkillId = "";
    public int rangedPassiveSkillLevel;
    public int pickedUpItemId = -1;
}

[Serializable]
public class OnlineMatchStateMessage
{
    public string type = "state";
    public int tick;
    public bool matchEnded;
    public bool matchEnding;
    public bool matchFinished;
    public bool pausedByHost;
    public int meleeKills;
    public int rangedKills;
    public int giantKills;
    public int elapsedSeconds;
    public int mapIndex;
    public bool exitActive;
    public float exitX;
    public float exitY;
    public OnlineExitState[] exits;
    public OnlinePlayerState[] players;
    public OnlineEnemyState[] enemies;
    public OnlineProjectileState[] projectiles;
    public OnlineItemDropState[] itemDrops;
    public OnlineAbilityState[] abilities;
}

[Serializable]
public class OnlineExitState
{
    public int id;
    public bool active;
    public float x;
    public float y;
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
    public string skinColor = "#FFFFFF";
    public int attackSeq;
    public string weaponType = "";
    public string weaponColor = "#FFFFFF";
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
    public bool isMinion;
}

[Serializable]
public class OnlineItemDropState
{
    public int id;
    public float x;
    public float y;
}

[Serializable]
public class OnlineAbilityState
{
    public int id;
    public int type;     // 0=burst 1=gravityBomb 2=well 3=thrownWeapon
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float scale;
    public float maxScale;
    public float remaining;
    public int cr;
    public int cg;
    public int cb;
}

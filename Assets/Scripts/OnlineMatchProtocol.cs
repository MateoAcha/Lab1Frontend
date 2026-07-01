using System;

[Serializable]
public class OnlineMatchInputMessage
{
    public string type = "input";
    public string username = "";
    public float moveX;
    public float moveY;
    public float aimX;
    public float aimY = -1f;
    public bool ready;
    public int readyMapIndex = -1;
    public int attackSeq;
    public int chargeSeq;
    public int burstSeq;
    public int consumableSeq;
    public int quickChatSeq;
    public string quickChatEmote = "";
    public bool reviveHeld;
    public int pickupSeq;
    public int pickupId;
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
}

[Serializable]
public class OnlineMatchStateMessage
{
    public string type = "state";
    public int tick;
    public bool matchStarted = true;
    public bool matchEnded;
    public bool matchEnding;
    public bool matchFinished;
    public bool pausedByHost;
    public int endingPlayerId = -1;
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
    public OnlineEffectState[] effects;
    public OnlineMaterialPickupState[] pickups;
}

public static class OnlineEffectType
{
    public const int ThrownWeapon = 1;
    public const int RangedAbilityProjectile = 2;
    public const int ExpansionBurst = 3;
    public const int GravityBombProjectile = 4;
    public const int GravityWell = 5;
    public const int PlayerMinion = 6;
    public const int FireTrail = 7;
    public const int TemporaryWall = 8;
    public const int PlayerDecoy = 9;
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
    public string username = "";
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float hp;
    public float maxHp;
    public bool present = true;
    public bool alive;
    public bool downed;
    public float reviveProgress;
    public int skinId;
    public string skinColor = "#FFFFFF";
    public int attackSeq;
    public int quickChatSeq;
    public string quickChatEmote = "";
    public int weaponItemId;
    public string weaponType = "Spear";
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
    public bool hasWeaponVisual;
    public int weaponItemId;
    public string weaponType = "Spear";
    public float size;
    public float scaleX;
    public float scaleY;
    public float rotationZ;
    public float visualOffsetX;
    public float visualOffsetY;
    public float visualScaleX = 1f;
    public float visualScaleY = 1f;
    public float visualRotationZ;
    public bool swordSwing;
    public float swingDirectionX;
    public float swingDirectionY;
    public float swingDistance;
    public float swingDuration;
    public float swingArcDegrees;
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float life;
}

[Serializable]
public class OnlineEffectState
{
    public int id;
    public int type;
    public int ownerId;
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float rotationZ;
    public float scaleX = 1f;
    public float scaleY = 1f;
    public string color = "#FFFFFFFF";
    public float life;
    public bool explosive;
    public bool boomerang;
    public int weaponItemId;
    public string weaponType = "Spear";
    public bool hasWeaponVisual;
    public float visualOffsetX;
    public float visualOffsetY;
    public float visualScaleX = 1f;
    public float visualScaleY = 1f;
    public float visualRotationZ;
    public float shadowScaleX = 1f;
    public float shadowScaleY = 0.35f;
    public int skinId;
    public string skinColor = "#FFFFFF";
}

[Serializable]
public class OnlineMaterialPickupState
{
    public int id;
    public float x;
    public float y;
    public string inventoryKey = "";
    public string itemName = "";
    public string rarity = "Rare";
    public string color = "#FFFFFFFF";
    public float size = 0.9f;
}

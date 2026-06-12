using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    public static PlayerController main;

    public int playerIndex = 0;
    public float speed = 5f;
    public float cooldown = 0.35f;
    public int damage = 1;
    public float range = 3f;
    public float length = 4.5f;
    public float width = 0.5f;
    public float time = 0.12f;
    [Header("Weapon Types")]
    public float swordRangeMultiplier = 0.7f;
    public float swordWidthMultiplier = 1.8f;
    public float swordArcDegrees = 90f;
    public float swordSwingDurationMultiplier = 1.5f;
    [Header("Sword Visual")]
    public Sprite swordSwingSprite;
    public Texture2D swordSwingTexture;
    public Vector2 swordSwingVisualOffset = Vector2.zero;
    public Vector2 swordSwingVisualScale = Vector2.one;
    public float swordSwingVisualRotationOffset;
    [Header("Carried Sword Visual")]
    public Vector2 carriedSwordVisualOffset = new Vector2(-0.18f, 0.18f);
    public Vector2 carriedSwordVisualScale = Vector2.one;
    public float carriedSwordVisualRotationOffset = -35f;
    public int carriedSwordSortingOrderOffset = -1;
    [Header("Spear Visual")]
    public Sprite spearSprite;
    public Texture2D spearTexture;
    public Vector2 spearVisualOffset = Vector2.zero;
    public Vector2 spearVisualScale = Vector2.one;
    public float spearVisualRotationOffset;
    public float spearThrustDistance = 0.35f;
    [Header("Carried Spear Visual")]
    public Vector2 carriedSpearVisualOffset = new Vector2(-0.18f, 0.12f);
    public Vector2 carriedSpearVisualScale = Vector2.one;
    public float carriedSpearVisualRotationOffset = -35f;
    public int carriedSpearSortingOrderOffset = 1;
    public float rangedProjectileSpeed = 12f;
    public float rangedProjectileLife = 2.2f;
    public float rangedProjectileSize = 0.35f;
    [Header("Carried Ranged Orb Visual")]
    public Vector2 carriedRangedOrbOffset = new Vector2(0.18f, 0.08f);
    public Vector2 carriedRangedOrbScale = Vector2.one;
    public int carriedRangedOrbSortingOrderOffset = 1;
    [Header("Ranged Powers (E)")]
    public float bombShotProjectileSize = 1.25f;
    public float bombShotSpeedMultiplier = 0.75f;
    public float bombShotDamageMultiplier = 1.25f;
    public float bombShotExplosionRadius = 3.6f;
    public float bombShotExplosionDamageMultiplier = 1.5f;
    public float bombShotExplosionDuration = 0.24f;
    public float bombShotExplosionPushMultiplier = 2.4f;
    public float bombShotLevelRangeBonus = 0.15f;
    public float bombShotLevelDamageBonus = 0.15f;
    public int quickBurstShotCount = 5;
    public float quickBurstInterval = 0.1f;
    public float quickBurstDamageMultiplier = 0.8f;
    public int quickBurstLevelExtraShots = 2;
    public float snipeShotSpeedMultiplier = 2.6f;
    public float snipeShotDamageMultiplier = 3f;
    public float snipeShotLevelDamageBonus = 0.25f;
    [Header("Charge Ability (E)")]
    public float chargeDuration = 0.3f;
    public float chargeCooldown = 5f;
    public float chargeSpeedMultiplier = 4f;
    public float chargeDamageMultiplier = 2f;
    public float chargeRangeMultiplier = 2f;
    public float chargeLevelDamageBonus = 0.15f;
    public float chargeLevelHitboxBonus = 0.12f;
    public float chargeLevelJumpBonus = 0.10f;
    [Header("Sword / Spear Throw (E)")]
    public float weaponThrowSpeed = 13f;
    public float weaponThrowRange = 8f;
    public float weaponThrowLife = 1.6f;
    public float weaponThrowDamageMultiplier = 1.6f;
    public float boomerangReturnSpeed = 15f;
    public float weaponThrowLevelRangeBonus = 0.18f;
    [Header("Fire Trail (E)")]
    public float fireTrailDuration = 3.2f;
    public float fireTrailSpeedMultiplier = 1.85f;
    public float fireTrailSpawnInterval = 0.09f;
    public float fireTrailSegmentLife = 3.75f;
    public float fireTrailSegmentSize = 1.15f;
    public float fireTrailDamageMultiplier = 0.1875f;
    public float fireTrailLevelDurationBonus = 0.20f;
    public float fireTrailLevelSegmentLifeBonus = 0.25f;
    [Header("Burst Ability (Q)")]
    public float burstRange = 6f;
    public float burstDuration = 0.2f;
    public float burstDamageMultiplier = 0.35f;
    public float burstPushMultiplier = 4f;
    public float burstCooldown = 6f;
    public float burstLevelRadiusBonus = 0.12f;
    [Header("Sword / Spear Utility (Q)")]
    public float wallDistance = 2.8f;
    public float wallLength = 4.2f;
    public float wallThickness = 0.45f;
    public float wallHealth = 18f;
    public float wallLife = 8f;
    public float wallDecayDamagePerSecond = 2f;
    public float wallLevelSizeBonus = 0.12f;
    public float gravityBombDistance = 6.5f;
    public float gravityBombTravelTime = 0.85f;
    public float gravityBombArcHeight = 2.6f;
    public float gravityBombPullRadius = 5f;
    public float gravityBombPullDuration = 3f;
    public float gravityBombPullStrength = 11f;
    public float gravityBombLevelRadiusBonus = 0.12f;
    [Header("Ranged Utility (Q)")]
    public float decoyLife = 5f;
    public float decoyHealth = 25f;
    public float decoyStealthDuration = 2.8f;
    public float stealthAlpha = 0.35f;
    public float decoyLevelLifeBonus = 0.20f;
    public float decoyLevelHealthBonus = 0.20f;
    public float minionLife = 18f;
    public float minionHealth = 8f;
    public float minionSizeMultiplier = 0.75f;
    public float minionSpeed = 4.2f;
    public int minionTouchDamage = 1;
    public float minionTouchCooldown = 0.45f;
    [Header("Minion Visual")]
    public Sprite[] minionMoveSprites;
    public Texture2D minionMoveTexture;
    public string minionMoveResource = "Sprites/Minion";
    public float minionSpriteScale = 1f;
    public float minionMoveFps = 8f;

    private Rigidbody2D body;
    private Vector2 look = Vector2.down;
    private Vector2 chargeDirection = Vector2.down;
    private float nextAttack;
    private float chargeUntil;
    private float nextChargeReady;
    private float nextBurstReady;
    private float nextConsumableReady;
    private float _activeChargeSpeedMultiplier = 1f;
    private float _speedBoostUntil;
    private float _fireTrailBoostUntil;
    private float _nextFireTrailAt;
    private bool _useExternalInput;
    private Vector2 _externalMove;
    private Vector2 _externalAim = Vector2.down;
    private bool _externalAttackDown;
    private bool _externalChargeDown;
    private bool _externalBurstDown;
    private bool _externalConsumableDown;
    private bool _hasNetworkLoadout;
    private int _networkWeaponDamage = 1;
    private int _networkWeaponItemId;
    private WeaponKind _networkWeaponKind = WeaponKind.Spear;
    private Color _networkWeaponColor = Color.white;
    private int _networkSkinId;
    private string _networkSkinColor = "#FFFFFF";
    private int _networkConsumableQuantity;
    private float _networkConsumableHealAmount;
    private float _networkConsumableCooldown;
    private bool _networkConsumableIsSpeedBoost;
    private float _networkSpeedBoostDuration = 3f;
    private float _networkSpeedBoostMultiplier = 2f;
    private string _networkSwordSpearActiveSkillId = "";
    private int _networkSwordSpearActiveSkillLevel;
    private string _networkSwordSpearPassiveSkillId = "";
    private int _networkSwordSpearPassiveSkillLevel;
    private string _networkRangedActiveSkillId = "";
    private int _networkRangedActiveSkillLevel;
    private string _networkRangedPassiveSkillId = "";
    private int _networkRangedPassiveSkillLevel;
    private Coroutine _quickBurstRoutine;
    private float _stealthUntil;
    private float _normalAlpha = 1f;
    private SpriteRenderer _playerRenderer;
    private SpriteRenderer _carriedSwordRenderer;
    private Transform _carriedSwordTransform;
    private Sprite _appliedCarriedSwordSprite;
    private bool _carriedSwordFacingLeft;
    private float _hideCarriedSwordUntil;
    private SpriteRenderer _carriedSpearRenderer;
    private Transform _carriedSpearTransform;
    private Sprite _appliedCarriedSpearSprite;
    private bool _carriedSpearFacingLeft;
    private float _hideCarriedSpearUntil;
    private Texture2D _cachedSwordSwingTexture;
    private Sprite _cachedSwordSwingTextureSprite;
    private Texture2D _cachedSpearTexture;
    private Sprite _cachedSpearTextureSprite;
    private SpriteRenderer _carriedRangedOrbRenderer;
    private Transform _carriedRangedOrbTransform;
    private bool _carriedRangedOrbFacingLeft;
    private float _hideCarriedRangedOrbUntil;
    private static Texture2D _chargeHitboxGlowTexture;
    private static Sprite _chargeHitboxGlowSprite;

    public float ConsumableCooldownRemaining => Mathf.Max(0f, nextConsumableReady - Time.time);
    public Vector2 LastMoveInput { get; private set; }
    public Vector2 LastAimDirection { get; private set; } = Vector2.down;
    public int NetworkAttackSequence { get; private set; }
    public int NetworkChargeSequence { get; private set; }
    public int NetworkBurstSequence { get; private set; }
    public int NetworkConsumableSequence { get; private set; }
    public int NetworkSkinId => _hasNetworkLoadout ? _networkSkinId : PlayerLoadout.EquippedSkinId;
    public string NetworkSkinColor => _hasNetworkLoadout ? _networkSkinColor : PlayerSkinVisuals.GetEquippedSkinColorHex();
    public bool EnemiesCanSee => Time.time >= _stealthUntil;

    public float ChargeCooldownProgress01
    {
        get
        {
            if (chargeCooldown <= 0f)
            {
                return 1f;
            }

            float remaining = Mathf.Max(0f, nextChargeReady - Time.time);
            return 1f - Mathf.Clamp01(remaining / chargeCooldown);
        }
    }

    public float BurstCooldownProgress01
    {
        get
        {
            if (burstCooldown <= 0f)
            {
                return 1f;
            }

            float remaining = Mathf.Max(0f, nextBurstReady - Time.time);
            return 1f - Mathf.Clamp01(remaining / burstCooldown);
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = 0f;
        body.freezeRotation = true;

        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        if (GetComponent<Health>() == null)
        {
            gameObject.AddComponent<Health>();
        }

        if (GetComponent<ChargeAbilityUI>() == null)
        {
            gameObject.AddComponent<ChargeAbilityUI>();
        }

        if (GetComponent<BurstAbilityUI>() == null)
        {
            gameObject.AddComponent<BurstAbilityUI>();
        }

        if (GetComponent<GameTimerUI>() == null)
        {
            gameObject.AddComponent<GameTimerUI>();
        }

        if (GetComponent<ConsumableUI>() == null)
        {
            gameObject.AddComponent<ConsumableUI>();
        }

        _playerRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_playerRenderer != null)
        {
            _normalAlpha = _playerRenderer.color.a;
        }
    }

    private void Start()
    {
        if (playerIndex == 0) main = this;
        MultiplayerState.RegisterPlayer(this);
    }

    private void OnDestroy()
    {
        if (main == this) main = null;
        MultiplayerState.UnregisterPlayer(this);
    }

    private void Update()
    {
        bool chargeDown = ReadChargeDown();
        if (!_useExternalInput && chargeDown) NetworkChargeSequence++;
        if (chargeDown && Time.time >= nextChargeReady)
        {
            ActivateCharge();
        }

        bool burstDown = ReadBurstDown();
        if (!_useExternalInput && burstDown) NetworkBurstSequence++;
        if (burstDown && Time.time >= nextBurstReady)
        {
            ActivateBurst();
        }

        bool consumableDown = ReadConsumableDown();
        if (!_useExternalInput && consumableDown) NetworkConsumableSequence++;
        if (consumableDown && Time.time >= nextConsumableReady)
        {
            UseConsumable();
        }

        if (Time.time < chargeUntil)
        {
            float boost = Mathf.Max(1f, _activeChargeSpeedMultiplier);
            body.linearVelocity = chargeDirection * (speed * boost);
        }
        else
        {
            Vector2 move = ReadMove();
            LastMoveInput = move;
            float activeSpeed = Time.time < _speedBoostUntil
                ? speed * GetSpeedBoostMultiplier()
                : speed;
            if (Time.time < _fireTrailBoostUntil)
            {
                activeSpeed = Mathf.Max(activeSpeed, speed * Mathf.Max(1f, fireTrailSpeedMultiplier));
                MaybeSpawnFireTrail(move);
            }

            body.linearVelocity = move * activeSpeed;
        }

        bool attackRequested = ReadAttackRequested();
        if (attackRequested && Time.time >= nextAttack)
        {
            if (!_useExternalInput) NetworkAttackSequence++;
            look = ReadMouseAimDirection();
            LastAimDirection = look;
            Attack(look, 1f, 1f);
            nextAttack = Time.time + cooldown;
        }

        UpdateStealthVisual();
        UpdateCarriedSwordVisual();
        UpdateCarriedSpearVisual();
        UpdateCarriedRangedOrbVisual();
    }

    public void SetExternalInputEnabled(bool enabled)
    {
        _useExternalInput = enabled;
        if (enabled)
        {
            _externalMove = Vector2.zero;
            _externalAim = look.sqrMagnitude > 0.001f ? look : Vector2.down;
        }
    }

    public void ApplyExternalInput(
        Vector2 move,
        Vector2 aim,
        bool attackDown,
        bool chargeDown,
        bool burstDown,
        bool consumableDown)
    {
        _useExternalInput = true;
        _externalMove = Vector2.ClampMagnitude(move, 1f);
        if (aim.sqrMagnitude > 0.001f)
        {
            _externalAim = aim.normalized;
            LastAimDirection = _externalAim;
        }

        _externalAttackDown |= attackDown;
        _externalChargeDown |= chargeDown;
        _externalBurstDown |= burstDown;
        _externalConsumableDown |= consumableDown;
    }

    public Vector2 GetAimDirectionForNetwork()
    {
        Vector2 aim = ReadMouseAimDirection();
        if (aim.sqrMagnitude > 0.001f)
        {
            LastAimDirection = aim.normalized;
        }
        return LastAimDirection;
    }

    public void ConfigureNetworkLoadout(
        int weaponDamage,
        string weaponType,
        string weaponColor,
        int skinId,
        string skinColor,
        float maxHp,
        int consumableQuantity,
        float consumableHealAmount,
        float consumableCooldown,
        bool consumableIsSpeedBoost,
        float speedBoostDuration,
        float speedBoostMultiplier,
        string swordSpearActiveSkillId = "",
        int swordSpearActiveSkillLevel = 0,
        string swordSpearPassiveSkillId = "",
        int swordSpearPassiveSkillLevel = 0,
        string rangedActiveSkillId = "",
        int rangedActiveSkillLevel = 0,
        string rangedPassiveSkillId = "",
        int rangedPassiveSkillLevel = 0,
        int weaponItemId = 0)
    {
        _hasNetworkLoadout = true;
        _networkWeaponDamage = Mathf.Max(1, weaponDamage);
        _networkWeaponItemId = Mathf.Max(0, weaponItemId);
        _networkWeaponKind = PlayerLoadout.ParseWeaponKind(weaponType);
        _networkWeaponColor = PlayerLoadout.ParseWeaponColor(weaponColor, Color.white);
        _networkSkinId = Mathf.Max(0, skinId);
        _networkSkinColor = string.IsNullOrWhiteSpace(skinColor) ? "#FFFFFF" : skinColor;
        ApplyNetworkSkin();
        _networkConsumableQuantity = Mathf.Max(0, consumableQuantity);
        _networkConsumableHealAmount = Mathf.Max(0f, consumableHealAmount);
        _networkConsumableCooldown = Mathf.Max(0f, consumableCooldown);
        _networkConsumableIsSpeedBoost = consumableIsSpeedBoost;
        _networkSpeedBoostDuration = Mathf.Max(0f, speedBoostDuration);
        _networkSpeedBoostMultiplier = Mathf.Max(1f, speedBoostMultiplier);
        _networkSwordSpearActiveSkillId = NormalizeNetworkSkillId(swordSpearActiveSkillId);
        _networkSwordSpearActiveSkillLevel = ClampNetworkSkillLevel(swordSpearActiveSkillLevel);
        _networkSwordSpearPassiveSkillId = NormalizeNetworkSkillId(swordSpearPassiveSkillId);
        _networkSwordSpearPassiveSkillLevel = ClampNetworkSkillLevel(swordSpearPassiveSkillLevel);
        _networkRangedActiveSkillId = NormalizeNetworkSkillId(rangedActiveSkillId);
        _networkRangedActiveSkillLevel = ClampNetworkSkillLevel(rangedActiveSkillLevel);
        _networkRangedPassiveSkillId = NormalizeNetworkSkillId(rangedPassiveSkillId);
        _networkRangedPassiveSkillLevel = ClampNetworkSkillLevel(rangedPassiveSkillLevel);

        Health health = GetComponent<Health>();
        if (health != null && maxHp > 0f)
        {
            bool wasFull = health.maxHp <= 0f || health.hp >= health.maxHp - 0.01f;
            health.maxHp = maxHp;
            health.hp = wasFull ? health.maxHp : Mathf.Min(health.hp, health.maxHp);
        }
    }

    private static string NormalizeNetworkSkillId(string skillId)
    {
        return string.IsNullOrWhiteSpace(skillId) ? "" : skillId;
    }

    private static int ClampNetworkSkillLevel(int level)
    {
        return Mathf.Clamp(level, 0, PlayerSkillLoadout.MaxSkillLevel);
    }

    private Vector2 ReadMove()
    {
        if (_useExternalInput)
        {
            return _externalMove;
        }

        if (playerIndex == 1)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = GetGamepad();
            return pad != null ? Vector2.ClampMagnitude(pad.leftStick.ReadValue(), 1f) : Vector2.zero;
#else
            return Vector2.zero;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
        }
        return Vector2.ClampMagnitude(move, 1f);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
    }

    private bool ReadAttackRequested()
    {
        if (_useExternalInput)
        {
            bool value = _externalAttackDown;
            _externalAttackDown = false;
            return value;
        }

        if (playerIndex == 1)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = GetGamepad();
            return pad != null && pad.rightTrigger.isPressed;
#else
            return false;
#endif
        }
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private bool ReadChargeDown()
    {
        if (_useExternalInput)
        {
            bool value = _externalChargeDown;
            _externalChargeDown = false;
            return value;
        }

        if (playerIndex == 1)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = GetGamepad();
            return pad != null && pad.rightShoulder.wasPressedThisFrame;
#else
            return false;
#endif
        }
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private bool ReadBurstDown()
    {
        if (_useExternalInput)
        {
            bool value = _externalBurstDown;
            _externalBurstDown = false;
            return value;
        }

        if (playerIndex == 1)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = GetGamepad();
            return pad != null && pad.leftShoulder.wasPressedThisFrame;
#else
            return false;
#endif
        }
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    private bool ReadConsumableDown()
    {
        if (_useExternalInput)
        {
            bool value = _externalConsumableDown;
            _externalConsumableDown = false;
            return value;
        }

        if (playerIndex == 1)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = GetGamepad();
            return pad != null && pad.buttonNorth.wasPressedThisFrame;
#else
            return false;
#endif
        }
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private Gamepad GetGamepad()
    {
        var all = Gamepad.all;
        return all.Count > 0 ? all[0] : null;
    }
#endif

    private void UseConsumable()
    {
        if (_hasNetworkLoadout)
        {
            UseNetworkConsumable();
            return;
        }

        if (!PlayerLoadout.UseConsumable()) return;
        GameAudio.PlayPotionDrink();

        if (PlayerLoadout.ConsumableIsSpeedBoost)
        {
            _speedBoostUntil = Time.time + PlayerLoadout.SpeedBoostDuration;
        }
        else
        {
            Health health = GetComponent<Health>();
            if (health != null)
                health.Hit(-PlayerLoadout.ConsumableHealAmount);
        }

        nextConsumableReady = Time.time + Mathf.Max(0f, PlayerLoadout.ConsumableCooldown);
    }

    private void UseNetworkConsumable()
    {
        if (_networkConsumableQuantity <= 0) return;
        if (!_networkConsumableIsSpeedBoost && _networkConsumableHealAmount <= 0f) return;

        _networkConsumableQuantity--;
        GameAudio.PlayPotionDrink();

        if (_networkConsumableIsSpeedBoost)
        {
            _speedBoostUntil = Time.time + _networkSpeedBoostDuration;
        }
        else
        {
            Health health = GetComponent<Health>();
            if (health != null)
                health.Hit(-_networkConsumableHealAmount);
        }

        nextConsumableReady = Time.time + _networkConsumableCooldown;
    }

    private Vector2 ReadMouseAimDirection()
    {
        if (_useExternalInput)
        {
            return _externalAim.sqrMagnitude > 0.001f ? _externalAim.normalized : look;
        }

        if (playerIndex == 1)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = GetGamepad();
            if (pad != null)
            {
                Vector2 stick = pad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                    return stick.normalized;
            }
#endif
            return look;
        }

        Camera cam = Camera.main;
        if (cam == null) return look;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return look;
        Vector3 screen = Mouse.current.position.ReadValue();
#else
        Vector3 screen = Input.mousePosition;
#endif
        screen.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(screen);
        Vector2 aim = (Vector2)(world - transform.position);
        return aim.sqrMagnitude > 0.001f ? aim.normalized : look;
    }

    private void ActivateCharge()
    {
        WeaponKind weaponKind = GetWeaponKind();
        if (weaponKind == WeaponKind.Ranged)
        {
            ActivateRangedPower();
            return;
        }

        string skillId = GetEquippedSwordSpearSkillId(SkillSlotKind.Active);
        if (IsSwordSpearWeapon(weaponKind) && skillId == "swordspear_active_2")
        {
            ActivateWeaponThrow(weaponKind);
            return;
        }

        if (IsSwordSpearWeapon(weaponKind) && skillId == "swordspear_active_3")
        {
            ActivateFireTrail();
            return;
        }

        ActivateDefaultCharge();
    }

    private void ActivateDefaultCharge()
    {
        Vector2 aim = ReadMouseAimDirection();
        look = aim;
        chargeDirection = aim;

        int level = GetEquippedSwordSpearSkillId(SkillSlotKind.Active) == "swordspear_active_1"
            ? GetSwordSpearSkillLevel("swordspear_active_1")
            : 0;
        float jumpMultiplier = 1f + level * Mathf.Max(0f, chargeLevelJumpBonus);
        float hitboxMultiplier = 1f + level * Mathf.Max(0f, chargeLevelHitboxBonus);
        float damageMultiplier = 1f + level * Mathf.Max(0f, chargeLevelDamageBonus);

        _activeChargeSpeedMultiplier = Mathf.Max(1f, chargeSpeedMultiplier * jumpMultiplier);
        chargeUntil = Time.time + Mathf.Max(0f, chargeDuration * jumpMultiplier);
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);

        Attack(
            aim,
            Mathf.Max(1f, chargeDamageMultiplier * damageMultiplier),
            Mathf.Max(1f, chargeRangeMultiplier * hitboxMultiplier),
            Mathf.Max(0.01f, chargeDuration * jumpMultiplier));
    }

    private void ActivateRangedPower()
    {
        string skillId = GetEquippedRangedSkillId(SkillSlotKind.Active);
        if (skillId == "ranged_active_1")
        {
            ActivateBombShot();
            return;
        }

        if (skillId == "ranged_active_2")
        {
            ActivateQuickBurst();
            return;
        }

        if (skillId == "ranged_active_3")
        {
            ActivateSnipeShot();
            return;
        }

        ActivateDefaultCharge();
    }

    private void ActivateBombShot()
    {
        Vector2 aim = GetCurrentAim();
        int level = GetRangedSkillLevel("ranged_active_1");
        float rangeMultiplier = 1f + level * Mathf.Max(0f, bombShotLevelRangeBonus);
        float damageMultiplier = 1f + level * Mathf.Max(0f, bombShotLevelDamageBonus);
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);
        SpawnRangedAbilityProjectile(
            "BombShot",
            aim,
            Mathf.Max(rangedProjectileSize, bombShotProjectileSize),
            Mathf.Max(0.1f, rangedProjectileSpeed * Mathf.Max(0.1f, bombShotSpeedMultiplier)),
            rangedProjectileLife * rangeMultiplier,
            Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(0.1f, bombShotDamageMultiplier) * damageMultiplier)),
            true,
            rangeMultiplier,
            damageMultiplier);
    }

    private void ActivateQuickBurst()
    {
        Vector2 aim = GetCurrentAim();
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);
        if (_quickBurstRoutine != null)
        {
            StopCoroutine(_quickBurstRoutine);
        }

        _quickBurstRoutine = StartCoroutine(QuickBurstRoutine(aim));
    }

    private IEnumerator QuickBurstRoutine(Vector2 aim)
    {
        int level = GetRangedSkillLevel("ranged_active_2");
        int shotCount = Mathf.Max(1, quickBurstShotCount + level * Mathf.Max(0, quickBurstLevelExtraShots));
        float interval = Mathf.Max(0.01f, quickBurstInterval);
        for (int i = 0; i < shotCount; i++)
        {
            Vector2 usedAim = i == 0 ? aim : GetCurrentAim();
            HideCarriedRangedOrbForAttackCooldown();
            SpawnRangedAbilityProjectile(
                "QuickBurstShot",
                usedAim,
                rangedProjectileSize,
                rangedProjectileSpeed,
                rangedProjectileLife,
                Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(0.1f, quickBurstDamageMultiplier))),
                false);

            if (i < shotCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        _quickBurstRoutine = null;
    }

    private void ActivateSnipeShot()
    {
        Vector2 aim = GetCurrentAim();
        int level = GetRangedSkillLevel("ranged_active_3");
        float damageMultiplier = 1f + level * Mathf.Max(0f, snipeShotLevelDamageBonus);
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);
        HideCarriedRangedOrbForAttackCooldown();
        SpawnRangedAbilityProjectile(
            "SnipeShot",
            aim,
            rangedProjectileSize,
            Mathf.Max(0.1f, rangedProjectileSpeed * Mathf.Max(0.1f, snipeShotSpeedMultiplier)),
            rangedProjectileLife,
            Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(0.1f, snipeShotDamageMultiplier) * damageMultiplier)),
            false);
    }

    private void SpawnRangedAbilityProjectile(
        string name,
        Vector2 aim,
        float projectileSize,
        float projectileSpeed,
        float projectileLife,
        int projectileDamage,
        bool explosive,
        float explosionRadiusMultiplier = 1f,
        float explosionDamageMultiplier = 1f)
    {
        if (aim.sqrMagnitude < 0.001f)
        {
            aim = Vector2.down;
        }

        aim.Normalize();
        GameObject projectile = new GameObject(name);
        projectile.transform.position = transform.position + (Vector3)(aim * 0.7f);
        projectile.transform.localScale = Vector3.one * Mathf.Max(0.05f, projectileSize);
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = explosive ? SimpleSprite.Square : SimpleSprite.Circle;
        renderer.color = explosive
            ? new Color(1f, 0.72f, 0.18f, 1f)
            : GetAttackColor(1f);
        renderer.sortingOrder = 12;

        BoxCollider2D box = projectile.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        RangedAbilityProjectile abilityProjectile = projectile.AddComponent<RangedAbilityProjectile>();
        abilityProjectile.direction = aim;
        abilityProjectile.speed = Mathf.Max(0.1f, projectileSpeed);
        abilityProjectile.life = Mathf.Max(0.05f, projectileLife);
        abilityProjectile.damage = Mathf.Max(1, projectileDamage);
        abilityProjectile.ownerPlayerIndex = playerIndex;
        abilityProjectile.projectileColor = renderer.color;
        abilityProjectile.explodesOnImpact = explosive;
        abilityProjectile.explosionRadius = Mathf.Max(0.2f, bombShotExplosionRadius * Mathf.Max(0.1f, explosionRadiusMultiplier));
        abilityProjectile.explosionDamage = Mathf.Max(0f, GetWeaponDamage() * Mathf.Max(0.1f, bombShotExplosionDamageMultiplier) * Mathf.Max(0.1f, explosionDamageMultiplier));
        abilityProjectile.explosionDuration = Mathf.Max(0.01f, bombShotExplosionDuration);
        abilityProjectile.explosionPushMultiplier = Mathf.Max(0f, bombShotExplosionPushMultiplier);
    }

    private void ActivateBurst()
    {
        WeaponKind weaponKind = GetWeaponKind();
        if (weaponKind == WeaponKind.Ranged)
        {
            ActivateRangedUtility();
            return;
        }

        string skillId = GetEquippedSwordSpearSkillId(SkillSlotKind.Passive);
        if (IsSwordSpearWeapon(weaponKind))
        {
            if (skillId == "swordspear_passive_2")
            {
                ActivateGuardWall();
                return;
            }

            if (skillId == "swordspear_passive_3")
            {
                ActivateGravityBomb();
                return;
            }
        }

        ActivateDefaultBurst();
    }

    private void ActivateRangedUtility()
    {
        string skillId = GetEquippedRangedSkillId(SkillSlotKind.Passive);
        if (skillId == "ranged_passive_2")
        {
            ActivateDecoy();
            return;
        }

        if (skillId == "ranged_passive_3")
        {
            ActivateMinion();
            return;
        }

        ActivateDefaultBurst();
    }

    private void ActivateDecoy()
    {
        int level = GetRangedSkillLevel("ranged_passive_2");
        nextBurstReady = Time.time + Mathf.Max(0f, burstCooldown);
        _stealthUntil = Mathf.Max(_stealthUntil, Time.time + Mathf.Max(0.1f, decoyStealthDuration));
        UpdateStealthVisual();
        SpawnDecoy(level);
    }

    private void SpawnDecoy(int level)
    {
        float lifeMultiplier = 1f + level * Mathf.Max(0f, decoyLevelLifeBonus);
        float healthMultiplier = 1f + level * Mathf.Max(0f, decoyLevelHealthBonus);

        GameObject decoy = new GameObject("PlayerDecoy");
        decoy.transform.position = transform.position;
        decoy.transform.rotation = transform.rotation;
        decoy.transform.localScale = transform.localScale;

        SpriteRenderer decoyRenderer = decoy.AddComponent<SpriteRenderer>();
        SpriteRenderer sourceRenderer = GetPlayerRenderer();
        decoyRenderer.sprite = sourceRenderer != null ? sourceRenderer.sprite : SimpleSprite.Square;
        Color decoyColor = sourceRenderer != null ? sourceRenderer.color : Color.white;
        decoyColor.a = _normalAlpha;
        decoyRenderer.color = decoyColor;
        decoyRenderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder - 1 : 4;

        BoxCollider2D box = decoy.AddComponent<BoxCollider2D>();
        Rigidbody2D decoyBody = decoy.AddComponent<Rigidbody2D>();
        decoyBody.bodyType = RigidbodyType2D.Kinematic;
        decoyBody.gravityScale = 0f;
        decoyBody.freezeRotation = true;

        Health health = decoy.AddComponent<Health>();
        health.hp = Mathf.Max(1f, decoyHealth * healthMultiplier);
        health.maxHp = Mathf.Max(1f, decoyHealth * healthMultiplier);

        PlayerDecoy playerDecoy = decoy.AddComponent<PlayerDecoy>();
        playerDecoy.life = Mathf.Max(0.1f, decoyLife * lifeMultiplier);
        playerDecoy.ownerPlayerIndex = playerIndex;
    }

    private void ActivateMinion()
    {
        int level = GetRangedSkillLevel("ranged_passive_3");
        int maxMinions = GetMinionMaxAlive(level);
        int activeMinions = CountActiveMinions();
        if (activeMinions >= maxMinions)
        {
            return;
        }

        int spawnCount = Mathf.Min(GetMinionSpawnCount(level), maxMinions - activeMinions);
        if (spawnCount <= 0)
        {
            return;
        }

        nextBurstReady = Time.time + Mathf.Max(0f, burstCooldown);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnMinion(i, spawnCount);
        }
    }

    private void SpawnMinion(int index, int total)
    {
        GameObject minion = new GameObject("PlayerMinion");
        Vector2 spawnOffset = LastAimDirection.sqrMagnitude > 0.001f ? LastAimDirection.normalized : Vector2.down;
        if (total > 1)
        {
            float angle = ((360f / total) * index + 90f) * Mathf.Deg2Rad;
            spawnOffset = (spawnOffset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.65f).normalized;
        }
        minion.transform.position = transform.position + (Vector3)(spawnOffset * 1.1f);
        float minionScale = Mathf.Max(0.1f, minionSizeMultiplier);
        minion.transform.localScale = transform.localScale * minionScale;

        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(minion.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = Vector3.one;

        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = new Color(1f, 0.88f, 0.12f, 1f);
        renderer.sortingOrder = 6;

        PlayerMinionAnimator animator = spriteObj.AddComponent<PlayerMinionAnimator>();
        animator.moveSprites = minionMoveSprites;
        animator.moveSheet = minionMoveTexture;
        animator.resourceSheetName = minionMoveResource;
        animator.fps = Mathf.Max(0.01f, minionMoveFps);
        animator.spriteScale = Mathf.Max(0.01f, minionSpriteScale);

        CircleCollider2D circle = minion.AddComponent<CircleCollider2D>();
        circle.radius = 0.5f;

        Rigidbody2D minionBody = minion.AddComponent<Rigidbody2D>();
        minionBody.gravityScale = 0f;
        minionBody.freezeRotation = true;

        Health health = minion.AddComponent<Health>();
        health.hp = Mathf.Max(1f, minionHealth);
        health.maxHp = Mathf.Max(1f, minionHealth);

        PlayerMinion playerMinion = minion.AddComponent<PlayerMinion>();
        playerMinion.life = Mathf.Max(0.1f, minionLife);
        playerMinion.speed = Mathf.Max(0.1f, minionSpeed);
        playerMinion.touchDamage = Mathf.Max(1, minionTouchDamage);
        playerMinion.touchCooldown = Mathf.Max(0.05f, minionTouchCooldown);
        playerMinion.ownerPlayerIndex = playerIndex;
    }

    private void ActivateDefaultBurst()
    {
        nextBurstReady = Time.time + Mathf.Max(0f, burstCooldown);
        int level = GetCurrentBurstSkillLevel();
        float radiusMultiplier = 1f + level * Mathf.Max(0f, burstLevelRadiusBonus);

        GameObject burst = new GameObject("PlayerBurst");
        burst.transform.position = transform.position;
        burst.transform.localScale = Vector3.one * 0.1f;

        SpriteRenderer renderer = burst.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = new Color(0.5f, 0.9f, 1f, 0.25f);
        renderer.sortingOrder = 11;

        CircleCollider2D circle = burst.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.5f;

        Rigidbody2D burstBody = burst.AddComponent<Rigidbody2D>();
        burstBody.bodyType = RigidbodyType2D.Kinematic;
        burstBody.gravityScale = 0f;

        ExpansionBurst expansion = burst.AddComponent<ExpansionBurst>();
        expansion.duration = Mathf.Max(0.01f, burstDuration);
        expansion.maxRadius = Mathf.Max(0.2f, burstRange * radiusMultiplier);
        expansion.pushMultiplier = Mathf.Max(0f, burstPushMultiplier);
        float scaledBurstDamage = GetWeaponDamage() * burstDamageMultiplier;
        expansion.damage = Mathf.Max(0f, scaledBurstDamage);
    }

    private void ActivateWeaponThrow(WeaponKind weaponKind)
    {
        Vector2 aim = ReadMouseAimDirection();
        if (aim.sqrMagnitude < 0.001f)
        {
            aim = look.sqrMagnitude > 0.001f ? look : Vector2.down;
        }

        aim.Normalize();
        look = aim;
        LastAimDirection = aim;
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);

        bool boomerang = weaponKind == WeaponKind.Sword;
        int level = GetSwordSpearSkillLevel("swordspear_active_2");
        float rangeMultiplier = 1f + level * Mathf.Max(0f, weaponThrowLevelRangeBonus);
        Vector3 thrownSize = GetThrownWeaponSize(weaponKind);
        GameObject weapon = new GameObject(boomerang ? "SwordBoomerang" : "ThrownSpear");
        weapon.transform.position = transform.position + (Vector3)(aim * 0.75f);
        weapon.transform.localScale = thrownSize;
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        weapon.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (boomerang)
        {
            SwordSwingVisualSettings visualSettings = ResolveSwordSwingVisualSettings();
            if (visualSettings.sprite != null)
            {
                CreateSwordWeaponVisual(weapon.transform, visualSettings, 12, true);
            }
            else
            {
                SpriteRenderer renderer = weapon.AddComponent<SpriteRenderer>();
                renderer.sprite = SimpleSprite.Square;
                renderer.color = GetAttackColor(0.95f);
                renderer.sortingOrder = 12;
            }

            _hideCarriedSwordUntil = Mathf.Max(
                _hideCarriedSwordUntil,
                Time.time + Mathf.Max(0.05f, weaponThrowLife * rangeMultiplier));
        }
        else
        {
            SpearVisualSettings visualSettings = ResolveSpearVisualSettings();
            if (visualSettings.sprite != null)
            {
                CreateWeaponVisual(weapon.transform, visualSettings, 12, true);
            }
            else
            {
                SpriteRenderer renderer = weapon.AddComponent<SpriteRenderer>();
                renderer.sprite = SimpleSprite.Square;
                renderer.color = GetAttackColor(0.95f);
                renderer.sortingOrder = 12;
            }

            _hideCarriedSpearUntil = Mathf.Max(
                _hideCarriedSpearUntil,
                Time.time + Mathf.Max(0.05f, weaponThrowLife * rangeMultiplier));
        }

        BoxCollider2D box = weapon.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D weaponBody = weapon.AddComponent<Rigidbody2D>();
        weaponBody.bodyType = RigidbodyType2D.Kinematic;
        weaponBody.gravityScale = 0f;

        PlayerThrownWeapon thrown = weapon.AddComponent<PlayerThrownWeapon>();
        thrown.owner = transform;
        thrown.direction = aim;
        thrown.boomerang = boomerang;
        thrown.speed = Mathf.Max(0.1f, weaponThrowSpeed);
        thrown.returnSpeed = Mathf.Max(0.1f, boomerangReturnSpeed);
        thrown.maxDistance = Mathf.Max(0.5f, weaponThrowRange * rangeMultiplier);
        thrown.life = Mathf.Max(0.05f, weaponThrowLife * rangeMultiplier);
        thrown.damage = Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(1f, weaponThrowDamageMultiplier)));
        thrown.ownerPlayerIndex = playerIndex;
        thrown.weaponColor = GetAttackColor(0.95f);
    }

    private void ActivateFireTrail()
    {
        int level = GetSwordSpearSkillLevel("swordspear_active_3");
        float durationMultiplier = 1f + level * Mathf.Max(0f, fireTrailLevelDurationBonus);
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);
        _fireTrailBoostUntil = Time.time + Mathf.Max(0.1f, fireTrailDuration * durationMultiplier);
        _nextFireTrailAt = 0f;
        MaybeSpawnFireTrail(LastMoveInput.sqrMagnitude > 0.001f ? LastMoveInput : ReadMove());
    }

    private void MaybeSpawnFireTrail(Vector2 move)
    {
        if (Time.time < _nextFireTrailAt)
        {
            return;
        }

        if (move.sqrMagnitude < 0.001f && body != null && body.linearVelocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        _nextFireTrailAt = Time.time + Mathf.Max(0.02f, fireTrailSpawnInterval);
        SpawnFireTrailSegment(transform.position);
    }

    private void SpawnFireTrailSegment(Vector3 position)
    {
        GameObject segment = new GameObject("FireTrailSegment");
        segment.transform.position = new Vector3(position.x, position.y, 0f);
        segment.transform.localScale = Vector3.one * Mathf.Max(0.1f, fireTrailSegmentSize);

        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = new Color(1f, 0.38f, 0.05f, 0.72f);
        renderer.sortingOrder = 5;

        CircleCollider2D circle = segment.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.5f;

        Rigidbody2D segmentBody = segment.AddComponent<Rigidbody2D>();
        segmentBody.bodyType = RigidbodyType2D.Kinematic;
        segmentBody.gravityScale = 0f;

        FireTrailSegment fire = segment.AddComponent<FireTrailSegment>();
        int level = GetSwordSpearSkillLevel("swordspear_active_3");
        float lifeMultiplier = 1f + level * Mathf.Max(0f, fireTrailLevelSegmentLifeBonus);
        fire.life = Mathf.Max(0.05f, fireTrailSegmentLife * lifeMultiplier);
        fire.damage = Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(0.1f, fireTrailDamageMultiplier)));
        fire.ownerPlayerIndex = playerIndex;
        fire.fireColor = new Color(1f, 0.38f, 0.05f, 0.72f);
    }

    private void ActivateGuardWall()
    {
        Vector2 aim = ReadMouseAimDirection();
        if (aim.sqrMagnitude < 0.001f)
        {
            aim = look.sqrMagnitude > 0.001f ? look : Vector2.down;
        }

        aim.Normalize();
        look = aim;
        LastAimDirection = aim;
        nextBurstReady = Time.time + Mathf.Max(0f, burstCooldown);
        int level = GetSwordSpearSkillLevel("swordspear_passive_2");
        float sizeMultiplier = 1f + level * Mathf.Max(0f, wallLevelSizeBonus);

        GameObject wall = new GameObject("PlayerGuardWall");
        wall.transform.position = transform.position + (Vector3)(aim * Mathf.Max(0.5f, wallDistance));
        wall.transform.localScale = new Vector3(
            Mathf.Max(0.4f, wallLength * sizeMultiplier),
            Mathf.Max(0.1f, wallThickness * sizeMultiplier),
            1f);
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + 90f;
        wall.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = new Color(0.48f, 0.78f, 1f, 0.86f);
        renderer.sortingOrder = 7;

        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        Rigidbody2D wallBody = wall.AddComponent<Rigidbody2D>();
        wallBody.bodyType = RigidbodyType2D.Static;
        wallBody.gravityScale = 0f;

        Health health = wall.AddComponent<Health>();
        health.hp = Mathf.Max(1f, wallHealth * sizeMultiplier);
        health.maxHp = Mathf.Max(1f, wallHealth * sizeMultiplier);

        TemporaryWall temporaryWall = wall.AddComponent<TemporaryWall>();
        temporaryWall.life = Mathf.Max(0.2f, wallLife * sizeMultiplier);
        temporaryWall.decayDamagePerSecond = Mathf.Max(0f, wallDecayDamagePerSecond);
        temporaryWall.ownerPlayerIndex = playerIndex;
    }

    private void ActivateGravityBomb()
    {
        Vector2 aim = ReadMouseAimDirection();
        if (aim.sqrMagnitude < 0.001f)
        {
            aim = look.sqrMagnitude > 0.001f ? look : Vector2.down;
        }

        aim.Normalize();
        look = aim;
        LastAimDirection = aim;
        nextBurstReady = Time.time + Mathf.Max(0f, burstCooldown);
        int level = GetSwordSpearSkillLevel("swordspear_passive_3");
        float radiusMultiplier = 1f + level * Mathf.Max(0f, gravityBombLevelRadiusBonus);

        GameObject bomb = new GameObject("GravityBomb");
        bomb.transform.position = transform.position + (Vector3)(aim * 0.7f);

        GravityBombProjectile gravityBomb = bomb.AddComponent<GravityBombProjectile>();
        gravityBomb.direction = aim;
        gravityBomb.distance = Mathf.Max(0.5f, gravityBombDistance);
        gravityBomb.travelTime = Mathf.Max(0.1f, gravityBombTravelTime);
        gravityBomb.arcHeight = Mathf.Max(0f, gravityBombArcHeight);
        gravityBomb.pullRadius = Mathf.Max(0.5f, gravityBombPullRadius * radiusMultiplier);
        gravityBomb.pullDuration = Mathf.Max(0.1f, gravityBombPullDuration);
        gravityBomb.pullStrength = Mathf.Max(0f, gravityBombPullStrength);
        gravityBomb.bombColor = GetAttackColor(1f);
    }

    private void Attack(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride = -1f)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;
        direction.Normalize();
        TriggerAttackVisual(lifeOverride);

        WeaponKind weaponKind = GetWeaponKind();
        if (weaponKind == WeaponKind.Sword)
        {
            SwingSword(direction, damageMultiplier, rangeMultiplier, lifeOverride);
            return;
        }

        if (weaponKind == WeaponKind.Ranged)
        {
            ShootProjectile(direction, damageMultiplier, rangeMultiplier, lifeOverride);
            return;
        }

        StabSpear(direction, damageMultiplier, rangeMultiplier, lifeOverride);
    }

    private void StabSpear(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride)
    {
        float usedScale = Mathf.Max(1f, rangeMultiplier);
        float usedRange = range * usedScale;
        float usedLength = length * usedScale;
        float usedWidth = width * usedScale;
        SpearVisualSettings visualSettings = ResolveSpearVisualSettings();
        float usedLife = lifeOverride > 0f ? lifeOverride : time;
        bool chargedStab = rangeMultiplier > 1.01f && lifeOverride > 0f;
        _hideCarriedSpearUntil = Mathf.Max(_hideCarriedSpearUntil, Time.time + usedLife);

        GameObject slash = new GameObject("PlayerSlash");
        slash.transform.position = transform.position + (Vector3)direction * usedRange;
        slash.transform.localScale = new Vector3(usedLength, usedWidth, 1f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Sprite spearAttackSprite = visualSettings.sprite;
        if (spearAttackSprite != null)
        {
            GameObject visual = chargedStab
                ? CreateSpearChargeVisual(direction, visualSettings, usedLife, usedLength, usedWidth)
                : CreateWeaponVisual(slash.transform, visualSettings, 10, true);
            if (visual != null)
            {
                SpearThrustVisual thrust = visual.AddComponent<SpearThrustVisual>();
                thrust.baseLocalPosition = visual.transform.localPosition;
                thrust.thrustDistance = visualSettings.thrustDistance / Mathf.Max(usedLength, 0.001f);
                thrust.duration = usedLife;
            }
        }
        else
        {
            SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSprite.Square;
            renderer.color = GetAttackColor(0.35f);
            renderer.sortingOrder = 10;
        }

        BoxCollider2D box = slash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D slashBody = slash.AddComponent<Rigidbody2D>();
        slashBody.bodyType = RigidbodyType2D.Kinematic;
        slashBody.gravityScale = 0f;

        HitBox hit = slash.AddComponent<HitBox>();
        hit.hitsPlayer = false;
        hit.life = usedLife;
        hit.damage = Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(1f, damageMultiplier)));
        hit.ownerPlayerIndex = playerIndex;
        hit.visualColor = GetAttackColor(0.35f);
    }

    private void SwingSword(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride)
    {
        GameAudio.PlaySwordCut();
        float usedScale = Mathf.Max(1f, rangeMultiplier);
        float usedRange = range * swordRangeMultiplier * usedScale;
        float usedLength = length * 0.75f * usedScale;
        float usedWidth = width * swordWidthMultiplier * usedScale;
        SwordSwingVisualSettings visualSettings = ResolveSwordSwingVisualSettings();
        float usedLife = (lifeOverride > 0f ? lifeOverride : Mathf.Max(time, 0.16f))
            * Mathf.Max(0.01f, visualSettings.durationMultiplier);
        _hideCarriedSwordUntil = Mathf.Max(_hideCarriedSwordUntil, Time.time + usedLife);

        GameObject slash = new GameObject("PlayerSwordSwing");
        slash.transform.position = transform.position + (Vector3)direction * usedRange;
        slash.transform.localScale = new Vector3(usedLength, usedWidth, 1f);

        Sprite swordSprite = visualSettings.sprite;
        bool chargedSwing = rangeMultiplier > 1.01f;
        if (chargedSwing)
            CreateChargeHitboxVisual(slash.transform, 9);

        if (swordSprite != null)
        {
            SwordSwingVisualSettings usedVisualSettings = chargedSwing
                ? GetChargeSwordVisualSettings(visualSettings, usedRange)
                : visualSettings;
            CreateSwordWeaponVisual(slash.transform, usedVisualSettings, 10, true);
        }
        else
        {
            SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSprite.Square;
            renderer.color = GetAttackColor(0.42f);
            renderer.sortingOrder = 10;
        }

        BoxCollider2D box = slash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D slashBody = slash.AddComponent<Rigidbody2D>();
        slashBody.bodyType = RigidbodyType2D.Kinematic;
        slashBody.gravityScale = 0f;

        HitBox hit = slash.AddComponent<HitBox>();
        hit.hitsPlayer = false;
        hit.life = usedLife;
        hit.damage = Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(1f, damageMultiplier)));
        hit.ownerPlayerIndex = playerIndex;
        hit.visualColor = GetAttackColor(0.42f);

        SwordSwingHitbox swing = slash.AddComponent<SwordSwingHitbox>();
        swing.owner = transform;
        swing.direction = direction;
        swing.distance = usedRange;
        swing.duration = usedLife;
        swing.arcDegrees = Mathf.Max(10f, swordArcDegrees);
    }

    private void ShootProjectile(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride)
    {
        GameAudio.PlayMagicBurst();
        GameObject projectile = new GameObject("PlayerProjectile");
        projectile.transform.position = transform.position + (Vector3)direction * 0.7f;
        projectile.transform.localScale = Vector3.one * Mathf.Max(0.05f, rangedProjectileSize);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = GetAttackColor(1f);
        renderer.sortingOrder = 10;

        BoxCollider2D box = projectile.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        PlayerProjectile playerProjectile = projectile.AddComponent<PlayerProjectile>();
        playerProjectile.direction = direction;
        playerProjectile.speed = Mathf.Max(0.1f, rangedProjectileSpeed) * Mathf.Max(1f, rangeMultiplier);
        playerProjectile.life = lifeOverride > 0f ? lifeOverride : Mathf.Max(0.05f, rangedProjectileLife);
        playerProjectile.damage = Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(1f, damageMultiplier)));
        playerProjectile.ownerPlayerIndex = playerIndex;
        playerProjectile.projectileColor = GetAttackColor(1f);
        HideCarriedRangedOrbUntil(Time.time + Mathf.Max(0f, cooldown));
    }

    private int GetWeaponDamage()
    {
        return _hasNetworkLoadout ? _networkWeaponDamage : PlayerLoadout.WeaponDamage;
    }

    private WeaponKind GetWeaponKind()
    {
        return _hasNetworkLoadout ? _networkWeaponKind : PlayerLoadout.CurrentWeaponKind;
    }

    private string GetEquippedSwordSpearSkillId(SkillSlotKind slotKind)
    {
        if (_hasNetworkLoadout)
        {
            return slotKind == SkillSlotKind.Active
                ? _networkSwordSpearActiveSkillId
                : _networkSwordSpearPassiveSkillId;
        }

        PlayerSkillDefinition skill = PlayerSkillLoadout.GetEquipped(SkillWeaponBranch.SwordSpear, slotKind);
        return skill != null ? skill.id : "";
    }

    private string GetEquippedRangedSkillId(SkillSlotKind slotKind)
    {
        if (_hasNetworkLoadout)
        {
            return slotKind == SkillSlotKind.Active
                ? _networkRangedActiveSkillId
                : _networkRangedPassiveSkillId;
        }

        PlayerSkillDefinition skill = PlayerSkillLoadout.GetEquipped(SkillWeaponBranch.Ranged, slotKind);
        return skill != null ? skill.id : "";
    }

    private int GetSwordSpearSkillLevel(string skillId)
    {
        if (_hasNetworkLoadout || string.IsNullOrWhiteSpace(skillId))
        {
            if (!_hasNetworkLoadout || string.IsNullOrWhiteSpace(skillId))
            {
                return 0;
            }

            if (string.Equals(skillId, _networkSwordSpearActiveSkillId, System.StringComparison.Ordinal))
            {
                return _networkSwordSpearActiveSkillLevel;
            }

            if (string.Equals(skillId, _networkSwordSpearPassiveSkillId, System.StringComparison.Ordinal))
            {
                return _networkSwordSpearPassiveSkillLevel;
            }

            return 0;
        }

        return PlayerSkillLoadout.GetSkillLevel(skillId);
    }

    private int GetCurrentBurstSkillLevel()
    {
        WeaponKind weaponKind = GetWeaponKind();
        if (_hasNetworkLoadout)
        {
            if (weaponKind == WeaponKind.Ranged)
            {
                return _networkRangedPassiveSkillId == "ranged_passive_1"
                    ? _networkRangedPassiveSkillLevel
                    : 0;
            }

            if (IsSwordSpearWeapon(weaponKind))
            {
                return _networkSwordSpearPassiveSkillId == "swordspear_passive_1"
                    ? _networkSwordSpearPassiveSkillLevel
                    : 0;
            }

            return 0;
        }

        if (weaponKind == WeaponKind.Ranged)
        {
            return GetEquippedRangedSkillId(SkillSlotKind.Passive) == "ranged_passive_1"
                ? PlayerSkillLoadout.GetSkillLevel("ranged_passive_1")
                : 0;
        }

        if (IsSwordSpearWeapon(weaponKind))
        {
            return GetEquippedSwordSpearSkillId(SkillSlotKind.Passive) == "swordspear_passive_1"
                ? PlayerSkillLoadout.GetSkillLevel("swordspear_passive_1")
                : 0;
        }

        return 0;
    }

    private int GetRangedSkillLevel(string skillId)
    {
        if (_hasNetworkLoadout || string.IsNullOrWhiteSpace(skillId))
        {
            if (!_hasNetworkLoadout || string.IsNullOrWhiteSpace(skillId))
            {
                return 0;
            }

            if (string.Equals(skillId, _networkRangedActiveSkillId, System.StringComparison.Ordinal))
            {
                return _networkRangedActiveSkillLevel;
            }

            if (string.Equals(skillId, _networkRangedPassiveSkillId, System.StringComparison.Ordinal))
            {
                return _networkRangedPassiveSkillLevel;
            }

            return 0;
        }

        return PlayerSkillLoadout.GetSkillLevel(skillId);
    }

    private int CountActiveMinions()
    {
        int count = 0;
        PlayerMinion[] minions = FindObjectsOfType<PlayerMinion>();
        for (int i = 0; i < minions.Length; i++)
        {
            if (minions[i] != null && minions[i].ownerPlayerIndex == playerIndex)
            {
                count++;
            }
        }
        return count;
    }

    public static int GetMinionSpawnCountForLevel(int level)
    {
        if (level >= 3)
            return 3;
        if (level >= 1)
            return 2;
        return 1;
    }

    public static int GetMinionMaxAliveForLevel(int level)
    {
        if (level >= 3)
            return 4;
        if (level >= 2)
            return 3;
        return 2;
    }

    private static int GetMinionSpawnCount(int level)
    {
        return GetMinionSpawnCountForLevel(level);
    }

    private static int GetMinionMaxAlive(int level)
    {
        return GetMinionMaxAliveForLevel(level);
    }

    private SpriteRenderer GetPlayerRenderer()
    {
        if (_playerRenderer == null)
        {
            _playerRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_playerRenderer != null)
            {
                _normalAlpha = Mathf.Max(_normalAlpha, _playerRenderer.color.a);
            }
        }

        return _playerRenderer;
    }

    private void UpdateStealthVisual()
    {
        SpriteRenderer renderer = GetPlayerRenderer();
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = EnemiesCanSee ? _normalAlpha : Mathf.Clamp01(stealthAlpha);
        renderer.color = color;
    }

    private void UpdateCarriedSwordVisual()
    {
        SwordSwingVisualSettings visualSettings = ResolveSwordSwingVisualSettings();
        Sprite sprite = visualSettings.sprite;
        bool visible = GetWeaponKind() == WeaponKind.Sword
            && sprite != null
            && Time.time >= _hideCarriedSwordUntil;

        if (!visible)
        {
            if (_carriedSwordRenderer != null)
                _carriedSwordRenderer.enabled = false;
            return;
        }

        EnsureCarriedSwordVisual();
        if (_carriedSwordRenderer == null || _carriedSwordTransform == null)
            return;

        if (_appliedCarriedSwordSprite != sprite)
        {
            _carriedSwordRenderer.sprite = sprite;
            _appliedCarriedSwordSprite = sprite;
        }

        _carriedSwordRenderer.enabled = true;
        UpdateCarriedSwordFacing();
        _carriedSwordRenderer.flipX = _carriedSwordFacingLeft;
        SpriteRenderer playerRenderer = GetPlayerRenderer();
        _carriedSwordRenderer.sortingOrder = playerRenderer != null
            ? playerRenderer.sortingOrder + visualSettings.carriedSortingOrderOffset
            : 5;
        float facingSign = _carriedSwordFacingLeft ? -1f : 1f;
        _carriedSwordTransform.localPosition = new Vector3(
            visualSettings.carriedOffset.x * facingSign,
            visualSettings.carriedOffset.y,
            0f);
        _carriedSwordTransform.localRotation = Quaternion.Euler(
            0f,
            0f,
            visualSettings.carriedRotationOffset * facingSign);
        _carriedSwordTransform.localScale = new Vector3(
            Mathf.Approximately(visualSettings.carriedScale.x, 0f) ? 1f : visualSettings.carriedScale.x,
            Mathf.Approximately(visualSettings.carriedScale.y, 0f) ? 1f : visualSettings.carriedScale.y,
            1f);
    }

    private void UpdateCarriedSwordFacing()
    {
        float x = LastMoveInput.x;
        if (x < -0.001f)
            _carriedSwordFacingLeft = true;
        else if (x > 0.001f)
            _carriedSwordFacingLeft = false;
    }

    private void EnsureCarriedSwordVisual()
    {
        if (_carriedSwordRenderer != null && _carriedSwordTransform != null)
            return;

        Transform existing = transform.Find("CarriedSwordVisual");
        GameObject visual = existing != null ? existing.gameObject : new GameObject("CarriedSwordVisual");
        visual.transform.SetParent(transform, false);
        _carriedSwordTransform = visual.transform;
        _carriedSwordRenderer = visual.GetComponent<SpriteRenderer>();
        if (_carriedSwordRenderer == null)
            _carriedSwordRenderer = visual.AddComponent<SpriteRenderer>();
        _carriedSwordRenderer.color = Color.white;
    }

    private void UpdateCarriedSpearVisual()
    {
        SpearVisualSettings visualSettings = ResolveSpearVisualSettings();
        Sprite sprite = visualSettings.sprite;
        bool visible = GetWeaponKind() == WeaponKind.Spear
            && sprite != null
            && Time.time >= _hideCarriedSpearUntil;

        if (!visible)
        {
            if (_carriedSpearRenderer != null)
                _carriedSpearRenderer.enabled = false;
            return;
        }

        EnsureCarriedSpearVisual();
        if (_carriedSpearRenderer == null || _carriedSpearTransform == null)
            return;

        if (_appliedCarriedSpearSprite != sprite)
        {
            _carriedSpearRenderer.sprite = sprite;
            _appliedCarriedSpearSprite = sprite;
        }

        _carriedSpearRenderer.enabled = true;
        UpdateCarriedSpearFacing();
        _carriedSpearRenderer.flipX = _carriedSpearFacingLeft;
        SpriteRenderer playerRenderer = GetPlayerRenderer();
        _carriedSpearRenderer.sortingOrder = playerRenderer != null
            ? playerRenderer.sortingOrder + Mathf.Max(1, visualSettings.carriedSortingOrderOffset)
            : 5;
        float facingSign = _carriedSpearFacingLeft ? -1f : 1f;
        _carriedSpearTransform.localPosition = new Vector3(
            visualSettings.carriedOffset.x * facingSign,
            visualSettings.carriedOffset.y,
            0f);
        _carriedSpearTransform.localRotation = Quaternion.Euler(
            0f,
            0f,
            visualSettings.carriedRotationOffset * facingSign);
        _carriedSpearTransform.localScale = new Vector3(
            Mathf.Approximately(visualSettings.carriedScale.x, 0f) ? 1f : visualSettings.carriedScale.x,
            Mathf.Approximately(visualSettings.carriedScale.y, 0f) ? 1f : visualSettings.carriedScale.y,
            1f);
    }

    private void UpdateCarriedSpearFacing()
    {
        float x = LastMoveInput.x;
        if (x < -0.001f)
            _carriedSpearFacingLeft = true;
        else if (x > 0.001f)
            _carriedSpearFacingLeft = false;
    }

    private void EnsureCarriedSpearVisual()
    {
        if (_carriedSpearRenderer != null && _carriedSpearTransform != null)
            return;

        Transform existing = transform.Find("CarriedSpearVisual");
        GameObject visual = existing != null ? existing.gameObject : new GameObject("CarriedSpearVisual");
        visual.transform.SetParent(transform, false);
        _carriedSpearTransform = visual.transform;
        _carriedSpearRenderer = visual.GetComponent<SpriteRenderer>();
        if (_carriedSpearRenderer == null)
            _carriedSpearRenderer = visual.AddComponent<SpriteRenderer>();
        _carriedSpearRenderer.color = Color.white;
    }

    private void UpdateCarriedRangedOrbVisual()
    {
        bool visible = GetWeaponKind() == WeaponKind.Ranged
            && Time.time >= _hideCarriedRangedOrbUntil;

        if (!visible)
        {
            if (_carriedRangedOrbRenderer != null)
                _carriedRangedOrbRenderer.enabled = false;
            return;
        }

        EnsureCarriedRangedOrbVisual();
        if (_carriedRangedOrbRenderer == null || _carriedRangedOrbTransform == null)
            return;

        _carriedRangedOrbRenderer.enabled = true;
        UpdateCarriedRangedOrbFacing();
        SpriteRenderer playerRenderer = GetPlayerRenderer();
        _carriedRangedOrbRenderer.sortingOrder = playerRenderer != null
            ? playerRenderer.sortingOrder + carriedRangedOrbSortingOrderOffset
            : 7;
        _carriedRangedOrbRenderer.color = GetAttackColor(0.95f);

        float facingSign = _carriedRangedOrbFacingLeft ? -1f : 1f;
        _carriedRangedOrbTransform.localPosition = new Vector3(
            carriedRangedOrbOffset.x * facingSign,
            carriedRangedOrbOffset.y,
            0f);
        _carriedRangedOrbTransform.localRotation = Quaternion.identity;
        _carriedRangedOrbTransform.localScale = new Vector3(
            Mathf.Max(0.01f, rangedProjectileSize) * (Mathf.Approximately(carriedRangedOrbScale.x, 0f) ? 1f : carriedRangedOrbScale.x),
            Mathf.Max(0.01f, rangedProjectileSize) * (Mathf.Approximately(carriedRangedOrbScale.y, 0f) ? 1f : carriedRangedOrbScale.y),
            1f);
    }

    private void UpdateCarriedRangedOrbFacing()
    {
        float x = LastMoveInput.x;
        if (x < -0.001f)
            _carriedRangedOrbFacingLeft = true;
        else if (x > 0.001f)
            _carriedRangedOrbFacingLeft = false;
    }

    private void EnsureCarriedRangedOrbVisual()
    {
        if (_carriedRangedOrbRenderer != null && _carriedRangedOrbTransform != null)
            return;

        Transform existing = transform.Find("CarriedRangedOrbVisual");
        GameObject visual = existing != null ? existing.gameObject : new GameObject("CarriedRangedOrbVisual");
        visual.transform.SetParent(transform, false);
        _carriedRangedOrbTransform = visual.transform;
        _carriedRangedOrbRenderer = visual.GetComponent<SpriteRenderer>();
        if (_carriedRangedOrbRenderer == null)
            _carriedRangedOrbRenderer = visual.AddComponent<SpriteRenderer>();
        _carriedRangedOrbRenderer.sprite = SimpleSprite.Circle;
    }

    private void HideCarriedRangedOrbUntil(float showAt)
    {
        _hideCarriedRangedOrbUntil = Mathf.Max(_hideCarriedRangedOrbUntil, showAt);
        if (_carriedRangedOrbRenderer != null)
            _carriedRangedOrbRenderer.enabled = false;
    }

    private void HideCarriedRangedOrbForAttackCooldown()
    {
        HideCarriedRangedOrbUntil(Time.time + Mathf.Max(0f, cooldown));
    }

    public void OnThrownSwordEnded()
    {
        _hideCarriedSwordUntil = Mathf.Min(_hideCarriedSwordUntil, Time.time);
        UpdateCarriedSwordVisual();
    }

    private Vector2 GetCurrentAim()
    {
        Vector2 aim = ReadMouseAimDirection();
        if (aim.sqrMagnitude < 0.001f)
        {
            aim = look.sqrMagnitude > 0.001f ? look : Vector2.down;
        }

        aim.Normalize();
        look = aim;
        LastAimDirection = aim;
        return aim;
    }

    private static bool IsSwordSpearWeapon(WeaponKind weaponKind)
    {
        return weaponKind == WeaponKind.Spear || weaponKind == WeaponKind.Sword;
    }

    private Vector3 GetThrownWeaponSize(WeaponKind weaponKind)
    {
        if (weaponKind == WeaponKind.Sword)
        {
            return new Vector3(
                Mathf.Max(0.4f, length * 0.75f),
                Mathf.Max(0.2f, width * swordWidthMultiplier),
                1f);
        }

        return new Vector3(
            Mathf.Max(0.4f, length),
            Mathf.Max(0.2f, width),
            1f);
    }

    private Sprite ResolveSwordSwingSprite()
    {
        if (swordSwingSprite != null)
            return swordSwingSprite;

        if (swordSwingTexture == null)
            return null;

        if (_cachedSwordSwingTextureSprite == null || _cachedSwordSwingTexture != swordSwingTexture)
        {
            _cachedSwordSwingTexture = swordSwingTexture;
            _cachedSwordSwingTextureSprite = Sprite.Create(
                swordSwingTexture,
                new Rect(0f, 0f, swordSwingTexture.width, swordSwingTexture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1, swordSwingTexture.width));
        }

        return _cachedSwordSwingTextureSprite;
    }

    private Sprite ResolveSpearSprite()
    {
        if (spearSprite != null)
            return spearSprite;

        if (spearTexture == null)
            return null;

        if (_cachedSpearTextureSprite == null || _cachedSpearTexture != spearTexture)
        {
            _cachedSpearTexture = spearTexture;
            _cachedSpearTextureSprite = Sprite.Create(
                spearTexture,
                new Rect(0f, 0f, spearTexture.width, spearTexture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1, spearTexture.width));
        }

        return _cachedSpearTextureSprite;
    }

    private GameObject CreateSwordWeaponVisual(
        Transform parent,
        SwordSwingVisualSettings visualSettings,
        int sortingOrder,
        bool compensateParentScale)
    {
        return CreateWeaponVisual(
            parent,
            visualSettings.sprite,
            visualSettings.offset,
            visualSettings.scale,
            visualSettings.rotationOffset,
            sortingOrder,
            compensateParentScale);
    }

    private GameObject CreateWeaponVisual(
        Transform parent,
        SpearVisualSettings visualSettings,
        int sortingOrder,
        bool compensateParentScale)
    {
        return CreateWeaponVisual(
            parent,
            visualSettings.sprite,
            visualSettings.offset,
            visualSettings.scale,
            visualSettings.rotationOffset,
            sortingOrder,
            compensateParentScale);
    }

    private GameObject CreateSpearChargeVisual(
        Vector2 direction,
        SpearVisualSettings visualSettings,
        float usedLife,
        float usedLength,
        float usedWidth)
    {
        if (visualSettings.sprite == null)
            return null;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;
        direction.Normalize();

        GameObject root = new GameObject("SpearChargeVisual");
        root.transform.position = transform.position;
        root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        root.transform.localScale = new Vector3(usedLength, usedWidth, 1f);

        GameObject visual = CreateWeaponVisual(root.transform, visualSettings, 10, true);
        StartCoroutine(FollowSpearChargeVisual(root.transform, direction, usedLife));
        Destroy(root, Mathf.Max(0.01f, usedLife));
        return visual;
    }

    private IEnumerator FollowSpearChargeVisual(Transform root, Vector2 direction, float duration)
    {
        float endAt = Time.time + Mathf.Max(0.01f, duration);
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        while (root != null && Time.time < endAt)
        {
            root.position = transform.position;
            root.rotation = rotation;
            yield return null;
        }
    }

    private GameObject CreateWeaponVisual(
        Transform parent,
        Sprite sprite,
        Vector2 offset,
        Vector2 scale,
        float rotationOffset,
        int sortingOrder,
        bool compensateParentScale)
    {
        if (parent == null || sprite == null)
            return null;

        GameObject visual = new GameObject("WeaponVisual");
        visual.transform.SetParent(parent, false);

        Vector3 parentScale = parent.localScale;
        float parentScaleX = Mathf.Approximately(parentScale.x, 0f) ? 1f : parentScale.x;
        float parentScaleY = Mathf.Approximately(parentScale.y, 0f) ? 1f : parentScale.y;
        float visualScaleX = Mathf.Approximately(scale.x, 0f) ? 1f : scale.x;
        float visualScaleY = Mathf.Approximately(scale.y, 0f) ? 1f : scale.y;

        visual.transform.localPosition = compensateParentScale
            ? new Vector3(offset.x / parentScaleX, offset.y / parentScaleY, 0f)
            : new Vector3(offset.x, offset.y, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, rotationOffset);
        visual.transform.localScale = compensateParentScale
            ? new Vector3(visualScaleX / parentScaleX, visualScaleY / parentScaleY, 1f)
            : new Vector3(visualScaleX, visualScaleY, 1f);

        SpriteRenderer visualRenderer = visual.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = sprite;
        visualRenderer.color = Color.white;
        visualRenderer.sortingOrder = sortingOrder;
        return visual;
    }

    private SwordSwingVisualSettings GetChargeSwordVisualSettings(SwordSwingVisualSettings visualSettings, float usedRange)
    {
        float normalRange = range * swordRangeMultiplier;
        float extraRange = Mathf.Max(0f, usedRange - normalRange);
        visualSettings.offset = new Vector2(
            visualSettings.offset.x - extraRange,
            visualSettings.offset.y);
        return visualSettings;
    }

    private void CreateChargeHitboxVisual(Transform parent, int sortingOrder)
    {
        if (parent == null)
            return;

        CreateChargeGlowPiece(parent, "ChargeFaintLight", Vector2.zero, Vector2.one, new Color(0.55f, 0.90f, 1f, 0.18f), sortingOrder);
        CreateChargeGlowPiece(parent, "ChargeSoftCenter", Vector2.zero, new Vector2(0.72f, 0.48f), new Color(0.85f, 1f, 1f, 0.10f), sortingOrder + 1);
    }

    private void CreateChargeGlowPiece(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 scale,
        Color color,
        int sortingOrder)
    {
        GameObject piece = new GameObject(name);
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = new Vector3(position.x, position.y, 0f);
        piece.transform.localRotation = Quaternion.identity;
        piece.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = ResolveChargeHitboxGlowSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite ResolveChargeHitboxGlowSprite()
    {
        if (_chargeHitboxGlowSprite != null)
            return _chargeHitboxGlowSprite;

        const int size = 64;
        _chargeHitboxGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _chargeHitboxGlowTexture.name = "GeneratedChargeHitboxGlow";
        _chargeHitboxGlowTexture.filterMode = FilterMode.Bilinear;
        _chargeHitboxGlowTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = ((float)x + 0.5f) / size;
                float v = ((float)y + 0.5f) / size;
                float dx = Mathf.Abs(u - 0.5f) * 2f;
                float dy = Mathf.Abs(v - 0.5f) * 2f;
                float edgeDistance = Mathf.Max(dx, dy);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - edgeDistance));
                alpha *= alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _chargeHitboxGlowTexture.SetPixels(pixels);
        _chargeHitboxGlowTexture.Apply();
        _chargeHitboxGlowSprite = Sprite.Create(
            _chargeHitboxGlowTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        return _chargeHitboxGlowSprite;
    }

    private SwordSwingVisualSettings ResolveSwordSwingVisualSettings()
    {
        if (WeaponVisualDatabase.TryGetSwordVisualGlobal(GetWeaponItemId(), out WeaponVisualEntry visual))
        {
            Sprite sprite = visual.ResolveSwordSwingSprite();
            if (sprite != null)
            {
                return new SwordSwingVisualSettings
                {
                    sprite = sprite,
                    offset = visual.swordSwingVisualOffset,
                    scale = visual.swordSwingVisualScale,
                    rotationOffset = visual.swordSwingVisualRotationOffset,
                    durationMultiplier = visual.swordSwingDurationMultiplier,
                    carriedOffset = visual.carriedSwordVisualOffset,
                    carriedScale = visual.carriedSwordVisualScale,
                    carriedRotationOffset = visual.carriedSwordVisualRotationOffset,
                    carriedSortingOrderOffset = visual.carriedSwordSortingOrderOffset
                };
            }
        }

        return new SwordSwingVisualSettings
        {
            sprite = ResolveSwordSwingSprite(),
            offset = swordSwingVisualOffset,
            scale = swordSwingVisualScale,
            rotationOffset = swordSwingVisualRotationOffset,
            durationMultiplier = swordSwingDurationMultiplier,
            carriedOffset = carriedSwordVisualOffset,
            carriedScale = carriedSwordVisualScale,
            carriedRotationOffset = carriedSwordVisualRotationOffset,
            carriedSortingOrderOffset = carriedSwordSortingOrderOffset
        };
    }

    private SpearVisualSettings ResolveSpearVisualSettings()
    {
        if (WeaponVisualDatabase.TryGetSpearVisualGlobal(GetWeaponItemId(), out WeaponVisualEntry visual))
        {
            Sprite sprite = visual.ResolveSpearSprite();
            if (sprite != null)
            {
                return new SpearVisualSettings
                {
                    sprite = sprite,
                    offset = visual.spearVisualOffset,
                    scale = visual.spearVisualScale,
                    rotationOffset = visual.spearVisualRotationOffset,
                    thrustDistance = visual.spearThrustDistance,
                    carriedOffset = visual.carriedSpearVisualOffset,
                    carriedScale = visual.carriedSpearVisualScale,
                    carriedRotationOffset = visual.carriedSpearVisualRotationOffset,
                    carriedSortingOrderOffset = visual.carriedSpearSortingOrderOffset
                };
            }
        }

        return new SpearVisualSettings
        {
            sprite = ResolveSpearSprite(),
            offset = spearVisualOffset,
            scale = spearVisualScale,
            rotationOffset = spearVisualRotationOffset,
            thrustDistance = spearThrustDistance,
            carriedOffset = carriedSpearVisualOffset,
            carriedScale = carriedSpearVisualScale,
            carriedRotationOffset = carriedSpearVisualRotationOffset,
            carriedSortingOrderOffset = carriedSpearSortingOrderOffset
        };
    }

    private int GetWeaponItemId()
    {
        return _hasNetworkLoadout ? _networkWeaponItemId : PlayerLoadout.EquippedWeaponItemId;
    }

    private struct SwordSwingVisualSettings
    {
        public Sprite sprite;
        public Vector2 offset;
        public Vector2 scale;
        public float rotationOffset;
        public float durationMultiplier;
        public Vector2 carriedOffset;
        public Vector2 carriedScale;
        public float carriedRotationOffset;
        public int carriedSortingOrderOffset;
    }

    private struct SpearVisualSettings
    {
        public Sprite sprite;
        public Vector2 offset;
        public Vector2 scale;
        public float rotationOffset;
        public float thrustDistance;
        public Vector2 carriedOffset;
        public Vector2 carriedScale;
        public float carriedRotationOffset;
        public int carriedSortingOrderOffset;
    }

    private Color GetAttackColor(float alpha)
    {
        Color color = _hasNetworkLoadout ? _networkWeaponColor : PlayerLoadout.WeaponColor;
        color.a = alpha;
        return color;
    }

    private void ApplyNetworkSkin()
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer == null) return;

        PlayerSkinVisuals.Apply(renderer, _networkSkinId, _networkSkinColor, renderer.sharedMaterial);
        PlayerAnimator animator = renderer.GetComponent<PlayerAnimator>();
        if (animator != null)
            animator.RefreshSkin();
    }

    private void TriggerAttackVisual(float lifeOverride)
    {
        SpriteRenderer renderer = GetPlayerRenderer();
        if (renderer == null)
            return;

        PlayerAnimator animator = renderer.GetComponent<PlayerAnimator>();
        if (animator == null)
            return;

        float visualDuration = lifeOverride > 0f ? lifeOverride : time;
        animator.TriggerAttack(Mathf.Clamp(visualDuration, 0.08f, 0.18f));
    }

    private float GetSpeedBoostMultiplier()
    {
        return _hasNetworkLoadout ? _networkSpeedBoostMultiplier : PlayerLoadout.SpeedBoostMultiplier;
    }
}

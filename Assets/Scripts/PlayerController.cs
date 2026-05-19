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
    public float swordArcDegrees = 110f;
    public float rangedProjectileSpeed = 12f;
    public float rangedProjectileLife = 2.2f;
    public float rangedProjectileSize = 0.35f;
    [Header("Charge Ability (E)")]
    public float chargeDuration = 0.3f;
    public float chargeCooldown = 5f;
    public float chargeSpeedMultiplier = 4f;
    public float chargeDamageMultiplier = 2f;
    public float chargeRangeMultiplier = 2f;
    [Header("Burst Ability (Q)")]
    public float burstRange = 6f;
    public float burstDuration = 0.2f;
    public float burstDamageMultiplier = 0.35f;
    public float burstPushMultiplier = 4f;
    public float burstCooldown = 6f;

    private Rigidbody2D body;
    private Vector2 look = Vector2.down;
    private Vector2 chargeDirection = Vector2.down;
    private float nextAttack;
    private float chargeUntil;
    private float nextChargeReady;
    private float nextBurstReady;
    private float nextConsumableReady;
    private float _speedBoostUntil;
    private bool _useExternalInput;
    private Vector2 _externalMove;
    private Vector2 _externalAim = Vector2.down;
    private bool _externalAttackDown;
    private bool _externalChargeDown;
    private bool _externalBurstDown;
    private bool _externalConsumableDown;
    private bool _hasNetworkLoadout;
    private int _networkWeaponDamage = 1;
    private WeaponKind _networkWeaponKind = WeaponKind.Spear;
    private Color _networkWeaponColor = Color.white;
    private int _networkSkinId;
    private string _networkSkinColor = "#4DBFFF";
    private int _networkConsumableQuantity;
    private float _networkConsumableHealAmount;
    private float _networkConsumableCooldown;
    private bool _networkConsumableIsSpeedBoost;
    private float _networkSpeedBoostDuration = 3f;
    private float _networkSpeedBoostMultiplier = 2f;

    public float ConsumableCooldownRemaining => Mathf.Max(0f, nextConsumableReady - Time.time);
    public Vector2 LastMoveInput { get; private set; }
    public Vector2 LastAimDirection { get; private set; } = Vector2.down;
    public int NetworkAttackSequence { get; private set; }
    public int NetworkChargeSequence { get; private set; }
    public int NetworkBurstSequence { get; private set; }
    public int NetworkConsumableSequence { get; private set; }
    public int NetworkSkinId => _hasNetworkLoadout ? _networkSkinId : PlayerLoadout.EquippedSkinId;
    public string NetworkSkinColor => _hasNetworkLoadout ? _networkSkinColor : PlayerSkinVisuals.GetEquippedSkinColorHex();

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
            float boost = Mathf.Max(1f, chargeSpeedMultiplier);
            body.linearVelocity = chargeDirection * (speed * boost);
        }
        else
        {
            Vector2 move = ReadMove();
            LastMoveInput = move;
            float activeSpeed = Time.time < _speedBoostUntil
                ? speed * GetSpeedBoostMultiplier()
                : speed;
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
        float speedBoostMultiplier)
    {
        _hasNetworkLoadout = true;
        _networkWeaponDamage = Mathf.Max(1, weaponDamage);
        _networkWeaponKind = PlayerLoadout.ParseWeaponKind(weaponType);
        _networkWeaponColor = PlayerLoadout.ParseWeaponColor(weaponColor, Color.white);
        _networkSkinId = Mathf.Max(0, skinId);
        _networkSkinColor = string.IsNullOrWhiteSpace(skinColor) ? "#4DBFFF" : skinColor;
        ApplyNetworkSkin();
        _networkConsumableQuantity = Mathf.Max(0, consumableQuantity);
        _networkConsumableHealAmount = Mathf.Max(0f, consumableHealAmount);
        _networkConsumableCooldown = Mathf.Max(0f, consumableCooldown);
        _networkConsumableIsSpeedBoost = consumableIsSpeedBoost;
        _networkSpeedBoostDuration = Mathf.Max(0f, speedBoostDuration);
        _networkSpeedBoostMultiplier = Mathf.Max(1f, speedBoostMultiplier);

        Health health = GetComponent<Health>();
        if (health != null && maxHp > 0f)
        {
            bool wasFull = health.maxHp <= 0f || health.hp >= health.maxHp - 0.01f;
            health.maxHp = maxHp;
            health.hp = wasFull ? health.maxHp : Mathf.Min(health.hp, health.maxHp);
        }
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
        Vector2 aim = ReadMouseAimDirection();
        look = aim;
        chargeDirection = aim;

        chargeUntil = Time.time + Mathf.Max(0f, chargeDuration);
        nextChargeReady = Time.time + Mathf.Max(0f, chargeCooldown);

        Attack(
            aim,
            Mathf.Max(1f, chargeDamageMultiplier),
            Mathf.Max(1f, chargeRangeMultiplier),
            Mathf.Max(0.01f, chargeDuration));
    }

    private void ActivateBurst()
    {
        nextBurstReady = Time.time + Mathf.Max(0f, burstCooldown);

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
        expansion.maxRadius = Mathf.Max(0.2f, burstRange);
        expansion.pushMultiplier = Mathf.Max(0f, burstPushMultiplier);
        float scaledBurstDamage = GetWeaponDamage() * burstDamageMultiplier;
        expansion.damage = Mathf.Max(0f, scaledBurstDamage);
    }

    private void Attack(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride = -1f)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;
        direction.Normalize();

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

        GameObject slash = new GameObject("PlayerSlash");
        slash.transform.position = transform.position + (Vector3)direction * usedRange;
        slash.transform.localScale = new Vector3(usedLength, usedWidth, 1f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = GetAttackColor(0.35f);
        renderer.sortingOrder = 10;

        BoxCollider2D box = slash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D slashBody = slash.AddComponent<Rigidbody2D>();
        slashBody.bodyType = RigidbodyType2D.Kinematic;
        slashBody.gravityScale = 0f;

        HitBox hit = slash.AddComponent<HitBox>();
        hit.hitsPlayer = false;
        hit.life = lifeOverride > 0f ? lifeOverride : time;
        hit.damage = Mathf.Max(1, Mathf.RoundToInt(GetWeaponDamage() * Mathf.Max(1f, damageMultiplier)));
        hit.ownerPlayerIndex = playerIndex;
        hit.visualColor = GetAttackColor(0.35f);
    }

    private void SwingSword(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride)
    {
        float usedScale = Mathf.Max(1f, rangeMultiplier);
        float usedRange = range * swordRangeMultiplier * usedScale;
        float usedLength = length * 0.75f * usedScale;
        float usedWidth = width * swordWidthMultiplier * usedScale;
        float usedLife = lifeOverride > 0f ? lifeOverride : Mathf.Max(time, 0.16f);

        GameObject slash = new GameObject("PlayerSwordSwing");
        slash.transform.position = transform.position + (Vector3)direction * usedRange;
        slash.transform.localScale = new Vector3(usedLength, usedWidth, 1f);

        SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = GetAttackColor(0.42f);
        renderer.sortingOrder = 10;

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
        GameObject projectile = new GameObject("PlayerProjectile");
        projectile.transform.position = transform.position + (Vector3)direction * 0.7f;
        projectile.transform.localScale = Vector3.one * Mathf.Max(0.05f, rangedProjectileSize);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
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
    }

    private int GetWeaponDamage()
    {
        return _hasNetworkLoadout ? _networkWeaponDamage : PlayerLoadout.WeaponDamage;
    }

    private WeaponKind GetWeaponKind()
    {
        return _hasNetworkLoadout ? _networkWeaponKind : PlayerLoadout.CurrentWeaponKind;
    }

    private Color GetAttackColor(float alpha)
    {
        Color color = _hasNetworkLoadout ? _networkWeaponColor : PlayerLoadout.WeaponColor;
        color.a = alpha;
        return color;
    }

    private void ApplyNetworkSkin()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        PlayerSkinVisuals.Apply(renderer, _networkSkinId, _networkSkinColor, renderer.sharedMaterial);
    }

    private float GetSpeedBoostMultiplier()
    {
        return _hasNetworkLoadout ? _networkSpeedBoostMultiplier : PlayerLoadout.SpeedBoostMultiplier;
    }
}

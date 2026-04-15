using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    public static PlayerController main;

    public float speed = 5f;
    public float cooldown = 0.35f;
    public int damage = 1;
    public float range = 3f;
    public float length = 4.5f;
    public float width = 0.5f;
    public float time = 0.12f;
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
        main = this;

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
    }

    private void OnDestroy()
    {
        if (main == this)
        {
            main = null;
        }
    }

    private void Update()
    {
        if (ReadChargeDown() && Time.time >= nextChargeReady)
        {
            ActivateCharge();
        }

        if (ReadBurstDown() && Time.time >= nextBurstReady)
        {
            ActivateBurst();
        }

        if (Time.time < chargeUntil)
        {
            float boost = Mathf.Max(1f, chargeSpeedMultiplier);
            body.linearVelocity = chargeDirection * (speed * boost);
        }
        else
        {
            Vector2 move = ReadMove();
            body.linearVelocity = move * speed;
        }

        if (ReadAttackDown() && Time.time >= nextAttack)
        {
            look = ReadMouseAimDirection();
            Attack(look, 1f, 1f);
            nextAttack = Time.time + cooldown;
        }
    }

    private Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > move.sqrMagnitude)
            {
                move = stick;
            }
        }

        return Vector2.ClampMagnitude(move, 1f);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
    }

    private bool ReadAttackDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool ReadChargeDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private bool ReadBurstDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    private Vector2 ReadMouseAimDirection()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return look;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return look;
        }

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
        float scaledBurstDamage = damage * burstDamageMultiplier;
        expansion.damage = Mathf.Max(0f, scaledBurstDamage);
    }

    private void Attack(Vector2 direction, float damageMultiplier, float rangeMultiplier, float lifeOverride = -1f)
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
        renderer.color = new Color(1f, 1f, 1f, 0.35f);
        renderer.sortingOrder = 10;

        BoxCollider2D box = slash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D slashBody = slash.AddComponent<Rigidbody2D>();
        slashBody.bodyType = RigidbodyType2D.Kinematic;
        slashBody.gravityScale = 0f;

        HitBox hit = slash.AddComponent<HitBox>();
        hit.hitsPlayer = false;
        hit.life = lifeOverride > 0f ? lifeOverride : time;
        hit.damage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Max(1f, damageMultiplier)));
    }
}

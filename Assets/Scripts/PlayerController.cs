using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    public static PlayerController main;

    public float speed = 5f;
    public float cooldown = 0.35f;
    public float range = 0.8f;
    public float length = 1.2f;
    public float width = 0.45f;
    public float time = 0.12f;

    private Rigidbody2D body;
    private Vector2 look = Vector2.down;
    private float nextAttack;

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
        Vector2 move = ReadMove();
        body.linearVelocity = move * speed;

        if (move.sqrMagnitude > 0f)
        {
            look = move;
        }

        if (ReadAttackDown() && Time.time >= nextAttack)
        {
            Attack();
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
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
    }

    private void Attack()
    {
        GameObject slash = new GameObject("PlayerSlash");
        slash.transform.position = transform.position + (Vector3)look * range;
        slash.transform.localScale = new Vector3(length, width, 1f);
        float angle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = new Color(1f, 1f, 1f, 0.35f);
        renderer.sortingOrder = 10;

        BoxCollider2D box = slash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        HitBox hit = slash.AddComponent<HitBox>();
        hit.hitsPlayer = false;
        hit.life = time;
    }
}

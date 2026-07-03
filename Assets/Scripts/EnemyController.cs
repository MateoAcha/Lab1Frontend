using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float speed = 2.5f;
    public float cooldown = 0.8f;
    public int touchDamage = 1;
    public float recoilSpeed = 10f;
    public float recoilTime = 0.1f;
    [Header("Obstacle Avoidance")]
    public float avoidProbeRadius = 0.5f;
    public float avoidProbeDistance = 2f;
    public float avoidTurnAngle = 50f;

    private Rigidbody2D body;
    private Transform player;
    private Vector2 look = Vector2.down;
    private float nextTouchDamageAt;
    private float recoilUntil;

    private void OnEnable()
    {
        OnlineNetworkRegistry.Register(this);
    }

    private void OnDisable()
    {
        OnlineNetworkRegistry.Unregister(this);
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

        Health health = GetComponent<Health>();
        if (health == null)
            health = gameObject.AddComponent<Health>();
        health.hp = 30f;
    }

    private void Start()
    {
        player = MultiplayerState.GetNearestPlayer(transform.position);
    }

    private void Update()
    {
        player = MultiplayerState.GetNearestPlayer(transform.position);

        if (player == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < recoilUntil)
        {
            return;
        }

        Vector2 to = player.position - transform.position;
        if (to.sqrMagnitude > 0.001f)
        {
            look = to.normalized;
        }

        Vector2 move = EnemyObstacleAvoidance.GetSteeredDirection(
            transform,
            body,
            look,
            avoidProbeRadius,
            avoidProbeDistance,
            avoidTurnAngle);
        body.linearVelocity = move * speed;
    }

    public void OnHit(Vector2 hitPoint, float pushMultiplier = 1f)
    {
        Vector2 push = ((Vector2)transform.position - hitPoint).normalized;
        if (push.sqrMagnitude < 0.001f)
        {
            push = Vector2.up;
        }

        body.linearVelocity = push * (recoilSpeed * Mathf.Max(0f, pushMultiplier));
        recoilUntil = Time.time + recoilTime;
        float effectScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        BloodBurst.SpawnNetworked(transform.position, hitPoint, effectScale);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (Time.time < nextTouchDamageAt)
        {
            return;
        }

        TemporaryWall wall = other.GetComponent<TemporaryWall>();
        if (wall != null)
        {
            wall.Hit(touchDamage);
            nextTouchDamageAt = Time.time + Mathf.Max(0.05f, cooldown);
            return;
        }

        if (TryDamageAllyTarget(other))
        {
            nextTouchDamageAt = Time.time + Mathf.Max(0.05f, cooldown);
            return;
        }

        if (other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health == null)
        {
            return;
        }

        health.Hit(touchDamage);
        nextTouchDamageAt = Time.time + Mathf.Max(0.05f, cooldown);
    }

    private bool TryDamageAllyTarget(Collider2D other)
    {
        if (other.GetComponent<PlayerDecoy>() == null && other.GetComponent<PlayerMinion>() == null)
        {
            return false;
        }

        Health health = other.GetComponent<Health>();
        if (health == null)
        {
            return false;
        }

        health.Hit(touchDamage);
        return true;
    }

}

using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float speed = 2.5f;
    public float stopDistance = 0.9f;
    public float cooldown = 0.8f;
    public float range = 0.7f;
    public float length = 0.8f;
    public float width = 0.4f;
    public float time = 0.15f;
    public float recoilSpeed = 10f;
    public float recoilTime = 0.1f;
    [Header("Obstacle Avoidance")]
    public float avoidProbeRadius = 0.5f;
    public float avoidProbeDistance = 2f;
    public float avoidTurnAngle = 50f;

    private Rigidbody2D body;
    private Transform player;
    private Vector2 look = Vector2.down;
    private float nextAttack;
    private float recoilUntil;

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
    }

    private void Start()
    {
        if (PlayerController.main != null)
        {
            player = PlayerController.main.transform;
        }
    }

    private void Update()
    {
        if (player == null && PlayerController.main != null)
        {
            player = PlayerController.main.transform;
        }

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
        float distance = to.magnitude;

        if (distance > stopDistance)
        {
            look = to.normalized;
            Vector2 move = EnemyObstacleAvoidance.GetSteeredDirection(
                transform,
                body,
                look,
                avoidProbeRadius,
                avoidProbeDistance,
                avoidTurnAngle);
            body.linearVelocity = move * speed;
            return;
        }

        body.linearVelocity = Vector2.zero;
        look = to.sqrMagnitude > 0f ? to.normalized : look;

        if (Time.time >= nextAttack)
        {
            Attack();
            nextAttack = Time.time + cooldown;
        }
    }

    private void Attack()
    {
        GameObject slash = new GameObject("EnemySlash");
        slash.transform.position = transform.position + (Vector3)look * range;
        slash.transform.localScale = new Vector3(length, width, 1f);
        float angle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = new Color(1f, 0.25f, 0.25f, 0.35f);
        renderer.sortingOrder = 9;

        BoxCollider2D box = slash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        HitBox hit = slash.AddComponent<HitBox>();
        hit.hitsPlayer = true;
        hit.life = time;
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
        SpawnSparkles();
    }

    private void SpawnSparkles()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject sparkle = new GameObject("Sparkle");
            sparkle.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.2f);
            sparkle.transform.localScale = new Vector3(0.12f, 0.12f, 1f);

            SpriteRenderer renderer = sparkle.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSprite.Square;
            renderer.color = new Color(1f, 1f, 0.8f, 1f);
            renderer.sortingOrder = 30;

            SparkleFx fx = sparkle.AddComponent<SparkleFx>();
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector2.up;
            }
            fx.velocity = dir * Random.Range(2f, 4f);
            fx.life = 0.2f;
        }
    }

}

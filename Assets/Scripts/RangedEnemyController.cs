using UnityEngine;

public class RangedEnemyController : MonoBehaviour
{
    public float speed = 1.6f;
    public float minDistance = 2.5f;
    public float maxDistance = 4.5f;
    public float attackDistance = 6f;
    public float cooldown = 1.5f;
    public int touchDamage = 1;
    public float projectileSpeed = 3f;
    public float projectileLife = 4f;
    public float recoilSpeed = 10f;
    public float recoilTime = 0.1f;
    public Material projectileMaterial;
    [Header("Projectile Visual")]
    public Sprite[] projectileSprites;
    public Texture2D projectileTexture;
    [Min(1)] public int projectileFrameCount = 3;
    [Min(0.01f)] public float projectileSize = 0.25f;
    [Min(0.01f)] public float projectileFps = 10f;
    [Header("Obstacle Avoidance")]
    public float avoidProbeRadius = 0.5f;
    public float avoidProbeDistance = 2f;
    public float avoidTurnAngle = 50f;

    public float LastShotTime { get; private set; }
    public Vector2 FacingDirection => look;

    private Rigidbody2D body;
    private Transform player;
    private Vector2 look = Vector2.down;
    private float nextAttack;
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
        health.hp = 15f;
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
        float distance = to.magnitude;

        if (to.sqrMagnitude > 0.001f)
        {
            look = to.normalized;
        }

        if (distance > maxDistance)
        {
            Vector2 move = EnemyObstacleAvoidance.GetSteeredDirection(
                transform,
                body,
                look,
                avoidProbeRadius,
                avoidProbeDistance,
                avoidTurnAngle);
            body.linearVelocity = move * speed;
        }
        else if (distance < minDistance)
        {
            Vector2 move = EnemyObstacleAvoidance.GetSteeredDirection(
                transform,
                body,
                -look,
                avoidProbeRadius,
                avoidProbeDistance,
                avoidTurnAngle);
            body.linearVelocity = move * speed;
        }
        else
        {
            body.linearVelocity = Vector2.zero;
        }

        if (distance <= attackDistance && Time.time >= nextAttack)
        {
            LastShotTime = Time.time;
            Shoot();
            nextAttack = Time.time + cooldown;
        }
    }

    private void Shoot()
    {
        GameAudio.PlayRangedEnemyShot();
        GameObject projectile = new GameObject("EnemyProjectile");
        projectile.transform.position = transform.position + (Vector3)look * 0.7f;
        float visualSize = Mathf.Max(0.01f, projectileSize);
        projectile.transform.localScale = new Vector3(visualSize, visualSize, 1f);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        Sprite[] frames = EnemyProjectileAnimator.ResolveFrames(projectileSprites, projectileTexture, projectileFrameCount);
        bool hasCustomSprite = frames != null && frames.Length > 0 && frames[0] != null;
        renderer.sprite = hasCustomSprite ? frames[0] : SimpleSprite.Square;
        renderer.color = hasCustomSprite ? Color.white : new Color(1f, 0.55f, 0.15f, 1f);
        renderer.sortingOrder = 9;
        if (projectileMaterial != null)
        {
            renderer.sharedMaterial = projectileMaterial;
            renderer.color = Color.white;
        }
        if (hasCustomSprite && frames.Length > 1)
        {
            EnemyProjectileAnimator animator = projectile.AddComponent<EnemyProjectileAnimator>();
            animator.frames = frames;
            animator.fps = projectileFps;
        }

        BoxCollider2D box = projectile.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        EnemyProjectile enemyProjectile = projectile.AddComponent<EnemyProjectile>();
        enemyProjectile.direction = look;
        enemyProjectile.speed = projectileSpeed;
        enemyProjectile.life = projectileLife;
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
            wall.Hit(EnemyDamage.Amount(touchDamage));
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

        health.Hit(EnemyDamage.Amount(touchDamage));
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

        health.Hit(EnemyDamage.Amount(touchDamage));
        return true;
    }

}

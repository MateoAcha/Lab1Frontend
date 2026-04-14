using UnityEngine;

public class RangedEnemyController : MonoBehaviour
{
    public float speed = 1.6f;
    public float minDistance = 2.5f;
    public float maxDistance = 4.5f;
    public float attackDistance = 6f;
    public float cooldown = 1.5f;
    public float projectileSpeed = 3f;
    public float projectileLife = 4f;
    public float recoilSpeed = 10f;
    public float recoilTime = 0.1f;

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

        if (to.sqrMagnitude > 0.001f)
        {
            look = to.normalized;
        }

        if (distance > maxDistance)
        {
            body.linearVelocity = look * speed;
        }
        else if (distance < minDistance)
        {
            body.linearVelocity = -look * speed;
        }
        else
        {
            body.linearVelocity = Vector2.zero;
        }

        if (distance <= attackDistance && Time.time >= nextAttack)
        {
            Shoot();
            nextAttack = Time.time + cooldown;
        }
    }

    private void Shoot()
    {
        GameObject projectile = new GameObject("EnemyProjectile");
        projectile.transform.position = transform.position + (Vector3)look * 0.7f;
        projectile.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = new Color(1f, 0.55f, 0.15f, 1f);
        renderer.sortingOrder = 9;

        BoxCollider2D box = projectile.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        EnemyProjectile enemyProjectile = projectile.AddComponent<EnemyProjectile>();
        enemyProjectile.direction = look;
        enemyProjectile.speed = projectileSpeed;
        enemyProjectile.life = projectileLife;
    }

    public void OnHit(Vector2 hitPoint)
    {
        Vector2 push = ((Vector2)transform.position - hitPoint).normalized;
        if (push.sqrMagnitude < 0.001f)
        {
            push = Vector2.up;
        }

        body.linearVelocity = push * recoilSpeed;
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

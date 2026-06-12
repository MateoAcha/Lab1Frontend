using UnityEngine;

public class RangedAbilityProjectile : MonoBehaviour
{
    public Vector2 direction = Vector2.down;
    public float speed = 12f;
    public float life = 2.2f;
    public int damage = 1;
    public int ownerPlayerIndex;
    public Color projectileColor = Color.white;
    public bool explodesOnImpact;
    public float explosionRadius = 3.5f;
    public float explosionDamage = 1f;
    public float explosionDuration = 0.24f;
    public float explosionPushMultiplier = 2.4f;

    private float _dieAt;
    private bool _finished;

    private void Start()
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        _dieAt = Time.time + Mathf.Max(0.01f, life);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * (Mathf.Max(0.1f, speed) * Time.deltaTime));
        if (Time.time >= _dieAt)
        {
            Finish(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_finished || other == null)
        {
            return;
        }

        EnemyProjectile enemyProjectile = other.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            Destroy(enemyProjectile.gameObject);
            Finish(explodesOnImpact);
            return;
        }

        EnemyController melee = other.GetComponent<EnemyController>();
        RangedEnemyController ranged = other.GetComponent<RangedEnemyController>();
        GiantEnemyController giant = other.GetComponent<GiantEnemyController>();
        GhostEnemy ghost = other.GetComponent<GhostEnemy>();

        if (melee == null && ranged == null && giant == null && ghost == null)
        {
            return;
        }

        if (melee != null) melee.OnHit(transform.position);
        if (ranged != null) ranged.OnHit(transform.position);
        if (giant != null) giant.OnHit(transform.position);
        if (ghost != null) ghost.OnHit(transform.position);

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }

        Finish(explodesOnImpact);
    }

    private void Finish(bool explode)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        if (explode)
        {
            SpawnExplosion();
        }

        Destroy(gameObject);
    }

    private void SpawnExplosion()
    {
        GameAudio.PlayExplosionSpecialAttack();

        GameObject explosion = new GameObject("BombShotExplosion");
        explosion.transform.position = transform.position;
        explosion.transform.localScale = Vector3.one * 0.1f;

        SpriteRenderer renderer = explosion.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = new Color(1f, 0.62f, 0.12f, 0.34f);
        renderer.sortingOrder = 13;

        CircleCollider2D circle = explosion.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.5f;

        Rigidbody2D body = explosion.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        ExpansionBurst burst = explosion.AddComponent<ExpansionBurst>();
        burst.duration = Mathf.Max(0.01f, explosionDuration);
        burst.maxRadius = Mathf.Max(0.2f, explosionRadius);
        burst.damage = Mathf.Max(0f, explosionDamage);
        burst.pushMultiplier = Mathf.Max(0f, explosionPushMultiplier);
    }
}

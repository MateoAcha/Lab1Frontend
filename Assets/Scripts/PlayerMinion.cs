using UnityEngine;

public class PlayerMinion : MonoBehaviour
{
    public float life = 18f;
    public float speed = 4.2f;
    public int touchDamage = 1;
    public float touchCooldown = 0.45f;
    public int ownerPlayerIndex = -1;

    private Rigidbody2D _body;
    private Transform _target;
    private float _dieAt;
    private float _nextTouchDamageAt;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<Rigidbody2D>();
        }

        _body.gravityScale = 0f;
        _body.freezeRotation = true;
    }

    private void Start()
    {
        _dieAt = Time.time + Mathf.Max(0.1f, life);
        IgnorePlayerCollisions();
    }

    private void Update()
    {
        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
            return;
        }

        _target = FindNearestEnemy();
        if (_target == null)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = _target.position - transform.position;
        _body.linearVelocity = toTarget.sqrMagnitude > 0.001f
            ? toTarget.normalized * Mathf.Max(0.1f, speed)
            : Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamageEnemy(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamageEnemy(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamageEnemy(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamageEnemy(other);
    }

    private void TryDamageEnemy(Collider2D other)
    {
        if (Time.time < _nextTouchDamageAt || other == null)
        {
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

        if (melee != null) melee.OnHit(transform.position, 0.4f);
        if (ranged != null) ranged.OnHit(transform.position, 0.4f);
        if (giant != null) giant.OnHit(transform.position, 0.2f);
        if (ghost != null) ghost.OnHit(transform.position);

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(Mathf.Max(1, touchDamage));
        }

        _nextTouchDamageAt = Time.time + Mathf.Max(0.05f, touchCooldown);
    }

    private Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        ConsiderEnemies(FindObjectsOfType<EnemyController>(), ref nearest, ref nearestSqDist);
        ConsiderEnemies(FindObjectsOfType<RangedEnemyController>(), ref nearest, ref nearestSqDist);
        ConsiderEnemies(FindObjectsOfType<GiantEnemyController>(), ref nearest, ref nearestSqDist);
        ConsiderEnemies(FindObjectsOfType<GhostEnemy>(), ref nearest, ref nearestSqDist);

        return nearest;
    }

    private void ConsiderEnemies<T>(T[] enemies, ref Transform nearest, ref float nearestSqDist)
        where T : Component
    {
        if (enemies == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            T enemy = enemies[i];
            if (enemy == null || enemy.gameObject == gameObject)
            {
                continue;
            }

            float sqDist = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = enemy.transform;
            }
        }
    }

    private void IgnorePlayerCollisions()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        if (ownCollider == null)
        {
            return;
        }

        PlayerController[] players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                continue;
            }

            Collider2D playerCollider = players[i].GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(ownCollider, playerCollider, true);
            }
        }
    }
}

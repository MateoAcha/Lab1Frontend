using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public Vector2 direction = Vector2.down;
    public float speed = 12f;
    public float life = 2.2f;
    public int damage = 1;
    public int ownerPlayerIndex;
    public Color projectileColor = Color.white;

    private float _dieAt;
    public float RemainingLife => _dieAt > 0f ? Mathf.Max(0f, _dieAt - Time.time) : Mathf.Max(0f, life);

    private void OnEnable()
    {
        OnlineNetworkRegistry.Register(this);
    }

    private void OnDisable()
    {
        OnlineNetworkRegistry.Unregister(this);
    }

    private void Start()
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;
        direction.Normalize();
        _dieAt = Time.time + Mathf.Max(0.01f, life);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));
        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyProjectile enemyProjectile = other.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            Destroy(enemyProjectile.gameObject);
            Destroy(gameObject);
            return;
        }

        EnemyController melee = other.GetComponent<EnemyController>();
        RangedEnemyController ranged = other.GetComponent<RangedEnemyController>();
        GhostEnemy ghost = other.GetComponent<GhostEnemy>();

        if (melee == null && ranged == null && ghost == null)
            return;

        if (melee != null) melee.OnHit(transform.position);
        if (ranged != null) ranged.OnHit(transform.position);
        if (ghost != null) ghost.OnHit(transform.position);

        Health health = other.GetComponent<Health>();
        if (health != null)
            health.Hit(damage);

        Destroy(gameObject);
    }
}

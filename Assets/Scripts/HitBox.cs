using UnityEngine;

public class HitBox : MonoBehaviour
{
    public static event System.Action<HitBox> Spawned;

    public bool hitsPlayer;
    public int damage = 1;
    public float life = 0.15f;
    public int ownerPlayerIndex = -1;
    public Color visualColor = Color.white;

    private float dieAt;
    public float RemainingLife => dieAt > 0f ? Mathf.Max(0f, dieAt - Time.time) : Mathf.Max(0f, life);

    private void Start()
    {
        dieAt = Time.time + life;
        if (ownerPlayerIndex >= 0)
        {
            OnlineNetworkRegistry.Register(this);
            Spawned?.Invoke(this);
        }
    }

    private void OnDestroy()
    {
        OnlineNetworkRegistry.Unregister(this);
    }

    private void Update()
    {
        if (Time.time >= dieAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hitsPlayer)
        {
            if (other.GetComponent<PlayerController>() == null &&
                other.GetComponent<TemporaryWall>() == null &&
                other.GetComponent<PlayerDecoy>() == null &&
                other.GetComponent<PlayerMinion>() == null)
            {
                return;
            }
        }
        else
        {
            EnemyProjectile projectile = other.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
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

            if (melee != null)  melee.OnHit(transform.position);
            if (ranged != null) ranged.OnHit(transform.position);
            if (giant != null)  giant.OnHit(transform.position);
            if (ghost != null)  ghost.OnHit(transform.position);
        }

        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }
    }
}

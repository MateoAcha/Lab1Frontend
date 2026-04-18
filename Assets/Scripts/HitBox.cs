using UnityEngine;

public class HitBox : MonoBehaviour
{
    public bool hitsPlayer;
    public int damage = 1;
    public float life = 0.15f;
    public EnemyController enemyOwner;

    private float dieAt;

    private void Start()
    {
        dieAt = Time.time + life;
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
            if (other.GetComponent<PlayerController>() == null)
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

            if (melee == null && ranged == null)
            {
                return;
            }

            if (melee != null)
            {
                melee.OnHit(transform.position);
            }

            if (ranged != null)
            {
                ranged.OnHit(transform.position);
            }
        }

        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
            enemyOwner?.OnAttackConnect(other.transform.position);
        }
    }
}

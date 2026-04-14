using UnityEngine;

public class HitBox : MonoBehaviour
{
    public bool hitsPlayer;
    public int damage = 1;
    public float life = 0.15f;

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
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy == null)
            {
                return;
            }

            enemy.OnHit(transform.position);
        }

        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }
    }
}

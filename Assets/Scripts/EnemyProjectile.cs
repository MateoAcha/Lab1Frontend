using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float speed = 5f;
    public float life = 2f;
    public int damage = 1;

    private float dieAt;

    private void Awake()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private void Start()
    {
        dieAt = Time.time + life;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time >= dieAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }

        Destroy(gameObject);
    }
}

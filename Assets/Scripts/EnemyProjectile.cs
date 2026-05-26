using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float speed = 5f;
    public float life = 2f;
    public int damage = 1;

    private float dieAt;

    public float RemainingLife => Mathf.Max(0f, dieAt - Time.time);

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
        bool hitPlayer = other.GetComponent<PlayerController>() != null;
        bool hitWall = other.GetComponent<TemporaryWall>() != null;
        bool hitAllyTarget = other.GetComponent<PlayerDecoy>() != null || other.GetComponent<PlayerMinion>() != null;
        if (!hitPlayer && !hitWall && !hitAllyTarget)
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

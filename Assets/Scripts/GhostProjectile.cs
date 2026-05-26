using UnityEngine;

public class GhostProjectile : MonoBehaviour
{
    public int     HostId;
    public Vector2 velocity;
    public float   remainingLife;

    private float _dieAt;

    private void Start()
    {
        _dieAt = Time.time + remainingLife;

        BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType    = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        if (Time.time >= _dieAt) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null &&
            other.GetComponent<TemporaryWall>() == null)
        {
            return;
        }

        other.GetComponent<Health>()?.Hit(1);
        Destroy(gameObject);
    }
}

using UnityEngine;

public class TemporaryWall : MonoBehaviour
{
    public float life = 8f;
    public float decayDamagePerSecond = 2f;
    public int ownerPlayerIndex = -1;

    private Health _health;
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
        _health = GetComponent<Health>();
        _dieAt = Time.time + Mathf.Max(0.2f, life);
        IgnorePlayerCollisions();
    }

    private void Update()
    {
        if (_health == null)
        {
            Destroy(gameObject);
            return;
        }

        if (decayDamagePerSecond > 0f)
        {
            _health.Hit(decayDamagePerSecond * Time.deltaTime);
        }

        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
        }
    }

    public void Hit(float damage)
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }

        if (_health != null)
        {
            _health.Hit(Mathf.Max(0f, damage));
        }
    }

    private void IgnorePlayerCollisions()
    {
        Collider2D wallCollider = GetComponent<Collider2D>();
        if (wallCollider == null)
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
                Physics2D.IgnoreCollision(wallCollider, playerCollider, true);
            }
        }
    }
}

using UnityEngine;

public class PlayerDecoy : MonoBehaviour
{
    public float life = 5f;
    public int ownerPlayerIndex = -1;

    private float _dieAt;
    public float RemainingLife => _dieAt > 0f ? Mathf.Max(0f, _dieAt - Time.time) : Mathf.Max(0f, life);

    private void OnEnable()
    {
        MultiplayerState.RegisterEnemyTarget(transform);
        OnlineNetworkRegistry.Register(this);
    }

    private void OnDisable()
    {
        MultiplayerState.UnregisterEnemyTarget(transform);
        OnlineNetworkRegistry.Unregister(this);
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

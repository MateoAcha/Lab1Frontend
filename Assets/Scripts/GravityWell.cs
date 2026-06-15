using UnityEngine;

public class GravityWell : MonoBehaviour
{
    public float radius = 5f;
    public float duration = 3f;
    public float pullStrength = 11f;
    public Color color = Color.white;
    public int ownerPlayerIndex = -1;

    private float _dieAt;
    private SpriteRenderer _renderer;
    public float RemainingLife => _dieAt > 0f ? Mathf.Max(0f, _dieAt - Time.time) : Mathf.Max(0f, duration);

    private void OnEnable()
    {
        OnlineNetworkRegistry.Register(this);
    }

    private void OnDisable()
    {
        OnlineNetworkRegistry.Unregister(this);
    }

    private void OnEnable()  => OnlineNetworkRegistry.Register(this);
    private void OnDisable() => OnlineNetworkRegistry.Unregister(this);
    public float RemainingLife => _dieAt > 0f ? Mathf.Max(0f, _dieAt - Time.time) : Mathf.Max(0.1f, duration);

    private void Start()
    {
        _dieAt = Time.time + Mathf.Max(0.1f, duration);
        transform.localScale = Vector3.one * Mathf.Max(0.5f, radius) * 2f;

        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = SimpleSprite.Circle;
        _renderer.color = new Color(0.35f, 0.65f, 1f, 0.22f);
        _renderer.sortingOrder = 6;
        if (color.a > 0f)
        {
            _renderer.color = new Color(color.r, color.g, color.b, 0.22f);
        }
    }

    private void LateUpdate()
    {
        float remaining = Mathf.Max(0f, _dieAt - Time.time);
        float progress = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.1f, duration));
        float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.05f;
        transform.localScale = Vector3.one * Mathf.Max(0.5f, radius) * 2f * pulse;

        if (_renderer != null)
        {
            Color c = _renderer.color;
            c.a = Mathf.Lerp(0.24f, 0.04f, progress);
            _renderer.color = c;
        }

        PullEnemies();

        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
        }
    }

    private void PullEnemies()
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.5f, radius));
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D col = overlaps[i];
            if (col == null || !IsEnemy(col))
            {
                continue;
            }

            Vector2 current = col.attachedRigidbody != null
                ? col.attachedRigidbody.position
                : (Vector2)col.transform.position;
            Vector2 target = transform.position;
            Vector2 toCenter = target - current;
            if (toCenter.sqrMagnitude < 0.0025f)
            {
                continue;
            }

            float pullStep = Mathf.Max(0f, pullStrength) * Time.deltaTime;
            Vector2 next = Vector2.MoveTowards(current, target, pullStep);

            if (col.attachedRigidbody != null)
            {
                col.attachedRigidbody.linearVelocity = Vector2.zero;
                col.attachedRigidbody.position = next;
            }
            else
            {
                col.transform.position = next;
            }
        }
    }

    private static bool IsEnemy(Collider2D col)
    {
        return col.GetComponent<EnemyController>() != null ||
            col.GetComponent<RangedEnemyController>() != null ||
            col.GetComponent<GiantEnemyController>() != null ||
            col.GetComponent<GhostEnemy>() != null;
    }
}

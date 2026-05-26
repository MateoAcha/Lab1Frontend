using System.Collections.Generic;
using UnityEngine;

public class FireTrailSegment : MonoBehaviour
{
    public float life = 1.25f;
    public int damage = 1;
    public int ownerPlayerIndex = -1;
    public Color fireColor = new Color(1f, 0.38f, 0.05f, 0.72f);

    private float _spawnedAt;
    private float _dieAt;
    private SpriteRenderer _renderer;
    private readonly HashSet<int> _hitIds = new HashSet<int>();

    private void Start()
    {
        _spawnedAt = Time.time;
        _dieAt = Time.time + Mathf.Max(0.05f, life);
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
        {
            _renderer.color = fireColor;
        }
    }

    private void Update()
    {
        float duration = Mathf.Max(0.05f, life);
        float t = Mathf.Clamp01((Time.time - _spawnedAt) / duration);
        transform.localScale = Vector3.Lerp(transform.localScale, transform.localScale * 0.94f, Time.deltaTime * 5f);

        if (_renderer != null)
        {
            Color color = fireColor;
            color.a = Mathf.Lerp(fireColor.a, 0f, t);
            _renderer.color = color;
        }

        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (other == null)
        {
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

        int id = other.GetInstanceID();
        if (_hitIds.Contains(id))
        {
            return;
        }

        _hitIds.Add(id);

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class ExpansionBurst : MonoBehaviour
{
    public float duration = 0.4f;
    public float maxRadius = 3f;
    public float damage = 1;
    public float pushMultiplier = 2f;

    private float startAt;
    private readonly HashSet<int> hitIds = new HashSet<int>();

    private void Start()
    {
        startAt = Time.time;
        transform.localScale = new Vector3(0.1f, 0.1f, 1f);
    }

    private void Update()
    {
        float usedDuration = Mathf.Max(0.01f, duration);
        float t = Mathf.Clamp01((Time.time - startAt) / usedDuration);
        float radius = Mathf.Lerp(0.1f, Mathf.Max(0.1f, maxRadius), t);
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAffect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryAffect(other);
    }

    private void TryAffect(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        int id = other.GetInstanceID();
        if (hitIds.Contains(id))
        {
            return;
        }

        EnemyProjectile projectile = other.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            hitIds.Add(id);
            Destroy(projectile.gameObject);
            return;
        }

        EnemyController melee = other.GetComponent<EnemyController>();
        RangedEnemyController ranged = other.GetComponent<RangedEnemyController>();
        if (melee == null && ranged == null)
        {
            return;
        }

        hitIds.Add(id);

        float push = Mathf.Max(0f, pushMultiplier);
        if (melee != null)
        {
            melee.OnHit(transform.position, push);
        }

        if (ranged != null)
        {
            ranged.OnHit(transform.position, push);
        }

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }
    }
}

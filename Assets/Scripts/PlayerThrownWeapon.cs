using System.Collections.Generic;
using UnityEngine;

public class PlayerThrownWeapon : MonoBehaviour
{
    public Transform owner;
    public Vector2 direction = Vector2.down;
    public bool boomerang;
    public float speed = 13f;
    public float returnSpeed = 15f;
    public float maxDistance = 8f;
    public float life = 1.6f;
    public int damage = 1;
    public int ownerPlayerIndex;
    public int weaponItemId;
    public string weaponType = "Spear";
    public Color weaponColor = Color.white;

    private Vector2 _origin;
    private float _dieAt;
    private bool _returning;
    private bool _notifiedBoomerangEnded;
    private readonly HashSet<int> _hitIds = new HashSet<int>();
    public float RemainingLife => _dieAt > 0f ? Mathf.Max(0f, _dieAt - Time.time) : Mathf.Max(0f, life);
    public Vector2 CurrentVelocity { get; private set; }

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
    public float RemainingLife => _dieAt > 0f ? Mathf.Max(0f, _dieAt - Time.time) : Mathf.Max(0.05f, life);
    public Vector2 Velocity => direction.normalized * Mathf.Max(0.1f, speed);

    private void Start()
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        _origin = transform.position;
        _dieAt = Time.time + Mathf.Max(0.05f, life);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector2 before = transform.position;
        if (Time.time >= _dieAt)
        {
            NotifyBoomerangEnded();
            Destroy(gameObject);
            return;
        }

        if (boomerang)
        {
            UpdateBoomerang(dt);
            UpdateVelocity(before, dt);
            return;
        }

        transform.position += (Vector3)(direction * (Mathf.Max(0.1f, speed) * dt));
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        UpdateVelocity(before, dt);

        if (Vector2.Distance(_origin, transform.position) >= Mathf.Max(0.5f, maxDistance))
        {
            Destroy(gameObject);
        }
    }

    private void UpdateBoomerang(float dt)
    {
        Vector2 position = transform.position;
        float outwardDistance = Mathf.Max(0.5f, maxDistance) * 0.65f;
        if (!_returning && Vector2.Distance(_origin, position) >= outwardDistance)
        {
            _returning = true;
        }

        Vector2 moveDirection = direction;
        if (_returning)
        {
            if (owner == null)
            {
                NotifyBoomerangEnded();
                Destroy(gameObject);
                return;
            }

            Vector2 toOwner = (Vector2)owner.position - position;
            if (toOwner.magnitude <= 0.45f)
            {
                NotifyBoomerangEnded();
                Destroy(gameObject);
                return;
            }

            moveDirection = toOwner.normalized;
        }

        float usedSpeed = _returning ? Mathf.Max(0.1f, returnSpeed) : Mathf.Max(0.1f, speed);
        transform.position += (Vector3)(moveDirection * (usedSpeed * dt));
        transform.Rotate(0f, 0f, 860f * dt);
    }

    private void UpdateVelocity(Vector2 before, float dt)
    {
        CurrentVelocity = dt > 0.0001f
            ? ((Vector2)transform.position - before) / dt
            : Vector2.zero;
    }

    private void OnDestroy()
    {
        NotifyBoomerangEnded();
    }

    private void NotifyBoomerangEnded()
    {
        if (!boomerang || _notifiedBoomerangEnded)
            return;

        _notifiedBoomerangEnded = true;
        if (owner == null)
            return;

        PlayerController controller = owner.GetComponent<PlayerController>();
        if (controller != null)
            controller.OnThrownSwordEnded();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        EnemyProjectile enemyProjectile = other.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            Destroy(enemyProjectile.gameObject);
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

        if (melee != null) melee.OnHit(transform.position);
        if (ranged != null) ranged.OnHit(transform.position);
        if (giant != null) giant.OnHit(transform.position);
        if (ghost != null) ghost.OnHit(transform.position);

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Hit(damage);
        }
    }
}

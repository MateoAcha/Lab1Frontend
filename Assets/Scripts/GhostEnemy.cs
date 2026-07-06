using UnityEngine;

public class GhostEnemy : MonoBehaviour
{
    public int  HostId    { get; set; }
    public bool IsRanged  { get; set; }
    public bool KilledByHost { get; set; }

    private Vector3 _targetPos;
    private float   _nextTouchDamage;
    private const float TouchCooldown = 0.8f;
    private const int   TouchDamage   = 1;

    public void SetTarget(Vector3 pos) { _targetPos = pos; }

    public void OnHit(Vector2 hitPoint) { /* visual recoil could go here */ }

    private void Start()
    {
        _targetPos = transform.position;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * 10f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < _nextTouchDamage) return;

        TemporaryWall wall = other.GetComponent<TemporaryWall>();
        if (wall != null)
        {
            wall.Hit(EnemyDamage.Amount(TouchDamage));
            _nextTouchDamage = Time.time + TouchCooldown;
            return;
        }

        if (other.GetComponent<PlayerController>() == null) return;

        Health h = other.GetComponent<Health>();
        if (h == null) return;

        h.Hit(EnemyDamage.Amount(TouchDamage));
        _nextTouchDamage = Time.time + TouchCooldown;
    }

    private void OnDestroy()
    {
        if (KilledByHost) return;

        if (IsRanged) GameStatsTracker.RegisterRangedEnemyKilled();
        else          GameStatsTracker.RegisterMeleeEnemyKilled();

    }
}

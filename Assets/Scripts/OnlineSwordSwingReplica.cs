using UnityEngine;

public class OnlineSwordSwingReplica : MonoBehaviour
{
    private Transform _owner;
    private Vector2 _direction = Vector2.down;
    private float _distance = 1.8f;
    private float _duration = 0.16f;
    private float _arcDegrees = 110f;
    private float _startTime;
    private bool _running;

    public bool IsRunning => _running;

    public void Begin(
        Transform owner,
        Vector2 direction,
        float distance,
        float duration,
        float arcDegrees)
    {
        _owner = owner;
        _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
        _distance = Mathf.Max(0.01f, distance);
        _duration = Mathf.Max(0.05f, duration);
        _arcDegrees = Mathf.Max(10f, arcDegrees);
        _startTime = Time.time;
        _running = true;
        UpdatePose(0f);
    }

    public void RefreshOwner(Transform owner)
    {
        if (owner != null)
            _owner = owner;
    }

    private void Update()
    {
        if (!_running)
            return;

        float progress = Mathf.Clamp01((Time.time - _startTime) / _duration);
        UpdatePose(progress);
    }

    private void UpdatePose(float progress)
    {
        float baseAngle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        float halfArc = _arcDegrees * 0.5f;
        bool facingRight = _direction.x >= 0f;
        float swingOffset = facingRight
            ? Mathf.Lerp(halfArc, -halfArc, progress)
            : Mathf.Lerp(-halfArc, halfArc, progress);
        float angle = baseAngle + swingOffset;
        Vector2 swungDirection = Quaternion.Euler(0f, 0f, swingOffset) * _direction;
        Vector3 origin = _owner != null ? _owner.position : transform.position;

        transform.position = origin + (Vector3)(swungDirection * _distance);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}

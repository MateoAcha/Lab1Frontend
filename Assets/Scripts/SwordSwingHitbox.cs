using UnityEngine;

public class SwordSwingHitbox : MonoBehaviour
{
    public Transform owner;
    public Vector2 direction = Vector2.down;
    public float distance = 1.8f;
    public float duration = 0.16f;
    public float arcDegrees = 110f;

    private float _startTime;

    private void Start()
    {
        _startTime = Time.time;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;
        direction.Normalize();
        UpdatePose(0f);
    }

    private void Update()
    {
        float progress = duration <= 0f ? 1f : Mathf.Clamp01((Time.time - _startTime) / duration);
        UpdatePose(progress);
    }

    private void UpdatePose(float progress)
    {
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float swingOffset = Mathf.Lerp(-arcDegrees * 0.5f, arcDegrees * 0.5f, progress);
        float angle = baseAngle + swingOffset;
        Vector2 swungDirection = Quaternion.Euler(0f, 0f, swingOffset) * direction;
        Vector3 origin = owner != null ? owner.position : transform.position;

        transform.position = origin + (Vector3)(swungDirection * distance);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}

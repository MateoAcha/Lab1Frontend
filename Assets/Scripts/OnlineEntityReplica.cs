using UnityEngine;

public class OnlineEntityReplica : MonoBehaviour
{
    private Vector3 _targetPosition;
    private Vector3 _targetVelocity;
    private float _targetReceivedAt;
    private float _lerpSpeed = 12f;

    private void Awake()
    {
        _targetPosition = transform.position;
        _targetReceivedAt = Time.time;
    }

    public void SnapTo(Vector3 position)
    {
        _targetPosition = position;
        _targetVelocity = Vector3.zero;
        _targetReceivedAt = Time.time;
        transform.position = position;
    }

    public void SetTarget(Vector3 position, float lerpSpeed = 12f)
    {
        SetTarget(position, Vector3.zero, lerpSpeed);
    }

    public void SetTarget(Vector3 position, Vector3 velocity, float lerpSpeed = 12f)
    {
        _targetPosition = position;
        _targetVelocity = velocity;
        _targetReceivedAt = Time.time;
        _lerpSpeed = Mathf.Max(1f, lerpSpeed);
    }

    private void Update()
    {
        float predictionTime = Mathf.Clamp(Time.time - _targetReceivedAt, 0f, 0.12f);
        Vector3 predictedPosition = _targetPosition + _targetVelocity * predictionTime;
        transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * _lerpSpeed);
    }
}

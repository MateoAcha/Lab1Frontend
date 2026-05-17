using UnityEngine;

public class OnlineEntityReplica : MonoBehaviour
{
    private Vector3 _targetPosition;
    private float _lerpSpeed = 12f;

    private void Awake()
    {
        _targetPosition = transform.position;
    }

    public void SnapTo(Vector3 position)
    {
        _targetPosition = position;
        transform.position = position;
    }

    public void SetTarget(Vector3 position, float lerpSpeed = 12f)
    {
        _targetPosition = position;
        _lerpSpeed = Mathf.Max(1f, lerpSpeed);
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _lerpSpeed);
    }
}

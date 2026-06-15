using UnityEngine;

public class OnlineLocalTransformSmoother : MonoBehaviour
{
    private Vector3 _targetLocalPosition;
    private Vector3 _targetLocalScale = Vector3.one;
    private float _lerpSpeed = 18f;
    private bool _initialized;

    public void SetTarget(Vector3 localPosition, Vector3 localScale, float lerpSpeed = 18f)
    {
        _targetLocalPosition = localPosition;
        _targetLocalScale = localScale;
        _lerpSpeed = Mathf.Max(1f, lerpSpeed);

        if (_initialized)
            return;

        transform.localPosition = _targetLocalPosition;
        transform.localScale = _targetLocalScale;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        float t = Time.deltaTime * _lerpSpeed;
        transform.localPosition = Vector3.Lerp(transform.localPosition, _targetLocalPosition, t);
        transform.localScale = Vector3.Lerp(transform.localScale, _targetLocalScale, t);
    }
}

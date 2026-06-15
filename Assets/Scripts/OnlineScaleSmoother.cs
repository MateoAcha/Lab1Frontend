using UnityEngine;

public class OnlineScaleSmoother : MonoBehaviour
{
    private Vector3 _targetScale = Vector3.one;
    private float _lerpSpeed = 18f;
    private bool _initialized;

    public void SetTarget(Vector3 scale, float lerpSpeed = 18f)
    {
        _targetScale = scale;
        _lerpSpeed = Mathf.Max(1f, lerpSpeed);

        if (_initialized)
            return;

        transform.localScale = _targetScale;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _lerpSpeed);
    }
}

using UnityEngine;

public class OnlineFireTrailReplica : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Color _baseColor = Color.white;
    private Vector3 _baseScale = Vector3.one;
    private float _startAt;
    private float _expireAt;
    private float _duration = 1f;
    private bool _initialized;

    public void Configure(SpriteRenderer renderer, Color color, float remainingLife)
    {
        _renderer = renderer;
        _baseColor = color;
        float life = Mathf.Max(0.05f, remainingLife);

        if (!_initialized)
        {
            _baseScale = transform.localScale;
            _startAt = Time.time;
            _duration = life;
            _expireAt = Time.time + life;
            _initialized = true;
            return;
        }

        _expireAt = Mathf.Max(_expireAt, Time.time + life);
        _duration = Mathf.Max(_duration, _expireAt - _startAt);
    }

    private void Update()
    {
        if (!_initialized)
            return;

        float remaining = Mathf.Max(0f, _expireAt - Time.time);
        float progress = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.05f, _duration));
        transform.localScale = Vector3.Lerp(_baseScale, _baseScale * 0.45f, progress);

        if (_renderer != null)
        {
            Color color = _baseColor;
            color.a = Mathf.Lerp(_baseColor.a, 0f, progress);
            _renderer.color = color;
        }
    }
}

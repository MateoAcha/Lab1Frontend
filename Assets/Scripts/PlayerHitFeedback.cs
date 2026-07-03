using UnityEngine;

public class PlayerHitFeedback : MonoBehaviour
{
    public float duration = 0.48f;
    public float blinkInterval = 0.06f;
    public float transparentAlpha = 0.32f;

    private SpriteRenderer _renderer;
    private Color _baseColor = Color.white;
    private float _startedAt;
    private float _endsAt;
    private bool _playing;

    private void Awake()
    {
        CacheRenderer();
        enabled = false;
    }

    public void Play()
    {
        CacheRenderer();
        if (_renderer == null)
            return;

        _baseColor = _renderer.color;
        _startedAt = Time.time;
        _endsAt = Time.time + Mathf.Max(0.05f, duration);
        _playing = true;
        enabled = true;
    }

    private void Update()
    {
        if (!_playing || _renderer == null)
        {
            enabled = false;
            return;
        }

        if (Time.time >= _endsAt)
        {
            Restore();
            return;
        }

        float elapsed = Time.time - _startedAt;
        int phase = Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, blinkInterval));
        if (phase % 2 == 0)
        {
            Color white = Color.white;
            white.a = Mathf.Max(_baseColor.a, 0.92f);
            _renderer.color = white;
        }
        else
        {
            Color transparent = _baseColor;
            transparent.a = Mathf.Min(_baseColor.a, transparentAlpha);
            _renderer.color = transparent;
        }
    }

    private void OnDisable()
    {
        if (_playing)
            Restore();
    }

    private void Restore()
    {
        if (_renderer != null)
            _renderer.color = _baseColor;
        _playing = false;
        enabled = false;
    }

    private void CacheRenderer()
    {
        if (_renderer != null)
            return;

        Transform sprite = transform.Find("Sprite");
        _renderer = sprite != null
            ? sprite.GetComponent<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>();
    }
}

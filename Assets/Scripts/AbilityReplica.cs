using UnityEngine;

public class AbilityReplica : MonoBehaviour
{
    public float dieAt;
    public float targetScale;  // burst: grow toward this; 0 = no growth
    public Vector2 velocity;   // bomb/weapon: move in this direction
    public float rotateSpeed;  // weapon: spin in deg/s
    public bool pulse;         // well: pulse the scale

    private SpriteRenderer _sr;
    private float _baseScale;
    private float _baseAlpha;
    private float _totalDuration;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale.x;
        _baseAlpha = _sr != null ? _sr.color.a : 0.5f;
        _totalDuration = Mathf.Max(0.01f, dieAt - Time.time);
    }

    private void Update()
    {
        float remaining = Mathf.Max(0f, dieAt - Time.time);

        if (velocity.sqrMagnitude > 0.001f)
            transform.position += (Vector3)(velocity * Time.deltaTime);

        if (rotateSpeed > 0f)
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        if (targetScale > 0f)
        {
            float t = 1f - remaining / _totalDuration;
            transform.localScale = Vector3.one * Mathf.Lerp(_baseScale, targetScale, t);
        }
        else if (pulse)
        {
            float p = 1f + Mathf.Sin(Time.time * 12f) * 0.05f;
            transform.localScale = Vector3.one * _baseScale * p;
        }

        if (_sr != null)
        {
            float fade = Mathf.Clamp01(remaining / 0.3f);
            Color c = _sr.color;
            c.a = _baseAlpha * fade;
            _sr.color = c;
        }

        if (remaining <= 0f)
            Destroy(gameObject);
    }
}

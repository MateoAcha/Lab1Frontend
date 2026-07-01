using UnityEngine;

public static class BloodBurst
{
    public static void Spawn(Vector2 position, Vector2 hitPoint, float sizeMultiplier = 1f)
    {
        float scale = Mathf.Clamp(sizeMultiplier, 0.7f, 2.6f);
        int count = Mathf.RoundToInt(Mathf.Lerp(12f, 24f, Mathf.InverseLerp(0.7f, 2.6f, scale)));
        Vector2 away = position - hitPoint;
        if (away.sqrMagnitude < 0.001f)
            away = Random.insideUnitCircle;
        if (away.sqrMagnitude < 0.001f)
            away = Vector2.up;
        away.Normalize();

        for (int i = 0; i < count; i++)
        {
            GameObject particle = new GameObject("BloodParticle");
            particle.transform.position = position + Random.insideUnitCircle * (0.18f * scale);

            float particleSize = Random.Range(0.09f, 0.18f) * Mathf.Sqrt(scale);
            particle.transform.localScale = new Vector3(particleSize, particleSize, 1f);

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSprite.Circle;
            renderer.color = Random.value > 0.35f
                ? new Color(0.82f, 0.02f, 0.02f, 0.95f)
                : new Color(0.42f, 0f, 0.01f, 0.95f);
            renderer.sortingOrder = 32;

            Vector2 spread = (away * Random.Range(0.7f, 1.25f) + Random.insideUnitCircle * 0.85f).normalized;
            BloodParticleFx fx = particle.AddComponent<BloodParticleFx>();
            fx.velocity = spread * Random.Range(2.8f, 5.8f) * Mathf.Sqrt(scale);
            fx.life = Random.Range(0.42f, 0.68f);
            fx.damping = Random.Range(0.84f, 0.91f);
            fx.gravity = Random.Range(0.4f, 1.1f);
        }
    }
}

public class BloodParticleFx : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 0.55f;
    public float damping = 0.88f;
    public float gravity = 0.8f;

    private float _age;
    private SpriteRenderer _sprite;
    private Color _baseColor;
    private Vector3 _baseScale;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite == null)
        {
            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = SimpleSprite.Circle;
            _sprite.color = Color.red;
        }

        _baseColor = _sprite.color;
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _age += dt;
        velocity += Vector2.down * (gravity * dt);
        transform.position += (Vector3)(velocity * dt);
        velocity *= Mathf.Pow(Mathf.Clamp01(damping), dt * 60f);

        float t = life > 0f ? Mathf.Clamp01(_age / life) : 1f;
        float alpha = Mathf.SmoothStep(1f, 0f, t);
        if (_sprite != null)
            _sprite.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * alpha);
        transform.localScale = _baseScale * Mathf.Lerp(1f, 0.45f, t);

        if (_age >= life)
            Object.Destroy(gameObject);
    }
}

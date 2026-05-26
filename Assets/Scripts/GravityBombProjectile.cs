using UnityEngine;

public class GravityBombProjectile : MonoBehaviour
{
    public Vector2 direction = Vector2.down;
    public float distance = 6.5f;
    public float travelTime = 0.85f;
    public float arcHeight = 2.6f;
    public float pullRadius = 5f;
    public float pullDuration = 3f;
    public float pullStrength = 11f;
    public Color bombColor = Color.white;

    private Vector2 _start;
    private Vector2 _end;
    private float _startAt;
    private Transform _visual;
    private Transform _shadow;

    private void Start()
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        _start = transform.position;
        _end = _start + direction * Mathf.Max(0.5f, distance);
        _startAt = Time.time;
        BuildVisual();
    }

    private void Update()
    {
        float t = Mathf.Clamp01((Time.time - _startAt) / Mathf.Max(0.1f, travelTime));
        Vector2 groundPosition = Vector2.Lerp(_start, _end, t);
        transform.position = groundPosition;

        float height = Mathf.Sin(t * Mathf.PI) * Mathf.Max(0f, arcHeight);
        if (_visual != null)
        {
            _visual.localPosition = new Vector3(0f, height, 0f);
            _visual.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, height / Mathf.Max(0.01f, arcHeight));
            _visual.Rotate(0f, 0f, 540f * Time.deltaTime);
        }

        if (_shadow != null)
        {
            float shadowScale = Mathf.Lerp(0.8f, 0.45f, height / Mathf.Max(0.01f, arcHeight));
            _shadow.localScale = new Vector3(shadowScale, shadowScale * 0.35f, 1f);
        }

        if (t >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        GameObject well = new GameObject("GravityBombWell");
        well.transform.position = transform.position;
        GravityWell gravityWell = well.AddComponent<GravityWell>();
        gravityWell.radius = Mathf.Max(0.5f, pullRadius);
        gravityWell.duration = Mathf.Max(0.1f, pullDuration);
        gravityWell.pullStrength = Mathf.Max(0f, pullStrength);
        gravityWell.color = bombColor;

        Destroy(gameObject);
    }

    private void BuildVisual()
    {
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(transform, false);
        _shadow = shadowObj.transform;
        SpriteRenderer shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = SimpleSprite.Circle;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.24f);
        shadowRenderer.sortingOrder = 8;
        shadowObj.transform.localScale = new Vector3(0.8f, 0.28f, 1f);

        GameObject visualObj = new GameObject("BombVisual");
        visualObj.transform.SetParent(transform, false);
        _visual = visualObj.transform;
        SpriteRenderer renderer = visualObj.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = bombColor;
        renderer.sortingOrder = 14;
        visualObj.transform.localScale = Vector3.one * 0.8f;
    }
}

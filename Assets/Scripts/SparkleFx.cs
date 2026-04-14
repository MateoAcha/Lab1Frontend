using UnityEngine;

public class SparkleFx : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 0.2f;

    private float age;
    private SpriteRenderer sprite;
    private Color baseColor;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null)
        {
            sprite = gameObject.AddComponent<SpriteRenderer>();
            sprite.sprite = SimpleSprite.Square;
            sprite.color = Color.white;
        }

        baseColor = sprite.color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= 0.88f;

        float alpha = 1f - (age / life);
        sprite.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));

        if (age >= life)
        {
            Destroy(gameObject);
        }
    }
}

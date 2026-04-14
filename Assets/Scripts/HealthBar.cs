using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public float y = 0.75f;
    public float width = 1f;
    public float height = 0.12f;

    private Health health;
    private Transform fill;

    private void Start()
    {
        health = GetComponent<Health>();
        CreateBar();
    }

    private void LateUpdate()
    {
        if (health == null || fill == null)
        {
            return;
        }

        float ratio = health.maxHp > 0f ? health.hp / health.maxHp : 0f;
        ratio = Mathf.Clamp01(ratio);

        fill.localScale = new Vector3(width * ratio, height, 1f);
        fill.localPosition = new Vector3((-width + (width * ratio)) * 0.5f, y, 0f);
    }

    private void CreateBar()
    {
        GameObject back = new GameObject("HpBack");
        back.transform.SetParent(transform, false);
        back.transform.localPosition = new Vector3(0f, y, 0f);
        back.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
        backRenderer.sprite = SimpleSprite.Square;
        backRenderer.color = new Color(0.25f, 0f, 0f, 0.85f);
        backRenderer.sortingOrder = 20;

        GameObject front = new GameObject("HpFill");
        front.transform.SetParent(transform, false);
        fill = front.transform;

        SpriteRenderer fillRenderer = front.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = SimpleSprite.Square;
        fillRenderer.color = new Color(1f, 0f, 0f, 0.95f);
        fillRenderer.sortingOrder = 21;
    }
}

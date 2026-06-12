using UnityEngine;

public class DroppedMaterialPickup : MonoBehaviour
{
    private MapMaterialDefinition materialDrop;
    private Sprite generatedSprite;
    private float bobOffset;

    public static void SpawnForCurrentMap(Vector3 position)
    {
        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        MapMaterialDefinition drop = bootstrap != null ? bootstrap.GetSelectedMapMaterialDrop() : null;
        if (drop == null || string.IsNullOrWhiteSpace(drop.inventoryKey))
        {
            return;
        }

        GameObject obj = new GameObject("DroppedMaterial_" + drop.inventoryKey);
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        DroppedMaterialPickup pickup = obj.AddComponent<DroppedMaterialPickup>();
        pickup.Configure(drop);
    }

    private void Configure(MapMaterialDefinition drop)
    {
        materialDrop = drop;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        float size = Mathf.Max(0.2f, materialDrop.pickupSize);
        transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ResolveSprite();
        renderer.sortingOrder = 4;
        renderer.color = materialDrop.pickupMaterial != null ? Color.white : materialDrop.pickupColor;
        if (materialDrop.pickupMaterial != null)
        {
            renderer.sharedMaterial = materialDrop.pickupMaterial;
        }

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.55f;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 4f + bobOffset) * 0.08f;
        transform.localScale = Vector3.one * (Mathf.Max(0.2f, materialDrop != null ? materialDrop.pickupSize : 0.9f) * pulse);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || materialDrop == null)
        {
            return;
        }

        if (MultiplayerState.IsOnline && player.playerIndex != 0)
        {
            return;
        }

        GameStatsTracker.RegisterMaterialCollected(materialDrop);
        GameAudio.PlayItemPickup();
        Destroy(gameObject);
    }

    private Sprite ResolveSprite()
    {
        if (materialDrop != null && materialDrop.pickupTexture != null)
        {
            Texture2D texture = materialDrop.pickupTexture;
            if (generatedSprite == null || generatedSprite.texture != texture)
            {
                generatedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(texture.width, texture.height),
                    0,
                    SpriteMeshType.Tight);
            }

            return generatedSprite;
        }

        return SimpleSprite.Circle;
    }
}

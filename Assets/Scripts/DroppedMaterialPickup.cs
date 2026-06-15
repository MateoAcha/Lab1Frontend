using UnityEngine;

public class DroppedMaterialPickup : MonoBehaviour
{
    private MapMaterialDefinition materialDrop;
    private Sprite generatedSprite;
    private float bobOffset;
    private bool networkReplica;
    private bool collected;
    private int networkId;

    public string InventoryKey => materialDrop != null ? materialDrop.inventoryKey : "";
    public string ItemName => materialDrop != null ? materialDrop.itemName : "";
    public string Rarity => materialDrop != null ? materialDrop.rarity : "Rare";
    public Color PickupColor => materialDrop != null ? materialDrop.pickupColor : Color.white;
    public float PickupSize => materialDrop != null ? materialDrop.pickupSize : 0.9f;

    public MapMaterialDefinition MaterialDrop => materialDrop;

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

    public static DroppedMaterialPickup SpawnNetworkReplica(OnlineMaterialPickupState state)
    {
        if (state == null)
            return null;

        GameObject obj = new GameObject("DroppedMaterialReplica_" + state.inventoryKey);
        obj.transform.position = new Vector3(state.x, state.y, 0f);

        NetworkEntityId entityId = obj.AddComponent<NetworkEntityId>();
        entityId.Id = state.id;

        DroppedMaterialPickup pickup = obj.AddComponent<DroppedMaterialPickup>();
        pickup.Configure(BuildDropFromState(state), true, state.id);
        return pickup;
    }

    public void ApplyNetworkState(OnlineMaterialPickupState state)
    {
        if (state == null)
            return;

        networkId = state.id;
        transform.position = new Vector3(state.x, state.y, 0f);
        if (materialDrop == null || !string.Equals(materialDrop.inventoryKey, state.inventoryKey, System.StringComparison.Ordinal))
            Configure(BuildDropFromState(state), true, state.id);
    }

    public void CollectFromNetworkGuest()
    {
        if (collected)
            return;

        collected = true;
        GameAudio.PlayItemPickup();
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        OnlineNetworkRegistry.Register(this);
    }

    private void OnDisable()
    {
        OnlineNetworkRegistry.Unregister(this);
    }

    private void Configure(MapMaterialDefinition drop, bool isNetworkReplica = false, int id = 0)
    {
        materialDrop = drop;
        networkReplica = isNetworkReplica;
        networkId = id;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        float size = Mathf.Max(0.2f, materialDrop.pickupSize);
        transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ResolveSprite();
        renderer.sortingOrder = 4;
        renderer.color = materialDrop.pickupMaterial != null ? Color.white : materialDrop.pickupColor;
        if (materialDrop.pickupMaterial != null)
        {
            renderer.sharedMaterial = materialDrop.pickupMaterial;
        }

        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<CircleCollider2D>();
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
        if (collected)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || materialDrop == null)
        {
            return;
        }

        if (networkReplica)
        {
            collected = true;
            GameStatsTracker.RegisterMaterialCollected(materialDrop);
            GameAudio.PlayItemPickup();

            GameStateGuest guest = FindObjectOfType<GameStateGuest>();
            if (guest != null)
                guest.RequestMaterialPickupCollect(networkId);

            Destroy(gameObject);
            return;
        }

        if (MultiplayerState.IsOnline && player.playerIndex != 0)
        {
            return;
        }

        collected = true;
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

    private static MapMaterialDefinition BuildDropFromState(OnlineMaterialPickupState state)
    {
        MapMaterialDefinition selectedDrop = null;
        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
            selectedDrop = bootstrap.GetSelectedMapMaterialDrop();

        Color color = selectedDrop != null ? selectedDrop.pickupColor : Color.white;
        if (!string.IsNullOrWhiteSpace(state.color))
            color = PlayerLoadout.ParseWeaponColor(state.color, color);

        return new MapMaterialDefinition
        {
            inventoryKey = string.IsNullOrWhiteSpace(state.inventoryKey) ? (selectedDrop != null ? selectedDrop.inventoryKey : "") : state.inventoryKey,
            itemName = string.IsNullOrWhiteSpace(state.itemName) ? (selectedDrop != null ? selectedDrop.itemName : "") : state.itemName,
            rarity = string.IsNullOrWhiteSpace(state.rarity) ? (selectedDrop != null ? selectedDrop.rarity : "Rare") : state.rarity,
            pickupTexture = selectedDrop != null && string.Equals(selectedDrop.inventoryKey, state.inventoryKey, System.StringComparison.Ordinal)
                ? selectedDrop.pickupTexture
                : null,
            pickupMaterial = selectedDrop != null && string.Equals(selectedDrop.inventoryKey, state.inventoryKey, System.StringComparison.Ordinal)
                ? selectedDrop.pickupMaterial
                : null,
            pickupColor = color,
            pickupSize = Mathf.Max(0.2f, state.size)
        };
    }
}

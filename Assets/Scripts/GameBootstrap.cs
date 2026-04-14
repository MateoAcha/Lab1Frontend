using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public float playerSize = 1f;
    public float meleeEnemySize = 0.8f;
    public float rangedEnemySize = 0.75f;
    [Header("Map")]
    public Vector2 mapSize = new Vector2(40f, 40f);
    [Header("Materials")]
    public Material backgroundMaterial;
    public Material playerMaterial;
    public Material meleeEnemyMaterial;
    public Material rangedEnemyMaterial;
    public Material enemyProjectileMaterial;
    [Header("Rocks")]
    public Sprite rockSprite;
    public Material rockMaterial;
    public Color rockColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public float rockBaseSize = 1.4f;
    [Range(0f, 1f)] public float rockSizeJitter = 0.3f;
    public float rocksPer100Units = 8f;
    [Range(0f, 1f)] public float centerRockChance = 0.08f;
    [Range(0f, 1f)] public float edgeRockChance = 0.55f;
    public float centerSafeRadius = 3.5f;
    public float borderInset = 1.2f;
    public float borderRockSize = 2f;
    public float borderSpacing = 1.4f;

    private Sprite generatedRockSprite;

    private void Start()
    {
        SetupCamera();
        SetupPlayer();
        SetupRocks();
        SetupSpawner();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject obj = new GameObject("Main Camera");
            obj.tag = "MainCamera";
            cam = obj.AddComponent<Camera>();
            obj.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        SetupBackground(cam);

        if (cam.GetComponent<CameraFollow>() == null)
        {
            cam.gameObject.AddComponent<CameraFollow>();
        }
    }

    private void SetupPlayer()
    {
        if (PlayerController.main != null || FindObjectOfType<PlayerController>() != null)
        {
            return;
        }

        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;
        player.transform.localScale = new Vector3(playerSize, playerSize, 1f);

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = new Color(0.3f, 0.75f, 1f, 1f);
        renderer.sortingOrder = 6;
        if (playerMaterial != null)
        {
            renderer.sharedMaterial = playerMaterial;
            renderer.color = Color.white;
        }

        Health health = player.AddComponent<Health>();
        health.hp = 10;

        player.AddComponent<PlayerController>();
    }

    private void SetupSpawner()
    {
        EnemySpawner enemySpawner = FindObjectOfType<EnemySpawner>();
        if (enemySpawner == null)
        {
            GameObject spawner = new GameObject("EnemySpawner");
            enemySpawner = spawner.AddComponent<EnemySpawner>();
        }

        enemySpawner.meleeEnemySize = meleeEnemySize;
        enemySpawner.rangedEnemySize = rangedEnemySize;
        enemySpawner.mapSize = mapSize;
        enemySpawner.spawnPadding = Mathf.Max(0.5f, borderInset);
        enemySpawner.meleeEnemyMaterial = meleeEnemyMaterial;
        enemySpawner.rangedEnemyMaterial = rangedEnemyMaterial;
        enemySpawner.enemyProjectileMaterial = enemyProjectileMaterial;
    }

    private void SetupBackground(Camera cam)
    {
        const string backgroundName = "RuntimeBackground";

        GameObject background = GameObject.Find(backgroundName);
        if (background == null)
        {
            background = new GameObject(backgroundName);
        }

        if (background.transform.parent != null)
        {
            background.transform.SetParent(null, true);
        }

        float width = Mathf.Max(1f, mapSize.x);
        float height = Mathf.Max(1f, mapSize.y);
        background.transform.position = Vector3.zero;
        background.transform.rotation = Quaternion.identity;
        background.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = background.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = SimpleSprite.Square;
        renderer.sortingOrder = -100;
        renderer.sharedMaterial = backgroundMaterial;
        renderer.color = backgroundMaterial != null ? Color.white : cam.backgroundColor;
    }

    private void SetupRocks()
    {
        const string rocksRootName = "RuntimeRocks";

        GameObject rocksRoot = GameObject.Find(rocksRootName);
        if (rocksRoot == null)
        {
            rocksRoot = new GameObject(rocksRootName);
        }
        else if (rocksRoot.transform.childCount > 0)
        {
            return;
        }

        float width = Mathf.Max(1f, mapSize.x);
        float height = Mathf.Max(1f, mapSize.y);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        SpawnBorderRocks(rocksRoot.transform, halfWidth, halfHeight);
        SpawnInteriorRocks(rocksRoot.transform, width, height, halfWidth, halfHeight);
    }

    private void SpawnInteriorRocks(Transform parent, float width, float height, float halfWidth, float halfHeight)
    {
        float usableHalfWidth = Mathf.Max(0.25f, halfWidth - borderInset - rockBaseSize * 0.5f);
        float usableHalfHeight = Mathf.Max(0.25f, halfHeight - borderInset - rockBaseSize * 0.5f);

        int targetCount = Mathf.RoundToInt((width * height / 100f) * Mathf.Max(0f, rocksPer100Units));
        int maxAttempts = Mathf.Max(24, targetCount * 10);
        int spawned = 0;

        for (int i = 0; i < maxAttempts && spawned < targetCount; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(-usableHalfWidth, usableHalfWidth),
                Random.Range(-usableHalfHeight, usableHalfHeight));

            if (pos.sqrMagnitude < centerSafeRadius * centerSafeRadius)
            {
                continue;
            }

            float edgeX = usableHalfWidth > 0.001f ? Mathf.Abs(pos.x) / usableHalfWidth : 1f;
            float edgeY = usableHalfHeight > 0.001f ? Mathf.Abs(pos.y) / usableHalfHeight : 1f;
            float edge01 = Mathf.Clamp01(Mathf.Max(edgeX, edgeY));
            // Quadratic falloff: sparse in center, denser near map limits.
            float chance = Mathf.Lerp(centerRockChance, edgeRockChance, edge01 * edge01);
            if (Random.value > chance)
            {
                continue;
            }

            float size = GetRockSize(rockBaseSize);
            if (Physics2D.OverlapBox(pos, Vector2.one * (size * 0.9f), 0f) != null)
            {
                continue;
            }

            CreateRock(parent, pos, size, "Rock");
            spawned++;
        }
    }

    private void SpawnBorderRocks(Transform parent, float halfWidth, float halfHeight)
    {
        float xEdge = Mathf.Max(0.25f, halfWidth - borderInset);
        float yEdge = Mathf.Max(0.25f, halfHeight - borderInset);
        float spacing = Mathf.Max(0.25f, borderSpacing);

        int horizontalSteps = Mathf.Max(2, Mathf.CeilToInt((xEdge * 2f) / spacing) + 1);
        int verticalSteps = Mathf.Max(2, Mathf.CeilToInt((yEdge * 2f) / spacing) + 1);

        for (int i = 0; i < horizontalSteps; i++)
        {
            float t = horizontalSteps > 1 ? i / (horizontalSteps - 1f) : 0f;
            float x = Mathf.Lerp(-xEdge, xEdge, t);
            CreateRock(parent, new Vector2(x, yEdge), borderRockSize, "BorderRock");
            CreateRock(parent, new Vector2(x, -yEdge), borderRockSize, "BorderRock");
        }

        for (int i = 1; i < verticalSteps - 1; i++)
        {
            float t = verticalSteps > 1 ? i / (verticalSteps - 1f) : 0f;
            float y = Mathf.Lerp(-yEdge, yEdge, t);
            CreateRock(parent, new Vector2(xEdge, y), borderRockSize, "BorderRock");
            CreateRock(parent, new Vector2(-xEdge, y), borderRockSize, "BorderRock");
        }
    }

    private float GetRockSize(float baseSize)
    {
        float jitter = Mathf.Max(0f, rockSizeJitter);
        float scale = Random.Range(1f - jitter, 1f + jitter);
        return Mathf.Max(0.2f, baseSize * scale);
    }

    private void CreateRock(Transform parent, Vector2 position, float size, string baseName)
    {
        size = Mathf.Max(0.2f, size);

        GameObject rock = new GameObject(baseName);
        rock.transform.SetParent(parent, false);
        rock.transform.position = new Vector3(position.x, position.y, 0f);
        rock.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = rock.AddComponent<SpriteRenderer>();
        Sprite sprite = ResolveRockSprite();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        renderer.color = rockColor;
        if (rockMaterial != null)
        {
            renderer.sharedMaterial = rockMaterial;
            renderer.color = Color.white;
        }

        if (sprite != null && sprite != SimpleSprite.Square)
        {
            PolygonCollider2D poly = rock.AddComponent<PolygonCollider2D>();
            if (poly.pathCount == 0)
            {
                Destroy(poly);
                rock.AddComponent<BoxCollider2D>();
            }
        }
        else
        {
            rock.AddComponent<BoxCollider2D>();
        }
    }

    private Sprite ResolveRockSprite()
    {
        if (rockSprite != null)
        {
            return rockSprite;
        }

        if (rockMaterial != null && rockMaterial.HasProperty("_OverlayTex"))
        {
            Texture overlayTexture = rockMaterial.GetTexture("_OverlayTex");
            if (overlayTexture is Texture2D overlayTexture2D &&
                overlayTexture2D.width > 0 &&
                overlayTexture2D.height > 0)
            {
                if (generatedRockSprite == null || generatedRockSprite.texture != overlayTexture2D)
                {
                    generatedRockSprite = Sprite.Create(
                        overlayTexture2D,
                        new Rect(0f, 0f, overlayTexture2D.width, overlayTexture2D.height),
                        new Vector2(0.5f, 0.5f),
                        overlayTexture2D.width,
                        0,
                        SpriteMeshType.Tight);
                }

                return generatedRockSprite;
            }
        }

        return SimpleSprite.Square;
    }
}

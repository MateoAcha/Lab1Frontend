using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private const int RockLayoutSeed = 834927;

    public float playerSize = 1f;
    public float meleeEnemySize = 0.8f;
    public float rangedEnemySize = 0.75f;
    [Header("Giant Enemy")]
    public float giantEnemySize = 2.8f;
    public Material giantEnemyMaterial;
    public float giantEnemyHealth = 40f;
    public float giantEnemyAttackRange = 14f;
    [Header("Map")]
    public Vector2 mapSize = new Vector2(40f, 40f);
    [Header("Materials")]
    public Material backgroundMaterial;
    public Material playerMaterial;
    public Material meleeEnemyMaterial;
    public Material rangedEnemyMaterial;
    public Material enemyProjectileMaterial;
    public SkinVisualDatabase skinVisualDatabase;
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
        bool isMultiplayer = MultiplayerState.IsMultiplayer;
        bool isOnline      = MultiplayerState.IsOnline;
        bool isHost        = MultiplayerState.IsHost;
        int onlineRoom     = MultiplayerState.OnlineRoomNumber;
        MultiplayerState.Reset();
        MultiplayerState.SetMultiplayer(isMultiplayer);
        if (isOnline) { MultiplayerState.SetOnline(true); MultiplayerState.SetHost(isHost); MultiplayerState.SetOnlineRoomNumber(onlineRoom); }
        SetupCamera();
        SetupPlayer(0, new Vector3(-1f, 0f, 0f));
        if (MultiplayerState.IsMultiplayer)
            SetupPlayer(1, new Vector3(1f, 0f, 0f));
        if (isOnline)
        {
            if (OnlinePlayerSync.Instance == null)
            {
                GameObject syncObj = new GameObject("OnlinePlayerSync");
                syncObj.AddComponent<OnlinePlayerSync>();
            }
            if (isHost)
                SetupPlayer(1, new Vector3(1f, 0f, 0f), true);
            else
                SetupRemotePlayerGhost();

            if (isHost)
            {
                new GameObject("GameStateHost").AddComponent<GameStateHost>();
            }
            else
            {
                new GameObject("GameStateGuest").AddComponent<GameStateGuest>();
            }
        }
        SetupRocks();
        SetupSpawner();
        SetupGameOverScreen();
        SetupPauseMenu();
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

    private void SetupRemotePlayerGhost()
    {
        GameObject ghost = new GameObject("RemotePlayerGhost");
        ghost.transform.localScale = new Vector3(playerSize, playerSize, 1f);

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        PlayerSkinVisuals.Apply(sr, 0, "", playerMaterial, 0.75f);
        sr.sortingOrder = 5;

        ghost.AddComponent<RemotePlayerGhost>();
    }

    private PlayerController SetupPlayer(int index, Vector3 spawnOffset, bool externalInput = false)
    {
        if (index == 0 && (PlayerController.main != null || FindObjectOfType<PlayerController>() != null))
            return PlayerController.main;

        GameObject player = new GameObject(index == 0 ? "Player" : "Player2");
        player.transform.position = spawnOffset;
        player.transform.localScale = new Vector3(playerSize, playerSize, 1f);

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 6;

        if (index == 0)
        {
            PlayerSkinVisuals.ApplyEquipped(renderer, playerMaterial);
        }
        else
        {
            renderer.sprite = SimpleSprite.Square;
            renderer.color = new Color(1f, 0.65f, 0.1f, 1f); // orange for P2
            if (playerMaterial != null)
            {
                renderer.sharedMaterial = playerMaterial;
                renderer.color = new Color(1f, 0.65f, 0.1f, 1f);
            }
        }

        Health health = player.AddComponent<Health>();
        health.hp = index == 0 ? PlayerLoadout.MaxHP : 10f;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.playerIndex = index;
        pc.SetExternalInputEnabled(externalInput);

        player.AddComponent<PlayerPointer>();
        return pc;
    }

    private void SetupGameOverScreen()
    {
        GameObject obj = new GameObject("GameOverScreen");
        obj.AddComponent<GameOverScreen>();
    }

    private void SetupPauseMenu()
    {
        GameObject obj = new GameObject("PauseMenu");
        obj.AddComponent<PauseMenu>();
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
        enemySpawner.giantEnemySize = giantEnemySize;
        enemySpawner.giantEnemyHealth = giantEnemyHealth;
        enemySpawner.giantEnemyAttackRange = giantEnemyAttackRange;
        enemySpawner.mapSize = mapSize;
        enemySpawner.spawnPadding = Mathf.Max(0.5f, borderInset);
        enemySpawner.meleeEnemyMaterial = meleeEnemyMaterial;
        enemySpawner.rangedEnemyMaterial = rangedEnemyMaterial;
        enemySpawner.giantEnemyMaterial = giantEnemyMaterial;
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
        var rng = new System.Random(RockLayoutSeed);
        var placed = new System.Collections.Generic.List<Vector3>(targetCount);
        foreach (Transform existing in parent)
            placed.Add(new Vector3(existing.position.x, existing.position.y, existing.localScale.x));

        for (int i = 0; i < maxAttempts && spawned < targetCount; i++)
        {
            Vector2 pos = new Vector2(
                Range(rng, -usableHalfWidth, usableHalfWidth),
                Range(rng, -usableHalfHeight, usableHalfHeight));

            if (pos.sqrMagnitude < centerSafeRadius * centerSafeRadius)
            {
                continue;
            }

            float edgeX = usableHalfWidth > 0.001f ? Mathf.Abs(pos.x) / usableHalfWidth : 1f;
            float edgeY = usableHalfHeight > 0.001f ? Mathf.Abs(pos.y) / usableHalfHeight : 1f;
            float edge01 = Mathf.Clamp01(Mathf.Max(edgeX, edgeY));
            float chance = Mathf.Lerp(centerRockChance, edgeRockChance, edge01 * edge01);
            if (rng.NextDouble() > chance)
            {
                continue;
            }

            float size = GetRockSize(rockBaseSize, rng);
            if (OverlapsPlacedRock(placed, pos, size))
            {
                continue;
            }

            CreateRock(parent, pos, size, "Rock");
            placed.Add(new Vector3(pos.x, pos.y, size));
            spawned++;
        }
    }

    private static float Range(System.Random rng, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)rng.NextDouble());
    }

    private static bool OverlapsPlacedRock(System.Collections.Generic.List<Vector3> placed, Vector2 pos, float size)
    {
        float radius = size * 0.55f;
        foreach (Vector3 rock in placed)
        {
            float otherRadius = rock.z * 0.55f;
            float minDistance = radius + otherRadius;
            Vector2 other = new Vector2(rock.x, rock.y);
            if ((pos - other).sqrMagnitude < minDistance * minDistance)
            {
                return true;
            }
        }

        return false;
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
        return GetRockSize(baseSize, null);
    }

    private float GetRockSize(float baseSize, System.Random rng)
    {
        float jitter = Mathf.Max(0f, rockSizeJitter);
        float scale = rng != null
            ? Range(rng, 1f - jitter, 1f + jitter)
            : Random.Range(1f - jitter, 1f + jitter);
        return Mathf.Max(0.2f, baseSize * scale);
    }

    public void CreateRock(Transform parent, Vector2 position, float size, string baseName)
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

    public Sprite ResolveRockSprite()
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

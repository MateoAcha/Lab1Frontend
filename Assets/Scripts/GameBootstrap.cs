using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private const int RockLayoutSeed = 834927;

    public float playerSize = 1f;
    public float meleeEnemySize = 0.8f;
    public float rangedEnemySize = 0.75f;
    [Header("Camera")]
    public float cameraOrthographicSize = 8f;
    [Header("Giant Enemy")]
    public float giantEnemySize = 2.8f;
    public Material giantEnemyMaterial;
    public float giantEnemyHealth = 40f;
    public float giantEnemyAttackRange = 14f;
    [Header("Map")]
    public Vector2 mapSize = new Vector2(40f, 40f);
    [HideInInspector]
    public Texture2D floorTexture;
    [HideInInspector]
    public Color floorColor = new Color(1f, 0.7853262f, 0.1273585f, 1f);
    public GameMapDefinition[] maps = GameMapSelection.CreateDefaultMapDefinitions();
    [Header("Exit")]
    public Texture2D exitTexture;
    public Color exitColor = new Color(0.35f, 0.95f, 1f, 1f);
    public float exitSize = 1.4f;
    public float exitTextureSize = 1.4f;
    public int exitCount = 4;
    [Header("Materials")]
    public Material backgroundMaterial;
    public Material playerMaterial;
    public Material meleeEnemyMaterial;
    public Material rangedEnemyMaterial;
    public Material enemyProjectileMaterial;
    public SkinVisualDatabase skinVisualDatabase;
    public WeaponVisualDatabase weaponVisualDatabase;
    [Header("Sound Effects")]
    public AudioClip giantAttackStompSound;
    public AudioClip menuButtonClickSound;
    public AudioClip explosionSpecialAttackSound;
    public AudioClip swordThrowSound;
    public AudioClip spearThrowSound;
    public AudioClip minionSpawnSound;
    public AudioClip gravityBombSound;
    public AudioClip exitPortalSound;
    public AudioClip genericPowerSound;
    public AudioClip fireTrailSound;
    [Header("Consumable UI")]
    public Sprite healthConsumableIcon;
    public Texture2D healthConsumableIconTexture;
    public Sprite speedConsumableIcon;
    public Texture2D speedConsumableIconTexture;
    [Header("Enemy Attack Orb Visual")]
    public Sprite[] enemyAttackOrbSprites;
    public Texture2D enemyAttackOrbTexture;
    [Min(1)] public int enemyAttackOrbFrameCount = 3;
    [Min(0.01f)] public float enemyAttackOrbSize = 0.25f;
    [Min(0.01f)] public float enemyAttackOrbFps = 10f;
    [Header("Sword Visual")]
    public Sprite swordSwingSprite;
    public Texture2D swordSwingTexture;
    public Vector2 swordSwingVisualOffset = Vector2.zero;
    public Vector2 swordSwingVisualScale = Vector2.one;
    public float swordSwingVisualRotationOffset;
    public float swordSwingDurationMultiplier = 1.5f;
    [Header("Carried Sword Visual")]
    public Vector2 carriedSwordVisualOffset = new Vector2(-0.18f, 0.18f);
    public Vector2 carriedSwordVisualScale = Vector2.one;
    public float carriedSwordVisualRotationOffset = -35f;
    public int carriedSwordSortingOrderOffset = -1;
    [Header("Spear Visual")]
    public Sprite spearSprite;
    public Texture2D spearTexture;
    public Vector2 spearVisualOffset = Vector2.zero;
    public Vector2 spearVisualScale = Vector2.one;
    public float spearVisualRotationOffset;
    public float spearThrustDistance = 0.35f;
    [Header("Carried Spear Visual")]
    public Vector2 carriedSpearVisualOffset = new Vector2(-0.18f, 0.12f);
    public Vector2 carriedSpearVisualScale = Vector2.one;
    public float carriedSpearVisualRotationOffset = -35f;
    public int carriedSpearSortingOrderOffset = 1;
    [Header("Carried Ranged Orb Visual")]
    public Vector2 carriedRangedOrbOffset = new Vector2(0.18f, 0.08f);
    public Vector2 carriedRangedOrbScale = Vector2.one;
    public int carriedRangedOrbSortingOrderOffset = 1;
    [Header("Minion Visual")]
    public Sprite[] minionMoveSprites;
    public Texture2D minionMoveTexture;
    public string minionMoveResource = "Sprites/Minion";
    public float minionSpriteScale = 1f;
    public float minionMoveFps = 8f;
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
    private Sprite generatedMapRockSprite;
    private Texture2D generatedMapRockTexture;
    private Sprite generatedExitSprite;
    private Sprite generatedFloorSprite;
    private Material activeFloorMaterial;
    private Material activeObstacleMaterial;
    private int appliedMapIndex = -1;

    private void Start()
    {
        bool isMultiplayer = MultiplayerState.IsMultiplayer;
        bool isOnline      = MultiplayerState.IsOnline;
        bool isHost        = MultiplayerState.IsHost;
        int onlineRoom     = MultiplayerState.OnlineRoomNumber;
        MultiplayerState.Reset();
        MultiplayerState.SetMultiplayer(isMultiplayer || isOnline);
        if (isOnline) { MultiplayerState.SetOnline(true); MultiplayerState.SetHost(isHost); MultiplayerState.SetOnlineRoomNumber(onlineRoom); }
        if (isOnline)
            OnlineMatchStartGate.Show(isHost ? "Waiting for guest..." : "Syncing online match...");
        else
            OnlineMatchStartGate.Reset();
        SkinVisualDatabase.Register(skinVisualDatabase);
        WeaponVisualDatabase.Register(weaponVisualDatabase);
        ApplySelectedMapDefinition(false);
        SetupCamera();
        SetupPlayer(0, new Vector3(-1f, 0f, 0f));
        if (MultiplayerState.IsMultiplayer && !isOnline)
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
        SetupExits();
        SetupSpawner();
        SetupGameOverScreen();
        SetupPauseMenu();
        if (isOnline)
            GameAudio.StopMusic();
        else
            GameAudio.EnsureMusic();
        GameAudio.ConfigureSoundEffects(
            giantAttackStompSound,
            menuButtonClickSound,
            explosionSpecialAttackSound,
            swordThrowSound,
            spearThrowSound,
            minionSpawnSound,
            gravityBombSound,
            exitPortalSound,
            genericPowerSound,
            fireTrailSound);
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
        cam.orthographicSize = Mathf.Max(1f, cameraOrthographicSize);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = floorColor;
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
        PlayerSkinVisuals.Apply(sr, 0, "", playerMaterial);
        sr.sortingOrder = 5;
        ghost.AddComponent<PlayerAnimator>();

        Health health = ghost.AddComponent<Health>();
        health.hp = 10f;
        health.maxHp = 10f;
        ghost.AddComponent<PlayerPointer>();
        ghost.AddComponent<RemotePlayerGhost>();
    }

    private PlayerController SetupPlayer(int index, Vector3 spawnOffset, bool externalInput = false)
    {
        if (index == 0 && (PlayerController.main != null || FindObjectOfType<PlayerController>() != null))
            return PlayerController.main;

        GameObject player = new GameObject(index == 0 ? "Player" : "Player2");
        player.transform.position = spawnOffset;
        player.transform.localScale = new Vector3(playerSize, playerSize, 1f);

        // Child "Sprite" holds the SpriteRenderer so its scale can be adjusted
        // independently from the parent's BoxCollider2D (hitbox).
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(player.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localRotation = Quaternion.identity;
        spriteObj.transform.localScale    = Vector3.one;

        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 6;

        if (index == 0)
        {
            PlayerSkinVisuals.ApplyEquipped(renderer, playerMaterial);
        }
        else
        {
            PlayerSkinVisuals.Apply(renderer, 0, playerMaterial);
            if (playerMaterial != null)
            {
                renderer.sharedMaterial = playerMaterial;
            }
        }
        spriteObj.AddComponent<PlayerAnimator>(); // walk/idle animation; adjust spriteScale to resize visuals

        Health health = player.AddComponent<Health>();
        health.hp = index == 0 ? PlayerLoadout.MaxHP : 10f;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.playerIndex = index;
        pc.SetExternalInputEnabled(externalInput);
        ApplySwordVisualSettings(pc);

        player.AddComponent<PlayerPointer>();
        return pc;
    }

    private void ApplySwordVisualSettings(PlayerController player)
    {
        if (player == null)
            return;

        player.swordSwingSprite = swordSwingSprite;
        player.swordSwingTexture = swordSwingTexture;
        player.swordSwingVisualOffset = swordSwingVisualOffset;
        player.swordSwingVisualScale = swordSwingVisualScale;
        player.swordSwingVisualRotationOffset = swordSwingVisualRotationOffset;
        player.swordSwingDurationMultiplier = swordSwingDurationMultiplier;
        player.carriedSwordVisualOffset = carriedSwordVisualOffset;
        player.carriedSwordVisualScale = carriedSwordVisualScale;
        player.carriedSwordVisualRotationOffset = carriedSwordVisualRotationOffset;
        player.carriedSwordSortingOrderOffset = carriedSwordSortingOrderOffset;
        player.spearSprite = spearSprite;
        player.spearTexture = spearTexture;
        player.spearVisualOffset = spearVisualOffset;
        player.spearVisualScale = spearVisualScale;
        player.spearVisualRotationOffset = spearVisualRotationOffset;
        player.spearThrustDistance = spearThrustDistance;
        player.carriedSpearVisualOffset = carriedSpearVisualOffset;
        player.carriedSpearVisualScale = carriedSpearVisualScale;
        player.carriedSpearVisualRotationOffset = carriedSpearVisualRotationOffset;
        player.carriedSpearSortingOrderOffset = carriedSpearSortingOrderOffset;
        player.carriedRangedOrbOffset = carriedRangedOrbOffset;
        player.carriedRangedOrbScale = carriedRangedOrbScale;
        player.carriedRangedOrbSortingOrderOffset = carriedRangedOrbSortingOrderOffset;
        player.healthConsumableIcon = healthConsumableIcon;
        player.healthConsumableIconTexture = healthConsumableIconTexture;
        player.speedConsumableIcon = speedConsumableIcon;
        player.speedConsumableIconTexture = speedConsumableIconTexture;
        player.minionMoveSprites = minionMoveSprites;
        player.minionMoveTexture = minionMoveTexture;
        player.minionMoveResource = minionMoveResource;
        player.minionSpriteScale = minionSpriteScale;
        player.minionMoveFps = minionMoveFps;
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
        GameMapDefinition selectedMap = GetSelectedMapDefinition();
        if (selectedMap != null)
        {
            enemySpawner.every = Mathf.Max(0.1f, selectedMap.enemySpawnInterval);
            enemySpawner.maxEnemies = Mathf.Max(1, selectedMap.maxEnemies);
            enemySpawner.spawnRules = selectedMap.enemySpawnRules;
            enemySpawner.giantMinuteSpawns = selectedMap.giantMinuteSpawns;
            enemySpawner.giantMinuteInterval = Mathf.Max(1f, selectedMap.giantMinuteIntervalSeconds);
        }
        enemySpawner.spawnPadding = Mathf.Max(0.5f, borderInset);
        enemySpawner.meleeEnemyMaterial = meleeEnemyMaterial;
        enemySpawner.rangedEnemyMaterial = rangedEnemyMaterial;
        enemySpawner.giantEnemyMaterial = giantEnemyMaterial;
        enemySpawner.enemyProjectileMaterial = enemyProjectileMaterial;
        enemySpawner.enemyAttackOrbSprites = enemyAttackOrbSprites;
        enemySpawner.enemyAttackOrbTexture = enemyAttackOrbTexture;
        enemySpawner.enemyAttackOrbFrameCount = enemyAttackOrbFrameCount;
        enemySpawner.enemyAttackOrbSize = enemyAttackOrbSize;
        enemySpawner.enemyAttackOrbFps = enemyAttackOrbFps;
    }

    private void SetupExits()
    {
        const string exitsRootName = "RuntimeExits";

        GameObject exitsRoot = GameObject.Find(exitsRootName);
        if (exitsRoot == null)
        {
            exitsRoot = new GameObject(exitsRootName);
        }
        else if (exitsRoot.transform.childCount > 0)
        {
            return;
        }

        int count = Mathf.Max(1, exitCount);
        var placed = new System.Collections.Generic.List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            Vector2 point = GetExitSpawnPoint(placed);
            CreateExit(exitsRoot.transform, point, i);
            placed.Add(point);
        }
    }

    private void CreateExit(Transform parent, Vector2 point, int index)
    {
        GameObject exit = new GameObject("MatchExit_" + index);
        exit.transform.SetParent(parent, false);
        exit.transform.position = point;
        exit.transform.localScale = Vector3.one;

        CircleCollider2D collider = exit.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = Mathf.Max(0.1f, exitSize * 0.5f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(exit.transform, false);
        visual.transform.localPosition = Vector3.zero;
        float visualSize = Mathf.Max(0.05f, exitTextureSize);
        visual.transform.localScale = new Vector3(visualSize, visualSize, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = ResolveExitSprite();
        renderer.color = exitColor;
        renderer.sortingOrder = 3;

        exit.AddComponent<MatchExit>();
    }

    private Vector2 GetExitSpawnPoint()
    {
        return GetExitSpawnPoint(null);
    }

    private Vector2 GetExitSpawnPoint(System.Collections.Generic.List<Vector2> placed)
    {
        float width = Mathf.Max(1f, mapSize.x);
        float height = Mathf.Max(1f, mapSize.y);
        float margin = Mathf.Max(0.5f, borderInset + exitSize * 0.5f);
        float minX = -width * 0.5f + margin;
        float maxX = width * 0.5f - margin;
        float minY = -height * 0.5f + margin;
        float maxY = height * 0.5f - margin;

        for (int i = 0; i < 80; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (IsExitSpawnPointFree(candidate, placed))
            {
                return candidate;
            }
        }

        const int gridSteps = 12;
        for (int y = 0; y < gridSteps; y++)
        {
            for (int x = 0; x < gridSteps; x++)
            {
                Vector2 candidate = new Vector2(
                    Mathf.Lerp(minX, maxX, (x + 0.5f) / gridSteps),
                    Mathf.Lerp(minY, maxY, (y + 0.5f) / gridSteps));
                if (IsExitSpawnPointFree(candidate, placed))
                {
                    return candidate;
                }
            }
        }

        return Vector2.zero;
    }

    private bool IsExitSpawnPointFree(Vector2 point, System.Collections.Generic.List<Vector2> placed = null)
    {
        float radius = Mathf.Max(0.25f, exitSize * 0.55f);
        if (placed != null)
        {
            float minExitDistance = Mathf.Max(exitSize * 1.6f, exitTextureSize * 0.8f);
            for (int i = 0; i < placed.Count; i++)
            {
                if ((point - placed[i]).sqrMagnitude < minExitDistance * minExitDistance)
                {
                    return false;
                }
            }
        }

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(point, radius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D col = overlaps[i];
            if (col == null || col.isTrigger)
            {
                continue;
            }

            return false;
        }

        return true;
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

        Material floorMaterial = ResolveFloorMaterial();
        renderer.sprite = floorMaterial != null ? SimpleSprite.Square : ResolveFloorSprite();
        renderer.sortingOrder = -100;
        renderer.sharedMaterial = floorMaterial;
        renderer.color = floorMaterial != null ? Color.white : floorColor;
    }

    public void ApplyMapSelection(int mapIndex, bool refreshRuntime)
    {
        GameMapSelection.Select(mapIndex);
        ApplySelectedMapDefinition(refreshRuntime);
    }

    private void ApplySelectedMapDefinition(bool refreshRuntime)
    {
        GameMapDefinition selectedMap = GetSelectedMapDefinition();
        if (selectedMap == null)
        {
            return;
        }

        int selectedIndex = Mathf.Clamp(GameMapSelection.SelectedMapIndex, 0, maps.Length - 1);
        if (refreshRuntime && selectedIndex == appliedMapIndex)
        {
            return;
        }

        appliedMapIndex = selectedIndex;
        floorTexture = selectedMap.floorTexture;
        generatedFloorSprite = null;
        generatedMapRockSprite = null;
        generatedMapRockTexture = null;
        floorColor = selectedMap.floorColor;
        rockColor = selectedMap.obstacleColor;
        activeFloorMaterial = selectedMap.floorMaterial != null ? selectedMap.floorMaterial : ResolveDefaultFloorMaterial();
        activeObstacleMaterial = selectedMap.obstacleMaterial != null ? selectedMap.obstacleMaterial : ResolveDefaultObstacleMaterial();

        if (!refreshRuntime)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = floorColor;
            SetupBackground(cam);
        }

        GameObject rocksRoot = GameObject.Find("RuntimeRocks");
        if (rocksRoot != null)
        {
            rocksRoot.name = "RuntimeRocks_Old";
            rocksRoot.SetActive(false);
            Destroy(rocksRoot);
            SetupRocks();
        }

        GameObject exitsRoot = GameObject.Find("RuntimeExits");
        if (exitsRoot != null)
        {
            exitsRoot.name = "RuntimeExits_Old";
            exitsRoot.SetActive(false);
            Destroy(exitsRoot);
            SetupExits();
        }

        SetupSpawner();
    }

    private GameMapDefinition GetSelectedMapDefinition()
    {
        if (maps == null || maps.Length == 0)
        {
            maps = GameMapSelection.CreateDefaultMapDefinitions();
        }

        int index = Mathf.Clamp(GameMapSelection.SelectedMapIndex, 0, maps.Length - 1);
        return maps[index];
    }

    public MapMaterialDefinition GetSelectedMapMaterialDrop()
    {
        GameMapDefinition selectedMap = GetSelectedMapDefinition();
        if (selectedMap == null)
        {
            return null;
        }

        if (selectedMap.materialDrop == null)
        {
            string mapName = string.IsNullOrWhiteSpace(selectedMap.mapName) ? "Map Material" : selectedMap.mapName;
            selectedMap.materialDrop = new MapMaterialDefinition
            {
                inventoryKey = mapName.ToLowerInvariant().Replace(" ", "_") + "_material",
                itemName = mapName + " Material"
            };
        }

        return selectedMap.materialDrop;
    }

    private Material ResolveFloorMaterial()
    {
        if (activeFloorMaterial != null)
        {
            return activeFloorMaterial;
        }

        return ResolveDefaultFloorMaterial();
    }

    private Material ResolveObstacleMaterial()
    {
        if (activeObstacleMaterial != null)
        {
            return activeObstacleMaterial;
        }

        return ResolveDefaultObstacleMaterial();
    }

    private Material ResolveDefaultFloorMaterial()
    {
        if (maps != null && maps.Length > 0 && maps[0] != null && maps[0].floorMaterial != null)
        {
            return maps[0].floorMaterial;
        }

        return backgroundMaterial;
    }

    private Material ResolveDefaultObstacleMaterial()
    {
        if (maps != null && maps.Length > 0 && maps[0] != null && maps[0].obstacleMaterial != null)
        {
            return maps[0].obstacleMaterial;
        }

        return rockMaterial;
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
        Material obstacleMaterial = ResolveObstacleMaterial();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        renderer.sharedMaterial = obstacleMaterial;
        renderer.color = obstacleMaterial != null ? Color.white : rockColor;

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
        GameMapDefinition selectedMap = GetSelectedMapDefinition();
        if (selectedMap != null)
        {
            if (selectedMap.obstacleSprite != null)
            {
                return selectedMap.obstacleSprite;
            }

            if (selectedMap.obstacleTexture != null &&
                selectedMap.obstacleTexture.width > 0 &&
                selectedMap.obstacleTexture.height > 0)
            {
                if (generatedMapRockSprite == null || generatedMapRockTexture != selectedMap.obstacleTexture)
                {
                    generatedMapRockTexture = selectedMap.obstacleTexture;
                    generatedMapRockSprite = Sprite.Create(
                        selectedMap.obstacleTexture,
                        new Rect(0f, 0f, selectedMap.obstacleTexture.width, selectedMap.obstacleTexture.height),
                        new Vector2(0.5f, 0.5f),
                        selectedMap.obstacleTexture.width,
                        0,
                        SpriteMeshType.Tight);
                }

                return generatedMapRockSprite;
            }
        }

        if (rockSprite != null)
        {
            return rockSprite;
        }

        Material obstacleMaterial = ResolveObstacleMaterial();
        if (obstacleMaterial != null && obstacleMaterial.HasProperty("_OverlayTex"))
        {
            Texture overlayTexture = obstacleMaterial.GetTexture("_OverlayTex");
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

    private Sprite ResolveExitSprite()
    {
        if (exitTexture != null)
        {
            if (generatedExitSprite == null || generatedExitSprite.texture != exitTexture)
            {
                generatedExitSprite = Sprite.Create(
                    exitTexture,
                    new Rect(0f, 0f, exitTexture.width, exitTexture.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(exitTexture.width, exitTexture.height),
                    0,
                    SpriteMeshType.Tight);
            }

            return generatedExitSprite;
        }

        return SimpleSprite.Circle;
    }

    private Sprite ResolveFloorSprite()
    {
        if (floorTexture != null)
        {
            if (generatedFloorSprite == null || generatedFloorSprite.texture != floorTexture)
            {
                generatedFloorSprite = Sprite.Create(
                    floorTexture,
                    new Rect(0f, 0f, floorTexture.width, floorTexture.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(floorTexture.width, floorTexture.height),
                    0,
                    SpriteMeshType.FullRect);
            }

            return generatedFloorSprite;
        }

        return SimpleSprite.Square;
    }
}

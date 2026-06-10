using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float every = 1.5f;
    public float radius = 7f;
    public int maxEnemies = 12;
    public Vector2 mapSize = new Vector2(40f, 40f);
    public float spawnPadding = 0.5f;
    public int spawnAttempts = 16;
    public float meleeEnemySize = 5f;
    public float rangedEnemySize = 2f;
    public float giantEnemySize = 2.8f;
    public float giantEnemyHealth = 40f;
    public float giantEnemyAttackRange = 14f;
    public bool giantMinuteSpawns = true;
    public float giantMinuteInterval = 60f;
    public MapEnemySpawnRule[] spawnRules;
    public Material meleeEnemyMaterial;
    public Material rangedEnemyMaterial;
    public Material giantEnemyMaterial;
    public Material enemyProjectileMaterial;
    [SerializeField] private Color meleeEnemyColor = new Color(0.25f, 1f, 0.25f, 1f);
    [SerializeField] private Color rangedEnemyColor = new Color(1f, 0.65f, 0.2f, 1f);
    [SerializeField] private Color giantEnemyColor = new Color(0.45f, 0.2f, 0.75f, 1f);

    public static float ElapsedTime { get; private set; }

    public static void SetNetworkElapsedTime(float elapsedTime)
    {
        ElapsedTime = Mathf.Max(0f, elapsedTime);
    }

    private float nextSpawn;
    private float startAt;
    private int nextGiantMinute = 1;

    private void Start()
    {
        startAt = Time.time;
        ElapsedTime = 0f;
        nextSpawn = Time.time + every;
        nextGiantMinute = 1;
        GameStatsTracker.StartMatch();
    }

    private void Update()
    {
        ElapsedTime = Time.time - startAt;

        if (MultiplayerState.GetNearestPlayer(Vector3.zero) == null)
        {
            return;
        }

        SpawnGiantsAtMinuteMarks();

        if (Time.time < nextSpawn)
        {
            return;
        }

        int totalEnemies = FindObjectsOfType<EnemyController>().Length +
            FindObjectsOfType<RangedEnemyController>().Length +
            FindObjectsOfType<GiantEnemyController>().Length;
        if (totalEnemies >= maxEnemies)
        {
            nextSpawn = Time.time + every;
            return;
        }

        Spawn();
        nextSpawn = Time.time + every;
    }

    private void Spawn()
    {
        MapEnemyType enemyType = ChooseEnemyType();
        float size = GetEnemySize(enemyType);

        if (!TryGetSpawnPoint(size, out Vector2 point, enemyType == MapEnemyType.Giant))
        {
            return;
        }

        if (enemyType == MapEnemyType.Ranged)
        {
            SpawnRanged(point);
            return;
        }

        if (enemyType == MapEnemyType.Giant)
        {
            SpawnGiant(point);
            return;
        }

        SpawnMelee(point);
    }

    private bool TryGetSpawnPoint(float enemySize, out Vector2 point, bool ignoreRocks = false)
    {
        point = Vector2.zero;
        Transform nearestPlayer = MultiplayerState.GetNearestPlayer(Vector3.zero);
        if (nearestPlayer == null)
        {
            return false;
        }

        Vector2 playerPos = nearestPlayer.position;
        float halfWidth = Mathf.Max(1f, mapSize.x) * 0.5f;
        float halfHeight = Mathf.Max(1f, mapSize.y) * 0.5f;
        float margin = Mathf.Max(0.2f, spawnPadding + enemySize * 0.5f);

        float minX = -halfWidth + margin;
        float maxX = halfWidth - margin;
        float minY = -halfHeight + margin;
        float maxY = halfHeight - margin;
        if (minX > maxX || minY > maxY)
        {
            return false;
        }

        int attempts = Mathf.Max(1, spawnAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector2.right;
            }
            else
            {
                dir.Normalize();
            }

            Vector2 candidate = playerPos + dir * radius;
            candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
            candidate.y = Mathf.Clamp(candidate.y, minY, maxY);
            if (IsSpawnPointFree(candidate, enemySize, ignoreRocks))
            {
                point = candidate;
                return true;
            }
        }

        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (IsSpawnPointFree(candidate, enemySize, ignoreRocks))
            {
                point = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsSpawnPointFree(Vector2 point, float enemySize, bool ignoreRocks = false)
    {
        float checkRadius = Mathf.Max(0.2f, enemySize * 0.45f);
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(point, checkRadius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D col = overlaps[i];
            if (col == null || col.isTrigger)
            {
                continue;
            }

            if (ignoreRocks && IsRockCollider(col))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private MapEnemyType ChooseEnemyType()
    {
        if (spawnRules == null || spawnRules.Length == 0)
        {
            return GetFallbackEnemyType();
        }

        float totalWeight = 0f;
        for (int i = 0; i < spawnRules.Length; i++)
        {
            MapEnemySpawnRule rule = spawnRules[i];
            if (rule == null || !rule.enabled || rule.spawnWeight <= 0f || ElapsedTime < rule.startsAfterSeconds)
            {
                continue;
            }

            totalWeight += rule.spawnWeight;
        }

        if (totalWeight <= 0f)
        {
            return MapEnemyType.Melee;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < spawnRules.Length; i++)
        {
            MapEnemySpawnRule rule = spawnRules[i];
            if (rule == null || !rule.enabled || rule.spawnWeight <= 0f || ElapsedTime < rule.startsAfterSeconds)
            {
                continue;
            }

            roll -= rule.spawnWeight;
            if (roll <= 0f)
            {
                return rule.enemyType;
            }
        }

        return MapEnemyType.Melee;
    }

    private MapEnemyType GetFallbackEnemyType()
    {
        if (ElapsedTime < 15f)
        {
            return MapEnemyType.Melee;
        }

        float ramp = Mathf.Clamp01((ElapsedTime - 15f) / 30f);
        return Random.value < 0.4f * ramp ? MapEnemyType.Ranged : MapEnemyType.Melee;
    }

    private float GetEnemySize(MapEnemyType enemyType)
    {
        if (enemyType == MapEnemyType.Ranged)
        {
            return rangedEnemySize;
        }

        if (enemyType == MapEnemyType.Giant)
        {
            return giantEnemySize;
        }

        return meleeEnemySize;
    }

    private void SpawnMelee(Vector2 point)
    {
        GameObject enemy = new GameObject("EnemyMelee");
        enemy.transform.position = point;
        enemy.transform.localScale = new Vector3(meleeEnemySize, meleeEnemySize, 1f);

        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(enemy.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale    = Vector3.one;

        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
        renderer.sprite       = SimpleSprite.Square;
        renderer.color        = Color.white;
        renderer.sortingOrder = 5;
        if (meleeEnemyMaterial != null)
            renderer.sharedMaterial = meleeEnemyMaterial;

        Health health = enemy.AddComponent<Health>();
        health.hp = 2;

        enemy.AddComponent<EnemyController>();
        spriteObj.AddComponent<MeleeEnemyAnimator>();
    }

    private void SpawnRanged(Vector2 point)
    {
        GameObject enemy = new GameObject("EnemyRanged");
        enemy.transform.position = point;
        enemy.transform.localScale = new Vector3(rangedEnemySize, rangedEnemySize, 1f);

        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(enemy.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale    = Vector3.one;

        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
        renderer.sprite       = SimpleSprite.Square;
        renderer.color        = Color.white;
        renderer.sortingOrder = 5;
        if (rangedEnemyMaterial != null)
            renderer.sharedMaterial = rangedEnemyMaterial;

        Health health = enemy.AddComponent<Health>();
        health.hp = 2;

        RangedEnemyController rangedEnemy = enemy.AddComponent<RangedEnemyController>();
        rangedEnemy.projectileMaterial = enemyProjectileMaterial;
        spriteObj.AddComponent<RangedEnemyAnimator>();
    }

    private void SpawnGiantsAtMinuteMarks()
    {
        if (!giantMinuteSpawns)
        {
            return;
        }

        float interval = Mathf.Max(1f, giantMinuteInterval);
        while (ElapsedTime >= nextGiantMinute * interval)
        {
            if (TryGetSpawnPoint(giantEnemySize, out Vector2 point, true))
            {
                SpawnGiant(point);
            }

            nextGiantMinute++;
        }
    }

    private void SpawnGiant(Vector2 point)
    {
        GameObject enemy = new GameObject("EnemyGiant");
        enemy.transform.position = point;
        enemy.transform.localScale = new Vector3(giantEnemySize, giantEnemySize, 1f);

        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(enemy.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale    = Vector3.one;

        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
        renderer.sprite       = SimpleSprite.Square;
        renderer.color        = Color.white;
        renderer.sortingOrder = 5;
        if (giantEnemyMaterial != null)
            renderer.sharedMaterial = giantEnemyMaterial;

        Health health = enemy.AddComponent<Health>();
        health.hp = Mathf.Max(1f, giantEnemyHealth);

        GiantEnemyController giant = enemy.AddComponent<GiantEnemyController>();
        giant.maxHealth   = Mathf.Max(1f, giantEnemyHealth);
        giant.attackRange = Mathf.Max(0.1f, giantEnemyAttackRange);
        spriteObj.AddComponent<GiantEnemyAnimator>();
    }

    private bool IsRockCollider(Collider2D col)
    {
        Transform current = col.transform;
        while (current != null)
        {
            if (current.name == "RuntimeRocks")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}

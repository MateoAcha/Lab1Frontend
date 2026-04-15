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
    public Material meleeEnemyMaterial;
    public Material rangedEnemyMaterial;
    public Material enemyProjectileMaterial;
    [SerializeField] private Color meleeEnemyColor = new Color(0.25f, 1f, 0.25f, 1f);
    [SerializeField] private Color rangedEnemyColor = new Color(1f, 0.65f, 0.2f, 1f);

    public static float ElapsedTime { get; private set; }

    private float nextSpawn;
    private float startAt;

    private void Start()
    {
        startAt = Time.time;
        ElapsedTime = 0f;
        GameStatsTracker.StartMatch();
    }

    private void Update()
    {
        ElapsedTime = Time.time - startAt;

        if (PlayerController.main == null)
        {
            return;
        }

        if (Time.time < nextSpawn)
        {
            return;
        }

        int totalEnemies = FindObjectsOfType<EnemyController>().Length + FindObjectsOfType<RangedEnemyController>().Length;
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
        float rangedChance = GetRangedChance(ElapsedTime);
        bool shouldSpawnRanged = Random.value < rangedChance;
        float size = shouldSpawnRanged ? rangedEnemySize : meleeEnemySize;

        if (!TryGetSpawnPoint(size, out Vector2 point))
        {
            return;
        }

        if (shouldSpawnRanged)
        {
            SpawnRanged(point);
            return;
        }

        SpawnMelee(point);
    }

    private bool TryGetSpawnPoint(float enemySize, out Vector2 point)
    {
        point = Vector2.zero;
        if (PlayerController.main == null)
        {
            return false;
        }

        Vector2 playerPos = PlayerController.main.transform.position;
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
            if (IsSpawnPointFree(candidate, enemySize))
            {
                point = candidate;
                return true;
            }
        }

        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (IsSpawnPointFree(candidate, enemySize))
            {
                point = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsSpawnPointFree(Vector2 point, float enemySize)
    {
        float checkRadius = Mathf.Max(0.2f, enemySize * 0.45f);
        return Physics2D.OverlapCircle(point, checkRadius) == null;
    }

    private float GetRangedChance(float timeSinceStart)
    {
        if (timeSinceStart < 5f)
        {
            return 0f;
        }

        float ramp = Mathf.Clamp01((timeSinceStart - 5f) / 30f);
        return 0.4f * ramp;
    }

    private void SpawnMelee(Vector2 point)
    {
        GameObject enemy = new GameObject("EnemyMelee");
        enemy.transform.position = point;
        enemy.transform.localScale = new Vector3(meleeEnemySize, meleeEnemySize, 1f);

        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = meleeEnemyColor;
        renderer.sortingOrder = 5;
        if (meleeEnemyMaterial != null)
        {
            renderer.sharedMaterial = meleeEnemyMaterial;
            renderer.color = Color.white;
        }

        Health health = enemy.AddComponent<Health>();
        health.hp = 2;

        enemy.AddComponent<EnemyController>();
    }

    private void SpawnRanged(Vector2 point)
    {
        GameObject enemy = new GameObject("EnemyRanged");
        enemy.transform.position = point;
        enemy.transform.localScale = new Vector3(rangedEnemySize, rangedEnemySize, 1f);

        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = rangedEnemyColor;
        renderer.sortingOrder = 5;
        if (rangedEnemyMaterial != null)
        {
            renderer.sharedMaterial = rangedEnemyMaterial;
            renderer.color = Color.white;
        }

        Health health = enemy.AddComponent<Health>();
        health.hp = 2;

        RangedEnemyController rangedEnemy = enemy.AddComponent<RangedEnemyController>();
        rangedEnemy.projectileMaterial = enemyProjectileMaterial;
    }
}

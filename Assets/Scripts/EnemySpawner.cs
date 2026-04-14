using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float every = 1.5f;
    public float radius = 7f;
    public int maxEnemies = 12;
    public float meleeEnemySize = 5f;
    public float rangedEnemySize = 2f;

    public static float ElapsedTime { get; private set; }

    private float nextSpawn;
    private float startAt;

    private void Start()
    {
        startAt = Time.time;
        ElapsedTime = 0f;
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
        Vector2 point = (Vector2)PlayerController.main.transform.position + Random.insideUnitCircle.normalized * radius;
        if (point == (Vector2)PlayerController.main.transform.position)
        {
            point += Vector2.right * radius;
        }

        float rangedChance = GetRangedChance(ElapsedTime);
        if (Random.value < rangedChance)
        {
            SpawnRanged(point);
            return;
        }

        SpawnMelee(point);
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
        renderer.color = new Color(0.25f, 1f, 0.25f, 1f);
        renderer.sortingOrder = 5;

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
        renderer.color = new Color(1f, 0.65f, 0.2f, 1f);
        renderer.sortingOrder = 5;

        Health health = enemy.AddComponent<Health>();
        health.hp = 2;

        enemy.AddComponent<RangedEnemyController>();
    }
}

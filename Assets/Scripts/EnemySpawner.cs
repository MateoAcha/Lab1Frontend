using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float every = 1.5f;
    public float radius = 7f;
    public int maxEnemies = 12;

    private float nextSpawn;

    private void Update()
    {
        if (PlayerController.main == null)
        {
            return;
        }

        if (Time.time < nextSpawn)
        {
            return;
        }

        if (FindObjectsOfType<EnemyController>().Length >= maxEnemies)
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

        GameObject enemy = new GameObject("Enemy");
        enemy.transform.position = point;
        enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = new Color(0.25f, 1f, 0.25f, 1f);
        renderer.sortingOrder = 5;

        Health health = enemy.AddComponent<Health>();
        health.hp = 2;

        enemy.AddComponent<EnemyController>();
    }
}

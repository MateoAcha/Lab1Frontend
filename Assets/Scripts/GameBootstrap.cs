using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public float playerSize = 1f;
    public float meleeEnemySize = 0.8f;
    public float rangedEnemySize = 0.75f;

    private void Start()
    {
        SetupCamera();
        SetupPlayer();
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

        Health health = player.AddComponent<Health>();
        health.hp = 10;

        player.AddComponent<PlayerController>();
    }

    private void SetupSpawner()
    {
        if (FindObjectOfType<EnemySpawner>() != null)
        {
            return;
        }

        GameObject spawner = new GameObject("EnemySpawner");
        EnemySpawner enemySpawner = spawner.AddComponent<EnemySpawner>();
        enemySpawner.meleeEnemySize = meleeEnemySize;
        enemySpawner.rangedEnemySize = rangedEnemySize;
    }
}

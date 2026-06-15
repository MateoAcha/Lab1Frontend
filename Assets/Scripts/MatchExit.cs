using System.Collections;
using UnityEngine;

public class MatchExit : MonoBehaviour
{
    public static MatchExit Instance { get; private set; }
    public static bool HasExit => FindObjectsOfType<MatchExit>().Length > 0;
    public static Vector2 Position
    {
        get
        {
            MatchExit[] exits = FindObjectsOfType<MatchExit>();
            return exits.Length > 0 ? (Vector2)exits[0].transform.position : Vector2.zero;
        }
    }
    public static bool IsEnding { get; private set; }
    public static int EndingPlayerId { get; private set; } = -1;

    public float warpDuration = 0.7f;

    private bool triggered;

    private void Awake()
    {
        Instance = this;
        IsEnding = false;
        EndingPlayerId = -1;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsEnding = false;
            EndingPlayerId = -1;
        }
    }

    public static void SetSyncedPosition(Vector2 position, bool active)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.transform.position = new Vector3(position.x, position.y, Instance.transform.position.z);
        Instance.gameObject.SetActive(active);
    }

    public static OnlineExitState[] GetActiveStates()
    {
        MatchExit[] exits = FindObjectsOfType<MatchExit>();
        System.Array.Sort(exits, (a, b) => string.CompareOrdinal(a.name, b.name));
        OnlineExitState[] states = new OnlineExitState[exits.Length];
        for (int i = 0; i < exits.Length; i++)
        {
            states[i] = new OnlineExitState
            {
                id = i,
                active = exits[i].gameObject.activeSelf,
                x = exits[i].transform.position.x,
                y = exits[i].transform.position.y
            };
        }

        return states;
    }

    public static void SetSyncedStates(OnlineExitState[] states)
    {
        if (states == null)
        {
            return;
        }

        GameObject root = GameObject.Find("RuntimeExits");
        if (root == null)
        {
            root = new GameObject("RuntimeExits");
        }

        for (int i = 0; i < states.Length; i++)
        {
            OnlineExitState state = states[i];
            if (state == null)
            {
                continue;
            }

            GameObject exit = FindOrCreateSyncedExit(root.transform, state.id);
            exit.transform.position = new Vector3(state.x, state.y, 0f);
            exit.SetActive(state.active);
        }

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            MatchExit exit = child.GetComponent<MatchExit>();
            if (exit == null)
            {
                continue;
            }

            int id = ParseSyncedId(child.name);
            if (id >= states.Length)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static GameObject FindOrCreateSyncedExit(Transform root, int id)
    {
        string exitName = "MatchExit_" + id;
        Transform existing = root.Find(exitName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject exit = new GameObject(exitName);
        exit.transform.SetParent(root, false);

        CircleCollider2D collider = exit.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.7f;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(exit.transform, false);
        visual.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Circle;
        renderer.color = new Color(0.35f, 0.95f, 1f, 1f);
        renderer.sortingOrder = 3;

        exit.AddComponent<MatchExit>();
        return exit;
    }

    private static int ParseSyncedId(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return -1;
        }

        int underscore = objectName.LastIndexOf('_');
        if (underscore < 0 || underscore + 1 >= objectName.Length)
        {
            return -1;
        }

        if (int.TryParse(objectName.Substring(underscore + 1), out int id))
        {
            return id;
        }

        return -1;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || triggered)
        {
            return;
        }

        bool authoritative = !MultiplayerState.IsOnline || MultiplayerState.IsHost;
        if (!authoritative)
        {
            return;
        }

        triggered = true;
        EndingPlayerId = player.playerIndex;
        GameAudio.PlayExitPortal();
        StartCoroutine(ExitRoutine(player, authoritative));
    }

    private IEnumerator ExitRoutine(PlayerController player, bool authoritative)
    {
        if (authoritative)
        {
            FreezeWorld();
        }

        yield return WarpPlayer(player);

        if (authoritative)
        {
            GameStatsTracker.RegisterMatchFinished();
        }
    }

    private void FreezeWorld()
    {
        IsEnding = true;
        Time.timeScale = 0f;

        Rigidbody2D[] bodies = FindObjectsOfType<Rigidbody2D>();
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] != null)
            {
                bodies[i].linearVelocity = Vector2.zero;
                bodies[i].angularVelocity = 0f;
            }
        }
    }

    private IEnumerator WarpPlayer(PlayerController player)
    {
        if (player == null)
        {
            yield break;
        }

        player.enabled = false;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        Transform target = player.transform;
        Vector3 startScale = target.localScale;
        Vector3 startPosition = target.position;
        float duration = Mathf.Max(0.05f, warpDuration);
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            float width = Mathf.Lerp(1f, 0.08f, eased);
            float height = Mathf.Lerp(1f, 2.4f, eased);
            target.localScale = new Vector3(startScale.x * width, startScale.y * height, startScale.z);
            target.position = startPosition + Vector3.up * Mathf.Lerp(0f, 1.1f, eased);

            SetRenderersAlpha(target, Mathf.Lerp(1f, 0f, eased));

            yield return null;
        }

        if (target != null)
        {
            target.gameObject.SetActive(false);
        }
    }

    private static void SetRenderersAlpha(Transform target, float alpha)
    {
        if (target == null)
            return;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Color color = renderers[i].color;
            color.a = alpha;
            renderers[i].color = color;
        }
    }
}

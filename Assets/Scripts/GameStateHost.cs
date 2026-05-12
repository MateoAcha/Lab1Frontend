using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateHost : MonoBehaviour
{
    private const float SyncInterval = 0.05f; // 20 updates/sec

    private GameWebSocketClient _ws;

    private void Start()
    {
        StartCoroutine(ConnectThenSync());
    }

    private IEnumerator ConnectThenSync()
    {
        string wsUrl = BuildWsUrl();
        _ws = new GameWebSocketClient();

        Task connectTask = _ws.ConnectAsync(wsUrl);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsFaulted || !_ws.IsConnected)
        {
            Debug.LogWarning("GameStateHost: WebSocket connect failed – " + connectTask.Exception?.GetBaseException()?.Message);
            yield break;
        }

        yield return Send("{\"type\":\"register\",\"role\":\"host\"}");
        StartCoroutine(SyncLoop());
    }

    private IEnumerator SyncLoop()
    {
        while (_ws.IsConnected)
        {
            // Handle messages from guest (position updates, kills)
            string msg;
            while (_ws.TryReceive(out msg))
                HandleGuestMessage(msg);

            // Broadcast current game state to guest
            yield return Send(BuildStateJson());
            yield return new WaitForSeconds(SyncInterval);
        }
    }

    private void HandleGuestMessage(string json)
    {
        if (json.Contains("\"pos\""))
        {
            try
            {
                PosMsg p = JsonUtility.FromJson<PosMsg>(json);
                // Update remote ghost so enemies can target guest player
                if (OnlinePlayerSync.Instance != null)
                    OnlinePlayerSync.Instance.SetRemotePosition(new Vector3(p.x, p.y, 0f));
            }
            catch { }
        }
        else if (json.Contains("\"kill\""))
        {
            try
            {
                KillMsg k = JsonUtility.FromJson<KillMsg>(json);
                ApplyGuestKill(k.id);
            }
            catch { }
        }
    }

    private void ApplyGuestKill(int hostId)
    {
        foreach (EnemyController e in FindObjectsOfType<EnemyController>())
        {
            if (e.gameObject.GetInstanceID() == hostId)
            {
                e.GetComponent<Health>()?.Hit(9999f);
                return;
            }
        }
        foreach (RangedEnemyController r in FindObjectsOfType<RangedEnemyController>())
        {
            if (r.gameObject.GetInstanceID() == hostId)
            {
                r.GetComponent<Health>()?.Hit(9999f);
                return;
            }
        }
    }

    // ── State serialisation ───────────────────────────────────────────────────

    private string BuildStateJson()
    {
        var sb = new StringBuilder(2048);
        sb.Append("{\"type\":\"state\"");

        // Host player position (so guest can render the ghost)
        if (PlayerController.main != null)
        {
            Vector3 pos = PlayerController.main.transform.position;
            sb.Append($",\"hostX\":{pos.x:F3},\"hostY\":{pos.y:F3}");
        }

        // Rocks – always included so a late-joining guest can place them
        sb.Append(",\"rocks\":[");
        GameObject rocksRoot = GameObject.Find("RuntimeRocks");
        if (rocksRoot != null)
        {
            bool first = true;
            foreach (Transform t in rocksRoot.transform)
            {
                if (!first) sb.Append(',');
                sb.Append($"{{\"x\":{t.position.x:F3},\"y\":{t.position.y:F3},\"size\":{t.localScale.x:F3}}}");
                first = false;
            }
        }
        sb.Append(']');

        // Enemies
        sb.Append(",\"enemies\":[");
        bool fe = true;
        foreach (EnemyController e in FindObjectsOfType<EnemyController>())
        {
            Health h = e.GetComponent<Health>();
            if (!fe) sb.Append(',');
            sb.Append($"{{\"id\":{e.gameObject.GetInstanceID()},\"type\":0");
            sb.Append($",\"x\":{e.transform.position.x:F3},\"y\":{e.transform.position.y:F3}");
            sb.Append($",\"hp\":{(h != null ? h.hp : 0f):F3},\"maxHp\":{(h != null ? h.maxHp : 2f):F3}");
            sb.Append($",\"size\":{e.transform.localScale.x:F3}}}");
            fe = false;
        }
        foreach (RangedEnemyController r in FindObjectsOfType<RangedEnemyController>())
        {
            Health h = r.GetComponent<Health>();
            if (!fe) sb.Append(',');
            sb.Append($"{{\"id\":{r.gameObject.GetInstanceID()},\"type\":1");
            sb.Append($",\"x\":{r.transform.position.x:F3},\"y\":{r.transform.position.y:F3}");
            sb.Append($",\"hp\":{(h != null ? h.hp : 0f):F3},\"maxHp\":{(h != null ? h.maxHp : 2f):F3}");
            sb.Append($",\"size\":{r.transform.localScale.x:F3}}}");
            fe = false;
        }
        sb.Append(']');

        // Projectiles
        sb.Append(",\"projectiles\":[");
        bool fp = true;
        foreach (EnemyProjectile proj in FindObjectsOfType<EnemyProjectile>())
        {
            if (!fp) sb.Append(',');
            sb.Append($"{{\"id\":{proj.gameObject.GetInstanceID()}");
            sb.Append($",\"x\":{proj.transform.position.x:F3},\"y\":{proj.transform.position.y:F3}");
            sb.Append($",\"vx\":{proj.direction.x * proj.speed:F3},\"vy\":{proj.direction.y * proj.speed:F3}");
            sb.Append($",\"life\":{proj.RemainingLife:F3}}}");
            fp = false;
        }
        sb.Append("]}");

        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator Send(string json)
    {
        if (_ws == null || !_ws.IsConnected) yield break;
        Task t = _ws.SendAsync(json);
        yield return new WaitUntil(() => t.IsCompleted);
    }

    private static string BuildWsUrl()
    {
        string baseUrl = GameStatsTracker.ApiBaseUrl.TrimEnd('/');
        string wsUrl   = baseUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        wsUrl += "/game-ws";
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            wsUrl += "?token=" + Uri.EscapeDataString(AuthSession.AccessToken);
        return wsUrl;
    }

    private void OnDestroy()
    {
        _ws?.Dispose();
    }

    [Serializable] private class PosMsg  { public string type; public float x, y; }
    [Serializable] private class KillMsg { public string type; public int id; }
}

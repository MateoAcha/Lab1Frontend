using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameStateHost : MonoBehaviour
{
    private const float SyncInterval = 0.15f;

    private void Start()
    {
        StartCoroutine(SyncLoop());
    }

    private IEnumerator SyncLoop()
    {
        while (true)
        {
            yield return FetchAndApplyKills();
            yield return BroadcastState();
            yield return new WaitForSeconds(SyncInterval);
        }
    }

    // ── Fetch guest kill reports and apply them to local enemies ──────────────

    private IEnumerator FetchAndApplyKills()
    {
        string url = GameStatsTracker.ApiBaseUrl.TrimEnd('/') + "/game/kills";
        var req = UnityWebRequest.Get(url);
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            req.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success
            || req.responseCode < 200 || req.responseCode >= 300)
            yield break;

        try
        {
            KillResponse kills = JsonUtility.FromJson<KillResponse>(req.downloadHandler.text);
            if (kills?.ids != null && kills.ids.Length > 0)
                ApplyKills(kills.ids);
        }
        catch { }
    }

    private void ApplyKills(int[] ids)
    {
        var killSet = new HashSet<int>(ids);

        foreach (EnemyController e in FindObjectsOfType<EnemyController>())
            if (killSet.Contains(e.gameObject.GetInstanceID()))
                e.GetComponent<Health>()?.Hit(9999f);

        foreach (RangedEnemyController r in FindObjectsOfType<RangedEnemyController>())
            if (killSet.Contains(r.gameObject.GetInstanceID()))
                r.GetComponent<Health>()?.Hit(9999f);
    }

    // ── Serialize and broadcast the current game state ────────────────────────

    private IEnumerator BroadcastState()
    {
        string json = JsonUtility.ToJson(BuildPayload());
        string url  = GameStatsTracker.ApiBaseUrl.TrimEnd('/') + "/game/state";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            req.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return req.SendWebRequest();
    }

    private GameStatePayload BuildPayload()
    {
        var p = new GameStatePayload();

        foreach (EnemyController e in FindObjectsOfType<EnemyController>())
        {
            Health h = e.GetComponent<Health>();
            p.enemies.Add(new EnemyData
            {
                id    = e.gameObject.GetInstanceID(),
                type  = 0,
                x     = e.transform.position.x,
                y     = e.transform.position.y,
                hp    = h != null ? h.hp    : 0f,
                maxHp = h != null ? h.maxHp : 2f,
                size  = e.transform.localScale.x
            });
        }

        foreach (RangedEnemyController r in FindObjectsOfType<RangedEnemyController>())
        {
            Health h = r.GetComponent<Health>();
            p.enemies.Add(new EnemyData
            {
                id    = r.gameObject.GetInstanceID(),
                type  = 1,
                x     = r.transform.position.x,
                y     = r.transform.position.y,
                hp    = h != null ? h.hp    : 0f,
                maxHp = h != null ? h.maxHp : 2f,
                size  = r.transform.localScale.x
            });
        }

        foreach (EnemyProjectile proj in FindObjectsOfType<EnemyProjectile>())
        {
            p.projectiles.Add(new ProjectileData
            {
                id   = proj.gameObject.GetInstanceID(),
                x    = proj.transform.position.x,
                y    = proj.transform.position.y,
                vx   = proj.direction.x * proj.speed,
                vy   = proj.direction.y * proj.speed,
                life = proj.RemainingLife
            });
        }

        return p;
    }

    // ── Serializable types ────────────────────────────────────────────────────

    [Serializable] private class KillResponse { public int[] ids; }

    [Serializable]
    private class GameStatePayload
    {
        public List<EnemyData>      enemies     = new List<EnemyData>();
        public List<ProjectileData> projectiles = new List<ProjectileData>();
    }

    [Serializable]
    private class EnemyData
    {
        public int   id, type;
        public float x, y, hp, maxHp, size;
    }

    [Serializable]
    private class ProjectileData
    {
        public int   id;
        public float x, y, vx, vy, life;
    }
}

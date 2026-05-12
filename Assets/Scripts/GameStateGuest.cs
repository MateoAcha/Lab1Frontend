using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameStateGuest : MonoBehaviour
{
    private const float PollInterval = 0.15f;

    // GhostEnemy.OnDestroy() enqueues here; we send kill reports in the poll loop.
    private static readonly Queue<int> _killQueue = new Queue<int>();
    public static void EnqueueKill(int hostId) { _killQueue.Enqueue(hostId); }

    private readonly Dictionary<int, GhostEnemy>      _ghostEnemies     = new Dictionary<int, GhostEnemy>();
    private readonly Dictionary<int, GhostProjectile> _ghostProjectiles = new Dictionary<int, GhostProjectile>();

    private Material      _meleeMat;
    private Material      _rangedMat;
    private Material      _projMat;
    private GameBootstrap _bootstrap;
    private bool          _rocksPlaced;

    private void Start()
    {
        // Disable local enemy spawner — host is the source of truth.
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        // Stats still need to be started for death tracking on the guest.
        GameStatsTracker.StartMatch();

        // Grab materials from the bootstrap so ghost visuals match host visuals.
        _bootstrap = FindObjectOfType<GameBootstrap>();
        if (_bootstrap != null)
        {
            _meleeMat  = _bootstrap.meleeEnemyMaterial;
            _rangedMat = _bootstrap.rangedEnemyMaterial;
            _projMat   = _bootstrap.enemyProjectileMaterial;
        }

        StartCoroutine(PollLoop());
    }

    private void OnDestroy()
    {
        _killQueue.Clear();
    }

    // ── Main poll loop ────────────────────────────────────────────────────────

    private IEnumerator PollLoop()
    {
        while (true)
        {
            // Send any pending kill reports first.
            while (_killQueue.Count > 0)
                yield return ReportKill(_killQueue.Dequeue());

            yield return PollState();
            yield return new WaitForSeconds(PollInterval);
        }
    }

    // ── Network calls ─────────────────────────────────────────────────────────

    private IEnumerator ReportKill(int hostId)
    {
        string url = GameStatsTracker.ApiBaseUrl.TrimEnd('/') + $"/game/kill/{hostId}";
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            req.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return req.SendWebRequest();
    }

    private IEnumerator PollState()
    {
        string url = GameStatsTracker.ApiBaseUrl.TrimEnd('/') + "/game/state";
        var req = UnityWebRequest.Get(url);
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            req.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success
            || req.responseCode < 200 || req.responseCode >= 300)
            yield break;

        GameStatePayload state;
        try { state = JsonUtility.FromJson<GameStatePayload>(req.downloadHandler.text); }
        catch { yield break; }
        if (state == null) yield break;

        if (!_rocksPlaced && state.rocks != null && state.rocks.Length > 0)
            PlaceRocks(state.rocks);

        ApplyEnemyState(state.enemies);
        ApplyProjectileState(state.projectiles);
    }

    // ── Apply received state ──────────────────────────────────────────────────

    private void ApplyEnemyState(EnemyData[] enemies)
    {
        // Clean up destroyed ghosts from the dictionary.
        var dead = new List<int>();
        foreach (var kv in _ghostEnemies)
            if (kv.Value == null) dead.Add(kv.Key);
        foreach (int k in dead) _ghostEnemies.Remove(k);

        // Build a set of host IDs in this tick.
        var activeIds = new HashSet<int>();
        if (enemies != null)
        {
            foreach (EnemyData ed in enemies)
            {
                activeIds.Add(ed.id);

                if (_ghostEnemies.TryGetValue(ed.id, out GhostEnemy existing) && existing != null)
                {
                    // Update position and HP.
                    existing.SetTarget(new Vector3(ed.x, ed.y, 0f));
                    Health h = existing.GetComponent<Health>();
                    if (h != null) h.hp = Mathf.Max(0.01f, ed.hp);
                }
                else
                {
                    // Spawn new ghost.
                    GhostEnemy ghost = SpawnGhostEnemy(ed);
                    if (ghost != null) _ghostEnemies[ed.id] = ghost;
                }
            }
        }

        // Remove ghosts whose enemy disappeared from host state (host killed it).
        foreach (var kv in _ghostEnemies)
        {
            if (!activeIds.Contains(kv.Key) && kv.Value != null)
            {
                kv.Value.KilledByHost = true;
                Destroy(kv.Value.gameObject);
            }
        }
    }

    private void ApplyProjectileState(ProjectileData[] projectiles)
    {
        // Clean up destroyed ghost projectiles.
        var dead = new List<int>();
        foreach (var kv in _ghostProjectiles)
            if (kv.Value == null) dead.Add(kv.Key);
        foreach (int k in dead) _ghostProjectiles.Remove(k);

        var activeIds = new HashSet<int>();
        if (projectiles != null)
        {
            foreach (ProjectileData pd in projectiles)
            {
                activeIds.Add(pd.id);
                if (!_ghostProjectiles.ContainsKey(pd.id))
                    SpawnGhostProjectile(pd);
            }
        }

        // Destroy ghost projectiles that are gone on the host.
        foreach (var kv in _ghostProjectiles)
        {
            if (!activeIds.Contains(kv.Key) && kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
    }

    // ── Rock placement ────────────────────────────────────────────────────────

    private void PlaceRocks(RockData[] rocks)
    {
        if (_bootstrap == null) return;

        const string rootName = "RuntimeRocks";
        GameObject rocksRoot = GameObject.Find(rootName) ?? new GameObject(rootName);

        // If rocks are already present (e.g., second call), skip.
        if (rocksRoot.transform.childCount > 0) { _rocksPlaced = true; return; }

        foreach (RockData r in rocks)
            _bootstrap.CreateRock(rocksRoot.transform, new Vector2(r.x, r.y), r.size, "Rock");

        _rocksPlaced = true;
    }

    // ── Ghost spawning ────────────────────────────────────────────────────────

    private GhostEnemy SpawnGhostEnemy(EnemyData ed)
    {
        if (ed.hp <= 0f) return null;

        bool isRanged = ed.type == 1;
        float size = Mathf.Max(0.2f, ed.size);

        GameObject go = new GameObject(isRanged ? "GhostEnemyRanged" : "GhostEnemyMelee");
        go.transform.position   = new Vector3(ed.x, ed.y, 0f);
        go.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SimpleSprite.Square;
        sr.sortingOrder = 5;
        if (isRanged)
        {
            sr.color = new Color(1f, 0.65f, 0.2f, 1f);
            if (_rangedMat != null) { sr.sharedMaterial = _rangedMat; sr.color = Color.white; }
        }
        else
        {
            sr.color = new Color(0.25f, 1f, 0.25f, 1f);
            if (_meleeMat != null) { sr.sharedMaterial = _meleeMat; sr.color = Color.white; }
        }

        // Kinematic body + trigger collider so HitBox detects it and touch damage works.
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType    = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Health health = go.AddComponent<Health>();
        health.maxHp  = Mathf.Max(0.1f, ed.maxHp);
        health.hp     = Mathf.Clamp(ed.hp, 0.01f, health.maxHp);

        GhostEnemy ghost   = go.AddComponent<GhostEnemy>();
        ghost.HostId       = ed.id;
        ghost.IsRanged     = isRanged;
        ghost.KilledByHost = false;
        ghost.SetTarget(go.transform.position);

        _ghostEnemies[ed.id] = ghost;
        return ghost;
    }

    private void SpawnGhostProjectile(ProjectileData pd)
    {
        if (pd.life <= 0f) return;

        GameObject go = new GameObject("GhostProjectile");
        go.transform.position   = new Vector3(pd.x, pd.y, 0f);
        go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SimpleSprite.Square;
        sr.color        = new Color(1f, 0.55f, 0.15f, 1f);
        sr.sortingOrder = 9;
        if (_projMat != null) { sr.sharedMaterial = _projMat; sr.color = Color.white; }

        GhostProjectile gp = go.AddComponent<GhostProjectile>();
        gp.HostId        = pd.id;
        gp.velocity      = new Vector2(pd.vx, pd.vy);
        gp.remainingLife = pd.life;

        _ghostProjectiles[pd.id] = gp;
    }

    // ── Serializable types ────────────────────────────────────────────────────

    [Serializable]
    private class GameStatePayload
    {
        public EnemyData[]      enemies;
        public ProjectileData[] projectiles;
        public RockData[]       rocks;
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

    [Serializable]
    private class RockData { public float x, y, size; }
}

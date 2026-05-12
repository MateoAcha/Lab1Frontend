using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateGuest : MonoBehaviour
{
    private const float SendInterval = 0.05f; // 20 updates/sec

    // GhostEnemy.OnDestroy() enqueues here; sent to host via WebSocket.
    private static readonly Queue<int> _killQueue = new Queue<int>();
    public static void EnqueueKill(int hostId) { _killQueue.Enqueue(hostId); }

    private readonly Dictionary<int, GhostEnemy>      _ghostEnemies     = new Dictionary<int, GhostEnemy>();
    private readonly Dictionary<int, GhostProjectile> _ghostProjectiles = new Dictionary<int, GhostProjectile>();

    private Material      _meleeMat;
    private Material      _rangedMat;
    private Material      _projMat;
    private GameBootstrap _bootstrap;
    private bool          _rocksPlaced;

    private GameWebSocketClient _ws;

    private void Start()
    {
        // Disable local enemy spawner – host is the source of truth.
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        GameStatsTracker.StartMatch();

        _bootstrap = FindObjectOfType<GameBootstrap>();
        if (_bootstrap != null)
        {
            _meleeMat  = _bootstrap.meleeEnemyMaterial;
            _rangedMat = _bootstrap.rangedEnemyMaterial;
            _projMat   = _bootstrap.enemyProjectileMaterial;
        }

        StartCoroutine(ConnectThenRun());
    }

    private IEnumerator ConnectThenRun()
    {
        string wsUrl = BuildWsUrl();
        _ws = new GameWebSocketClient();

        Task connectTask = _ws.ConnectAsync(wsUrl);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsFaulted || !_ws.IsConnected)
        {
            Debug.LogWarning("GameStateGuest: WebSocket connect failed – " + connectTask.Exception?.GetBaseException()?.Message);
            yield break;
        }

        yield return Send("{\"type\":\"register\",\"role\":\"guest\"}");
        StartCoroutine(ReceiveLoop());
        StartCoroutine(SendLoop());
    }

    // ── Receive loop: apply state snapshots from host ─────────────────────────

    private IEnumerator ReceiveLoop()
    {
        while (_ws.IsConnected)
        {
            string msg;
            while (_ws.TryReceive(out msg))
                HandleHostMessage(msg);
            yield return null; // check every frame
        }
    }

    private void HandleHostMessage(string json)
    {
        try
        {
            StateMsg state = JsonUtility.FromJson<StateMsg>(json);
            if (state == null) return;

            // Update host player ghost position
            if (OnlinePlayerSync.Instance != null)
                OnlinePlayerSync.Instance.SetRemotePosition(new Vector3(state.hostX, state.hostY, 0f));

            if (!_rocksPlaced && state.rocks != null && state.rocks.Length > 0)
                PlaceRocks(state.rocks);

            ApplyEnemyState(state.enemies);
            ApplyProjectileState(state.projectiles);
        }
        catch { }
    }

    // ── Send loop: send guest position + queued kills ─────────────────────────

    private IEnumerator SendLoop()
    {
        while (_ws.IsConnected)
        {
            // Send kill reports first
            while (_killQueue.Count > 0)
            {
                int id = _killQueue.Dequeue();
                yield return Send($"{{\"type\":\"kill\",\"id\":{id}}}");
            }

            // Send current position
            if (PlayerController.main != null)
            {
                Vector3 pos = PlayerController.main.transform.position;
                yield return Send($"{{\"type\":\"pos\",\"x\":{pos.x:F3},\"y\":{pos.y:F3}}}");
            }

            yield return new WaitForSeconds(SendInterval);
        }
    }

    // ── Apply enemy/projectile state ──────────────────────────────────────────

    private void ApplyEnemyState(EnemyData[] enemies)
    {
        // Remove null refs from dictionary
        var dead = new List<int>();
        foreach (var kv in _ghostEnemies)
            if (kv.Value == null) dead.Add(kv.Key);
        foreach (int k in dead) _ghostEnemies.Remove(k);

        var activeIds = new HashSet<int>();
        if (enemies != null)
        {
            foreach (EnemyData ed in enemies)
            {
                activeIds.Add(ed.id);
                if (_ghostEnemies.TryGetValue(ed.id, out GhostEnemy existing) && existing != null)
                {
                    existing.SetTarget(new Vector3(ed.x, ed.y, 0f));
                    Health h = existing.GetComponent<Health>();
                    if (h != null) h.hp = Mathf.Max(0.01f, ed.hp);
                }
                else
                {
                    GhostEnemy ghost = SpawnGhostEnemy(ed);
                    if (ghost != null) _ghostEnemies[ed.id] = ghost;
                }
            }
        }

        // Destroy ghosts for enemies no longer in host state
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
        float size    = Mathf.Max(0.2f, ed.size);

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

        Rigidbody2D body  = go.AddComponent<Rigidbody2D>();
        body.bodyType     = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Health health  = go.AddComponent<Health>();
        health.maxHp   = Mathf.Max(0.1f, ed.maxHp);
        health.hp      = Mathf.Clamp(ed.hp, 0.01f, health.maxHp);

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

        GhostProjectile gp   = go.AddComponent<GhostProjectile>();
        gp.HostId            = pd.id;
        gp.velocity          = new Vector2(pd.vx, pd.vy);
        gp.remainingLife     = pd.life;

        _ghostProjectiles[pd.id] = gp;
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
        _killQueue.Clear();
        _ws?.Dispose();
    }

    // ── Serializable types ────────────────────────────────────────────────────

    [Serializable]
    private class StateMsg
    {
        public string          type;
        public float           hostX, hostY;
        public EnemyData[]     enemies;
        public ProjectileData[] projectiles;
        public RockData[]      rocks;
    }

    [Serializable] private class EnemyData     { public int id, type; public float x, y, hp, maxHp, size; }
    [Serializable] private class ProjectileData { public int id; public float x, y, vx, vy, life; }
    [Serializable] private class RockData       { public float x, y, size; }
}

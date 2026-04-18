using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PlayerStatsData
{
    public int matchesPlayed;
    public int meleeEnemiesKilled;
    public int rangedEnemiesKilled;
    public int deaths;
    public float timePlayedSeconds;
}

public static class GameStatsTracker
{
    private const string StatsKeyPrefix = "player_stats_v1_";
    private const string GuestSuffix = "guest";
    private const string DefaultApiBaseUrl = "http://localhost:8080";

    public static event Action<int, int, int> OnPlayerDied;

    public static int LastRunMeleeKills { get; private set; }
    public static int LastRunRangedKills { get; private set; }
    public static int LastRunTimeSeconds { get; private set; }

    private static bool _runActive;
    private static float _runStartAt;
    private static int _runMeleeKills;
    private static int _runRangedKills;
    private static string _apiBaseUrl = DefaultApiBaseUrl;
    private static StatsSyncRunner _syncRunner;

    public static string ApiBaseUrl => _apiBaseUrl;

    private class StatsSyncRunner : MonoBehaviour
    {
    }

    public static void StartMatch()
    {
        _runActive = true;
        _runStartAt = Time.time;
        _runMeleeKills = 0;
        _runRangedKills = 0;
    }

    public static void RegisterMeleeEnemyKilled()
    {
        if (!_runActive)
        {
            return;
        }

        _runMeleeKills++;
    }

    public static void RegisterRangedEnemyKilled()
    {
        if (!_runActive)
        {
            return;
        }

        _runRangedKills++;
    }

    public static void RegisterPlayerDied()
    {
        if (!_runActive)
        {
            return;
        }

        int meleeKills = _runMeleeKills;
        int rangedKills = _runRangedKills;
        int timePlayedSeconds = Mathf.Max(0, Mathf.FloorToInt(Time.time - _runStartAt));

        PlayerStatsData stats = LoadStats();

        stats.matchesPlayed += 1;
        stats.deaths += 1;
        stats.meleeEnemiesKilled += meleeKills;
        stats.rangedEnemiesKilled += rangedKills;
        stats.timePlayedSeconds += timePlayedSeconds;

        LastRunMeleeKills = meleeKills;
        LastRunRangedKills = rangedKills;
        LastRunTimeSeconds = timePlayedSeconds;

        SaveStats(stats);
        TrySyncSessionToBackend(meleeKills, rangedKills, timePlayedSeconds);
        OnPlayerDied?.Invoke(meleeKills, rangedKills, timePlayedSeconds);
        _runActive = false;
    }

    public static void SetApiBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _apiBaseUrl = DefaultApiBaseUrl;
            return;
        }

        _apiBaseUrl = baseUrl.TrimEnd('/');
    }

    public static PlayerStatsData GetCurrentPlayerStats()
    {
        return LoadStats();
    }

    private static PlayerStatsData LoadStats()
    {
        string key = GetStatsKey();
        if (!PlayerPrefs.HasKey(key))
        {
            return new PlayerStatsData();
        }

        string raw = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new PlayerStatsData();
        }

        try
        {
            PlayerStatsData data = JsonUtility.FromJson<PlayerStatsData>(raw);
            return data ?? new PlayerStatsData();
        }
        catch
        {
            return new PlayerStatsData();
        }
    }

    private static void SaveStats(PlayerStatsData stats)
    {
        if (stats == null)
        {
            return;
        }

        string key = GetStatsKey();
        PlayerPrefs.SetString(key, JsonUtility.ToJson(stats));
        PlayerPrefs.Save();
    }

    private static string GetStatsKey()
    {
        string suffix = GuestSuffix;
        if (AuthSession.IsLoggedIn)
        {
            suffix = AuthSession.UserId.ToString();
        }

        return StatsKeyPrefix + suffix;
    }

    private static void TrySyncSessionToBackend(int meleeKills, int rangedKills, int timePlayedSeconds)
    {
        if (!AuthSession.IsLoggedIn || string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            return;
        }

        var payload = new PlayerStatsSessionUpdateRequest
        {
            matchesPlayed = 1,
            meleeEnemiesKilled = Mathf.Max(0, meleeKills),
            rangedEnemiesKilled = Mathf.Max(0, rangedKills),
            deaths = 1,
            gamesWon = 0,
            highScore = 0,
            timePlayedSeconds = Mathf.Max(0, timePlayedSeconds),
            coins = 0
        };

        EnsureRunner().StartCoroutine(PostSessionStats(payload));
    }

    private static StatsSyncRunner EnsureRunner()
    {
        if (_syncRunner != null)
        {
            return _syncRunner;
        }

        GameObject runnerObj = new GameObject("StatsSyncRunner");
        UnityEngine.Object.DontDestroyOnLoad(runnerObj);
        _syncRunner = runnerObj.AddComponent<StatsSyncRunner>();
        return _syncRunner;
    }

    private static IEnumerator PostSessionStats(PlayerStatsSessionUpdateRequest payload)
    {
        string endpoint = $"{_apiBaseUrl}/users/{AuthSession.UserId}/stats/session";
        string json = JsonUtility.ToJson(payload);
        var request = new UnityWebRequest(endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[StatsSync] POST {endpoint}");
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200
            && request.responseCode < 300;

        if (success)
        {
            Debug.Log("[StatsSync] Session stats synced successfully.");
            yield break;
        }

        string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
        Debug.LogWarning($"[StatsSync] Failed to sync session stats. code={request.responseCode}, result={request.result}, body={responseText}");
    }

    [Serializable]
    private class PlayerStatsSessionUpdateRequest
    {
        public int matchesPlayed;
        public int meleeEnemiesKilled;
        public int rangedEnemiesKilled;
        public int deaths;
        public int gamesWon;
        public int highScore;
        public int timePlayedSeconds;
        public int coins;
    }
}

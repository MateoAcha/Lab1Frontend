using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PlayerStatsData
{
    public int matchesPlayed;
    public int meleeEnemiesKilled;
    public int rangedEnemiesKilled;
    public int giantEnemiesKilled;
    public int deaths;
    public int gamesWon;
    public int highScore;
    public float timePlayedSeconds;
    public int coins;
    public int totalXp;
    public int level;
    public int unspentSkillPoints;
    public int spentSkillPoints;
}

public static class GameStatsTracker
{
    private const string StatsKeyPrefix = "player_stats_v1_";
    private const string GuestSuffix = "guest";
    private const string DefaultApiBaseUrl = "http://localhost:8080";
    private const int XpPerSecond = 1;
    private const int XpPerMeleeKill = 5;
    private const int XpPerRangedKill = 8;
    private const int XpPerGiantKill = 60;
    private const int BaseXpForNextLevel = 100;
    private const int ExtraXpPerLevel = 50;

    public static event Action<int, int, int, int> OnPlayerDied;
    public static event Action<int, int, int, int> OnMatchFinished;
    public static event Action<PlayerStatsData> OnPlayerStatsSynced;

    public static int LastRunMeleeKills { get; private set; }
    public static int LastRunRangedKills { get; private set; }
    public static int LastRunGiantKills { get; private set; }
    public static int LastRunTimeSeconds { get; private set; }
    public static bool LastRunWasFinished { get; private set; }
    public static int LastRunXpEarned { get; private set; }
    public static bool IsRunActive => _runActive;
    public static int CurrentMeleeKills => _runMeleeKills;
    public static int CurrentRangedKills => _runRangedKills;
    public static int CurrentGiantKills => _runGiantKills;
    public static int CurrentRunTimeSeconds => _runActive
        ? Mathf.Max(0, Mathf.FloorToInt(Time.time - _runStartAt))
        : LastRunTimeSeconds;

    private static bool _runActive;
    private static float _runStartAt;
    private static int _runMeleeKills;
    private static int _runRangedKills;
    private static int _runGiantKills;
    private static readonly List<PendingMaterialReward> _pendingMaterials = new List<PendingMaterialReward>();
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
        _runGiantKills = 0;
        _pendingMaterials.Clear();
        LastRunWasFinished = false;
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

    public static void RegisterGiantEnemyKilled()
    {
        if (!_runActive)
        {
            return;
        }

        _runGiantKills++;
    }

    public static void RegisterMaterialCollected(MapMaterialDefinition materialDrop)
    {
        if (!_runActive || materialDrop == null || string.IsNullOrWhiteSpace(materialDrop.inventoryKey))
        {
            return;
        }

        string key = materialDrop.inventoryKey.Trim();
        for (int i = 0; i < _pendingMaterials.Count; i++)
        {
            if (string.Equals(_pendingMaterials[i].materialKey, key, StringComparison.OrdinalIgnoreCase))
            {
                _pendingMaterials[i].quantity += 1;
                return;
            }
        }

        _pendingMaterials.Add(new PendingMaterialReward
        {
            materialKey = key,
            itemName = string.IsNullOrWhiteSpace(materialDrop.itemName) ? key : materialDrop.itemName.Trim(),
            rarity = string.IsNullOrWhiteSpace(materialDrop.rarity) ? "Rare" : materialDrop.rarity.Trim(),
            quantity = 1
        });
    }

    public static void RegisterPlayerDied()
    {
        if (!_runActive)
        {
            return;
        }

        CompleteRun(_runMeleeKills, _runRangedKills, _runGiantKills, Mathf.Max(0, Mathf.FloorToInt(Time.time - _runStartAt)), false);
    }

    public static void RegisterMatchFinished()
    {
        if (!_runActive)
        {
            return;
        }

        CompleteRun(_runMeleeKills, _runRangedKills, _runGiantKills, Mathf.Max(0, Mathf.FloorToInt(Time.time - _runStartAt)), true);
    }

    public static void CompleteNetworkMatch(int meleeKills, int rangedKills, int timePlayedSeconds, bool finished = false)
    {
        CompleteNetworkMatch(meleeKills, rangedKills, 0, timePlayedSeconds, finished);
    }

    public static void CompleteNetworkMatch(int meleeKills, int rangedKills, int giantKills, int timePlayedSeconds, bool finished = false)
    {
        if (!_runActive)
        {
            return;
        }

        CompleteRun(meleeKills, rangedKills, giantKills, timePlayedSeconds, finished);
    }

    private static void CompleteRun(int meleeKills, int rangedKills, int giantKills, int timePlayedSeconds, bool finished)
    {
        PlayerStatsData stats = LoadStats();

        stats.matchesPlayed += 1;
        if (finished)
        {
            stats.gamesWon += 1;
        }
        else
        {
            stats.deaths += 1;
        }
        stats.meleeEnemiesKilled += meleeKills;
        stats.rangedEnemiesKilled += rangedKills;
        stats.giantEnemiesKilled += giantKills;
        stats.timePlayedSeconds += timePlayedSeconds;
        LastRunXpEarned = CalculateRunXp(meleeKills, rangedKills, giantKills, timePlayedSeconds);
        ApplyXp(stats, LastRunXpEarned);

        LastRunMeleeKills = meleeKills;
        LastRunRangedKills = rangedKills;
        LastRunGiantKills = giantKills;
        LastRunTimeSeconds = timePlayedSeconds;
        LastRunWasFinished = finished;

        SaveStats(stats);
        _runActive = false;
        TrySyncSessionToBackend(meleeKills, rangedKills, giantKills, timePlayedSeconds, finished);
        TrySyncConsumedConsumablesToBackend();
        if (finished)
        {
            TrySyncCollectedMaterialsToBackend();
        }
        else
        {
            _pendingMaterials.Clear();
        }

        if (finished)
        {
            OnMatchFinished?.Invoke(meleeKills, rangedKills, giantKills, timePlayedSeconds);
        }
        else
        {
            OnPlayerDied?.Invoke(meleeKills, rangedKills, giantKills, timePlayedSeconds);
        }
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

    public static int CalculateRunXp(int meleeKills, int rangedKills, int giantKills, int timePlayedSeconds)
    {
        return Mathf.Max(0, timePlayedSeconds) * XpPerSecond
            + Mathf.Max(0, meleeKills) * XpPerMeleeKill
            + Mathf.Max(0, rangedKills) * XpPerRangedKill
            + Mathf.Max(0, giantKills) * XpPerGiantKill;
    }

    public static int GetXpRequiredForLevel(int level)
    {
        return BaseXpForNextLevel + Mathf.Max(0, level - 1) * ExtraXpPerLevel;
    }

    public static int GetTotalXpForLevel(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        int total = 0;
        for (int current = 1; current < safeLevel; current++)
            total += GetXpRequiredForLevel(current);
        return total;
    }

    public static int GetXpIntoCurrentLevel(PlayerStatsData stats)
    {
        if (stats == null)
            return 0;

        NormalizeProgression(stats);
        return Mathf.Max(0, stats.totalXp - GetTotalXpForLevel(stats.level));
    }

    public static int GetXpNeededForNextLevel(PlayerStatsData stats)
    {
        if (stats == null)
            return GetXpRequiredForLevel(1);

        NormalizeProgression(stats);
        return GetXpRequiredForLevel(stats.level);
    }

    public static bool SpendSkillPointsLocal(int quantity)
    {
        int safeQuantity = Mathf.Max(0, quantity);
        if (safeQuantity == 0)
            return true;

        PlayerStatsData stats = LoadStats();
        NormalizeProgression(stats);
        if (stats.unspentSkillPoints < safeQuantity)
            return false;

        stats.unspentSkillPoints -= safeQuantity;
        stats.spentSkillPoints += safeQuantity;
        SaveStats(stats);
        PlayerSkillLoadout.SetProgressionFromStats(stats);
        return true;
    }

    private static PlayerStatsData LoadStats()
    {
        string key = GetStatsKey();
        if (!PlayerPrefs.HasKey(key))
        {
            return new PlayerStatsData { level = 1 };
        }

        string raw = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new PlayerStatsData { level = 1 };
        }

        try
        {
            PlayerStatsData data = JsonUtility.FromJson<PlayerStatsData>(raw);
            NormalizeProgression(data);
            return data ?? new PlayerStatsData { level = 1 };
        }
        catch
        {
            return new PlayerStatsData { level = 1 };
        }
    }

    private static void SaveStats(PlayerStatsData stats)
    {
        if (stats == null)
        {
            return;
        }

        NormalizeProgression(stats);
        string key = GetStatsKey();
        PlayerPrefs.SetString(key, JsonUtility.ToJson(stats));
        PlayerPrefs.Save();
    }

    public static void SaveSyncedStats(PlayerStatsData stats)
    {
        if (stats == null)
            return;

        SaveStats(stats);
        PlayerSkillLoadout.SetProgressionFromStats(stats);
        OnPlayerStatsSynced?.Invoke(stats);
    }

    private static void ApplyXp(PlayerStatsData stats, int xp)
    {
        if (stats == null || xp <= 0)
            return;

        NormalizeProgression(stats);
        int oldLevel = stats.level;
        stats.totalXp += xp;
        stats.level = CalculateLevelForTotalXp(stats.totalXp);
        int levelsGained = Mathf.Max(0, stats.level - oldLevel);
        stats.unspentSkillPoints += levelsGained;
    }

    private static int CalculateLevelForTotalXp(int totalXp)
    {
        int safeTotal = Mathf.Max(0, totalXp);
        int level = 1;
        while (safeTotal >= GetTotalXpForLevel(level + 1))
            level++;
        return level;
    }

    private static void NormalizeProgression(PlayerStatsData stats)
    {
        if (stats == null)
            return;

        stats.totalXp = Mathf.Max(0, stats.totalXp);
        stats.level = Mathf.Max(1, stats.level);
        stats.unspentSkillPoints = Mathf.Max(0, stats.unspentSkillPoints);
        stats.spentSkillPoints = Mathf.Max(0, stats.spentSkillPoints);

        int expectedLevel = CalculateLevelForTotalXp(stats.totalXp);
        if (expectedLevel > stats.level)
            stats.level = expectedLevel;
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

    private static void TrySyncSessionToBackend(int meleeKills, int rangedKills, int giantKills, int timePlayedSeconds, bool finished)
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
            giantEnemiesKilled = Mathf.Max(0, giantKills),
            deaths = finished ? 0 : 1,
            gamesWon = finished ? 1 : 0,
            highScore = 0,
            timePlayedSeconds = Mathf.Max(0, timePlayedSeconds),
            coins = 0
        };

        EnsureRunner().StartCoroutine(PostSessionStats(payload));
    }

    private static void TrySyncCollectedMaterialsToBackend()
    {
        if (_pendingMaterials.Count == 0)
        {
            return;
        }

        if (!AuthSession.IsLoggedIn || string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            _pendingMaterials.Clear();
            return;
        }

        var payload = new MaterialInventoryRewardsRequest
        {
            materials = _pendingMaterials.ToArray()
        };
        _pendingMaterials.Clear();

        EnsureRunner().StartCoroutine(PostCollectedMaterials(payload));
    }

    private static void TrySyncConsumedConsumablesToBackend()
    {
        if (!PlayerLoadout.TryGetPendingConsumableUsage(out int userInventoryId, out int quantity))
        {
            return;
        }

        if (!AuthSession.IsLoggedIn || string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            PlayerLoadout.ClearPendingConsumableUsage();
            return;
        }

        EnsureRunner().StartCoroutine(PostConsumedConsumable(userInventoryId, quantity));
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
            try
            {
                PlayerStatsData syncedStats = JsonUtility.FromJson<PlayerStatsData>(request.downloadHandler.text);
                SaveSyncedStats(syncedStats);
            }
            catch
            {
                Debug.LogWarning("[StatsSync] Could not parse synced player stats.");
            }
            yield break;
        }

        string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
        Debug.LogWarning($"[StatsSync] Failed to sync session stats. code={request.responseCode}, result={request.result}, body={responseText}");
    }

    private static IEnumerator PostCollectedMaterials(MaterialInventoryRewardsRequest payload)
    {
        string endpoint = $"{_apiBaseUrl}/users/{AuthSession.UserId}/inventory/add-materials";
        string json = JsonUtility.ToJson(payload);
        var request = new UnityWebRequest(endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[MaterialSync] POST {endpoint}");
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200
            && request.responseCode < 300;

        if (success)
        {
            Debug.Log("[MaterialSync] Collected materials synced successfully.");
            yield break;
        }

        string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
        Debug.LogWarning($"[MaterialSync] Failed to sync collected materials. code={request.responseCode}, result={request.result}, body={responseText}");
    }

    private static IEnumerator PostConsumedConsumable(int userInventoryId, int quantity)
    {
        int safeQuantity = Mathf.Max(1, quantity);
        string endpoint = $"{_apiBaseUrl}/users/{AuthSession.UserId}/inventory/{userInventoryId}/consume";
        string json = JsonUtility.ToJson(new ConsumeInventoryItemRequest { quantity = safeQuantity });
        var request = new UnityWebRequest(endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[ConsumableSync] POST {endpoint} qty={safeQuantity}");
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200
            && request.responseCode < 300;

        if (success)
        {
            PlayerLoadout.MarkPendingConsumableUsageSynced(userInventoryId, safeQuantity);
            Debug.Log("[ConsumableSync] Consumed consumable synced successfully.");
            yield break;
        }

        string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
        Debug.LogWarning($"[ConsumableSync] Failed to sync consumed consumable. code={request.responseCode}, result={request.result}, body={responseText}");
    }

    [Serializable]
    private class PendingMaterialReward
    {
        public string materialKey;
        public string itemName;
        public string itemType = "Material";
        public string rarity;
        public int quantity;
    }

    [Serializable]
    private class MaterialInventoryRewardsRequest
    {
        public PendingMaterialReward[] materials;
    }

    [Serializable]
    private class ConsumeInventoryItemRequest
    {
        public int quantity;
    }

    [Serializable]
    private class PlayerStatsSessionUpdateRequest
    {
        public int matchesPlayed;
        public int meleeEnemiesKilled;
        public int rangedEnemiesKilled;
        public int giantEnemiesKilled;
        public int deaths;
        public int gamesWon;
        public int highScore;
        public int timePlayedSeconds;
        public int coins;
    }
}

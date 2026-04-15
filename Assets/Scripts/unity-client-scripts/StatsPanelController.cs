using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class StatListItem
{
    public string label;
    public string value;
}

public class StatsPanelController : MonoBehaviour
{
    private enum StatsTab
    {
        Player,
        Global
    }

    [Header("Tabs")]
    public Button playerTabButton;
    public Button globalTabButton;
    public GameObject playerTabSelectedVisual;
    public GameObject globalTabSelectedVisual;

    [Header("List")]
    public Transform statsContentRoot;
    public GameObject statSlotPrefab;
    public TextMeshProUGUI emptyStateText;

    [Header("Preview Data")]
    public bool usePlaceholderDataOnEnable = true;
    public List<StatListItem> placeholderGlobalStats = new List<StatListItem>();

    [Header("API")]
    public string apiBaseUrl = "http://localhost:8080";

    private readonly List<StatListItem> _playerStats = new List<StatListItem>();
    private readonly List<StatListItem> _globalStats = new List<StatListItem>();
    private readonly List<GameObject> _spawnedSlots = new List<GameObject>();
    private StatsTab _activeTab = StatsTab.Player;

    private void Awake()
    {
        HookTabButtons();
    }

    private void OnEnable()
    {
        LoadPlayerStatsFromSavedData();

        if (usePlaceholderDataOnEnable && _globalStats.Count == 0)
        {
            LoadGlobalPlaceholderData();
        }

        ShowPlayerTab();
    }

    public void RefreshPlayerStatsFromSave()
    {
        LoadPlayerStatsFromSavedData();
        if (_activeTab == StatsTab.Player)
        {
            RebuildVisibleList();
        }
    }

    public void ShowPlayerTab()
    {
        SetActiveTab(StatsTab.Player);
    }

    public void ShowGlobalTab()
    {
        SetActiveTab(StatsTab.Global);
        StartCoroutine(LoadGlobalStatsFromBackend());
    }

    public void BeginPlayerStatsLoad()
    {
        _playerStats.Clear();
        if (_activeTab == StatsTab.Player)
        {
            RebuildVisibleList();
        }
    }

    public void BeginGlobalStatsLoad()
    {
        _globalStats.Clear();
        if (_activeTab == StatsTab.Global)
        {
            RebuildVisibleList();
        }
    }

    public void AddPlayerStat(string label, string value)
    {
        AddStat(_playerStats, StatsTab.Player, label, value);
    }

    public void AddGlobalStat(string label, string value)
    {
        AddStat(_globalStats, StatsTab.Global, label, value);
    }

    private void AddStat(List<StatListItem> target, StatsTab ownerTab, string label, string value)
    {
        var item = new StatListItem
        {
            label = label,
            value = value
        };

        target.Add(item);

        if (_activeTab != ownerTab)
        {
            return;
        }

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(false);
        }

        SpawnStatSlot(item);
    }

    private void SetActiveTab(StatsTab tab)
    {
        _activeTab = tab;
        RefreshTabVisuals();
        RebuildVisibleList();
    }

    private void RefreshTabVisuals()
    {
        if (playerTabSelectedVisual != null) playerTabSelectedVisual.SetActive(_activeTab == StatsTab.Player);
        if (globalTabSelectedVisual != null) globalTabSelectedVisual.SetActive(_activeTab == StatsTab.Global);

        if (playerTabButton != null) playerTabButton.interactable = _activeTab != StatsTab.Player;
        if (globalTabButton != null) globalTabButton.interactable = _activeTab != StatsTab.Global;
    }

    private void RebuildVisibleList()
    {
        ClearSlots();

        List<StatListItem> source = _activeTab == StatsTab.Player ? _playerStats : _globalStats;
        bool hasItems = source.Count > 0;

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(!hasItems);
            emptyStateText.text = _activeTab == StatsTab.Player
                ? "No player stats loaded yet."
                : "No global stats loaded yet.";
        }

        if (!hasItems)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            SpawnStatSlot(source[i]);
        }
    }

    private void SpawnStatSlot(StatListItem item)
    {
        if (statsContentRoot == null || statSlotPrefab == null)
        {
            Debug.LogWarning("[StatsUI] Missing statsContentRoot or statSlotPrefab reference.");
            return;
        }

        GameObject slot = Instantiate(statSlotPrefab, statsContentRoot);
        _spawnedSlots.Add(slot);

        string statLabel = string.IsNullOrWhiteSpace(item.label) ? "-" : item.label;
        string statValue = string.IsNullOrWhiteSpace(item.value) ? "-" : item.value;

        var slotView = slot.GetComponent<StatSlotView>();
        if (slotView != null)
        {
            slotView.Bind(statLabel, statValue);
            return;
        }

        TextMeshProUGUI[] labels = slot.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (labels.Length >= 2)
        {
            labels[0].text = statLabel;
            labels[1].text = statValue;
        }
        else if (labels.Length == 1)
        {
            labels[0].text = $"{statLabel}: {statValue}";
        }
    }

    private void ClearSlots()
    {
        for (int i = _spawnedSlots.Count - 1; i >= 0; i--)
        {
            if (_spawnedSlots[i] != null)
            {
                Destroy(_spawnedSlots[i]);
            }
        }

        _spawnedSlots.Clear();
    }

    private void HookTabButtons()
    {
        if (playerTabButton != null)
        {
            playerTabButton.onClick.RemoveListener(ShowPlayerTab);
            playerTabButton.onClick.AddListener(ShowPlayerTab);
        }

        if (globalTabButton != null)
        {
            globalTabButton.onClick.RemoveListener(ShowGlobalTab);
            globalTabButton.onClick.AddListener(ShowGlobalTab);
        }
    }

    private void LoadPlayerStatsFromSavedData()
    {
        _playerStats.Clear();

        PlayerStatsData stats = GameStatsTracker.GetCurrentPlayerStats();
        _playerStats.Add(new StatListItem { label = "Matches Played", value = stats.matchesPlayed.ToString() });
        _playerStats.Add(new StatListItem { label = "Melee Enemies Killed", value = stats.meleeEnemiesKilled.ToString() });
        _playerStats.Add(new StatListItem { label = "Ranged Enemies Killed", value = stats.rangedEnemiesKilled.ToString() });
        _playerStats.Add(new StatListItem { label = "Deaths", value = stats.deaths.ToString() });
        _playerStats.Add(new StatListItem { label = "Time Played", value = FormatDuration(stats.timePlayedSeconds) });
    }

    private void LoadGlobalPlaceholderData()
    {
        _globalStats.Clear();
        if (placeholderGlobalStats.Count == 0)
        {
            _globalStats.Add(new StatListItem { label = "Top Kills (All Players)", value = "2,194" });
            _globalStats.Add(new StatListItem { label = "Total Matches (Global)", value = "88,702" });
            _globalStats.Add(new StatListItem { label = "Average Match Duration", value = "11m 34s" });
        }
        else
        {
            _globalStats.AddRange(placeholderGlobalStats);
        }
    }

    private IEnumerator LoadGlobalStatsFromBackend()
    {
        if (!AuthSession.IsLoggedIn || string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            BeginGlobalStatsLoad();
            AddGlobalStat("Global Stats", "Please log in to view.");
            yield break;
        }

        string baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
            ? GameStatsTracker.ApiBaseUrl
            : apiBaseUrl.TrimEnd('/');

        string endpoint = $"{baseUrl}/stats/global";
        var request = UnityWebRequest.Get(endpoint);
        request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[StatsUI] GET {endpoint}");
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200
            && request.responseCode < 300;

        if (!success)
        {
            Debug.LogWarning($"[StatsUI] Global stats request failed ({request.responseCode}). Falling back to placeholder.");
            BeginGlobalStatsLoad();
            LoadGlobalPlaceholderData();
            if (_activeTab == StatsTab.Global)
            {
                RebuildVisibleList();
            }
            yield break;
        }

        GlobalStatsResponse response;
        try
        {
            response = JsonUtility.FromJson<GlobalStatsResponse>(request.downloadHandler.text);
        }
        catch
        {
            response = null;
        }

        BeginGlobalStatsLoad();
        AddGlobalStat("Highest Matches Played", FormatLeader(response != null ? response.highestMatchesPlayed : null, false));
        AddGlobalStat("Highest Kill Count", FormatLeader(response != null ? response.highestKillCount : null, false));
        AddGlobalStat("Highest Time Played", FormatLeader(response != null ? response.highestTimePlayed : null, true));
    }

    private string FormatLeader(GlobalStatEntry entry, bool duration)
    {
        string username = entry != null && !string.IsNullOrWhiteSpace(entry.username)
            ? entry.username
            : "-";

        long safeValue = entry != null ? Math.Max(0L, entry.value) : 0L;
        string valueText = duration ? FormatDuration(safeValue) : safeValue.ToString();
        return $"{username} - {valueText}";
    }

    private string FormatDuration(float totalSeconds)
    {
        long safeSeconds = Math.Max(0L, (long)Mathf.FloorToInt(totalSeconds));
        return FormatDuration(safeSeconds);
    }

    private string FormatDuration(long totalSeconds)
    {
        long safeSeconds = Math.Max(0L, totalSeconds);
        long hours = safeSeconds / 3600;
        long minutes = (safeSeconds % 3600) / 60;
        long seconds = safeSeconds % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    [Serializable]
    private class GlobalStatsResponse
    {
        public GlobalStatEntry highestMatchesPlayed;
        public GlobalStatEntry highestKillCount;
        public GlobalStatEntry highestTimePlayed;
    }

    [Serializable]
    private class GlobalStatEntry
    {
        public string username;
        public long value;
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public List<StatListItem> placeholderPlayerStats = new List<StatListItem>();
    public List<StatListItem> placeholderGlobalStats = new List<StatListItem>();

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
        if (usePlaceholderDataOnEnable && _playerStats.Count == 0 && _globalStats.Count == 0)
        {
            LoadPlaceholderData();
        }

        ShowPlayerTab();
    }

    public void ShowPlayerTab()
    {
        SetActiveTab(StatsTab.Player);
    }

    public void ShowGlobalTab()
    {
        SetActiveTab(StatsTab.Global);
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

    private void LoadPlaceholderData()
    {
        _playerStats.Clear();
        _globalStats.Clear();

        if (placeholderPlayerStats.Count == 0)
        {
            _playerStats.Add(new StatListItem { label = "Total Kills", value = "42" });
            _playerStats.Add(new StatListItem { label = "Matches Played", value = "17" });
            _playerStats.Add(new StatListItem { label = "Highest Combo", value = "9" });
        }
        else
        {
            _playerStats.AddRange(placeholderPlayerStats);
        }

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
}

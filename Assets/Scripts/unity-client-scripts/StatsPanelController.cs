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

    [Header("Preview Data")]
    public bool usePlaceholderDataOnEnable = true;
    public List<StatListItem> placeholderGlobalStats = new List<StatListItem>();

    [Header("API")]
    public string apiBaseUrl = "http://localhost:8080";

    private static readonly Color TabActive = new Color(0.12f, 0.40f, 0.52f, 1f);
    private static readonly Color TabInactive = new Color(0.18f, 0.22f, 0.28f, 1f);
    private static readonly Color TabGlobalActive = new Color(0.34f, 0.16f, 0.50f, 1f);

    private readonly List<StatListItem> _playerStats = new List<StatListItem>();
    private readonly List<StatListItem> _globalStats = new List<StatListItem>();
    private readonly List<GameObject> _rows = new List<GameObject>();

    private StatsTab _activeTab = StatsTab.Player;
    private bool _built;
    private RectTransform _contentRoot;
    private TextMeshProUGUI _stateText;
    private Button _playerTabButton;
    private Button _globalTabButton;
    private Image _playerTabImage;
    private Image _globalTabImage;
    private Coroutine _globalLoadRoutine;

    private void Awake()
    {
        EnsureBuilt();
    }

    private void OnEnable()
    {
        EnsureBuilt();
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
        if (_globalLoadRoutine != null)
        {
            StopCoroutine(_globalLoadRoutine);
        }
        _globalLoadRoutine = StartCoroutine(LoadGlobalStatsFromBackend());
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
        target.Add(new StatListItem
        {
            label = label,
            value = value
        });

        if (_activeTab == ownerTab)
        {
            RebuildVisibleList();
        }
    }

    private void SetActiveTab(StatsTab tab)
    {
        _activeTab = tab;
        RefreshTabVisuals();
        RebuildVisibleList();
    }

    private void RefreshTabVisuals()
    {
        if (_playerTabButton != null) _playerTabButton.interactable = _activeTab != StatsTab.Player;
        if (_globalTabButton != null) _globalTabButton.interactable = _activeTab != StatsTab.Global;
        if (_playerTabImage != null) _playerTabImage.color = _activeTab == StatsTab.Player ? TabActive : TabInactive;
        if (_globalTabImage != null) _globalTabImage.color = _activeTab == StatsTab.Global ? TabGlobalActive : TabInactive;
    }

    private void RebuildVisibleList()
    {
        EnsureBuilt();
        ClearRows();

        List<StatListItem> source = _activeTab == StatsTab.Player ? _playerStats : _globalStats;
        if (source.Count == 0)
        {
            SetState(_activeTab == StatsTab.Player
                ? "No player stats loaded yet."
                : "No global stats loaded yet.");
            return;
        }

        HideState();
        for (int i = 0; i < source.Count; i++)
        {
            CreateStatRow(source[i], i);
        }
    }

    private void CreateStatRow(StatListItem item, int index)
    {
        GameObject row = CreateUIObj("StatRow", _contentRoot);
        _rows.Add(row);

        Image rowImage = GetOrAddImage(row);
        rowImage.color = index % 2 == 0
            ? new Color(0.16f, 0.20f, 0.27f, 0.98f)
            : new Color(0.14f, 0.18f, 0.24f, 0.98f);

        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 150f;

        VerticalLayoutGroup group = row.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(22, 22, 22, 20);
        group.spacing = 10f;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        ContentSizeFitter fitter = row.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        string label = string.IsNullOrWhiteSpace(item.label) ? "-" : item.label;
        string value = string.IsNullOrWhiteSpace(item.value) ? "-" : item.value;

        MakeLabel(row.transform, label, 28f, FontStyles.Bold,
            new Color(0.97f, 0.98f, 1f, 1f), 46f, false);

        Color valueColor = _activeTab == StatsTab.Player
            ? new Color(0.76f, 0.90f, 0.76f, 1f)
            : new Color(0.66f, 0.84f, 1f, 1f);

        MakeLabel(row.transform, value, 24f, FontStyles.Normal, valueColor, 44f, true);
    }

    private void ClearRows()
    {
        for (int i = _rows.Count - 1; i >= 0; i--)
        {
            if (_rows[i] != null)
            {
                Destroy(_rows[i]);
            }
        }
        _rows.Clear();
    }

    private void LoadPlayerStatsFromSavedData()
    {
        _playerStats.Clear();

        PlayerStatsData stats = GameStatsTracker.GetCurrentPlayerStats();
        _playerStats.Add(new StatListItem { label = "Matches Played", value = stats.matchesPlayed.ToString() });
        _playerStats.Add(new StatListItem { label = "Melee Enemies Killed", value = stats.meleeEnemiesKilled.ToString() });
        _playerStats.Add(new StatListItem { label = "Ranged Enemies Killed", value = stats.rangedEnemiesKilled.ToString() });
        _playerStats.Add(new StatListItem { label = "Giants Killed", value = stats.giantEnemiesKilled.ToString() });
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

        SetState("Loading global stats...");

        string baseUrl = !string.IsNullOrWhiteSpace(GameStatsTracker.ApiBaseUrl)
            ? GameStatsTracker.ApiBaseUrl.TrimEnd('/')
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

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        ClearExistingChildren();

        RectTransform rootRect = GetOrAddRT(gameObject);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(gameObject, GameUiThemeRuntime.Current.statsBackground, true);

        GameObject backBtn = CreateButton("BackBtn", transform, "Back",
            new Vector2(16f, -16f), new Vector2(130f, 46f),
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Color(0.18f, 0.23f, 0.30f, 1f));
        backBtn.GetComponent<Button>().onClick.AddListener(HandleBackPressed);

        BuildTitle();
        BuildTabBar();
        BuildStateText();
        BuildScrollView();
        RefreshTabVisuals();
    }

    private void ClearExistingChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void BuildTitle()
    {
        GameObject titleObj = CreateUIObj("Title", transform);
        RectTransform titleRT = GetOrAddRT(titleObj);
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(560f, 70f);
        titleRT.anchoredPosition = new Vector2(0f, -18f);

        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "STATS";
        title.fontSize = 46f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.95f, 0.97f, 1f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.font = TMP_Settings.defaultFontAsset;
        title.raycastTarget = false;
    }

    private void BuildTabBar()
    {
        GameObject tabBar = CreateUIObj("TabBar", transform);
        RectTransform tabRT = GetOrAddRT(tabBar);
        tabRT.anchorMin = new Vector2(0.5f, 1f);
        tabRT.anchorMax = new Vector2(0.5f, 1f);
        tabRT.pivot = new Vector2(0.5f, 1f);
        tabRT.sizeDelta = new Vector2(460f, 58f);
        tabRT.anchoredPosition = new Vector2(0f, -86f);

        HorizontalLayoutGroup group = tabBar.AddComponent<HorizontalLayoutGroup>();
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = true;
        group.spacing = 6f;

        (_playerTabButton, _playerTabImage) = BuildTabButton(tabBar.transform, "Player", TabActive);
        (_globalTabButton, _globalTabImage) = BuildTabButton(tabBar.transform, "Global", TabInactive);

        _playerTabButton.onClick.AddListener(ShowPlayerTab);
        _globalTabButton.onClick.AddListener(ShowGlobalTab);
    }

    private (Button, Image) BuildTabButton(Transform parent, string label, Color color)
    {
        GameObject obj = CreateUIObj("Tab_" + label, parent);
        Image image = obj.AddComponent<Image>();
        Button button = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, color);

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform labelRT = GetOrAddRT(labelObj);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.color = GameUiThemeRuntime.Current.text;
        text.alignment = TextAlignmentOptions.Center;
        text.font = TMP_Settings.defaultFontAsset;
        text.raycastTarget = false;

        return (button, image);
    }

    private void BuildStateText()
    {
        GameObject stateObj = CreateUIObj("StateText", transform);
        RectTransform stateRT = GetOrAddRT(stateObj);
        stateRT.anchorMin = new Vector2(0.1f, 0.4f);
        stateRT.anchorMax = new Vector2(0.9f, 0.6f);
        stateRT.pivot = new Vector2(0.5f, 0.5f);
        stateRT.offsetMin = Vector2.zero;
        stateRT.offsetMax = Vector2.zero;
        stateRT.anchoredPosition = Vector2.zero;

        _stateText = stateObj.AddComponent<TextMeshProUGUI>();
        _stateText.text = "";
        _stateText.fontSize = 28f;
        _stateText.color = new Color(0.88f, 0.90f, 0.96f, 1f);
        _stateText.alignment = TextAlignmentOptions.Center;
        _stateText.font = TMP_Settings.defaultFontAsset;
        _stateText.raycastTarget = false;
    }

    private void BuildScrollView()
    {
        GameObject scrollRoot = CreateUIObj("ScrollView", transform);
        RectTransform scrollRT = GetOrAddRT(scrollRoot);
        scrollRT.anchorMin = new Vector2(0.01f, 0.04f);
        scrollRT.anchorMax = new Vector2(0.99f, 0.88f);
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        scrollRT.anchoredPosition = Vector2.zero;
        GameUiThemeRuntime.StyleSurface(scrollRoot);

        ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        GameObject viewport = CreateUIObj("Viewport", scrollRoot.transform);
        RectTransform viewportRT = GetOrAddRT(viewport);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(10f, 10f);
        viewportRT.offsetMax = new Vector2(-10f, -10f);
        GetOrAddImage(viewport).color = new Color(1f, 1f, 1f, 0.02f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObj("Content", viewport.transform);
        _contentRoot = GetOrAddRT(content);
        _contentRoot.anchorMin = new Vector2(0f, 1f);
        _contentRoot.anchorMax = new Vector2(1f, 1f);
        _contentRoot.pivot = new Vector2(0.5f, 1f);
        _contentRoot.offsetMin = Vector2.zero;
        _contentRoot.offsetMax = Vector2.zero;
        _contentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 10f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentSize = content.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = viewportRT;
        scroll.content = _contentRoot;
    }

    private void MakeLabel(Transform parent, string textValue, float size, FontStyles style,
        Color color, float height, bool wrap)
    {
        GameObject obj = CreateUIObj("Label", parent);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.font = TMP_Settings.defaultFontAsset;
        text.enableWordWrapping = wrap;
        text.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
    }

    private void SetState(string text)
    {
        ClearRows();
        if (_stateText == null) return;
        _stateText.gameObject.SetActive(true);
        _stateText.text = text;
    }

    private void HideState()
    {
        if (_stateText != null)
        {
            _stateText.gameObject.SetActive(false);
        }
    }

    private void HandleBackPressed()
    {
        AuthMenuController menu = FindObjectOfType<AuthMenuController>();
        if (menu != null)
        {
            menu.BackToProfile();
            return;
        }

        gameObject.SetActive(false);
    }

    private GameObject CreateButton(string name, Transform parent, string label,
        Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = CreateUIObj(name, parent);
        RectTransform rect = GetOrAddRT(obj);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = GetOrAddImage(obj);
        Button button = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, color);

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform labelRT = GetOrAddRT(labelObj);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.color = GameUiThemeRuntime.Current.text;
        text.alignment = TextAlignmentOptions.Center;
        text.font = TMP_Settings.defaultFontAsset;
        text.raycastTarget = false;

        return obj;
    }

    private static GameObject CreateUIObj(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static RectTransform GetOrAddRT(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        return rect != null ? rect : obj.AddComponent<RectTransform>();
    }

    private static Image GetOrAddImage(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();
        return image != null ? image : obj.AddComponent<Image>();
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

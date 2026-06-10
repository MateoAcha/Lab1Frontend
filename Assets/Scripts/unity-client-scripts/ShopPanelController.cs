using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    private enum ShopTab { Store, Challenges }

    private AuthApiClient _apiClient;
    private Action _backAction;
    private bool _built;
    private ShopTab _activeTab = ShopTab.Store;
    private int _userId;
    private int _userCoins;
    private ShopItemData[] _shopItems;
    private Coroutine _loadRoutine;
    private SkinVisualDatabase _skinVisualDatabase;
    private readonly HashSet<int> _claimedChallenges = new HashSet<int>();

    private TextMeshProUGUI _coinsText;
    private TextMeshProUGUI _stateText;
    private RectTransform _contentRoot;
    private Button _storeTabBtn;
    private Button _challengesTabBtn;
    private Image _storeTabImg;
    private Image _challengesTabImg;
    private readonly List<GameObject> _rows = new List<GameObject>();

    private static readonly Color TabActive   = new Color(0.12f, 0.40f, 0.52f, 1f);
    private static readonly Color TabInactive = new Color(0.18f, 0.22f, 0.28f, 1f);
    private static readonly Color TabActiveChallenge = new Color(0.34f, 0.16f, 0.50f, 1f);
    private const int ChallengeRewardCoins = 100;

    public void Initialize(AuthApiClient apiClient, Action backAction, SkinVisualDatabase skinVisualDatabase = null)
    {
        _apiClient = apiClient;
        _backAction = backAction;
        _skinVisualDatabase = skinVisualDatabase;
        EnsureBuilt();
    }

    public void SetApiClient(AuthApiClient apiClient) => _apiClient = apiClient;

    public void SetSkinVisualDatabase(SkinVisualDatabase skinVisualDatabase) => _skinVisualDatabase = skinVisualDatabase;

    public void Open(int userId)
    {
        _userId = userId;
        EnsureBuilt();
        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _activeTab = ShopTab.Store;
        RefreshTabVisuals();
        _loadRoutine = StartCoroutine(LoadRoutine());
    }

    // ── Load ────────────────────────────────────────────────────────────────

    private IEnumerator LoadRoutine()
    {
        SetState("Loading...");
        ClearRows();
        _userCoins = 0;
        if (_coinsText != null) _coinsText.text = "Coins: ...";

        UserInventoryData inv = null;
        yield return _apiClient.GetInventory(_userId, d => inv = d, _ => { });

        if (inv?.items != null)
        {
            foreach (var item in inv.items)
            {
                if (string.Equals(item.itemType, "Currency", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.itemName, "Gold Coins", StringComparison.OrdinalIgnoreCase))
                {
                    _userCoins = item.quantity;
                    break;
                }
            }
        }

        if (_coinsText != null) _coinsText.text = $"Coins: {_userCoins}";

        if (_activeTab == ShopTab.Store)
            yield return LoadStoreItems();
        else
        {
            yield return LoadChallengeClaims();
            HideState();
            RenderChallenges();
        }
    }

    private IEnumerator LoadStoreItems()
    {
        SetState("Loading shop...");
        ShopCatalogData catalog = null;
        string error = null;

        yield return _apiClient.GetShopItems(d => catalog = d, e => error = e);

        _shopItems = catalog?.items;

        if (error != null) { SetState($"Error: {error}"); yield break; }
        if (_shopItems == null || _shopItems.Length == 0) { SetState("No items available."); yield break; }

        ClearRows();
        HideState();
        foreach (var item in _shopItems)
            CreateStoreRow(item);
    }

    private IEnumerator DoPurchase(int shopItemId)
    {
        string error = null;
        yield return _apiClient.BuyShopItem(_userId, shopItemId, () => { }, e => error = e);

        if (error != null)
            Debug.LogWarning($"[Shop] Purchase failed: {error}");

        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadRoutine());
    }

    // ── Tab switching ────────────────────────────────────────────────────────

    private void OnStoreTabClicked()
    {
        _activeTab = ShopTab.Store;
        RefreshTabVisuals();
        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadRoutine());
    }

    private void OnChallengesTabClicked()
    {
        _activeTab = ShopTab.Challenges;
        RefreshTabVisuals();
        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadRoutine());
    }

    private void RefreshTabVisuals()
    {
        if (_storeTabBtn != null)      _storeTabBtn.interactable      = _activeTab != ShopTab.Store;
        if (_challengesTabBtn != null) _challengesTabBtn.interactable = _activeTab != ShopTab.Challenges;
        if (_storeTabImg != null)      _storeTabImg.color      = _activeTab == ShopTab.Store      ? TabActive          : TabInactive;
        if (_challengesTabImg != null) _challengesTabImg.color = _activeTab == ShopTab.Challenges ? TabActiveChallenge : TabInactive;
    }

    // ── Store rows ──────────────────────────────────────────────────────────

    private void CreateStoreRow(ShopItemData item)
    {
        bool canAfford = _userCoins >= item.goldPrice;

        GameObject row = CreateUIObj("ShopRow", _contentRoot);
        _rows.Add(row);
        GameUiThemeRuntime.StyleSurface(row);
        row.AddComponent<LayoutElement>().minHeight = 220f;

        VerticalLayoutGroup vg = row.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(22, 22, 22, 20);
        vg.spacing = 9f;
        vg.childAlignment = TextAnchor.UpperLeft;
        vg.childControlWidth  = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;
        row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Name + quantity badge
        string qty = item.purchaseQuantity > 1 ? $"  ×{item.purchaseQuantity}" : "";
        MakeStoreTitleRow(row.transform, item, $"{item.itemName}{qty}", 46f);

        // Type • Rarity
        MakeLabel(row.transform, $"{item.itemType}  •  {item.rarity}", 22, FontStyles.Normal,
            new Color(0.60f, 0.82f, 1f, 1f), 36f);

        // Stats
        if (!string.IsNullOrWhiteSpace(item.detailSummary))
            MakeLabel(row.transform, item.detailSummary, 20, FontStyles.Italic,
                new Color(0.74f, 0.90f, 0.76f, 1f), 36f);

        // Price + Buy button row
        GameObject bottom = CreateUIObj("Bottom", row.transform);
        bottom.AddComponent<LayoutElement>().preferredHeight = 58f;

        HorizontalLayoutGroup hlg = bottom.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 12f;

        // Price
        GameObject priceObj = CreateUIObj("Price", bottom.transform);
        priceObj.AddComponent<LayoutElement>().preferredWidth = 220f;
        var priceTMP = priceObj.AddComponent<TextMeshProUGUI>();
        priceTMP.text = $"{item.goldPrice} coins";
        priceTMP.fontSize = 22f;
        priceTMP.fontStyle = FontStyles.Bold;
        priceTMP.color = canAfford ? new Color(1f, 0.85f, 0.15f, 1f) : new Color(0.75f, 0.40f, 0.40f, 1f);
        priceTMP.font = TMP_Settings.defaultFontAsset;
        priceTMP.enableWordWrapping = false;
        priceTMP.alignment = TextAlignmentOptions.MidlineLeft;
        priceTMP.raycastTarget = false;

        // Buy button
        int capturedId = item.shopItemId;
        Color btnColor = canAfford ? new Color(0.14f, 0.42f, 0.22f, 1f) : new Color(0.22f, 0.22f, 0.22f, 0.88f);
        GameObject btnObj = CreateUIObj("BuyBtn", bottom.transform);
        btnObj.AddComponent<LayoutElement>().preferredWidth = 145f;
        Image btnImg = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, btnImg, btnColor);
        btn.interactable = canAfford;
        btn.onClick.AddListener(() => StartCoroutine(DoPurchase(capturedId)));

        GameObject btnLabel = CreateUIObj("Label", btnObj.transform);
        var btnLabelRT = btnLabel.AddComponent<RectTransform>();
        btnLabelRT.anchorMin = Vector2.zero;
        btnLabelRT.anchorMax = Vector2.one;
        btnLabelRT.offsetMin = Vector2.zero;
        btnLabelRT.offsetMax = Vector2.zero;
        var btnTMP = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "Buy";
        btnTMP.fontSize = 21f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.color = Color.white;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.font = TMP_Settings.defaultFontAsset;
        btnTMP.raycastTarget = false;
    }

    // ── Challenge rows ───────────────────────────────────────────────────────

    private void RenderChallenges()
    {
        PlayerStatsData stats = GameStatsTracker.GetCurrentPlayerStats();
        CreateChallengeRow(0, "Kill 100 Melee Enemies",  stats.meleeEnemiesKilled, 100);
        CreateChallengeRow(1, "Kill 100 Ranged Enemies", stats.rangedEnemiesKilled, 100);
        CreateChallengeRow(2, "Survive 1 Minute Total",  (int)stats.timePlayedSeconds, 60);
    }

    private static string GetChallengeKey(int index)
    {
        switch (index)
        {
            case 0: return "kill_melee_100";
            case 1: return "kill_ranged_100";
            case 2: return "survive_total_60";
            default: return "challenge_" + index;
        }
    }

    private static string ClaimKey(int userId, int index) =>
        $"challenge_claimed_{userId}_{index}";

    private bool IsClaimed(int index) =>
        _claimedChallenges.Contains(index) || PlayerPrefs.GetInt(ClaimKey(_userId, index), 0) == 1;

    private void MarkClaimed(int index)
    {
        _claimedChallenges.Add(index);
        PlayerPrefs.SetInt(ClaimKey(_userId, index), 1);
        PlayerPrefs.Save();
    }

    private IEnumerator LoadChallengeClaims()
    {
        _claimedChallenges.Clear();
        LoadLocalChallengeClaims();

        if (_apiClient == null || !AuthSession.IsLoggedIn || string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            yield break;
        }

        ChallengeClaimsData data = null;
        string error = null;
        yield return _apiClient.GetClaimedChallengeRewards(_userId, d => data = d, e => error = e);

        if (error != null)
        {
            Debug.LogWarning($"[Shop] Challenge claim load failed: {error}");
            yield break;
        }

        _claimedChallenges.Clear();
        if (data?.claimedChallengeIds != null)
        {
            for (int i = 0; i < data.claimedChallengeIds.Length; i++)
            {
                _claimedChallenges.Add(data.claimedChallengeIds[i]);
            }
        }

        if (data?.claims != null)
        {
            for (int i = 0; i < data.claims.Length; i++)
            {
                if (data.claims[i] != null)
                {
                    _claimedChallenges.Add(data.claims[i].challengeId);
                }
            }
        }

        foreach (int claimedIndex in _claimedChallenges)
        {
            PlayerPrefs.SetInt(ClaimKey(_userId, claimedIndex), 1);
        }
        PlayerPrefs.Save();
    }

    private void LoadLocalChallengeClaims()
    {
        for (int i = 0; i < 3; i++)
        {
            if (PlayerPrefs.GetInt(ClaimKey(_userId, i), 0) == 1)
            {
                _claimedChallenges.Add(i);
            }
        }
    }

    private void CreateChallengeRow(int index, string title, int current, int goal)
    {
        float progress  = goal > 0 ? Mathf.Clamp01((float)current / goal) : 0f;
        bool  completed = current >= goal;
        bool  claimed   = IsClaimed(index);
        bool  claimable = completed && !claimed;

        GameObject row = CreateUIObj("ChallengeRow", _contentRoot);
        _rows.Add(row);
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = claimed
            ? new Color(0.10f, 0.14f, 0.18f, 0.98f)
            : completed
                ? new Color(0.12f, 0.22f, 0.15f, 0.98f)
                : new Color(0.16f, 0.18f, 0.24f, 0.98f);
        GameUiThemeRuntime.ApplyBorder(row);
        row.AddComponent<LayoutElement>().minHeight = 205f;

        VerticalLayoutGroup vg = row.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(22, 22, 20, 18);
        vg.spacing = 10f;
        vg.childAlignment = TextAnchor.UpperLeft;
        vg.childControlWidth  = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;
        row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title + reward hint
        Color titleColor = claimed
            ? new Color(0.55f, 0.60f, 0.65f, 1f)
            : completed
                ? new Color(0.35f, 1f, 0.50f, 1f)
                : new Color(0.95f, 0.97f, 1f, 1f);
        MakeLabel(row.transform, $"{title}  —  🏆 {ChallengeRewardCoins} coins", 27, FontStyles.Bold, titleColor, 44f);

        // Progress bar
        int filled = Mathf.RoundToInt(24 * progress);
        string bar = new string('\u2588', filled) + new string('\u2591', 24 - filled);
        string progressText = $"{bar}   {current} / {goal}";
        Color barColor = claimed
            ? new Color(0.45f, 0.50f, 0.55f, 1f)
            : completed
                ? new Color(0.30f, 0.95f, 0.45f, 1f)
                : new Color(0.40f, 0.70f, 1f, 1f);
        MakeLabel(row.transform, progressText, 20, FontStyles.Normal, barColor, 36f);

        // Bottom row
        GameObject bottom = CreateUIObj("Bottom", row.transform);
        bottom.AddComponent<LayoutElement>().preferredHeight = 56f;

        HorizontalLayoutGroup hlg = bottom.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 14f;

        // Status label
        GameObject statusObj = CreateUIObj("Status", bottom.transform);
        statusObj.AddComponent<LayoutElement>().preferredWidth = 260f;
        var statusTMP = statusObj.AddComponent<TextMeshProUGUI>();
        statusTMP.text = claimed ? "Reward claimed." : completed ? "Completed!" : "In progress...";
        statusTMP.fontSize = 20f;
        statusTMP.fontStyle = (completed && !claimed) ? FontStyles.Bold : FontStyles.Italic;
        statusTMP.color = claimed
            ? new Color(0.50f, 0.55f, 0.60f, 0.85f)
            : completed
                ? new Color(0.30f, 1f, 0.45f, 1f)
                : new Color(0.65f, 0.70f, 0.80f, 0.85f);
        statusTMP.font = TMP_Settings.defaultFontAsset;
        statusTMP.enableWordWrapping = false;
        statusTMP.alignment = TextAlignmentOptions.MidlineLeft;
        statusTMP.raycastTarget = false;

        // Claim button
        Color claimColor = claimable
            ? new Color(0.55f, 0.38f, 0.10f, 1f)
            : new Color(0.25f, 0.25f, 0.27f, 0.85f);
        GameObject claimObj = CreateUIObj("ClaimBtn", bottom.transform);
        claimObj.AddComponent<LayoutElement>().preferredWidth = 190f;
        Image claimImg = claimObj.AddComponent<Image>();
        Button claimBtn = claimObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(claimBtn, claimImg, claimColor);
        claimBtn.interactable = claimable;

        if (claimable)
        {
            int capturedIndex = index;
            claimBtn.onClick.AddListener(() => StartCoroutine(ClaimReward(capturedIndex)));
        }

        GameObject claimLabel = CreateUIObj("Label", claimObj.transform);
        var claimLabelRT = claimLabel.AddComponent<RectTransform>();
        claimLabelRT.anchorMin = Vector2.zero;
        claimLabelRT.anchorMax = Vector2.one;
        claimLabelRT.offsetMin = Vector2.zero;
        claimLabelRT.offsetMax = Vector2.zero;
        var claimTMP = claimLabel.AddComponent<TextMeshProUGUI>();
        claimTMP.text = claimed ? "Claimed" : $"Claim {ChallengeRewardCoins} coins";
        claimTMP.fontSize = 20f;
        claimTMP.fontStyle = FontStyles.Bold;
        claimTMP.color = claimable ? Color.white : new Color(0.60f, 0.62f, 0.65f, 0.85f);
        claimTMP.alignment = TextAlignmentOptions.Center;
        claimTMP.font = TMP_Settings.defaultFontAsset;
        claimTMP.raycastTarget = false;
    }

    private IEnumerator ClaimReward(int index)
    {
        string error = null;
        yield return _apiClient.SaveChallengeClaim(
            _userId,
            index,
            GetChallengeKey(index),
            ChallengeRewardCoins,
            () => { },
            e => error = e);

        if (error != null)
        {
            Debug.LogWarning($"[Shop] Claim reward failed: {error}");
            yield break;
        }

        MarkClaimed(index);

        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadRoutine());
    }

    // ── UI Building ──────────────────────────────────────────────────────────

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        // Root fills the screen
        RectTransform rt = GetOrAddRT(gameObject);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(gameObject, GameUiThemeRuntime.Current.shopBackground, true);

        // Back button — top-left
        GameObject backBtn = CreateButton("BackBtn", transform, "Back",
            new Vector2(16f, -16f), new Vector2(130f, 46f),
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Color(0.18f, 0.23f, 0.30f, 1f));
        backBtn.GetComponent<Button>().onClick.AddListener(() => _backAction?.Invoke());

        // Coins — top-right
        GameObject coinsObj = CreateUIObj("Coins", transform);
        RectTransform coinsRT = GetOrAddRT(coinsObj);
        coinsRT.anchorMin = new Vector2(1f, 1f);
        coinsRT.anchorMax = new Vector2(1f, 1f);
        coinsRT.pivot     = new Vector2(1f, 1f);
        coinsRT.sizeDelta = new Vector2(300f, 54f);
        coinsRT.anchoredPosition = new Vector2(-16f, -16f);
        _coinsText = coinsObj.AddComponent<TextMeshProUGUI>();
        _coinsText.text = "Coins: ...";
        _coinsText.fontSize = 24f;
        _coinsText.fontStyle = FontStyles.Bold;
        _coinsText.color = new Color(1f, 0.85f, 0.15f, 1f);
        _coinsText.font = TMP_Settings.defaultFontAsset;
        _coinsText.alignment = TextAlignmentOptions.MidlineRight;
        _coinsText.raycastTarget = false;

        // Title
        GameObject titleObj = CreateUIObj("Title", transform);
        RectTransform titleRT = GetOrAddRT(titleObj);
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(560f, 70f);
        titleRT.anchoredPosition = new Vector2(0f, -18f);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "STORE";
        titleTMP.fontSize = 46f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.95f, 0.97f, 1f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.font = TMP_Settings.defaultFontAsset;
        titleTMP.raycastTarget = false;

        // Tab bar
        BuildTabBar();

        // State text (loading / error)
        GameObject stateObj = CreateUIObj("StateText", transform);
        RectTransform stateRT = GetOrAddRT(stateObj);
        stateRT.anchorMin = new Vector2(0.1f, 0.4f);
        stateRT.anchorMax = new Vector2(0.9f, 0.6f);
        stateRT.pivot     = new Vector2(0.5f, 0.5f);
        stateRT.offsetMin = Vector2.zero;
        stateRT.offsetMax = Vector2.zero;
        stateRT.sizeDelta = Vector2.zero;
        stateRT.anchoredPosition = Vector2.zero;
        _stateText = stateObj.AddComponent<TextMeshProUGUI>();
        _stateText.text = "";
        _stateText.fontSize = 28f;
        _stateText.color = new Color(0.88f, 0.90f, 0.96f, 1f);
        _stateText.alignment = TextAlignmentOptions.Center;
        _stateText.font = TMP_Settings.defaultFontAsset;
        _stateText.raycastTarget = false;

        // Scroll view
        BuildScrollView();
    }

    private void BuildTabBar()
    {
        GameObject tabBar = CreateUIObj("TabBar", transform);
        RectTransform tabRT = GetOrAddRT(tabBar);
        tabRT.anchorMin = new Vector2(0.5f, 1f);
        tabRT.anchorMax = new Vector2(0.5f, 1f);
        tabRT.pivot     = new Vector2(0.5f, 1f);
        tabRT.sizeDelta = new Vector2(500f, 58f);
        tabRT.anchoredPosition = new Vector2(0f, -86f);

        HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 6f;

        (_storeTabBtn, _storeTabImg)           = BuildTabButton(tabBar.transform, "Store",      TabActive);
        (_challengesTabBtn, _challengesTabImg) = BuildTabButton(tabBar.transform, "Challenges", TabInactive);

        _storeTabBtn.onClick.AddListener(OnStoreTabClicked);
        _challengesTabBtn.onClick.AddListener(OnChallengesTabClicked);
        _storeTabBtn.interactable = false; // active by default
    }

    private (Button, Image) BuildTabButton(Transform parent, string label, Color color)
    {
        GameObject obj = CreateUIObj("Tab_" + label, parent);
        Image img = obj.AddComponent<Image>();
        Button btn = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, img, color);

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = GameUiThemeRuntime.Current.text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.raycastTarget = false;

        return (btn, img);
    }

    private void BuildScrollView()
    {
        GameObject scrollRoot = CreateUIObj("ScrollView", transform);
        RectTransform scrollRT = GetOrAddRT(scrollRoot);
        scrollRT.anchorMin = new Vector2(0.01f, 0.04f);
        scrollRT.anchorMax = new Vector2(0.99f, 0.88f);
        scrollRT.pivot     = new Vector2(0.5f, 0.5f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        scrollRT.sizeDelta = Vector2.zero;
        scrollRT.anchoredPosition = Vector2.zero;

        GameUiThemeRuntime.StyleSurface(scrollRoot);

        ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.movementType      = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        GameObject viewport = CreateUIObj("Viewport", scrollRoot.transform);
        RectTransform vpRT = GetOrAddRT(viewport);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(10f, 10f);
        vpRT.offsetMax = new Vector2(-10f, -10f);
        GetOrAddImage(viewport).color = new Color(1f, 1f, 1f, 0.02f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObj("Content", viewport.transform);
        _contentRoot = GetOrAddRT(content);
        _contentRoot.anchorMin = new Vector2(0f, 1f);
        _contentRoot.anchorMax = new Vector2(1f, 1f);
        _contentRoot.pivot     = new Vector2(0.5f, 1f);
        _contentRoot.offsetMin = Vector2.zero;
        _contentRoot.offsetMax = Vector2.zero;
        _contentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentVG = content.AddComponent<VerticalLayoutGroup>();
        contentVG.padding              = new RectOffset(0, 0, 0, 0);
        contentVG.spacing              = 10f;
        contentVG.childAlignment       = TextAnchor.UpperCenter;
        contentVG.childControlWidth    = true;
        contentVG.childControlHeight   = true;
        contentVG.childForceExpandWidth  = true;
        contentVG.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = vpRT;
        scroll.content  = _contentRoot;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void MakeLabel(Transform parent, string text, float size, FontStyles style, Color color, float height)
    {
        GameObject obj = CreateUIObj("Label", parent);
        obj.AddComponent<LayoutElement>().preferredHeight = height;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    private void MakeStoreTitleRow(Transform parent, ShopItemData item, string title, float height)
    {
        GameObject row = CreateUIObj("TitleRow", parent);
        row.AddComponent<LayoutElement>().preferredHeight = height;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        if (IsSkinItem(item))
            CreateSkinPreview(row.transform, item.skinId, new Vector2(34f, 34f));
        else if (IsWeaponItem(item))
            CreateWeaponSwatch(row.transform, ResolveWeaponColor(item), new Vector2(24f, 24f));

        GameObject label = CreateUIObj("Label", row.transform);
        LayoutElement labelLayout = label.AddComponent<LayoutElement>();
        labelLayout.minWidth = 0f;
        labelLayout.preferredHeight = height;
        labelLayout.flexibleWidth = 1f;

        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 30f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.97f, 0.98f, 1f, 1f);
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    private void CreateWeaponSwatch(Transform parent, Color color, Vector2 size)
    {
        GameObject border = CreateUIObj("WeaponColor", parent);
        LayoutElement borderLayout = border.AddComponent<LayoutElement>();
        borderLayout.preferredWidth = size.x;
        borderLayout.preferredHeight = size.y;
        Image borderImage = GetOrAddImage(border);
        borderImage.color = new Color(0.93f, 0.96f, 1f, 0.85f);

        GameObject fill = CreateUIObj("Fill", border.transform);
        RectTransform fillRect = GetOrAddRT(fill);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        GetOrAddImage(fill).color = color;
    }

    private void CreateSkinPreview(Transform parent, int skinId, Vector2 size)
    {
        GameObject preview = CreateUIObj("SkinPreview", parent);
        LayoutElement layout = preview.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        GetOrAddImage(preview).color = new Color(0.88f, 0.94f, 1f, 0.25f);

        GameObject body = CreateUIObj("SkinColor", preview.transform);
        RectTransform bodyRect = GetOrAddRT(body);
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(4f, 4f);
        bodyRect.offsetMax = new Vector2(-4f, -4f);
        Image bodyImage = GetOrAddImage(body);
        if (TryGetSkinSprite(skinId, out Sprite sprite))
        {
            bodyImage.sprite = sprite;
            bodyImage.color = Color.white;
            bodyImage.preserveAspect = true;
        }
        else
        {
            bodyImage.sprite = SimpleSprite.Square;
            bodyImage.color = Color.white;
            bodyImage.preserveAspect = false;
        }
    }

    private bool TryGetSkinSprite(int skinId, out Sprite sprite)
    {
        sprite = null;
        if (_skinVisualDatabase == null)
            _skinVisualDatabase = FindObjectOfType<SkinVisualDatabase>();

        if (_skinVisualDatabase != null && _skinVisualDatabase.TryGetSprite(skinId, out sprite))
            return true;

        if (SkinVisualDatabase.TryGetSpriteGlobal(skinId, out sprite))
            return true;

        sprite = SkinVisualDatabase.GetSpriteSetOrDefault(skinId).PreviewOrFirstSprite;
        return sprite != null;
    }

    private static bool IsWeaponItem(ShopItemData item)
    {
        return item != null && string.Equals(item.itemType, "Weapon", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSkinItem(ShopItemData item)
    {
        return item != null && (item.skinId > 0 || string.Equals(item.itemType, "Skin", StringComparison.OrdinalIgnoreCase));
    }

    private static Color ResolveWeaponColor(ShopItemData item)
    {
        if (item == null) return Color.white;
        Color fallback = Color.white;
        Color color = PlayerLoadout.ParseWeaponColor(item.weaponColor, fallback);
        if (color != fallback || !string.IsNullOrWhiteSpace(item.weaponColor))
            return color;
        return PlayerLoadout.ParseWeaponColor(item.weapon_color, fallback);
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
        if (_stateText != null) _stateText.gameObject.SetActive(false);
    }

    private void ClearRows()
    {
        for (int i = _rows.Count - 1; i >= 0; i--)
            if (_rows[i] != null) Destroy(_rows[i]);
        _rows.Clear();
    }

    private GameObject CreateButton(string name, Transform parent, string label,
        Vector2 anchoredPos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = CreateUIObj(name, parent);
        RectTransform rt = GetOrAddRT(obj);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = anchorMin;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Image img = GetOrAddImage(obj);
        Button btn = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, img, color);

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = GameUiThemeRuntime.Current.text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.raycastTarget = false;

        return obj;
    }

    private static GameObject CreateUIObj(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static RectTransform GetOrAddRT(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        return rt != null ? rt : obj.AddComponent<RectTransform>();
    }

    private static Image GetOrAddImage(GameObject obj)
    {
        var img = obj.GetComponent<Image>();
        return img != null ? img : obj.AddComponent<Image>();
    }
}

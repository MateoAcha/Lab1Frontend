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

    public void Initialize(AuthApiClient apiClient, Action backAction)
    {
        _apiClient = apiClient;
        _backAction = backAction;
        EnsureBuilt();
    }

    public void SetApiClient(AuthApiClient apiClient) => _apiClient = apiClient;

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
        ClearRows();
        HideState();
        RenderChallenges();
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
        row.AddComponent<Image>().color = new Color(0.16f, 0.20f, 0.27f, 0.98f);

        VerticalLayoutGroup vg = row.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(18, 18, 12, 10);
        vg.spacing = 5f;
        vg.childAlignment = TextAnchor.UpperLeft;
        vg.childControlWidth  = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;
        row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Name + quantity badge
        string qty = item.purchaseQuantity > 1 ? $"  ×{item.purchaseQuantity}" : "";
        MakeLabel(row.transform, $"{item.itemName}{qty}", 22, FontStyles.Bold,
            new Color(0.97f, 0.98f, 1f, 1f), 30f);

        // Type • Rarity
        MakeLabel(row.transform, $"{item.itemType}  •  {item.rarity}", 16, FontStyles.Normal,
            new Color(0.60f, 0.82f, 1f, 1f), 22f);

        // Stats
        if (!string.IsNullOrWhiteSpace(item.detailSummary))
            MakeLabel(row.transform, item.detailSummary, 14, FontStyles.Italic,
                new Color(0.74f, 0.90f, 0.76f, 1f), 20f);

        // Price + Buy button row
        GameObject bottom = CreateUIObj("Bottom", row.transform);
        bottom.AddComponent<LayoutElement>().preferredHeight = 40f;

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
        priceTMP.fontSize = 17f;
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
        btnObj.AddComponent<LayoutElement>().preferredWidth = 120f;
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = btnColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable = canAfford;
        ColorBlock cb = btn.colors;
        cb.normalColor   = btnColor;
        cb.highlightedColor = canAfford ? btnColor * 1.15f : btnColor;
        cb.pressedColor  = canAfford ? btnColor * 0.85f : btnColor;
        cb.selectedColor = btnColor;
        cb.disabledColor = new Color(0.22f, 0.22f, 0.22f, 0.55f);
        btn.colors = cb;
        btn.onClick.AddListener(() => StartCoroutine(DoPurchase(capturedId)));

        GameObject btnLabel = CreateUIObj("Label", btnObj.transform);
        var btnLabelRT = btnLabel.AddComponent<RectTransform>();
        btnLabelRT.anchorMin = Vector2.zero;
        btnLabelRT.anchorMax = Vector2.one;
        btnLabelRT.offsetMin = Vector2.zero;
        btnLabelRT.offsetMax = Vector2.zero;
        var btnTMP = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "Buy";
        btnTMP.fontSize = 16f;
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
        CreateChallengeRow("Kill 100 Melee Enemies",  stats.meleeEnemiesKilled, 100);
        CreateChallengeRow("Kill 100 Ranged Enemies", stats.rangedEnemiesKilled, 100);
        CreateChallengeRow("Survive 1 Minute Total",  (int)stats.timePlayedSeconds, 60);
    }

    private void CreateChallengeRow(string title, int current, int goal)
    {
        float progress  = goal > 0 ? Mathf.Clamp01((float)current / goal) : 0f;
        bool  completed = current >= goal;

        GameObject row = CreateUIObj("ChallengeRow", _contentRoot);
        _rows.Add(row);
        row.AddComponent<Image>().color = completed
            ? new Color(0.12f, 0.22f, 0.15f, 0.98f)
            : new Color(0.16f, 0.18f, 0.24f, 0.98f);

        VerticalLayoutGroup vg = row.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(18, 18, 14, 12);
        vg.spacing = 8f;
        vg.childAlignment = TextAnchor.UpperLeft;
        vg.childControlWidth  = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;
        row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        Color titleColor = completed ? new Color(0.35f, 1f, 0.50f, 1f) : new Color(0.95f, 0.97f, 1f, 1f);
        MakeLabel(row.transform, title, 20, FontStyles.Bold, titleColor, 28f);

        // Progress bar (text-based)
        int filled = Mathf.RoundToInt(24 * progress);
        string bar = new string('\u2588', filled) + new string('\u2591', 24 - filled);
        string progressText = $"{bar}   {current} / {goal}";
        Color barColor = completed ? new Color(0.30f, 0.95f, 0.45f, 1f) : new Color(0.40f, 0.70f, 1f, 1f);
        MakeLabel(row.transform, progressText, 14, FontStyles.Normal, barColor, 22f);

        // Claim button (disabled for now)
        GameObject bottom = CreateUIObj("Bottom", row.transform);
        bottom.AddComponent<LayoutElement>().preferredHeight = 38f;

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
        statusTMP.text = completed ? "Completed!" : "In progress...";
        statusTMP.fontSize = 15f;
        statusTMP.fontStyle = completed ? FontStyles.Bold : FontStyles.Italic;
        statusTMP.color = completed ? new Color(0.30f, 1f, 0.45f, 1f) : new Color(0.65f, 0.70f, 0.80f, 0.85f);
        statusTMP.font = TMP_Settings.defaultFontAsset;
        statusTMP.enableWordWrapping = false;
        statusTMP.alignment = TextAlignmentOptions.MidlineLeft;
        statusTMP.raycastTarget = false;

        // Claim button (always disabled — rewards coming soon)
        Color claimColor = new Color(0.25f, 0.25f, 0.27f, 0.85f);
        GameObject claimObj = CreateUIObj("ClaimBtn", bottom.transform);
        claimObj.AddComponent<LayoutElement>().preferredWidth = 160f;
        Image claimImg = claimObj.AddComponent<Image>();
        claimImg.color = claimColor;
        Button claimBtn = claimObj.AddComponent<Button>();
        claimBtn.targetGraphic = claimImg;
        claimBtn.interactable = false;
        ColorBlock cb = claimBtn.colors;
        cb.disabledColor = new Color(0.25f, 0.25f, 0.27f, 0.60f);
        claimBtn.colors = cb;

        GameObject claimLabel = CreateUIObj("Label", claimObj.transform);
        var claimLabelRT = claimLabel.AddComponent<RectTransform>();
        claimLabelRT.anchorMin = Vector2.zero;
        claimLabelRT.anchorMax = Vector2.one;
        claimLabelRT.offsetMin = Vector2.zero;
        claimLabelRT.offsetMax = Vector2.zero;
        var claimTMP = claimLabel.AddComponent<TextMeshProUGUI>();
        claimTMP.text = "Claim (coming soon)";
        claimTMP.fontSize = 14f;
        claimTMP.fontStyle = FontStyles.Normal;
        claimTMP.color = new Color(0.60f, 0.62f, 0.65f, 0.85f);
        claimTMP.alignment = TextAlignmentOptions.Center;
        claimTMP.font = TMP_Settings.defaultFontAsset;
        claimTMP.raycastTarget = false;
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
        GetOrAddImage(gameObject).color = new Color(0.07f, 0.10f, 0.14f, 0.97f);

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
        coinsRT.sizeDelta = new Vector2(240f, 46f);
        coinsRT.anchoredPosition = new Vector2(-16f, -16f);
        _coinsText = coinsObj.AddComponent<TextMeshProUGUI>();
        _coinsText.text = "Coins: ...";
        _coinsText.fontSize = 18f;
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
        titleRT.sizeDelta = new Vector2(500f, 60f);
        titleRT.anchoredPosition = new Vector2(0f, -18f);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "STORE";
        titleTMP.fontSize = 36f;
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
        _stateText.fontSize = 22f;
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
        tabRT.sizeDelta = new Vector2(400f, 48f);
        tabRT.anchoredPosition = new Vector2(0f, -74f);

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
        img.color = color;
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = color;
        cb.highlightedColor = color * 1.12f;
        cb.pressedColor     = color * 0.88f;
        cb.selectedColor    = color;
        cb.disabledColor    = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
        btn.colors = cb;

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 19f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
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

        GetOrAddImage(scrollRoot).color = new Color(0.11f, 0.14f, 0.19f, 0.92f);

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
        img.color = color;
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = color;
        cb.highlightedColor = color * 1.10f;
        cb.pressedColor     = color * 0.90f;
        cb.selectedColor    = color;
        btn.colors = cb;

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
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

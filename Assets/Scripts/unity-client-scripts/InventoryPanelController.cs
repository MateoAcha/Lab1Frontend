using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelController : MonoBehaviour
{
    private readonly List<GameObject> _rows = new List<GameObject>();

    private AuthApiClient _apiClient;
    private Action _backAction;
    private RectTransform _contentRoot;
    private TextMeshProUGUI _stateText;
    private TextMeshProUGUI _equippedWeaponText;
    private TextMeshProUGUI _equippedArmorText;
    private TextMeshProUGUI _equippedConsumableText;
    private TextMeshProUGUI _equippedSkinText;
    private Coroutine _loadRoutine;
    private bool _built;
    private int _userId;
    private SkinData[] _userSkins;
    private SkinVisualDatabase _skinVisualDatabase;

    // Inspect overlay
    private GameObject _inspectOverlay;
    private TextMeshProUGUI _inspectNameText;
    private TextMeshProUGUI _inspectMetaText;
    private TextMeshProUGUI _inspectDescText;
    private TextMeshProUGUI _inspectStatsText;
    private Button _equipButton;
    private TextMeshProUGUI _equipButtonLabel;
    private InventoryItemData _inspectItem;

    public void Initialize(AuthApiClient apiClient, Action backAction, SkinVisualDatabase skinVisualDatabase = null)
    {
        _apiClient = apiClient;
        _backAction = backAction;
        _skinVisualDatabase = skinVisualDatabase;
        EnsureBuilt();
    }

    public void SetSkinVisualDatabase(SkinVisualDatabase skinVisualDatabase)
    {
        _skinVisualDatabase = skinVisualDatabase;
    }

    public void SetApiClient(AuthApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public void LoadInventory(int userId)
    {
        _userId = userId;
        EnsureBuilt();

        if (_loadRoutine != null)
            StopCoroutine(_loadRoutine);

        _loadRoutine = StartCoroutine(LoadInventoryRoutine(userId));
    }

    private IEnumerator LoadInventoryRoutine(int userId)
    {
        ClearRows();
        SetState("Loading inventory...");
        PopulateEquipped();

        if (_apiClient == null)
        {
            SetState("Inventory API is not configured.");
            yield break;
        }

        UserInventoryData response = null;
        string inventoryError = null;
        yield return _apiClient.GetInventory(
            userId,
            onSuccess: data => response = data,
            onError: message => inventoryError = message);

        if (!string.IsNullOrWhiteSpace(inventoryError))
        {
            SetState(inventoryError);
            yield break;
        }

        UserSkinsData skinsResponse = null;
        yield return _apiClient.GetSkins(
            userId,
            onSuccess: data => skinsResponse = data,
            onError: _ => { });

        _userSkins = skinsResponse?.skins;
        PlayerLoadout.ApplySkin(_userSkins);

        InventoryItemData[] items = response != null ? response.items : null;
        PlayerLoadout.ApplyFromItems(items);
        PopulateEquipped();

        bool hasItems = items != null && items.Length > 0;
        bool hasSkins = _userSkins != null && _userSkins.Length > 0;

        if (!hasItems && !hasSkins)
        {
            SetState("This player has no inventory items yet.");
            yield break;
        }

        _stateText.gameObject.SetActive(false);

        if (hasItems)
        {
            for (int i = 0; i < items.Length; i++)
                CreateRow(items[i]);
        }

        if (hasSkins)
        {
            CreateSectionHeader("── Skins ──");
            for (int i = 0; i < _userSkins.Length; i++)
                CreateSkinRow(_userSkins[i]);
        }
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        RectTransform panelRect = GetOrAddRectTransform(gameObject);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameUiThemeRuntime.StylePanel(gameObject, GameUiThemeRuntime.Current.inventoryBackground, true);

        GameObject titleObj = CreateUIObject("Title", transform);
        RectTransform titleRect = GetOrAddRectTransform(titleObj);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(760f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        TextMeshProUGUI titleText = CreateText(titleObj.transform, "Inventory", 34, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.96f, 1f, 1f);

        GameObject subtitleObj = CreateUIObject("Subtitle", transform);
        RectTransform subtitleRect = GetOrAddRectTransform(subtitleObj);
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.sizeDelta = new Vector2(820f, 36f);
        subtitleRect.anchoredPosition = new Vector2(0f, -78f);
        TextMeshProUGUI subtitleText = CreateText(subtitleObj.transform,
            "Click an item to inspect it and equip it.", 18, FontStyles.Normal);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.78f, 0.84f, 0.9f, 1f);

        CreateButton("BackButton", transform, "Back",
            new Vector2(100f, -34f), new Vector2(150f, 46f),
            new Color(0.18f, 0.23f, 0.3f, 1f))
            .GetComponent<Button>().onClick.AddListener(HandleBackPressed);

        GameObject stateObj = CreateUIObject("StateText", transform);
        RectTransform stateRect = GetOrAddRectTransform(stateObj);
        stateRect.anchorMin = new Vector2(0.5f, 0.5f);
        stateRect.anchorMax = new Vector2(0.5f, 0.5f);
        stateRect.pivot = new Vector2(0.5f, 0.5f);
        stateRect.sizeDelta = new Vector2(760f, 80f);
        stateRect.anchoredPosition = new Vector2(0f, -12f);
        _stateText = CreateText(stateObj.transform, "", 22, FontStyles.Normal);
        _stateText.alignment = TextAlignmentOptions.Center;
        _stateText.color = new Color(0.9f, 0.92f, 0.96f, 1f);

        // Scroll view
        GameObject scrollRoot = CreateUIObject("ScrollView", transform);
        RectTransform scrollRect = GetOrAddRectTransform(scrollRoot);
        scrollRect.anchorMin = new Vector2(0.01f, 0.05f);
        scrollRect.anchorMax = new Vector2(0.64f, 0.93f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        scrollRect.sizeDelta = Vector2.zero;
        scrollRect.anchoredPosition = Vector2.zero;
        GameUiThemeRuntime.StyleSurface(scrollRoot);

        ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        GameObject viewport = CreateUIObject("Viewport", scrollRoot.transform);
        RectTransform viewportRect = GetOrAddRectTransform(viewport);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(14f, 14f);
        viewportRect.offsetMax = new Vector2(-14f, -14f);
        GetOrAddImage(viewport).color = new Color(1f, 1f, 1f, 0.025f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        _contentRoot = GetOrAddRectTransform(content);
        _contentRoot.anchorMin = new Vector2(0f, 1f);
        _contentRoot.anchorMax = new Vector2(1f, 1f);
        _contentRoot.pivot = new Vector2(0.5f, 1f);
        _contentRoot.offsetMin = Vector2.zero;
        _contentRoot.offsetMax = Vector2.zero;
        _contentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 12f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentSize = content.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = viewportRect;
        scroll.content = _contentRoot;

        BuildEquippedPanel();
        BuildInspectOverlay();
    }

    private void CreateRow(InventoryItemData item)
    {
        GameObject row = CreateUIObject("InventoryRow", _contentRoot);
        _rows.Add(row);

        bool equipped = IsEquipped(item);
        Image rowImage = GetOrAddImage(row);
        rowImage.color = equipped
            ? new Color(0.14f, 0.28f, 0.22f, 0.98f)
            : new Color(0.18f, 0.22f, 0.29f, 0.98f);

        Button rowButton = row.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(rowButton, rowImage, rowImage.color);

        InventoryItemData captured = item;
        rowButton.onClick.AddListener(() => ShowInspectPanel(captured));

        VerticalLayoutGroup group = row.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(18, 18, 12, 12);
        group.spacing = 6f;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        ContentSizeFitter rowSize = row.AddComponent<ContentSizeFitter>();
        rowSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        string name = !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : "Unnamed Item";
        string detailSummary = !string.IsNullOrWhiteSpace(item.detailSummary) ? item.detailSummary : "—";
        string equippedTag = equipped ? "  [EQUIPPED]" : "";

        TextMeshProUGUI nameText = CreateItemTitleRow(
            row.transform,
            item,
            $"{name}  x{Mathf.Max(0, item.quantity)}{equippedTag}",
            42f,
            30f);
        nameText.color = equipped ? new Color(0.6f, 1f, 0.72f, 1f) : new Color(0.97f, 0.98f, 1f, 1f);

        TextMeshProUGUI statsText = CreateText(
            CreateSection("Stats", row.transform, 30f).transform,
            detailSummary, 19, FontStyles.Italic, true);
        statsText.color = new Color(0.76f, 0.86f, 0.77f, 1f);
    }

    private bool IsEquipped(InventoryItemData item)
    {
        if (item == null) return false;
        string type = item.itemType ?? "";
        InventoryItemData equipped = null;
        if (string.Equals(type, "Weapon", StringComparison.OrdinalIgnoreCase))
            equipped = PlayerLoadout.EquippedWeapon;
        else if (string.Equals(type, "Armor", StringComparison.OrdinalIgnoreCase))
            equipped = PlayerLoadout.EquippedArmor;
        else if (string.Equals(type, "Consumable", StringComparison.OrdinalIgnoreCase))
            equipped = PlayerLoadout.EquippedConsumable;
        return equipped != null && equipped.itemId == item.itemId;
    }

    private void BuildInspectOverlay()
    {
        _inspectOverlay = CreateUIObject("InspectOverlay", transform);
        RectTransform overlayRect = GetOrAddRectTransform(_inspectOverlay);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        GetOrAddImage(_inspectOverlay).color = new Color(0f, 0f, 0f, 0.7f);

        GameObject card = CreateUIObject("InspectCard", _inspectOverlay.transform);
        RectTransform cardRect = GetOrAddRectTransform(card);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(540f, 380f);
        cardRect.anchoredPosition = Vector2.zero;
        GameUiThemeRuntime.StyleSurface(card);

        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(28, 28, 24, 24);
        cardLayout.spacing = 14f;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        ContentSizeFitter cardFitter = card.AddComponent<ContentSizeFitter>();
        cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _inspectNameText = CreateText(CreateSection("IName", card.transform, 38f).transform,
            "", 26, FontStyles.Bold, true);
        _inspectNameText.alignment = TextAlignmentOptions.Center;
        _inspectNameText.color = new Color(0.97f, 0.98f, 1f, 1f);

        _inspectMetaText = CreateText(CreateSection("IMeta", card.transform, 30f).transform,
            "", 18, FontStyles.Normal, true);
        _inspectMetaText.alignment = TextAlignmentOptions.Center;
        _inspectMetaText.color = new Color(0.66f, 0.84f, 1f, 1f);

        _inspectDescText = CreateText(CreateSection("IDesc", card.transform, 60f).transform,
            "", 17, FontStyles.Normal, true);
        _inspectDescText.alignment = TextAlignmentOptions.Center;
        _inspectDescText.color = new Color(0.88f, 0.9f, 0.95f, 1f);

        _inspectStatsText = CreateText(CreateSection("IStats", card.transform, 36f).transform,
            "", 16, FontStyles.Italic, true);
        _inspectStatsText.alignment = TextAlignmentOptions.Center;
        _inspectStatsText.color = new Color(0.76f, 0.86f, 0.77f, 1f);

        // Button row
        GameObject buttonRow = CreateUIObject("ButtonRow", card.transform);
        GetOrAddRectTransform(buttonRow).sizeDelta = new Vector2(0f, 52f);
        LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
        buttonRowLayout.preferredHeight = 52f;

        HorizontalLayoutGroup buttonRowGroup = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonRowGroup.padding = new RectOffset(0, 0, 0, 0);
        buttonRowGroup.spacing = 20f;
        buttonRowGroup.childAlignment = TextAnchor.MiddleCenter;
        buttonRowGroup.childControlWidth = false;
        buttonRowGroup.childControlHeight = true;
        buttonRowGroup.childForceExpandWidth = false;
        buttonRowGroup.childForceExpandHeight = false;

        GameObject equipBtnObj = CreateStyledButton("EquipBtn", buttonRow.transform,
            "Equip", new Color(0.15f, 0.55f, 0.3f, 1f), new Vector2(180f, 46f));
        _equipButton = equipBtnObj.GetComponent<Button>();
        _equipButtonLabel = equipBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        _equipButton.onClick.AddListener(HandleEquipPressed);

        GameObject closeBtnObj = CreateStyledButton("CloseBtn", buttonRow.transform,
            "Close", new Color(0.3f, 0.18f, 0.18f, 1f), new Vector2(140f, 46f));
        closeBtnObj.GetComponent<Button>().onClick.AddListener(HideInspectPanel);

        _inspectOverlay.SetActive(false);
    }

    private void ShowInspectPanel(InventoryItemData item)
    {
        _inspectItem = item;

        string name = !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : "Unnamed Item";
        string type = !string.IsNullOrWhiteSpace(item.itemType) ? item.itemType : "Unknown";
        string rarity = !string.IsNullOrWhiteSpace(item.rarity) ? item.rarity : "Unknown";
        string description = !string.IsNullOrWhiteSpace(item.description) ? item.description : "No description.";
        string stats = !string.IsNullOrWhiteSpace(item.detailSummary) ? item.detailSummary : "No subtype details.";

        _inspectNameText.text = $"{name}  x{Mathf.Max(0, item.quantity)}";
        _inspectMetaText.text = $"Type: {type}    Rarity: {rarity}";
        _inspectDescText.text = description;
        _inspectStatsText.text = stats;

        bool canEquip = string.Equals(type, "Weapon", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "Armor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "Consumable", StringComparison.OrdinalIgnoreCase);

        _equipButton.gameObject.SetActive(canEquip);
        bool alreadyEquipped = IsEquipped(item);
        if (_equipButtonLabel != null)
            _equipButtonLabel.text = alreadyEquipped ? "Equipped" : "Equip";
        _equipButton.interactable = !alreadyEquipped;

        _inspectOverlay.SetActive(true);
    }

    private void HideInspectPanel()
    {
        _inspectOverlay.SetActive(false);
        _inspectItem = null;
    }

    private void HandleEquipPressed()
    {
        if (_inspectItem == null) return;
        if (_apiClient != null && AuthSession.IsLoggedIn)
        {
            StartCoroutine(EquipItemRoutine(_inspectItem));
            return;
        }

        PlayerLoadout.EquipItem(_inspectItem);
        HideInspectPanel();
        LoadInventory(_userId);
    }

    private IEnumerator EquipItemRoutine(InventoryItemData item)
    {
        if (item == null)
            yield break;

        string error = null;
        UserInventoryData inventory = null;
        yield return _apiClient.EquipInventoryItem(
            _userId,
            item.userInventoryId,
            data => inventory = data,
            err => error = err);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Debug.LogWarning($"[Inventory] Equip item failed: {error}");
            yield break;
        }

        PlayerLoadout.ApplyFromItems(inventory?.items);
        HideInspectPanel();
        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadInventoryRoutine(_userId));
    }

    private void ClearRows()
    {
        for (int i = _rows.Count - 1; i >= 0; i--)
        {
            if (_rows[i] != null)
                Destroy(_rows[i]);
        }
        _rows.Clear();
    }

    private void SetState(string text)
    {
        if (_stateText == null) return;
        _stateText.gameObject.SetActive(true);
        _stateText.text = text;
    }

    private void PopulateEquipped()
    {
        SetEquippedSlot(_equippedWeaponText, PlayerLoadout.EquippedWeapon);
        SetEquippedSlot(_equippedArmorText, PlayerLoadout.EquippedArmor);
        SetEquippedSlot(_equippedConsumableText, PlayerLoadout.EquippedConsumable);
        SetEquippedSkinSlot(_equippedSkinText);
    }

    private void SetEquippedSlot(TextMeshProUGUI text, InventoryItemData item)
    {
        if (text == null) return;
        string itemName = item != null && !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : "(none)";
        string rarity = item != null && !string.IsNullOrWhiteSpace(item.rarity) ? item.rarity : "—";
        string detail = item != null && !string.IsNullOrWhiteSpace(item.detailSummary) ? item.detailSummary : "—";
        text.text = $"{itemName}\nRarity: {rarity}\n{detail}";
    }

    private void SetEquippedSkinSlot(TextMeshProUGUI text)
    {
        if (text == null) return;
        if (PlayerLoadout.EquippedSkinId == 0 || string.IsNullOrWhiteSpace(PlayerLoadout.EquippedSkinName))
            text.text = "(none)\nRarity: —\n—";
        else
            text.text = $"{PlayerLoadout.EquippedSkinName}\nEquipped";
    }

    private void CreateSectionHeader(string label)
    {
        GameObject header = CreateUIObject("SectionHeader", _contentRoot);
        _rows.Add(header);

        GetOrAddRectTransform(header).sizeDelta = new Vector2(0f, 36f);
        GetOrAddImage(header).color = new Color(0.1f, 0.12f, 0.17f, 0f);

        LayoutElement le = header.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;

        TextMeshProUGUI text = CreateText(header.transform, label, 18, FontStyles.Bold, false);
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.6f, 0.75f, 0.9f, 0.85f);
    }

    private void CreateSkinRow(SkinData skin)
    {
        if (skin == null) return;

        GameObject row = CreateUIObject("SkinRow", _contentRoot);
        _rows.Add(row);

        bool equipped = skin.equipped;
        Image rowImage = GetOrAddImage(row);
        rowImage.color = equipped
            ? new Color(0.18f, 0.26f, 0.35f, 0.98f)
            : new Color(0.15f, 0.18f, 0.24f, 0.98f);

        VerticalLayoutGroup group = row.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(18, 18, 10, 10);
        group.spacing = 4f;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        ContentSizeFitter rowSize = row.AddComponent<ContentSizeFitter>();
        rowSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        string equippedTag = equipped ? "  [EQUIPPED]" : "";
        string name = !string.IsNullOrWhiteSpace(skin.skinName) ? skin.skinName : "Unknown Skin";
        string rarity = !string.IsNullOrWhiteSpace(skin.rarity) ? skin.rarity : "—";

        GameObject skinHeader = CreateSection("SkinHeader", row.transform, 64f);
        HorizontalLayoutGroup headerLayout = skinHeader.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        CreateSkinPreview(skinHeader.transform, skin.skinId, new Vector2(48f, 48f));

        GameObject skinTextStack = CreateUIObject("SkinTextStack", skinHeader.transform);
        LayoutElement skinTextLayout = skinTextStack.AddComponent<LayoutElement>();
        skinTextLayout.minWidth = 0f;
        skinTextLayout.preferredHeight = 64f;
        skinTextLayout.flexibleWidth = 1f;
        VerticalLayoutGroup skinTextGroup = skinTextStack.AddComponent<VerticalLayoutGroup>();
        skinTextGroup.spacing = 2f;
        skinTextGroup.childAlignment = TextAnchor.MiddleLeft;
        skinTextGroup.childControlWidth = true;
        skinTextGroup.childControlHeight = true;
        skinTextGroup.childForceExpandWidth = true;
        skinTextGroup.childForceExpandHeight = false;

        TextMeshProUGUI nameText = CreateText(
            CreateSection("SkinName", skinTextStack.transform, 34f).transform,
            $"{name}{equippedTag}", 26, FontStyles.Bold, false);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.color = equipped ? new Color(0.5f, 0.85f, 1f, 1f) : new Color(0.9f, 0.93f, 1f, 1f);

        TextMeshProUGUI rarityText = CreateText(
            CreateSection("SkinRarity", skinTextStack.transform, 24f).transform,
            $"Rarity: {rarity}", 17, FontStyles.Italic, false);
        rarityText.color = new Color(0.76f, 0.86f, 0.77f, 1f);

        if (!equipped)
        {
            SkinData captured = skin;
            GameObject equipBtnObj = CreateStyledButton("SkinEquipBtn", row.transform,
                "Equip Skin", new Color(0.12f, 0.4f, 0.6f, 1f), new Vector2(150f, 36f));
            LayoutElement le = equipBtnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 150f;
            le.preferredHeight = 36f;
            equipBtnObj.GetComponent<Button>().onClick.AddListener(() => HandleEquipSkinPressed(captured));
        }
    }

    private void HandleEquipSkinPressed(SkinData skin)
    {
        if (skin == null || _apiClient == null) return;
        StartCoroutine(EquipSkinRoutine(skin));
    }

    private IEnumerator EquipSkinRoutine(SkinData skin)
    {
        string error = null;
        yield return _apiClient.EquipSkin(
            _userId, skin.skinId,
            onSuccess: () => { },
            onError: e => error = e);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Debug.LogWarning($"[Inventory] Equip skin failed: {error}");
            yield break;
        }

        PlayerLoadout.ApplySkin(new[] { new SkinData { skinId = skin.skinId, skinName = skin.skinName, rarity = skin.rarity, equipped = true } });

        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadInventoryRoutine(_userId));
    }

    private void HandleBackPressed()
    {
        _backAction?.Invoke();
    }

    private void BuildEquippedPanel()
    {
        GameObject equippedRoot = CreateUIObject("EquippedPanel", transform);
        RectTransform equippedRect = GetOrAddRectTransform(equippedRoot);
        equippedRect.anchorMin = new Vector2(0.66f, 0.05f);
        equippedRect.anchorMax = new Vector2(0.99f, 0.93f);
        equippedRect.pivot = new Vector2(0.5f, 0.5f);
        equippedRect.offsetMin = Vector2.zero;
        equippedRect.offsetMax = Vector2.zero;
        equippedRect.sizeDelta = Vector2.zero;
        equippedRect.anchoredPosition = Vector2.zero;
        GameUiThemeRuntime.StyleSurface(equippedRoot);

        VerticalLayoutGroup layout = equippedRoot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 18, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = equippedRoot.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        TextMeshProUGUI heading = CreateText(
            CreateSection("EquippedHeading", equippedRoot.transform, 36f).transform,
            "Equipped", 28, FontStyles.Bold, false);
        heading.alignment = TextAlignmentOptions.Center;
        heading.color = new Color(0.95f, 0.97f, 1f, 1f);

        TextMeshProUGUI subtitle = CreateText(
            CreateSection("EquippedSubtitle", equippedRoot.transform, 48f).transform,
            "Click an item row to inspect and equip it.", 16, FontStyles.Normal, true);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.75f, 0.82f, 0.9f, 1f);

        _equippedWeaponText = CreateEquippedSlot(equippedRoot.transform, "Weapon");
        _equippedArmorText = CreateEquippedSlot(equippedRoot.transform, "Armor");
        _equippedConsumableText = CreateEquippedSlot(equippedRoot.transform, "Consumable");
        _equippedSkinText = CreateEquippedSlot(equippedRoot.transform, "Skin");
        PopulateEquipped();
    }

    private TextMeshProUGUI CreateEquippedSlot(Transform parent, string slotName)
    {
        GameObject slot = CreateUIObject(slotName + "Slot", parent);
        GetOrAddRectTransform(slot).sizeDelta = new Vector2(0f, 112f);
        GameUiThemeRuntime.StyleSurface(slot);

        LayoutElement layout = slot.AddComponent<LayoutElement>();
        layout.preferredHeight = 112f;

        VerticalLayoutGroup group = slot.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(14, 14, 12, 12);
        group.spacing = 6f;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        TextMeshProUGUI label = CreateText(
            CreateSection(slotName + "Label", slot.transform, 24f).transform,
            slotName.ToUpperInvariant(), 16, FontStyles.Bold, false);
        label.color = new Color(0.66f, 0.84f, 1f, 1f);

        TextMeshProUGUI body = CreateText(
            CreateSection(slotName + "Body", slot.transform, 58f).transform,
            "", 16, FontStyles.Normal, true);
        body.color = new Color(0.94f, 0.96f, 1f, 1f);
        return body;
    }

    private GameObject CreateStyledButton(string name, Transform parent, string label, Color color, Vector2 size)
    {
        GameObject buttonObj = CreateUIObject(name, parent);
        RectTransform rect = GetOrAddRectTransform(buttonObj);
        rect.sizeDelta = size;

        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        Image image = GetOrAddImage(buttonObj);
        Button button = buttonObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, color);

        GameObject labelObj = CreateUIObject("Label", buttonObj.transform);
        RectTransform labelRect = GetOrAddRectTransform(labelObj);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = CreateText(labelObj.transform, label, 20, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = GameUiThemeRuntime.Current.text;

        return buttonObj;
    }

    private GameObject CreateButton(string name, Transform parent, string label,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObj = CreateUIObject(name, parent);
        RectTransform rect = GetOrAddRectTransform(buttonObj);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = GetOrAddImage(buttonObj);
        Button button = buttonObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, color);

        GameObject labelObj = CreateUIObject("Label", buttonObj.transform);
        RectTransform labelRect = GetOrAddRectTransform(labelObj);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = CreateText(labelObj.transform, label, 22, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = GameUiThemeRuntime.Current.text;

        return buttonObj;
    }

    private TextMeshProUGUI CreateText(Transform parent, string textValue, float fontSize,
        FontStyles fontStyle, bool wrap = false)
    {
        GameObject textObj = CreateUIObject("Text", parent);
        RectTransform rect = GetOrAddRectTransform(textObj);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = wrap;
        text.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;
        text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private TextMeshProUGUI CreateItemTitleRow(Transform parent, InventoryItemData item, string title, float height, float fontSize)
    {
        GameObject section = CreateSection("Name", parent, height);
        HorizontalLayoutGroup layout = section.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (IsWeaponItem(item))
            CreateWeaponSwatch(section.transform, ResolveWeaponColor(item), new Vector2(24f, 24f));

        GameObject textSection = CreateUIObject("NameText", section.transform);
        LayoutElement textLayout = textSection.AddComponent<LayoutElement>();
        textLayout.minWidth = 0f;
        textLayout.preferredHeight = height;
        textLayout.flexibleWidth = 1f;
        textLayout.minHeight = height;

        TextMeshProUGUI text = CreateText(textSection.transform, title, fontSize, FontStyles.Bold, false);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private void CreateWeaponSwatch(Transform parent, Color color, Vector2 size)
    {
        GameObject border = CreateUIObject("WeaponColor", parent);
        LayoutElement borderLayout = border.AddComponent<LayoutElement>();
        borderLayout.preferredWidth = size.x;
        borderLayout.preferredHeight = size.y;
        Image borderImage = GetOrAddImage(border);
        borderImage.color = new Color(0.93f, 0.96f, 1f, 0.85f);

        GameObject fill = CreateUIObject("Fill", border.transform);
        RectTransform fillRect = GetOrAddRectTransform(fill);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        GetOrAddImage(fill).color = color;
    }

    private void CreateSkinPreview(Transform parent, int skinId, Vector2 size)
    {
        GameObject preview = CreateUIObject("SkinPreview", parent);
        LayoutElement layout = preview.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        Image frame = GetOrAddImage(preview);
        frame.color = new Color(0.88f, 0.94f, 1f, 0.25f);

        GameObject body = CreateUIObject("SkinColor", preview.transform);
        RectTransform bodyRect = GetOrAddRectTransform(body);
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(5f, 5f);
        bodyRect.offsetMax = new Vector2(-5f, -5f);
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

    private static bool IsWeaponItem(InventoryItemData item)
    {
        return item != null && string.Equals(item.itemType, "Weapon", StringComparison.OrdinalIgnoreCase);
    }

    private static Color ResolveWeaponColor(InventoryItemData item)
    {
        if (item == null) return Color.white;
        Color fallback = Color.white;
        Color color = PlayerLoadout.ParseWeaponColor(item.weaponColor, fallback);
        if (color != fallback || !string.IsNullOrWhiteSpace(item.weaponColor))
            return color;
        return PlayerLoadout.ParseWeaponColor(item.weapon_color, fallback);
    }

    private GameObject CreateSection(string name, Transform parent, float minHeight)
    {
        GameObject section = CreateUIObject(name, parent);
        RectTransform rect = GetOrAddRectTransform(section);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, minHeight);

        LayoutElement layout = section.AddComponent<LayoutElement>();
        layout.minHeight = minHeight;
        layout.flexibleHeight = 0f;

        ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        return section;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private RectTransform GetOrAddRectTransform(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        return rect != null ? rect : obj.AddComponent<RectTransform>();
    }

    private Image GetOrAddImage(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();
        return image != null ? image : obj.AddComponent<Image>();
    }
}

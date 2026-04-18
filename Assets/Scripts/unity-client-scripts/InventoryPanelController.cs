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
    private Coroutine _loadRoutine;
    private bool _built;

    public void Initialize(AuthApiClient apiClient, Action backAction)
    {
        _apiClient = apiClient;
        _backAction = backAction;
        EnsureBuilt();
    }

    public void SetApiClient(AuthApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public void LoadInventory(int userId)
    {
        EnsureBuilt();

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
        }

        _loadRoutine = StartCoroutine(LoadInventoryRoutine(userId));
    }

    private IEnumerator LoadInventoryRoutine(int userId)
    {
        ClearRows();
        SetState("Loading inventory...");
        PopulateEquipped(null);

        if (_apiClient == null)
        {
            SetState("Inventory API is not configured.");
            yield break;
        }

        UserInventoryData response = null;
        string error = null;

        yield return _apiClient.GetInventory(
            userId,
            onSuccess: data => response = data,
            onError: message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetState(error);
            yield break;
        }

        InventoryItemData[] items = response != null ? response.items : null;
        PopulateEquipped(items);

        PlayerLoadout.ApplyFromItems(items);

        if (items == null || items.Length == 0)
        {
            SetState("This player has no inventory items yet.");
            yield break;
        }

        _stateText.gameObject.SetActive(false);

        for (int i = 0; i < items.Length; i++)
        {
            CreateRow(items[i]);
        }
    }

    private void EnsureBuilt()
    {
        if (_built)
        {
            return;
        }

        _built = true;

        RectTransform panelRect = GetOrAddRectTransform(gameObject);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = GetOrAddImage(gameObject);
        panelImage.color = new Color(0.07f, 0.1f, 0.14f, 0.96f);

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

        TextMeshProUGUI subtitleText = CreateText(
            subtitleObj.transform,
            "All item rows are loaded from the Java backend for the currently logged-in player.",
            18,
            FontStyles.Normal);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.78f, 0.84f, 0.9f, 1f);

        GameObject backButton = CreateButton(
            "BackButton",
            transform,
            "Back",
            new Vector2(100f, -34f),
            new Vector2(150f, 46f),
            new Color(0.18f, 0.23f, 0.3f, 1f));
        backButton.GetComponent<Button>().onClick.AddListener(HandleBackPressed);

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

        GameObject scrollRoot = CreateUIObject("ScrollView", transform);
        RectTransform scrollRect = GetOrAddRectTransform(scrollRoot);
        scrollRect.anchorMin = new Vector2(0.01f, 0.05f);
        scrollRect.anchorMax = new Vector2(0.64f, 0.93f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        scrollRect.sizeDelta = Vector2.zero;
        scrollRect.anchoredPosition = Vector2.zero;

        Image scrollImage = GetOrAddImage(scrollRoot);
        scrollImage.color = new Color(0.12f, 0.15f, 0.2f, 0.92f);

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

        Image viewportImage = GetOrAddImage(viewport);
        viewportImage.color = new Color(1f, 1f, 1f, 0.025f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        _contentRoot = GetOrAddRectTransform(content);
        _contentRoot.anchorMin = new Vector2(0f, 1f);
        _contentRoot.anchorMax = new Vector2(1f, 1f);
        _contentRoot.pivot = new Vector2(0.5f, 1f);
        _contentRoot.offsetMin = new Vector2(0f, 0f);
        _contentRoot.offsetMax = new Vector2(0f, 0f);
        _contentRoot.sizeDelta = new Vector2(0f, 0f);

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
    }

    private void CreateRow(InventoryItemData item)
    {
        GameObject row = CreateUIObject("InventoryRow", _contentRoot);
        _rows.Add(row);

        Image rowImage = GetOrAddImage(row);
        rowImage.color = new Color(0.18f, 0.22f, 0.29f, 0.98f);

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 180f;
        layout.flexibleHeight = 0f;

        VerticalLayoutGroup group = row.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(18, 18, 14, 14);
        group.spacing = 8f;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        ContentSizeFitter rowSize = row.AddComponent<ContentSizeFitter>();
        rowSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform rowRect = GetOrAddRectTransform(row);
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, 180f);

        string name = !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : "Unnamed Item";
        string type = !string.IsNullOrWhiteSpace(item.itemType) ? item.itemType : "Unknown";
        string rarity = !string.IsNullOrWhiteSpace(item.rarity) ? item.rarity : "Unknown";
        string description = !string.IsNullOrWhiteSpace(item.description) ? item.description : "No description.";
        string detailSummary = !string.IsNullOrWhiteSpace(item.detailSummary) ? item.detailSummary : "No subtype details.";

        TextMeshProUGUI nameText = CreateText(
            CreateSection("Name", row.transform, 34f).transform,
            $"{name}  x{Mathf.Max(0, item.quantity)}",
            24,
            FontStyles.Bold,
            true);
        nameText.color = new Color(0.97f, 0.98f, 1f, 1f);

        TextMeshProUGUI metaText = CreateText(
            CreateSection("Meta", row.transform, 28f).transform,
            $"Type: {type}    Rarity: {rarity}",
            18,
            FontStyles.Normal,
            true);
        metaText.color = new Color(0.66f, 0.84f, 1f, 1f);

        TextMeshProUGUI descriptionText = CreateText(
            CreateSection("Description", row.transform, 50f).transform,
            description,
            17,
            FontStyles.Normal,
            true);
        descriptionText.color = new Color(0.88f, 0.9f, 0.95f, 1f);

        TextMeshProUGUI detailsText = CreateText(
            CreateSection("Details", row.transform, 36f).transform,
            detailSummary,
            16,
            FontStyles.Italic,
            true);
        detailsText.color = new Color(0.76f, 0.86f, 0.77f, 1f);
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

    private void SetState(string text)
    {
        if (_stateText == null)
        {
            return;
        }

        _stateText.gameObject.SetActive(true);
        _stateText.text = text;
    }

    private void PopulateEquipped(InventoryItemData[] items)
    {
        SetEquippedSlot(_equippedWeaponText, "Starter Spear", FindEquippedItem(items, "Weapon", "Starter Spear"));
        SetEquippedSlot(_equippedArmorText, "Training Vest", FindEquippedItem(items, "Armor", "Training Vest"));
        SetEquippedSlot(_equippedConsumableText, "Health Potion", FindEquippedItem(items, "Consumable", "Health Potion"));
    }

    private InventoryItemData FindEquippedItem(InventoryItemData[] items, string itemType, string fallbackName)
    {
        if (items == null)
        {
            return null;
        }

        for (int i = 0; i < items.Length; i++)
        {
            InventoryItemData item = items[i];
            if (item == null)
            {
                continue;
            }

            if (string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.itemName, fallbackName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            InventoryItemData item = items[i];
            if (item == null)
            {
                continue;
            }

            if (string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private void SetEquippedSlot(TextMeshProUGUI text, string fallbackName, InventoryItemData item)
    {
        if (text == null)
        {
            return;
        }

        string itemName = item != null && !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : fallbackName;
        string rarity = item != null && !string.IsNullOrWhiteSpace(item.rarity) ? item.rarity : "Common";
        string detail = item != null && !string.IsNullOrWhiteSpace(item.detailSummary) ? item.detailSummary : "Equipped by default";

        text.text = $"{itemName}\nRarity: {rarity}\n{detail}";
    }

    private void HandleBackPressed()
    {
        _backAction?.Invoke();
    }

    private GameObject CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObj = CreateUIObject(name, parent);
        RectTransform rect = GetOrAddRectTransform(buttonObj);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = GetOrAddImage(buttonObj);
        image.color = color;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.08f;
        colors.pressedColor = color * 0.92f;
        colors.selectedColor = color;
        colors.disabledColor = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.85f);
        button.colors = colors;
        button.targetGraphic = image;

        GameObject labelObj = CreateUIObject("Label", buttonObj.transform);
        RectTransform labelRect = GetOrAddRectTransform(labelObj);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = CreateText(labelObj.transform, label, 22, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return buttonObj;
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

        Image equippedImage = GetOrAddImage(equippedRoot);
        equippedImage.color = new Color(0.11f, 0.14f, 0.19f, 0.94f);

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
            "Equipped",
            28,
            FontStyles.Bold,
            false);
        heading.alignment = TextAlignmentOptions.Center;
        heading.color = new Color(0.95f, 0.97f, 1f, 1f);

        TextMeshProUGUI subtitle = CreateText(
            CreateSection("EquippedSubtitle", equippedRoot.transform, 48f).transform,
            "Every profile keeps one equipped weapon, one armor piece, and one consumable.",
            16,
            FontStyles.Normal,
            true);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.75f, 0.82f, 0.9f, 1f);

        _equippedWeaponText = CreateEquippedSlot(equippedRoot.transform, "Weapon");
        _equippedArmorText = CreateEquippedSlot(equippedRoot.transform, "Armor");
        _equippedConsumableText = CreateEquippedSlot(equippedRoot.transform, "Consumable");
        PopulateEquipped(null);
    }

    private TextMeshProUGUI CreateEquippedSlot(Transform parent, string slotName)
    {
        GameObject slot = CreateUIObject(slotName + "Slot", parent);
        RectTransform slotRect = GetOrAddRectTransform(slot);
        slotRect.sizeDelta = new Vector2(0f, 112f);

        Image slotImage = GetOrAddImage(slot);
        slotImage.color = new Color(0.17f, 0.21f, 0.28f, 1f);

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
            slotName.ToUpperInvariant(),
            16,
            FontStyles.Bold,
            false);
        label.color = new Color(0.66f, 0.84f, 1f, 1f);

        TextMeshProUGUI body = CreateText(
            CreateSection(slotName + "Body", slot.transform, 58f).transform,
            "",
            16,
            FontStyles.Normal,
            true);
        body.color = new Color(0.94f, 0.96f, 1f, 1f);
        return body;
    }

    private TextMeshProUGUI CreateText(Transform parent, string textValue, float fontSize, FontStyles fontStyle, bool wrap = false)
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
        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }

        return rect;
    }

    private Image GetOrAddImage(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }

        return image;
    }
}

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreePanelController : MonoBehaviour
{
    private const float NodeW  = 220f;
    private const float NodeH  = 88f;
    private const float RootW  = 240f;
    private const float RootH  = 72f;
    private const float LabelH = 36f;

    private Action _backAction;
    private AuthApiClient _apiClient;
    private bool _built;
    private bool _loadingInventory;
    private bool _loadingProgression;
    private bool _loadingSkills;
    private bool _unlockingSkill;
    private bool _upgradingSkill;
    private RectTransform _branchRoot;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _progressionText;
    private Image _xpFillImage;
    private UserInventoryData _inventory;
    private string _message = "";

    // Skill inspect overlay
    private GameObject _skillOverlay;
    private TextMeshProUGUI _overlayNameText;
    private TextMeshProUGUI _overlaySlotText;
    private TextMeshProUGUI _overlayDescText;
    private TextMeshProUGUI _overlayMetaText;
    private Button _overlayActionBtn;
    private TextMeshProUGUI _overlayActionLabel;
    private Button _overlayLvUpBtn;
    private TextMeshProUGUI _overlayLvUpLabel;
    private PlayerSkillDefinition _inspectedSkill;

    public void Initialize(Action backAction)
    {
        Initialize(null, backAction);
    }

    public void Initialize(AuthApiClient apiClient, Action backAction)
    {
        _backAction = backAction;
        _apiClient = apiClient;
        EnsureBuilt();
    }

    public void Open()
    {
        EnsureBuilt();
        Render();
        if (_apiClient != null && AuthSession.IsLoggedIn)
        {
            StartCoroutine(LoadProgressionRoutine());
            StartCoroutine(LoadInventoryRoutine());
            StartCoroutine(LoadSkillTreeRoutine());
        }
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        RectTransform root = GetOrAddRT(gameObject);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(gameObject, GameUiThemeRuntime.Current.skillTreeBackground, true);

        GameObject backButton = CreateButton("BackButton", transform, "Back",
            new Vector2(16f, -16f), new Vector2(160f, 64f),
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Color(0.18f, 0.23f, 0.30f, 1f));
        backButton.GetComponent<Button>().onClick.AddListener(() => _backAction?.Invoke());

        GameObject titleObj = CreateUIObj("Title", transform);
        RectTransform titleRect = GetOrAddRT(titleObj);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(640f, 64f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        TextMeshProUGUI title = CreateText(titleObj.transform, "SKILL TREE", 42f, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.97f, 1f, 1f);

        GameObject statusObj = CreateUIObj("Status", transform);
        RectTransform statusRect = GetOrAddRT(statusObj);
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.sizeDelta = new Vector2(1100f, 72f);
        statusRect.anchoredPosition = new Vector2(0f, -78f);
        _statusText = CreateText(statusObj.transform, "", 36f, FontStyles.Normal);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.color = new Color(0.78f, 0.84f, 0.92f, 1f);

        GameObject progressRoot = CreateUIObj("Progression", transform);
        RectTransform progressRect = GetOrAddRT(progressRoot);
        progressRect.anchorMin = new Vector2(0.5f, 1f);
        progressRect.anchorMax = new Vector2(0.5f, 1f);
        progressRect.pivot = new Vector2(0.5f, 1f);
        progressRect.sizeDelta = new Vector2(700f, 60f);
        progressRect.anchoredPosition = new Vector2(0f, -148f);

        GameObject progressLabel = CreateUIObj("Label", progressRoot.transform);
        RectTransform progressLabelRect = GetOrAddRT(progressLabel);
        progressLabelRect.anchorMin = new Vector2(0f, 0.45f);
        progressLabelRect.anchorMax = new Vector2(1f, 1f);
        progressLabelRect.offsetMin = Vector2.zero;
        progressLabelRect.offsetMax = Vector2.zero;
        _progressionText = CreateText(progressLabel.transform, "", 30f, FontStyles.Bold);
        _progressionText.alignment = TextAlignmentOptions.Center;
        _progressionText.color = new Color(0.86f, 0.91f, 1f, 1f);

        GameObject progressBg = CreateUIObj("XpBar", progressRoot.transform);
        RectTransform progressBgRect = GetOrAddRT(progressBg);
        progressBgRect.anchorMin = new Vector2(0f, 0f);
        progressBgRect.anchorMax = new Vector2(1f, 0.38f);
        progressBgRect.offsetMin = Vector2.zero;
        progressBgRect.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StyleSurface(progressBg);

        GameObject progressFill = CreateUIObj("Fill", progressBg.transform);
        RectTransform progressFillRect = GetOrAddRT(progressFill);
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = new Vector2(0f, 1f);
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;
        _xpFillImage = GetOrAddImage(progressFill);
        _xpFillImage.color = new Color(0.38f, 0.72f, 1f, 1f);

        GameObject branchRootObj = CreateUIObj("TreeArea", transform);
        _branchRoot = GetOrAddRT(branchRootObj);
        _branchRoot.anchorMin = new Vector2(0.035f, 0.045f);
        _branchRoot.anchorMax = new Vector2(0.965f, 0.76f);
        _branchRoot.offsetMin = Vector2.zero;
        _branchRoot.offsetMax = Vector2.zero;

        BuildSkillInspectOverlay();
    }

    private void Render()
    {
        if (_branchRoot == null) return;

        for (int i = _branchRoot.childCount - 1; i >= 0; i--)
            Destroy(_branchRoot.GetChild(i).gameObject);

        PlayerSkillLoadout.RefreshForWeapon(PlayerLoadout.CurrentWeaponKind);
        PlayerSkillDefinition active = PlayerSkillLoadout.CurrentActiveSkill;
        PlayerSkillDefinition passive = PlayerSkillLoadout.CurrentPassiveSkill;

        if (_statusText != null)
        {
            string activeName = active != null ? active.title : "none";
            string passiveName = passive != null ? passive.title : "none";
            int rusty = GetMaterialQuantity(PlayerSkillLoadout.RustyScrapKey);
            int metal = GetMaterialQuantity(PlayerSkillLoadout.MetalScrapKey);
            int diamond = GetMaterialQuantity(PlayerSkillLoadout.DiamondScrapKey);
            string pointsText = PlayerSkillLoadout.AvailableSkillPoints.ToString();
            string prefix = _loadingInventory ? "Loading materials...   " : _loadingProgression ? "Loading progression...   " : _loadingSkills ? "Loading skills...   " : "";
            string suffix = string.IsNullOrWhiteSpace(_message) ? "" : "   " + _message;
            _statusText.text =
                $"{prefix}Skill Points: {pointsText}   Rusty: {rusty}   Metal: {metal}   Diamond: {diamond}   Current weapon: {PlayerSkillLoadout.GetBranchLabel(PlayerSkillLoadout.CurrentBranch)}   E: {activeName}   Q: {passiveName}{suffix}";
        }

        UpdateProgressionBar();
        CreateTreeHalf(SkillWeaponBranch.SwordSpear, new Color(0.17f, 0.28f, 0.39f, 1f), isLeft: true);
        CreateTreeHalf(SkillWeaponBranch.Ranged,     new Color(0.39f, 0.15f, 0.15f, 1f), isLeft: false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tree layout
    // ──────────────────────────────────────────────────────────────────────────

    private void CreateTreeHalf(SkillWeaponBranch branch, Color accent, bool isLeft)
    {
        GameObject container = CreateUIObj(PlayerSkillLoadout.GetBranchLabel(branch) + "Tree", _branchRoot);
        RectTransform crt = GetOrAddRT(container);
        if (isLeft)
        {
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = new Vector2(-4f, 0f);
        }
        else
        {
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.offsetMin = new Vector2(4f, 0f);
            crt.offsetMax = Vector2.zero;
        }

        Image bg = GetOrAddImage(container);
        bg.color = new Color(0.09f, 0.12f, 0.17f, 0.97f);

        PlayerSkillDefinition a1 = FindSkill(branch, SkillSlotKind.Active,  1);
        PlayerSkillDefinition a2 = FindSkill(branch, SkillSlotKind.Active,  2);
        PlayerSkillDefinition a3 = FindSkill(branch, SkillSlotKind.Active,  3);
        PlayerSkillDefinition p1 = FindSkill(branch, SkillSlotKind.Passive, 1);
        PlayerSkillDefinition p2 = FindSkill(branch, SkillSlotKind.Passive, 2);
        PlayerSkillDefinition p3 = FindSkill(branch, SkillSlotKind.Passive, 3);

        // Lines added first so they render behind nodes (lower sibling index = behind).
        UITreeLine lineRA1 = CreateLineUI(container.transform, accent);
        UITreeLine lineRP1 = CreateLineUI(container.transform, accent);
        UITreeLine lineA12 = CreateLineUI(container.transform, accent);
        UITreeLine lineA23 = CreateLineUI(container.transform, accent);
        UITreeLine lineP12 = CreateLineUI(container.transform, accent);
        UITreeLine lineP23 = CreateLineUI(container.transform, accent);

        RectTransform rootRT = CreateRootNode(container.transform, PlayerSkillLoadout.GetBranchLabel(branch), accent);

        AddSlotLabel(container.transform, "Active  (E)",  new Vector2(0.25f, 0.82f), accent);
        AddSlotLabel(container.transform, "Passive  (Q)", new Vector2(0.75f, 0.82f), accent);

        RectTransform a1RT = CreateNameNode(container.transform, a1, new Vector2(0.25f, 0.70f), accent);
        RectTransform p1RT = CreateNameNode(container.transform, p1, new Vector2(0.75f, 0.70f), accent);
        RectTransform a2RT = CreateNameNode(container.transform, a2, new Vector2(0.25f, 0.46f), accent);
        RectTransform p2RT = CreateNameNode(container.transform, p2, new Vector2(0.75f, 0.46f), accent);
        RectTransform a3RT = CreateNameNode(container.transform, a3, new Vector2(0.25f, 0.22f), accent);
        RectTransform p3RT = CreateNameNode(container.transform, p3, new Vector2(0.75f, 0.22f), accent);

        WireLine(lineRA1, rootRT, a1RT);
        WireLine(lineRP1, rootRT, p1RT);
        WireLine(lineA12, a1RT,   a2RT);
        WireLine(lineA23, a2RT,   a3RT);
        WireLine(lineP12, p1RT,   p2RT);
        WireLine(lineP23, p2RT,   p3RT);
    }

    private UITreeLine CreateLineUI(Transform parent, Color accent)
    {
        GameObject obj = CreateUIObj("Line", parent);
        RectTransform rt = GetOrAddRT(obj);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(8f, 0f);
        rt.anchoredPosition = Vector2.zero;
        Image img = GetOrAddImage(obj);
        Color lineColor = Color.Lerp(accent, new Color(0.05f, 0.07f, 0.10f, 1f), 0.45f);
        img.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.85f);
        img.raycastTarget = false;
        return obj.AddComponent<UITreeLine>();
    }

    private static void WireLine(UITreeLine line, RectTransform from, RectTransform to)
    {
        if (line == null || from == null || to == null) return;
        line.nodeA = from;
        line.nodeB = to;
    }

    private RectTransform CreateRootNode(Transform parent, string label, Color accent)
    {
        GameObject obj = CreateUIObj("Root", parent);
        RectTransform rt = GetOrAddRT(obj);
        rt.anchorMin = new Vector2(0.5f, 0.91f);
        rt.anchorMax = new Vector2(0.5f, 0.91f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(RootW, RootH);
        rt.anchoredPosition = Vector2.zero;

        Image img = GetOrAddImage(obj);
        img.color = Color.Lerp(accent, new Color(0.08f, 0.10f, 0.14f, 1f), 0.3f);
        GameUiThemeRuntime.ApplyBorder(obj);

        GameObject textObj = CreateUIObj("Label", obj.transform);
        RectTransform textRT = GetOrAddRT(textObj);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI text = CreateText(textObj.transform, label, 34f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.Lerp(accent, Color.white, 0.65f);

        return rt;
    }

    private void AddSlotLabel(Transform parent, string text, Vector2 anchor, Color accent)
    {
        GameObject obj = CreateUIObj("SlotLabel", parent);
        RectTransform rt = GetOrAddRT(obj);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(NodeW, LabelH);
        rt.anchoredPosition = Vector2.zero;

        GameObject labelObj = CreateUIObj("Text", obj.transform);
        RectTransform lr = GetOrAddRT(labelObj);
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = CreateText(labelObj.transform, text, 24f, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.Lerp(accent, Color.white, 0.5f);
    }

    private RectTransform CreateNameNode(Transform parent, PlayerSkillDefinition skill, Vector2 anchor, Color accent)
    {
        if (skill == null) return null;

        bool unlocked = PlayerSkillLoadout.IsUnlocked(skill.id);
        bool equipped = IsEquipped(skill);
        bool prerequisiteMet = string.IsNullOrWhiteSpace(skill.prerequisiteId) || PlayerSkillLoadout.IsUnlocked(skill.prerequisiteId);

        Color baseColor = unlocked
            ? Color.Lerp(accent, new Color(0.15f, 0.18f, 0.23f, 1f), 0.45f)
            : new Color(0.13f, 0.14f, 0.17f, 1f);
        if (equipped)
            baseColor = Color.Lerp(accent, new Color(0.82f, 0.86f, 0.92f, 1f), 0.15f);

        GameObject node = CreateUIObj(skill.id, parent);
        RectTransform rt = GetOrAddRT(node);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(NodeW, NodeH);
        rt.anchoredPosition = Vector2.zero;

        Image nodeImg = GetOrAddImage(node);
        nodeImg.color = baseColor;
        GameUiThemeRuntime.ApplyBorder(node);

        VerticalLayoutGroup vg = node.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(12, 12, 10, 10);
        vg.spacing = 4f;
        vg.childAlignment = TextAnchor.MiddleCenter;
        vg.childControlWidth = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth = true;
        vg.childForceExpandHeight = false;

        // Name
        GameObject nameRow = CreateUIObj("Name", node.transform);
        nameRow.AddComponent<LayoutElement>().preferredHeight = 36f;
        TextMeshProUGUI nameTmp = CreateText(nameRow.transform, skill.title, 26f, FontStyles.Bold);
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.color = Color.white;

        // State tag
        string stateTag = equipped ? "EQUIPPED" : unlocked ? "UNLOCKED" : "LOCKED";
        Color stateColor = equipped
            ? new Color(0.65f, 1f, 0.63f, 1f)
            : unlocked
                ? new Color(0.60f, 0.75f, 1f, 1f)
                : new Color(0.55f, 0.55f, 0.60f, 1f);
        GameObject tagRow = CreateUIObj("Tag", node.transform);
        tagRow.AddComponent<LayoutElement>().preferredHeight = 24f;
        TextMeshProUGUI tagTmp = CreateText(tagRow.transform, stateTag, 18f, FontStyles.Bold);
        tagTmp.alignment = TextAlignmentOptions.Center;
        tagTmp.color = stateColor;

        // The whole card is a button that opens the inspect overlay
        Button btn = node.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, nodeImg, baseColor);
        PlayerSkillDefinition captured = skill;
        btn.onClick.AddListener(() => ShowSkillInspect(captured, accent));

        return rt;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Skill inspect overlay (same pattern as InventoryPanelController)
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildSkillInspectOverlay()
    {
        _skillOverlay = CreateUIObj("SkillInspectOverlay", transform);
        RectTransform overlayRT = GetOrAddRT(_skillOverlay);
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        GetOrAddImage(_skillOverlay).color = new Color(0f, 0f, 0f, 0.72f);

        // Dismiss on background click
        Button bgBtn = _skillOverlay.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(HideSkillInspect);

        GameObject card = CreateUIObj("Card", _skillOverlay.transform);
        RectTransform cardRT = GetOrAddRT(card);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(780f, 0f);
        cardRT.anchoredPosition = Vector2.zero;
        GameUiThemeRuntime.StyleSurface(card);
        GameUiThemeRuntime.ApplyBorder(card);

        // Block background dismiss clicks from reaching the overlay button
        Image cardBlocker = GetOrAddImage(card);
        cardBlocker.raycastTarget = true;
        Button cardBtnBlocker = card.AddComponent<Button>();
        cardBtnBlocker.transition = Selectable.Transition.None;
        cardBtnBlocker.onClick.AddListener(() => { });

        ContentSizeFitter cardFitter = card.AddComponent<ContentSizeFitter>();
        cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup cardVG = card.AddComponent<VerticalLayoutGroup>();
        cardVG.padding = new RectOffset(48, 48, 40, 40);
        cardVG.spacing = 24f;
        cardVG.childAlignment = TextAnchor.UpperCenter;
        cardVG.childControlWidth = true;
        cardVG.childControlHeight = true;
        cardVG.childForceExpandWidth = true;
        cardVG.childForceExpandHeight = false;

        // Name
        GameObject nameSection = CreateOverlaySection("OName", card.transform, 64f);
        _overlayNameText = CreateText(nameSection.transform, "", 48f, FontStyles.Bold);
        _overlayNameText.alignment = TextAlignmentOptions.Center;
        _overlayNameText.color = new Color(0.97f, 0.98f, 1f, 1f);

        // Slot
        GameObject slotSection = CreateOverlaySection("OSlot", card.transform, 40f);
        _overlaySlotText = CreateText(slotSection.transform, "", 28f, FontStyles.Normal);
        _overlaySlotText.alignment = TextAlignmentOptions.Center;
        _overlaySlotText.color = new Color(0.66f, 0.84f, 1f, 1f);

        // Description
        GameObject descSection = CreateOverlaySection("ODesc", card.transform, 100f);
        _overlayDescText = CreateText(descSection.transform, "", 30f, FontStyles.Normal);
        _overlayDescText.alignment = TextAlignmentOptions.Center;
        _overlayDescText.enableWordWrapping = true;
        _overlayDescText.overflowMode = TextOverflowModes.Overflow;
        _overlayDescText.color = new Color(0.88f, 0.90f, 0.95f, 1f);

        // Meta
        GameObject metaSection = CreateOverlaySection("OMeta", card.transform, 56f);
        _overlayMetaText = CreateText(metaSection.transform, "", 26f, FontStyles.Bold);
        _overlayMetaText.alignment = TextAlignmentOptions.Center;
        _overlayMetaText.color = new Color(0.90f, 0.80f, 0.44f, 1f);

        // Button row
        GameObject btnRow = CreateUIObj("BtnRow", card.transform);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 80f;
        HorizontalLayoutGroup btnHG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHG.spacing = 24f;
        btnHG.childAlignment = TextAnchor.MiddleCenter;
        btnHG.childControlWidth = false;
        btnHG.childControlHeight = true;
        btnHG.childForceExpandWidth = false;
        btnHG.childForceExpandHeight = false;

        GameObject actionBtnObj = CreateOverlayButton("ActionBtn", btnRow.transform,
            "Unlock", new Color(0.17f, 0.28f, 0.39f, 1f), new Vector2(240f, 72f));
        _overlayActionBtn = actionBtnObj.GetComponent<Button>();
        _overlayActionLabel = actionBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        _overlayActionBtn.onClick.AddListener(() =>
        {
            PlayerSkillDefinition s = _inspectedSkill;
            HideSkillInspect();
            if (s != null) HandleSkillPressed(s);
        });

        GameObject lvUpBtnObj = CreateOverlayButton("LvUpBtn", btnRow.transform,
            "Lv.Up", new Color(0.48f, 0.36f, 0.12f, 1f), new Vector2(190f, 72f));
        _overlayLvUpBtn = lvUpBtnObj.GetComponent<Button>();
        _overlayLvUpLabel = lvUpBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        _overlayLvUpBtn.onClick.AddListener(() =>
        {
            PlayerSkillDefinition s = _inspectedSkill;
            HideSkillInspect();
            if (s != null) HandleLevelUpPressed(s);
        });

        GameObject closeBtnObj = CreateOverlayButton("CloseBtn", btnRow.transform,
            "Close", new Color(0.30f, 0.18f, 0.18f, 1f), new Vector2(160f, 72f));
        closeBtnObj.GetComponent<Button>().onClick.AddListener(HideSkillInspect);

        _skillOverlay.SetActive(false);
    }

    private void ShowSkillInspect(PlayerSkillDefinition skill, Color accent)
    {
        if (skill == null) return;
        _inspectedSkill = skill;

        bool unlocked = PlayerSkillLoadout.IsUnlocked(skill.id);
        bool equipped = IsEquipped(skill);
        bool prerequisiteMet = string.IsNullOrWhiteSpace(skill.prerequisiteId) || PlayerSkillLoadout.IsUnlocked(skill.prerequisiteId);
        int skillLevel = PlayerSkillLoadout.GetSkillLevel(skill.id);
        string nextMaterialKey = PlayerSkillLoadout.GetRequiredMaterialKeyForNextLevel(skill);
        int ownedMaterial = GetMaterialQuantity(nextMaterialKey);
        bool maxLevel = skillLevel >= PlayerSkillLoadout.MaxSkillLevel;
        bool hasUpgradeMaterial = ownedMaterial >= PlayerSkillLoadout.MaterialUpgradeCost;
        bool canUnlock = PlayerSkillLoadout.CanUnlock(skill) && !_unlockingSkill && !_upgradingSkill;
        bool canUpgrade = PlayerSkillLoadout.CanLevelUp(skill) && !_unlockingSkill && !_upgradingSkill &&
            !_loadingInventory && hasUpgradeMaterial;

        string slotLabel = skill.slotKind == SkillSlotKind.Active ? "Active  (E)" : "Passive  (Q)";
        string branchLabel = PlayerSkillLoadout.GetBranchLabel(skill.branch);
        _overlayNameText.text = skill.title;
        _overlaySlotText.text = $"{branchLabel}  ·  {slotLabel}  ·  Tier {skill.tier}";
        _overlayDescText.text = BuildDescriptionText(skill, skillLevel);
        _overlayMetaText.text = BuildNodeMeta(unlocked, prerequisiteMet, skill, skillLevel, nextMaterialKey, ownedMaterial, maxLevel);
        _overlayMetaText.color = equipped
            ? new Color(0.7f, 1f, 0.68f, 1f)
            : new Color(0.90f, 0.80f, 0.44f, 1f);

        bool canAct = !equipped && (unlocked || canUnlock) && prerequisiteMet;
        _overlayActionLabel.text = equipped ? "Equipped" : unlocked ? "Equip" : !prerequisiteMet ? "Locked" : "Unlock";
        _overlayActionBtn.interactable = canAct;

        bool showLvUp = unlocked;
        _overlayLvUpBtn.gameObject.SetActive(showLvUp);
        if (showLvUp)
        {
            _overlayLvUpLabel.text = maxLevel ? "Max" : "Lv.Up";
            _overlayLvUpBtn.interactable = canUpgrade;
        }

        _skillOverlay.SetActive(true);
    }

    private void HideSkillInspect()
    {
        if (_skillOverlay != null)
            _skillOverlay.SetActive(false);
        _inspectedSkill = null;
    }

    private GameObject CreateOverlaySection(string name, Transform parent, float minHeight)
    {
        GameObject obj = CreateUIObj(name, parent);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = minHeight;
        le.preferredHeight = minHeight;
        ContentSizeFitter fitter = obj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        return obj;
    }

    private GameObject CreateOverlayButton(string name, Transform parent, string label, Color color, Vector2 size)
    {
        GameObject obj = CreateUIObj(name, parent);
        RectTransform rt = GetOrAddRT(obj);
        rt.sizeDelta = size;
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        Image img = GetOrAddImage(obj);
        Button btn = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, img, color);

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform lr = GetOrAddRT(labelObj);
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = CreateText(labelObj.transform, label, 32f, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GameUiThemeRuntime.Current.text;

        return obj;
    }

    private static string BuildNodeMeta(bool unlocked, bool prerequisiteMet, PlayerSkillDefinition skill, int skillLevel, string nextMaterialKey, int ownedMaterial, bool maxLevel)
    {
        if (!prerequisiteMet) return "Requires previous skill";
        if (!unlocked) return $"Cost: {skill.cost} skill pt{(skill.cost == 1 ? "" : "s")}";
        if (maxLevel) return $"Lv {skillLevel}/{PlayerSkillLoadout.MaxSkillLevel}  (max)";
        return $"Lv {skillLevel}/{PlayerSkillLoadout.MaxSkillLevel}  ·  next: {PlayerSkillLoadout.MaterialUpgradeCost} {PlayerSkillLoadout.GetMaterialName(nextMaterialKey)} (owned: {ownedMaterial})";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logic (unchanged)
    // ──────────────────────────────────────────────────────────────────────────

    private void HandleSkillPressed(PlayerSkillDefinition skill)
    {
        if (!PlayerSkillLoadout.IsUnlocked(skill.id))
        {
            if (!_unlockingSkill)
                StartCoroutine(UnlockSkillRoutine(skill));
            return;
        }
        else
        {
            if (!_unlockingSkill)
                StartCoroutine(EquipSkillRoutine(skill));
        }

        Render();
    }

    private IEnumerator UnlockSkillRoutine(PlayerSkillDefinition skill)
    {
        if (skill == null || !PlayerSkillLoadout.CanUnlock(skill))
            yield break;

        _unlockingSkill = true;
        _message = "Unlocking...";
        Render();

        bool success = false;
        string error = null;
        PlayerStatsData updatedStats = null;
        if (_apiClient != null && AuthSession.IsLoggedIn)
        {
            yield return _apiClient.UnlockSkill(AuthSession.UserId, skill.id,
                data =>
                {
                    updatedStats = data?.stats;
                    PlayerSkillLoadout.ApplyServerState(data?.skillTree);
                    success = true;
                },
                err => error = err);
        }
        else
        {
            success = PlayerSkillLoadout.Unlock(skill);
            if (!success)
                error = "Need more skill points.";
        }

        if (success)
        {
            if (updatedStats != null)
                GameStatsTracker.SaveSyncedStats(updatedStats);

            _message = "";
        }
        else
        {
            _message = string.IsNullOrWhiteSpace(error) ? "Could not unlock skill." : error;
        }

        _unlockingSkill = false;
        Render();
    }

    private IEnumerator EquipSkillRoutine(PlayerSkillDefinition skill)
    {
        if (skill == null || !PlayerSkillLoadout.IsUnlocked(skill.id))
            yield break;

        _unlockingSkill = true;
        _message = "Equipping...";
        Render();

        bool success = false;
        string error = null;
        PlayerStatsData updatedStats = null;

        if (_apiClient != null && AuthSession.IsLoggedIn)
        {
            yield return _apiClient.EquipSkill(AuthSession.UserId, skill.id,
                data =>
                {
                    updatedStats = data?.stats;
                    PlayerSkillLoadout.ApplyServerState(data?.skillTree);
                    success = true;
                },
                err => error = err);
        }
        else
        {
            success = PlayerSkillLoadout.Equip(skill);
        }

        if (success)
        {
            if (updatedStats != null)
                GameStatsTracker.SaveSyncedStats(updatedStats);
            _message = "";
        }
        else
        {
            _message = string.IsNullOrWhiteSpace(error) ? "Could not equip skill." : error;
        }

        _unlockingSkill = false;
        Render();
    }

    private void HandleLevelUpPressed(PlayerSkillDefinition skill)
    {
        if (!_upgradingSkill)
            StartCoroutine(LevelUpSkillRoutine(skill));
    }

    private IEnumerator LoadInventoryRoutine()
    {
        if (_apiClient == null || !AuthSession.IsLoggedIn)
            yield break;

        _loadingInventory = true;
        _message = "";
        Render();

        UserInventoryData inventory = null;
        string error = null;
        yield return _apiClient.GetInventory(AuthSession.UserId, data => inventory = data, err => error = err);

        _loadingInventory = false;
        if (!string.IsNullOrWhiteSpace(error))
        {
            _message = error;
        }
        else
        {
            _inventory = inventory;
            _message = "";
        }
        Render();
    }

    private IEnumerator LoadProgressionRoutine()
    {
        if (_apiClient == null || !AuthSession.IsLoggedIn)
            yield break;

        _loadingProgression = true;
        Render();

        PlayerStatsData stats = null;
        string error = null;
        yield return _apiClient.GetPlayerStats(AuthSession.UserId, data => stats = data, err => error = err);

        _loadingProgression = false;
        if (!string.IsNullOrWhiteSpace(error))
        {
            _message = error;
        }
        else
        {
            GameStatsTracker.SaveSyncedStats(stats);
            _message = "";
        }
        Render();
    }

    private IEnumerator LoadSkillTreeRoutine()
    {
        if (_apiClient == null || !AuthSession.IsLoggedIn)
            yield break;

        _loadingSkills = true;
        Render();

        SkillTreeStateData state = null;
        string error = null;
        yield return _apiClient.GetSkillTree(AuthSession.UserId, data => state = data, err => error = err);

        _loadingSkills = false;
        if (!string.IsNullOrWhiteSpace(error))
        {
            _message = error;
        }
        else
        {
            PlayerSkillLoadout.ApplyServerState(state);
            _message = "";
        }
        Render();
    }

    private void UpdateProgressionBar()
    {
        PlayerStatsData stats = GameStatsTracker.GetCurrentPlayerStats();
        int currentXp = GameStatsTracker.GetXpIntoCurrentLevel(stats);
        int neededXp = Mathf.Max(1, GameStatsTracker.GetXpNeededForNextLevel(stats));

        if (_progressionText != null)
            _progressionText.text = $"Level {stats.level}   {currentXp}/{neededXp} XP to next level";

        if (_xpFillImage != null)
        {
            RectTransform fillRect = _xpFillImage.rectTransform;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01((float)currentXp / neededXp), 1f);
            fillRect.offsetMax = Vector2.zero;
        }
    }

    private IEnumerator LevelUpSkillRoutine(PlayerSkillDefinition skill)
    {
        if (skill == null)
            yield break;

        if (!PlayerSkillLoadout.CanLevelUp(skill))
        {
            Render();
            yield break;
        }

        string materialKey = PlayerSkillLoadout.GetRequiredMaterialKeyForNextLevel(skill);
        int materialCost = PlayerSkillLoadout.MaterialUpgradeCost;
        if (GetMaterialQuantity(materialKey) < materialCost)
        {
            _message = $"Need {materialCost} {PlayerSkillLoadout.GetMaterialName(materialKey)}.";
            Render();
            yield break;
        }

        _upgradingSkill = true;
        _message = "Leveling up...";
        Render();

        bool success = false;
        string error = null;
        if (_apiClient == null || !AuthSession.IsLoggedIn)
        {
            _upgradingSkill = false;
            _message = "Please log in to spend materials.";
            Render();
            yield break;
        }

        yield return _apiClient.LevelUpSkill(AuthSession.UserId, skill.id,
            data =>
            {
                PlayerSkillLoadout.ApplyServerState(data?.skillTree);
                if (data?.stats != null)
                    GameStatsTracker.SaveSyncedStats(data.stats);
                success = true;
            },
            err => error = err);

        _upgradingSkill = false;
        if (success)
        {
            yield return LoadInventoryRoutine();
            _message = "";
        }
        else
        {
            _message = string.IsNullOrWhiteSpace(error) ? "Could not level up skill." : error;
        }

        Render();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private bool IsEquipped(PlayerSkillDefinition skill)
    {
        PlayerSkillDefinition equipped = PlayerSkillLoadout.GetEquipped(skill.branch, skill.slotKind);
        return equipped != null && string.Equals(equipped.id, skill.id, StringComparison.Ordinal);
    }

    private PlayerSkillDefinition FindSkill(SkillWeaponBranch branch, SkillSlotKind slotKind, int tier)
    {
        PlayerSkillDefinition[] skills = PlayerSkillLoadout.DefaultSkills;
        for (int i = 0; i < skills.Length; i++)
        {
            PlayerSkillDefinition skill = skills[i];
            if (skill != null && skill.branch == branch && skill.slotKind == slotKind && skill.tier == tier)
                return skill;
        }
        return null;
    }

    private static string BuildDescriptionText(PlayerSkillDefinition skill, int skillLevel)
    {
        if (skill == null)
            return "";

        string text = skill.description ?? "";
        if (string.Equals(skill.id, "ranged_passive_3", StringComparison.Ordinal))
            text += $" Max minions: {PlayerController.GetMinionMaxAliveForLevel(skillLevel)}";

        return text;
    }

    private int GetMaterialQuantity(string materialKey)
    {
        if (_inventory?.items == null || string.IsNullOrWhiteSpace(materialKey))
            return 0;

        int total = 0;
        for (int i = 0; i < _inventory.items.Length; i++)
        {
            InventoryItemData item = _inventory.items[i];
            if (IsMaterialItem(item, materialKey))
                total += Mathf.Max(0, item.quantity);
        }
        return total;
    }

    private static bool IsMaterialItem(InventoryItemData item, string materialKey)
    {
        if (item == null || string.IsNullOrWhiteSpace(materialKey))
            return false;

        string itemMaterialKey = !string.IsNullOrWhiteSpace(item.materialKey) ? item.materialKey : item.material_key;
        if (!string.IsNullOrWhiteSpace(itemMaterialKey) &&
            string.Equals(itemMaterialKey.Trim(), materialKey.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(item.itemName, PlayerSkillLoadout.GetMaterialName(materialKey), StringComparison.OrdinalIgnoreCase);
    }

    private GameObject CreateButton(string name, Transform parent, string label,
        Vector2 anchoredPos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = CreateUIObj(name, parent);
        RectTransform rect = GetOrAddRT(obj);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

        Image image = GetOrAddImage(obj);
        Button button = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, color);

        GameObject labelObj = CreateUIObj("Label", obj.transform);
        RectTransform labelRect = GetOrAddRT(labelObj);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = CreateText(labelObj.transform, label, 36f, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.Center;

        return obj;
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, float size, FontStyles style)
    {
        TextMeshProUGUI tmp = parent.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
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
}

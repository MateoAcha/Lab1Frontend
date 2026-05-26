using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreePanelController : MonoBehaviour
{
    private const int VisibleBranchCount = 2;

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
            new Vector2(16f, -16f), new Vector2(130f, 46f),
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
        statusRect.sizeDelta = new Vector2(1100f, 36f);
        statusRect.anchoredPosition = new Vector2(0f, -78f);
        _statusText = CreateText(statusObj.transform, "", 18f, FontStyles.Normal);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.color = new Color(0.78f, 0.84f, 0.92f, 1f);

        GameObject progressRoot = CreateUIObj("Progression", transform);
        RectTransform progressRect = GetOrAddRT(progressRoot);
        progressRect.anchorMin = new Vector2(0.5f, 1f);
        progressRect.anchorMax = new Vector2(0.5f, 1f);
        progressRect.pivot = new Vector2(0.5f, 1f);
        progressRect.sizeDelta = new Vector2(520f, 42f);
        progressRect.anchoredPosition = new Vector2(0f, -112f);

        GameObject progressLabel = CreateUIObj("Label", progressRoot.transform);
        RectTransform progressLabelRect = GetOrAddRT(progressLabel);
        progressLabelRect.anchorMin = new Vector2(0f, 0.45f);
        progressLabelRect.anchorMax = new Vector2(1f, 1f);
        progressLabelRect.offsetMin = Vector2.zero;
        progressLabelRect.offsetMax = Vector2.zero;
        _progressionText = CreateText(progressLabel.transform, "", 15f, FontStyles.Bold);
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

        GameObject branchRootObj = CreateUIObj("BranchGrid", transform);
        _branchRoot = GetOrAddRT(branchRootObj);
        _branchRoot.anchorMin = new Vector2(0.035f, 0.045f);
        _branchRoot.anchorMax = new Vector2(0.965f, 0.80f);
        _branchRoot.offsetMin = Vector2.zero;
        _branchRoot.offsetMax = Vector2.zero;

        HorizontalLayoutGroup grid = branchRootObj.AddComponent<HorizontalLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.spacing = 18f;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.childControlWidth = true;
        grid.childControlHeight = true;
        grid.childForceExpandWidth = true;
        grid.childForceExpandHeight = true;
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
        CreateBranchColumn(SkillWeaponBranch.SwordSpear, new Color(0.17f, 0.28f, 0.39f, 1f));
        CreateBranchColumn(SkillWeaponBranch.Ranged, new Color(0.15f, 0.34f, 0.27f, 1f));
    }

    private void CreateBranchColumn(SkillWeaponBranch branch, Color accent)
    {
        GameObject column = CreateUIObj(PlayerSkillLoadout.GetBranchLabel(branch), _branchRoot);
        Image columnImage = GetOrAddImage(column);
        columnImage.color = new Color(0.10f, 0.13f, 0.18f, 0.96f);

        LayoutElement columnLayout = column.AddComponent<LayoutElement>();
        columnLayout.flexibleWidth = 1f;
        columnLayout.flexibleHeight = 1f;
        columnLayout.preferredWidth = 1f / VisibleBranchCount;

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject header = CreateUIObj("Header", column.transform);
        header.AddComponent<LayoutElement>().preferredHeight = 48f;
        TextMeshProUGUI headerText = CreateText(header.transform, PlayerSkillLoadout.GetBranchLabel(branch), 26f, FontStyles.Bold);
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = new Color(0.96f, 0.98f, 1f, 1f);

        CreateSectionLabel(column.transform, "Active Skills  (E)", accent);
        CreateSkillNodes(column.transform, branch, SkillSlotKind.Active, accent);
        CreateSectionLabel(column.transform, "Passive Skills  (Q)", accent);
        CreateSkillNodes(column.transform, branch, SkillSlotKind.Passive, accent);
    }

    private void CreateSectionLabel(Transform parent, string text, Color accent)
    {
        GameObject label = CreateUIObj(text, parent);
        label.AddComponent<LayoutElement>().preferredHeight = 30f;
        TextMeshProUGUI tmp = CreateText(label.transform, text, 18f, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.Lerp(accent, Color.white, 0.45f);
    }

    private void CreateSkillNodes(Transform parent, SkillWeaponBranch branch, SkillSlotKind slotKind, Color accent)
    {
        for (int tier = 1; tier <= 3; tier++)
        {
            PlayerSkillDefinition skill = FindSkill(branch, slotKind, tier);
            if (skill != null)
                CreateSkillNode(parent, skill, accent);
        }
    }

    private void CreateSkillNode(Transform parent, PlayerSkillDefinition skill, Color accent)
    {
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

        Color baseColor = unlocked
            ? Color.Lerp(accent, new Color(0.15f, 0.18f, 0.23f, 1f), 0.45f)
            : new Color(0.13f, 0.14f, 0.17f, 1f);
        if (equipped)
            baseColor = Color.Lerp(accent, new Color(0.82f, 0.86f, 0.92f, 1f), 0.15f);

        GameObject node = CreateUIObj(skill.id, parent);
        Image nodeImage = GetOrAddImage(node);
        nodeImage.color = baseColor;
        node.AddComponent<LayoutElement>().preferredHeight = 112f;

        HorizontalLayoutGroup row = node.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(14, 12, 10, 10);
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        GameObject textBlock = CreateUIObj("Text", node.transform);
        LayoutElement textLayout = textBlock.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.preferredHeight = 92f;
        VerticalLayoutGroup textGroup = textBlock.AddComponent<VerticalLayoutGroup>();
        textGroup.spacing = 1f;
        textGroup.childControlWidth = true;
        textGroup.childControlHeight = true;
        textGroup.childForceExpandWidth = true;
        textGroup.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText(CreateUIObj("Title", textBlock.transform).transform, skill.title, 18f, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.color = Color.white;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI desc = CreateText(CreateUIObj("Description", textBlock.transform).transform, BuildDescriptionText(skill, skillLevel), 14f, FontStyles.Normal);
        desc.alignment = TextAlignmentOptions.MidlineLeft;
        desc.color = new Color(0.78f, 0.83f, 0.9f, 1f);
        desc.enableWordWrapping = false;
        desc.overflowMode = TextOverflowModes.Ellipsis;

        string metaText = unlocked
            ? $"Level {skillLevel}/{PlayerSkillLoadout.MaxSkillLevel}"
            : $"Cost {skill.cost} skill point{(skill.cost == 1 ? "" : "s")}";
        if (!prerequisiteMet)
            metaText = "Requires previous";
        else if (unlocked && !maxLevel)
            metaText = $"Level {skillLevel}/{PlayerSkillLoadout.MaxSkillLevel} - next: {PlayerSkillLoadout.MaterialUpgradeCost} {PlayerSkillLoadout.GetMaterialName(nextMaterialKey)} ({ownedMaterial})";
        TextMeshProUGUI meta = CreateText(CreateUIObj("Meta", textBlock.transform).transform, metaText, 13f, FontStyles.Bold);
        meta.alignment = TextAlignmentOptions.MidlineLeft;
        meta.color = equipped ? new Color(0.74f, 1f, 0.72f, 1f) : new Color(0.92f, 0.82f, 0.48f, 1f);

        GameObject actionColumn = CreateUIObj("Actions", node.transform);
        LayoutElement actionLayout = actionColumn.AddComponent<LayoutElement>();
        actionLayout.preferredWidth = 112f;
        actionLayout.preferredHeight = 92f;
        VerticalLayoutGroup actionGroup = actionColumn.AddComponent<VerticalLayoutGroup>();
        actionGroup.spacing = 6f;
        actionGroup.childAlignment = TextAnchor.MiddleCenter;
        actionGroup.childControlWidth = true;
        actionGroup.childControlHeight = true;
        actionGroup.childForceExpandWidth = true;
        actionGroup.childForceExpandHeight = false;

        string buttonText = equipped ? "Equipped" : unlocked ? "Equip" : "Unlock";
        GameObject buttonObj = CreateButton("Action", actionColumn.transform, buttonText,
            Vector2.zero, new Vector2(112f, 40f),
            Vector2.zero, Vector2.zero,
            equipped ? new Color(0.18f, 0.43f, 0.22f, 1f) : Color.Lerp(accent, Color.white, 0.08f));
        LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
        buttonLayout.preferredWidth = 112f;
        buttonLayout.preferredHeight = 40f;

        Button button = buttonObj.GetComponent<Button>();
        button.interactable = equipped || unlocked || canUnlock;
        if (equipped)
        {
            button.interactable = false;
        }
        button.onClick.AddListener(() => HandleSkillPressed(skill));

        if (unlocked)
        {
            string levelButtonText = maxLevel ? "Max Lv" : "Level Up";
            GameObject levelObj = CreateButton("LevelUp", actionColumn.transform, levelButtonText,
                Vector2.zero, new Vector2(112f, 40f),
                Vector2.zero, Vector2.zero,
                maxLevel ? new Color(0.24f, 0.26f, 0.30f, 1f) : new Color(0.48f, 0.36f, 0.12f, 1f));
            LayoutElement levelLayout = levelObj.AddComponent<LayoutElement>();
            levelLayout.preferredWidth = 112f;
            levelLayout.preferredHeight = 40f;

            Button levelButton = levelObj.GetComponent<Button>();
            levelButton.interactable = canUpgrade;
            levelButton.onClick.AddListener(() => HandleLevelUpPressed(skill));
        }
    }

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
        {
            text += $" Max minions: {PlayerController.GetMinionMaxAliveForLevel(skillLevel)}";
        }

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
        TextMeshProUGUI tmp = CreateText(labelObj.transform, label, 18f, FontStyles.Bold);
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

using System;
using System.Globalization;
using UnityEngine;

public enum WeaponKind
{
    Spear,
    Sword,
    Hammer,
    Ranged
}

public static class PlayerLoadout
{
    public static int WeaponDamage { get; private set; } = 1;
    public static WeaponKind CurrentWeaponKind { get; private set; } = WeaponKind.Spear;
    public static Color WeaponColor { get; private set; } = Color.white;
    public static string WeaponColorHex { get; private set; } = "#FFFFFF";
    public static float MaxHP { get; private set; } = 10f;
    public static int ConsumableQuantity { get; private set; } = 0;
    public static float ConsumableHealAmount { get; private set; } = 0f;
    public static float ConsumableCooldown { get; private set; } = 0f;
    public static string ConsumableName { get; private set; } = "";
    public static bool ConsumableIsSpeedBoost { get; private set; } = false;
    public static float SpeedBoostDuration { get; private set; } = 3f;
    public static float SpeedBoostMultiplier { get; private set; } = 2f;

    public static InventoryItemData EquippedWeapon { get; private set; }
    public static InventoryItemData EquippedArmor { get; private set; }
    public static InventoryItemData EquippedConsumable { get; private set; }
    public static int EquippedSkinId { get; private set; } = 0;
    public static string EquippedSkinName { get; private set; } = "";
    public static int EquippedWeaponItemId => EquippedWeapon != null ? EquippedWeapon.itemId : 0;
    public static int EquippedConsumableUserInventoryId => EquippedConsumable != null ? EquippedConsumable.userInventoryId : 0;

    private static int _pendingConsumableUserInventoryId;
    private static int _pendingConsumableUsedQuantity;

    public static void ApplySkin(SkinData[] skins)
    {
        if (skins == null) return;
        foreach (SkinData skin in skins)
        {
            if (skin != null && skin.equipped)
            {
                EquippedSkinId = skin.skinId;
                EquippedSkinName = skin.skinName ?? "";
                return;
            }
        }
    }

    public static Color GetSkinColor()
    {
        return GetSkinColor(EquippedSkinId);
    }

    public static Color GetSkinColor(int skinId)
    {
        return skinId switch
        {
            2001 => new Color(1f, 0.25f, 0.25f, 1f),    // Crimson Edge
            2002 => new Color(0.25f, 0.85f, 0.35f, 1f), // Field Green
            _ => new Color(0.3f, 0.75f, 1f, 1f)
        };
    }

    public static void ApplyFromItems(InventoryItemData[] items)
    {
        EquippedWeapon = ResolveSlot(items, "Weapon", EquippedWeapon);
        EquippedArmor = ResolveSlot(items, "Armor", EquippedArmor);
        EquippedConsumable = ResolveSlot(items, "Consumable", EquippedConsumable);
        Apply(EquippedWeapon, EquippedArmor, EquippedConsumable);
    }

    public static void EquipItem(InventoryItemData item)
    {
        if (item == null) return;
        string type = item.itemType ?? "";
        if (string.Equals(type, "Weapon", StringComparison.OrdinalIgnoreCase))
            EquippedWeapon = item;
        else if (string.Equals(type, "Armor", StringComparison.OrdinalIgnoreCase))
            EquippedArmor = item;
        else if (string.Equals(type, "Consumable", StringComparison.OrdinalIgnoreCase))
            EquippedConsumable = item;
        else
            return;

        Apply(EquippedWeapon, EquippedArmor, EquippedConsumable);
    }

    // Keeps the current equipped item if it's still in inventory; otherwise picks the best.
    private static InventoryItemData ResolveSlot(InventoryItemData[] items, string type, InventoryItemData current)
    {
        InventoryItemData serverEquipped = FindEquipped(items, type);
        if (serverEquipped != null)
            return serverEquipped;

        if (current != null && items != null)
        {
            foreach (InventoryItemData item in items)
            {
                if (item != null && item.itemId == current.itemId &&
                    string.Equals(item.itemType, type, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }
        return FindBest(items, type);
    }

    private static InventoryItemData FindEquipped(InventoryItemData[] items, string itemType)
    {
        if (items == null) return null;
        foreach (InventoryItemData item in items)
        {
            if (item == null) continue;
            if (!item.equipped) continue;
            if (string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase))
                return item;
        }
        return null;
    }

    // Picks the highest-rarity item; breaks ties by primary stat (DMG for weapons, DEF for armor).
    private static InventoryItemData FindBest(InventoryItemData[] items, string itemType)
    {
        if (items == null) return null;
        InventoryItemData best = null;
        foreach (InventoryItemData item in items)
        {
            if (item == null) continue;
            if (!string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase)) continue;
            if (best == null || CompareItems(item, best, itemType) > 0)
                best = item;
        }
        return best;
    }

    private static int CompareItems(InventoryItemData a, InventoryItemData b, string itemType)
    {
        int rarityDiff = RarityRank(a.rarity) - RarityRank(b.rarity);
        if (rarityDiff != 0) return rarityDiff;
        float statA = GetPrimaryStat(a, itemType);
        float statB = GetPrimaryStat(b, itemType);
        return statA.CompareTo(statB);
    }

    private static int RarityRank(string rarity)
    {
        if (string.IsNullOrWhiteSpace(rarity)) return 0;
        return rarity.ToLowerInvariant() switch
        {
            "legendary" => 5,
            "epic"      => 4,
            "rare"      => 3,
            "uncommon"  => 2,
            "common"    => 1,
            _           => 0
        };
    }

    private static float GetPrimaryStat(InventoryItemData item, string itemType)
    {
        if (string.Equals(itemType, "Weapon", StringComparison.OrdinalIgnoreCase))
            return ParseKeyValue(item.detailSummary, "DMG");
        if (string.Equals(itemType, "Armor", StringComparison.OrdinalIgnoreCase))
            return ParseKeyValue(item.detailSummary, "DEF");
        return 0f;
    }

    public static void Apply(InventoryItemData weapon, InventoryItemData armor, InventoryItemData consumable)
    {
        WeaponDamage = 1;
        CurrentWeaponKind = WeaponKind.Spear;
        WeaponColor = Color.white;
        WeaponColorHex = "#FFFFFF";
        MaxHP = 10f;
        ConsumableQuantity = 0;
        ConsumableHealAmount = 0f;
        ConsumableCooldown = 0f;
        ConsumableName = "";
        ConsumableIsSpeedBoost = false;
        SpeedBoostDuration = 3f;

        if (weapon != null)
        {
            int dmg = ParseKeyValue(weapon.detailSummary, "DMG");
            if (dmg > 0) WeaponDamage = dmg;

            CurrentWeaponKind = ResolveWeaponKind(weapon);
            WeaponColorHex = ResolveWeaponColorHex(weapon);
            if (!ColorUtility.TryParseHtmlString(WeaponColorHex, out Color parsed))
            {
                parsed = Color.white;
                WeaponColorHex = "#FFFFFF";
            }
            WeaponColor = parsed;
        }

        PlayerSkillLoadout.RefreshForWeapon(CurrentWeaponKind);

        if (armor != null)
        {
            int def = ParseKeyValue(armor.detailSummary, "DEF");
            MaxHP = 10f + Mathf.Max(0, def);
        }

        if (consumable != null)
        {
            ConsumableQuantity = Mathf.Max(0, consumable.quantity);
            ConsumableName = consumable.itemName ?? "";
            ConsumableCooldown = ParseCooldown(consumable.detailSummary);

            ConsumableIsSpeedBoost = !string.IsNullOrWhiteSpace(consumable.detailSummary) &&
                consumable.detailSummary.IndexOf("Speed Boost", StringComparison.OrdinalIgnoreCase) >= 0;

            if (ConsumableIsSpeedBoost)
            {
                float dur = ParseDuration(consumable.detailSummary);
                SpeedBoostDuration = dur > 0f ? dur : 3f;
            }
            else
            {
                ConsumableHealAmount = ParseHealAmount(consumable.detailSummary);
            }
        }
    }

    public static bool UseConsumable()
    {
        if (ConsumableQuantity <= 0) return false;
        if (!ConsumableIsSpeedBoost && ConsumableHealAmount <= 0f) return false;
        ConsumableQuantity--;
        if (EquippedConsumable != null)
        {
            EquippedConsumable.quantity = Mathf.Max(0, EquippedConsumable.quantity - 1);
            RegisterPendingConsumableUse(EquippedConsumable.userInventoryId);
        }
        return true;
    }

    public static bool TryGetPendingConsumableUsage(out int userInventoryId, out int quantity)
    {
        userInventoryId = _pendingConsumableUserInventoryId;
        quantity = _pendingConsumableUsedQuantity;
        return userInventoryId > 0 && quantity > 0;
    }

    public static void MarkPendingConsumableUsageSynced(int userInventoryId, int quantity)
    {
        if (userInventoryId <= 0 || quantity <= 0)
        {
            return;
        }

        if (_pendingConsumableUserInventoryId != userInventoryId)
        {
            return;
        }

        _pendingConsumableUsedQuantity = Mathf.Max(0, _pendingConsumableUsedQuantity - quantity);
        if (_pendingConsumableUsedQuantity == 0)
        {
            _pendingConsumableUserInventoryId = 0;
        }
    }

    public static void ClearPendingConsumableUsage()
    {
        _pendingConsumableUserInventoryId = 0;
        _pendingConsumableUsedQuantity = 0;
    }

    private static void RegisterPendingConsumableUse(int userInventoryId)
    {
        if (userInventoryId <= 0)
        {
            return;
        }

        if (_pendingConsumableUserInventoryId != 0 && _pendingConsumableUserInventoryId != userInventoryId)
        {
            Debug.LogWarning("PlayerLoadout: replacing unsynced consumable usage with the currently equipped consumable.");
            _pendingConsumableUsedQuantity = 0;
        }

        _pendingConsumableUserInventoryId = userInventoryId;
        _pendingConsumableUsedQuantity++;
    }

    private static int ParseKeyValue(string summary, string key)
    {
        if (string.IsNullOrWhiteSpace(summary)) return 0;
        string[] parts = summary.Split('|');
        foreach (string part in parts)
        {
            string t = part.Trim();
            string prefix = key + ":";
            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string num = t.Substring(prefix.Length).Trim();
                if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    return Mathf.RoundToInt(val);
            }
        }
        return 0;
    }

    public static WeaponKind ParseWeaponKind(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return WeaponKind.Spear;

        string normalized = NormalizeWeaponKindText(value);
        if (normalized.Contains("hammer") ||
            normalized.Contains("mace") ||
            normalized.Contains("maul") ||
            normalized.Contains("club"))
        {
            return WeaponKind.Hammer;
        }

        if (normalized.Contains("ranged") ||
            normalized.Contains("range") ||
            normalized.Contains("bow") ||
            normalized.Contains("projectile") ||
            normalized.Contains("shoot") ||
            normalized.Contains("gun"))
        {
            return WeaponKind.Ranged;
        }

        if (normalized.Contains("sword") ||
            normalized.Contains("blade") ||
            normalized.Contains("slash"))
        {
            return WeaponKind.Sword;
        }

        return WeaponKind.Spear;
    }

    public static Color ParseWeaponColor(string value, Color fallback)
    {
        if (ColorUtility.TryParseHtmlString(value, out Color color))
            return color;
        if (ColorUtility.TryParseHtmlString(NormalizeColorHex(value), out color))
            return color;
        return fallback;
    }

    private static WeaponKind ResolveWeaponKind(InventoryItemData weapon)
    {
        string explicitType = FirstNonEmpty(
            weapon.weaponType,
            weapon.weapon_type,
            weapon.weaponSubtype,
            weapon.weapon_subtype,
            weapon.weaponClass,
            weapon.weapon_class);
        if (!string.IsNullOrWhiteSpace(explicitType))
            return ParseWeaponKind(explicitType);

        string summaryType = FirstNonEmpty(
            ParseTextKeyValue(weapon.detailSummary, "WEAPON_TYPE"),
            ParseTextKeyValue(weapon.detailSummary, "WEAPON TYPE"),
            ParseTextKeyValue(weapon.detailSummary, "TYPE"),
            ParseTextKeyValue(weapon.detailSummary, "KIND"),
            ParseTextKeyValue(weapon.detailSummary, "CLASS"));
        if (!string.IsNullOrWhiteSpace(summaryType))
            return ParseWeaponKind(summaryType);

        string searchable = string.Join(" ",
            weapon.itemName ?? "",
            weapon.description ?? "",
            weapon.detailSummary ?? "");
        WeaponKind inferred = ParseWeaponKind(searchable);
        if (inferred != WeaponKind.Spear)
            return inferred;

        return WeaponKind.Spear;
    }

    private static string ResolveWeaponColorHex(InventoryItemData weapon)
    {
        string color = NormalizeColorHex(FirstNonEmpty(weapon.weaponColor, weapon.weapon_color));
        if (!string.IsNullOrWhiteSpace(color))
            return color;

        color = NormalizeColorHex(FirstNonEmpty(
            ParseTextKeyValue(weapon.detailSummary, "WEAPON_COLOR"),
            ParseTextKeyValue(weapon.detailSummary, "WEAPON COLOR"),
            ParseTextKeyValue(weapon.detailSummary, "COLOR")));
        return string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color;
    }

    private static string ParseTextKeyValue(string summary, string key)
    {
        if (string.IsNullOrWhiteSpace(summary)) return "";
        string[] parts = summary.Split('|');
        foreach (string part in parts)
        {
            string t = part.Trim();
            string prefix = key + ":";
            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return t.Substring(prefix.Length).Trim();
        }
        return "";
    }

    private static string NormalizeColorHex(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return "";

        string trimmed = color.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
            return trimmed;
        if ((trimmed.Length == 6 || trimmed.Length == 8) && IsHex(trimmed))
            return "#" + trimmed;
        return trimmed;
    }

    private static string NormalizeWeaponKindText(string value)
    {
        string lower = value.Trim().ToLowerInvariant();
        return lower
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "");
    }

    private static bool IsHex(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool hex = (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!hex) return false;
        }
        return true;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        if (values == null) return "";
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return "";
    }

    private static float ParseHealAmount(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return 0f;
        int hpIdx = summary.IndexOf("HP", StringComparison.OrdinalIgnoreCase);
        if (hpIdx < 0) return 0f;
        int end = hpIdx;
        while (end > 0 && summary[end - 1] == ' ') end--;
        int start = end;
        while (start > 0 && (char.IsDigit(summary[start - 1]) || summary[start - 1] == '.')) start--;
        if (start == end) return 0f;
        string numStr = summary.Substring(start, end - start);
        if (float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        return 0f;
    }

    private static float ParseCooldown(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return 0f;
        const string key = "Cooldown:";
        int idx = summary.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return 0f;
        string after = summary.Substring(idx + key.Length).Trim();
        int len = 0;
        while (len < after.Length && (char.IsDigit(after[len]) || after[len] == '.')) len++;
        if (len == 0) return 0f;
        if (float.TryParse(after.Substring(0, len), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        return 0f;
    }

    private static float ParseDuration(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return 0f;
        const string key = "Duration:";
        int idx = summary.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return 0f;
        string after = summary.Substring(idx + key.Length).Trim();
        int len = 0;
        while (len < after.Length && (char.IsDigit(after[len]) || after[len] == '.')) len++;
        if (len == 0) return 0f;
        if (float.TryParse(after.Substring(0, len), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        return 0f;
    }
}

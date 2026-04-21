using System;
using System.Globalization;
using UnityEngine;

public static class PlayerLoadout
{
    public static int WeaponDamage { get; private set; } = 1;
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
        }

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
        return true;
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

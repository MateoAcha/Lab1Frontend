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

    public static void ApplyFromItems(InventoryItemData[] items)
    {
        Apply(
            FindEquipped(items, "Weapon", "Starter Spear"),
            FindEquipped(items, "Armor", "Training Vest"),
            FindEquipped(items, "Consumable", "Health Potion"));
    }

    private static InventoryItemData FindEquipped(InventoryItemData[] items, string itemType, string preferredName)
    {
        if (items == null) return null;
        InventoryItemData fallback = null;
        foreach (InventoryItemData item in items)
        {
            if (item == null) continue;
            if (!string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(item.itemName, preferredName, StringComparison.OrdinalIgnoreCase)) return item;
            if (fallback == null) fallback = item;
        }
        return fallback;
    }

    public static void Apply(InventoryItemData weapon, InventoryItemData armor, InventoryItemData consumable)
    {
        WeaponDamage = 1;
        MaxHP = 10f;
        ConsumableQuantity = 0;
        ConsumableHealAmount = 0f;
        ConsumableCooldown = 0f;
        ConsumableName = "";

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
            ConsumableHealAmount = ParseHealAmount(consumable.detailSummary);
            ConsumableCooldown = ParseCooldown(consumable.detailSummary);
        }
    }

    // Returns true and decrements quantity if a consumable is available.
    public static bool UseConsumable()
    {
        if (ConsumableQuantity <= 0 || ConsumableHealAmount <= 0f)
            return false;
        ConsumableQuantity--;
        return true;
    }

    // Parses "KEY: value" segments separated by "|", returns rounded int or 0.
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

    // Finds a number immediately before "HP" in the summary (e.g. "Restores 35 HP instantly.").
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

    // Parses "Cooldown: Xs" from the summary.
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
}

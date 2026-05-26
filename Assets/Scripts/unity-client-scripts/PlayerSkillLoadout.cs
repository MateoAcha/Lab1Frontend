using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillWeaponBranch
{
    SwordSpear,
    Hammer,
    Ranged
}

public enum SkillSlotKind
{
    Active,
    Passive
}

[Serializable]
public class PlayerSkillDefinition
{
    public string id;
    public string title;
    public string description;
    public SkillWeaponBranch branch;
    public SkillSlotKind slotKind;
    public int tier;
    public int cost;
    public string prerequisiteId;

    public PlayerSkillDefinition(
        string id,
        string title,
        string description,
        SkillWeaponBranch branch,
        SkillSlotKind slotKind,
        int tier,
        int cost,
        string prerequisiteId)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.branch = branch;
        this.slotKind = slotKind;
        this.tier = tier;
        this.cost = cost;
        this.prerequisiteId = prerequisiteId;
    }
}

public static class PlayerSkillLoadout
{
    public const int MaterialUpgradeCost = 3;
    public const int MaxSkillLevel = 3;
    public const string RustyScrapKey = "Rusty_Scrap";
    public const string MetalScrapKey = "Metal_Scrap";
    public const string DiamondScrapKey = "Diamond_Scrap";

    public static readonly PlayerSkillDefinition[] DefaultSkills =
    {
        new PlayerSkillDefinition("swordspear_active_1", "Charge", "Dash forward and strike through enemies.", SkillWeaponBranch.SwordSpear, SkillSlotKind.Active, 1, 1, ""),
        new PlayerSkillDefinition("swordspear_active_2", "Weapon Throw", "Throw sword as a boomerang or spear as a piercing shot.", SkillWeaponBranch.SwordSpear, SkillSlotKind.Active, 2, 2, "swordspear_active_1"),
        new PlayerSkillDefinition("swordspear_active_3", "Fire Trail", "Gain speed and leave a burning trail behind.", SkillWeaponBranch.SwordSpear, SkillSlotKind.Active, 3, 3, "swordspear_active_2"),
        new PlayerSkillDefinition("swordspear_passive_1", "Expansive Wave", "Release the current push wave.", SkillWeaponBranch.SwordSpear, SkillSlotKind.Passive, 1, 1, ""),
        new PlayerSkillDefinition("swordspear_passive_2", "Guard Wall", "Raise a temporary wall in front of you.", SkillWeaponBranch.SwordSpear, SkillSlotKind.Passive, 2, 2, "swordspear_passive_1"),
        new PlayerSkillDefinition("swordspear_passive_3", "Gravity Bomb", "Throw a bomb that pulls enemies together.", SkillWeaponBranch.SwordSpear, SkillSlotKind.Passive, 3, 3, "swordspear_passive_2"),

        new PlayerSkillDefinition("hammer_active_1", "Ground Crack", "Short line shockwave placeholder.", SkillWeaponBranch.Hammer, SkillSlotKind.Active, 1, 1, ""),
        new PlayerSkillDefinition("hammer_active_2", "Heavy Slam", "Large impact attack placeholder.", SkillWeaponBranch.Hammer, SkillSlotKind.Active, 2, 2, "hammer_active_1"),
        new PlayerSkillDefinition("hammer_active_3", "Meteor Breaker", "Delayed area smash placeholder.", SkillWeaponBranch.Hammer, SkillSlotKind.Active, 3, 3, "hammer_active_2"),
        new PlayerSkillDefinition("hammer_passive_1", "Iron Grip", "Hammer damage bonus placeholder.", SkillWeaponBranch.Hammer, SkillSlotKind.Passive, 1, 1, ""),
        new PlayerSkillDefinition("hammer_passive_2", "Shock Handle", "Impact radius bonus placeholder.", SkillWeaponBranch.Hammer, SkillSlotKind.Passive, 2, 2, "hammer_passive_1"),
        new PlayerSkillDefinition("hammer_passive_3", "Stonebreaker", "Armor break placeholder.", SkillWeaponBranch.Hammer, SkillSlotKind.Passive, 3, 3, "hammer_passive_2"),

        new PlayerSkillDefinition("ranged_active_1", "Bomb Shot", "Fire a huge projectile that explodes on impact.", SkillWeaponBranch.Ranged, SkillSlotKind.Active, 1, 1, ""),
        new PlayerSkillDefinition("ranged_active_2", "Quick Burst", "Fire five shots in rapid succession.", SkillWeaponBranch.Ranged, SkillSlotKind.Active, 2, 2, "ranged_active_1"),
        new PlayerSkillDefinition("ranged_active_3", "Snipe Shot", "Fire one fast shot for triple damage.", SkillWeaponBranch.Ranged, SkillSlotKind.Active, 3, 3, "ranged_active_2"),
        new PlayerSkillDefinition("ranged_passive_1", "Expansive Wave", "Release the current push wave.", SkillWeaponBranch.Ranged, SkillSlotKind.Passive, 1, 1, ""),
        new PlayerSkillDefinition("ranged_passive_2", "Decoy", "Become briefly unseen and leave a fake target behind.", SkillWeaponBranch.Ranged, SkillSlotKind.Passive, 2, 2, "ranged_passive_1"),
        new PlayerSkillDefinition("ranged_passive_3", "Minion", "Summon a small ally that chases and bites enemies.", SkillWeaponBranch.Ranged, SkillSlotKind.Passive, 3, 3, "ranged_passive_2")
    };

    public static SkillWeaponBranch CurrentBranch { get; private set; } = SkillWeaponBranch.SwordSpear;
    public static PlayerSkillDefinition CurrentActiveSkill { get; private set; }
    public static PlayerSkillDefinition CurrentPassiveSkill { get; private set; }
    public static int CachedLevel { get; private set; } = 1;
    public static int CachedTotalXp { get; private set; }
    public static int CachedUnspentSkillPoints { get; private set; }
    public static int CachedSpentSkillPoints { get; private set; }
    private static readonly Dictionary<string, SkillRuntimeState> SkillStates = new Dictionary<string, SkillRuntimeState>();
    private static readonly Dictionary<string, string> EquippedSkillIds = new Dictionary<string, string>();

    public static int AvailableSkillPoints
    {
        get
        {
            PlayerStatsData stats = GameStatsTracker.GetCurrentPlayerStats();
            SetProgressionFromStats(stats);
            return CachedUnspentSkillPoints;
        }
    }

    public static void SetProgressionFromStats(PlayerStatsData stats)
    {
        if (stats == null)
            return;

        CachedLevel = Mathf.Max(1, stats.level);
        CachedTotalXp = Mathf.Max(0, stats.totalXp);
        CachedUnspentSkillPoints = Mathf.Max(0, stats.unspentSkillPoints);
        CachedSpentSkillPoints = Mathf.Max(0, stats.spentSkillPoints);
    }

    public static void ApplyServerState(SkillTreeStateData state)
    {
        SkillStates.Clear();
        EquippedSkillIds.Clear();

        if (state?.skills != null)
        {
            for (int i = 0; i < state.skills.Length; i++)
            {
                SkillStateData skill = state.skills[i];
                if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
                    continue;

                SkillStates[skill.skillId] = new SkillRuntimeState
                {
                    unlocked = skill.unlocked,
                    level = Mathf.Clamp(skill.level, 0, MaxSkillLevel)
                };
            }
        }

        if (state?.equippedSkills != null)
        {
            for (int i = 0; i < state.equippedSkills.Length; i++)
            {
                EquippedSkillData equipped = state.equippedSkills[i];
                if (equipped == null || string.IsNullOrWhiteSpace(equipped.skillId))
                    continue;

                string key = EquipKey(ParseBranch(equipped.branch), ParseSlot(equipped.slotKind));
                EquippedSkillIds[key] = equipped.skillId;
            }
        }

        RefreshForWeapon(PlayerLoadout.CurrentWeaponKind);
    }

    public static string GetRequiredMaterialKeyForNextLevel(PlayerSkillDefinition skill)
    {
        return GetRequiredMaterialKeyForLevel(GetSkillLevel(skill != null ? skill.id : "") + 1);
    }

    public static string GetRequiredMaterialKeyForLevel(int level)
    {
        switch (Mathf.Clamp(level, 1, MaxSkillLevel))
        {
            case 1:
                return RustyScrapKey;
            case 2:
                return MetalScrapKey;
            default:
                return DiamondScrapKey;
        }
    }

    public static string GetRequiredMaterialName(PlayerSkillDefinition skill)
    {
        return GetMaterialName(GetRequiredMaterialKeyForNextLevel(skill));
    }

    public static string GetMaterialName(string materialKey)
    {
        if (string.Equals(materialKey, RustyScrapKey, StringComparison.OrdinalIgnoreCase))
            return "Rusty Scrap";
        if (string.Equals(materialKey, MetalScrapKey, StringComparison.OrdinalIgnoreCase))
            return "Metal Scrap";
        if (string.Equals(materialKey, DiamondScrapKey, StringComparison.OrdinalIgnoreCase))
            return "Diamond Scrap";
        return materialKey;
    }

    public static void RefreshForWeapon(WeaponKind weaponKind)
    {
        CurrentBranch = BranchForWeapon(weaponKind);
        CurrentActiveSkill = GetEquipped(CurrentBranch, SkillSlotKind.Active);
        CurrentPassiveSkill = GetEquipped(CurrentBranch, SkillSlotKind.Passive);
    }

    public static SkillWeaponBranch BranchForWeapon(WeaponKind weaponKind)
    {
        if (weaponKind == WeaponKind.Ranged)
            return SkillWeaponBranch.Ranged;
        if (weaponKind == WeaponKind.Hammer)
            return SkillWeaponBranch.Hammer;
        return SkillWeaponBranch.SwordSpear;
    }

    public static string GetBranchLabel(SkillWeaponBranch branch)
    {
        switch (branch)
        {
            case SkillWeaponBranch.Hammer:
                return "Hammer";
            case SkillWeaponBranch.Ranged:
                return "Ranged";
            default:
                return "Sword / Spear";
        }
    }

    public static bool IsUnlocked(string skillId)
    {
        return !string.IsNullOrWhiteSpace(skillId) &&
            SkillStates.TryGetValue(skillId, out SkillRuntimeState state) &&
            state.unlocked;
    }

    public static bool CanUnlock(PlayerSkillDefinition skill)
    {
        if (skill == null || IsUnlocked(skill.id))
            return false;
        if (!string.IsNullOrWhiteSpace(skill.prerequisiteId) && !IsUnlocked(skill.prerequisiteId))
            return false;
        return AvailableSkillPoints >= Mathf.Max(0, skill.cost);
    }

    public static bool Unlock(PlayerSkillDefinition skill, bool spendPointsLocally = true)
    {
        if (!CanUnlock(skill))
            return false;

        if (spendPointsLocally)
        {
            if (!GameStatsTracker.SpendSkillPointsLocal(Mathf.Max(0, skill.cost)))
                return false;
        }
        SkillStates[skill.id] = new SkillRuntimeState { unlocked = true, level = 0 };
        return true;
    }

    public static bool MarkUnlocked(PlayerSkillDefinition skill)
    {
        if (skill == null)
            return false;
        if (!string.IsNullOrWhiteSpace(skill.prerequisiteId) && !IsUnlocked(skill.prerequisiteId))
            return false;

        SkillStates[skill.id] = new SkillRuntimeState
        {
            unlocked = true,
            level = GetSkillLevel(skill.id)
        };
        return true;
    }

    public static int GetSkillLevel(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return 0;
        return SkillStates.TryGetValue(skillId, out SkillRuntimeState state)
            ? Mathf.Clamp(state.level, 0, MaxSkillLevel)
            : 0;
    }

    public static bool CanLevelUp(PlayerSkillDefinition skill)
    {
        return skill != null && IsUnlocked(skill.id) && GetSkillLevel(skill.id) < MaxSkillLevel;
    }

    public static bool LevelUp(PlayerSkillDefinition skill)
    {
        if (!CanLevelUp(skill))
            return false;

        int nextLevel = GetSkillLevel(skill.id) + 1;
        SkillStates[skill.id] = new SkillRuntimeState { unlocked = true, level = nextLevel };
        return true;
    }

    public static bool Equip(PlayerSkillDefinition skill)
    {
        if (skill == null || !IsUnlocked(skill.id))
            return false;

        EquippedSkillIds[EquipKey(skill.branch, skill.slotKind)] = skill.id;
        RefreshForWeapon(PlayerLoadout.CurrentWeaponKind);
        return true;
    }

    public static PlayerSkillDefinition GetEquipped(SkillWeaponBranch branch, SkillSlotKind slotKind)
    {
        EquippedSkillIds.TryGetValue(EquipKey(branch, slotKind), out string skillId);
        if (!IsUnlocked(skillId))
            return null;
        return GetSkillById(skillId);
    }

    public static PlayerSkillDefinition GetSkillById(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return null;

        for (int i = 0; i < DefaultSkills.Length; i++)
        {
            PlayerSkillDefinition skill = DefaultSkills[i];
            if (skill != null && string.Equals(skill.id, skillId, StringComparison.Ordinal))
                return skill;
        }
        return null;
    }

    private static SkillWeaponBranch ParseBranch(string branch)
    {
        if (string.Equals(branch, "Ranged", StringComparison.OrdinalIgnoreCase))
            return SkillWeaponBranch.Ranged;
        if (string.Equals(branch, "Hammer", StringComparison.OrdinalIgnoreCase))
            return SkillWeaponBranch.Hammer;
        return SkillWeaponBranch.SwordSpear;
    }

    private static SkillSlotKind ParseSlot(string slotKind)
    {
        return string.Equals(slotKind, "Passive", StringComparison.OrdinalIgnoreCase)
            ? SkillSlotKind.Passive
            : SkillSlotKind.Active;
    }

    private static string EquipKey(SkillWeaponBranch branch, SkillSlotKind slotKind)
    {
        return branch + "_" + slotKind;
    }

    private struct SkillRuntimeState
    {
        public bool unlocked;
        public int level;
    }
}

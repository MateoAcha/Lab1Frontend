using System;

[Serializable]
public class SkillTreeStateData
{
    public SkillStateData[] skills;
    public EquippedSkillData[] equippedSkills;
}

[Serializable]
public class SkillStateData
{
    public string skillId;
    public bool unlocked;
    public int level;
    public string unlockedAt;
}

[Serializable]
public class EquippedSkillData
{
    public string branch;
    public string slotKind;
    public string skillId;
}

[Serializable]
public class SkillTreeActionData
{
    public SkillTreeStateData skillTree;
    public PlayerStatsData stats;
}

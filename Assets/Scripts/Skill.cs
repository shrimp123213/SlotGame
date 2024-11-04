using UnityEngine;

[System.Serializable]
public class Skill
{
    public SkillType skillType;      // 技能類型
    public int value;                // 技能數值
    public int range;                // 技能範圍
    public int delay;                // 延遲（如果有）
    public bool isPassive;           // 是否為被動技能
    public string description;       // 技能描述

    public Skill(SkillType type, int val, int rng = 0, int dly = 0, bool passive = false, string desc = "")
    {
        skillType = type;
        value = val;
        range = rng;
        delay = dly;
        isPassive = passive;
        description = desc;
    }
}
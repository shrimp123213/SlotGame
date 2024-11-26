using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skill
{
    public List<SkillActionData> Actions = new List<SkillActionData>();
    public string skillName;

    /// <summary>
    /// 从 SkillSO 克隆技能
    /// </summary>
    /// <param name="skillSO">SkillSO 资产</param>
    /// <returns>克隆的 Skill 实例</returns>
    public static Skill FromSkillSO(SkillSO skillSO)
    {
        Skill newSkill = new Skill();
        newSkill.skillName = skillSO.skillName;
        newSkill.Actions = new List<SkillActionData>();

        foreach (var action in skillSO.actions)
        {
            // 创建 SkillActionData 的深拷贝
            SkillActionData newAction = new SkillActionData();
            newAction.Type = action.Type;
            newAction.TargetType = action.TargetType;
            newAction.Value = action.Value;
            newAction.Delay = action.Delay;

            // 确保复制 UnitsToAdd 列表
            if (action.UnitsToAdd != null)
            {
                newAction.UnitsToAdd = new List<UnitToAdd>(action.UnitsToAdd);
            }
            else
            {
                newAction.UnitsToAdd = new List<UnitToAdd>();
            }
            newSkill.Actions.Add(newAction);
        }

        return newSkill;
    }


    /// <summary>
    /// 添加技能动作
    /// </summary>
    /// <param name="action">技能动作</param>
    public void AddAction(SkillActionData action)
    {
        if (action != null)
        {
            Actions.Add(action);
        }
        else
        {
            Debug.LogWarning("Skill: 试图添加一个 null 的 SkillActionData！");
        }
    }
}

[System.Serializable]
public class SkillActionData
{
    public SkillType Type;
    public int Value;
    public TargetType TargetType;
    public int Delay = 0; // 默认为0，无延迟
    
    public List<UnitToAdd> UnitsToAdd; // 要添加到牌组的单位列表
}

[System.Serializable]
public class UnitToAdd
{
    public UnitData unitData; // 要添加的单位
    public int quantity;      // 添加的数量
}

public enum SkillType
{
    Move,
    Melee,
    Ranged,
    Defense,
    Repair,
    Breakage,
    AddToDeck,
    // 可以根据需要添加更多的技能类型
}

public enum TargetType
{
    Self,       // 只對自身生效
    Enemy,      // 只對敵方生效
    Friendly,   // 只對友方生效
    All         // 针对所有相关目标生效（可选）
}
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
        if (skillSO == null)
        {
            Debug.LogError("Skill: 无法从 null SkillSO 创建 Skill 实例！");
            return null;
        }

        Skill newSkill = new Skill
        {
            skillName = skillSO.skillName,
            Actions = new List<SkillActionData>()
        };

        foreach (var action in skillSO.actions)
        {
            // 创建 SkillActionData 的深拷贝
            SkillActionData newAction = new SkillActionData
            {
                Type = action.Type,
                Value = action.Value,
                TargetType = action.TargetType,
                Delay = action.Delay
            };
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
    public int Delay;
}

public enum SkillType
{
    Move,
    Melee,
    Ranged,
    Defense,
    Repair,
    Breakage,
    // 可以根据需要添加更多的技能类型
}

public enum TargetType
{
    Self,       // 只對自身生效
    Enemy,      // 只對敵方生效
    Friendly,   // 只對友方生效
    All         // 针对所有相关目标生效（可选）
}
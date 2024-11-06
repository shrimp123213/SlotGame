using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skill
{
    public List<SkillActionData> Actions = new List<SkillActionData>();

    /// <summary>
    /// 从 SkillSO 克隆技能
    /// </summary>
    /// <param name="skillSO">SkillSO 资产</param>
    /// <returns>克隆的 Skill 实例</returns>
    public static Skill FromSkillSO(SkillSO skillSO)
    {
        if (skillSO == null || skillSO.actions == null)
        {
            Debug.LogError("Skill: SkillSO 或其动作列表为 null！");
            return null;
        }

        Skill skill = new Skill();
        // 克隆动作列表，避免引用相同的动作数据
        foreach (var action in skillSO.actions)
        {
            SkillActionData clonedAction = new SkillActionData
            {
                Type = action.Type,
                Value = action.Value
            };
            skill.Actions.Add(clonedAction);
        }
        return skill;
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
}

public enum SkillType
{
    Move,
    Melee,
    Ranged,
    Defense,
    // 可以根据需要添加更多的技能类型
}
using System.Collections.Generic;

public class Skill
{
    public List<SkillActionData> Actions { get; private set; }

    public Skill()
    {
        Actions = new List<SkillActionData>();
    }

    /// <summary>
    /// 添加一个技能动作
    /// </summary>
    public void AddAction(SkillActionData action)
    {
        Actions.Add(action);
    }

    /// <summary>
    /// 克隆SkillSO到Skill实例
    /// </summary>
    public static Skill FromSkillSO(SkillSO skillSO)
    {
        Skill skill = new Skill();
        foreach (var action in skillSO.actions)
        {
            // 深拷贝 SkillActionData
            SkillActionData copiedAction = new SkillActionData
            {
                Type = action.Type,
                Value = action.Value
            };
            skill.AddAction(copiedAction);
        }
        return skill;
    }
}
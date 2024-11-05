using System.Collections.Generic;

public class Skill
{
    public List<SkillAction> Actions { get; private set; }

    public Skill()
    {
        Actions = new List<SkillAction>();
    }

    /// <summary>
    /// 添加一个技能动作
    /// </summary>
    public void AddAction(SkillAction action)
    {
        Actions.Add(action);
    }
}
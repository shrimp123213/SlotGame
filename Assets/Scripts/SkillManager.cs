using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    /// <summary>
    /// 解析技能字符串并返回一个 Skill 对象
    /// 示例输入："2[Move] 2[Melee]"
    /// </summary>
    public Skill ParseSkill(string skillString)
    {
        Skill skill = new Skill();

        // 使用正则表达式解析技能字符串
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"(\d+)\[(\w+)\]");
        var matches = regex.Matches(skillString);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count == 3)
            {
                int value = int.Parse(match.Groups[1].Value);
                string typeString = match.Groups[2].Value;

                // 尝试将字符串转换为 SkillType 枚举
                if (System.Enum.TryParse(typeString, true, out SkillType type))
                {
                    skill.AddAction(new SkillAction(type, value));
                }
                else
                {
                    Debug.LogWarning($"未知的技能类型：{typeString}");
                }
            }
        }

        return skill;
    }
    
    /// <summary>
    /// 执行技能，支持动作的前后顺序和阻塞
    /// </summary>
    public void ExecuteSkill(Skill skill, UnitController unit)
    {
        // 执行移动动作
        foreach (var action in skill.Actions)
        {
            if (action.Type == SkillType.Move)
            {
                for (int i = 0; i < action.Value; i++)
                {
                    // 检查是否还能移动
                    if (!unit.CanMoveForward())
                    {
                        Debug.Log($"{unit.unitData.unitName} 无法继续移动，动作被阻挡！");
                        break;
                    }

                    unit.MoveForward();
                }
            }
        }

        // 执行攻击和防卫动作
        foreach (var action in skill.Actions)
        {
            switch (action.Type)
            {
                case SkillType.Melee:
                    for (int i = 0; i < action.Value; i++)
                    {
                        unit.PerformMeleeAttack();
                    }
                    break;
                case SkillType.Ranged:
                    for (int i = 0; i < action.Value; i++)
                    {
                        unit.PerformRangedAttack();
                    }
                    break;
                case SkillType.Defense:
                    unit.IncreaseDefense(action.Value);
                    break;
                default:
                    Debug.LogWarning($"未处理的技能类型：{action.Type}");
                    break;
            }
        }
    }


}

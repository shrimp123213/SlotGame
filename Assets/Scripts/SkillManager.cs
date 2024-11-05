using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    /// <summary>
    /// 执行技能，支持动作的前后顺序和阻塞
    /// </summary>
    /// <param name="skillSO">要执行的技能ScriptableObject</param>
    /// <param name="unit">执行技能的单位</param>
    public void ExecuteSkill(SkillSO skillSO, UnitController unit)
    {
        if (skillSO == null || unit == null)
        {
            Debug.LogError("SkillSO或UnitController为null！");
            return;
        }

        // 克隆SkillSO为运行时Skill实例
        Skill runtimeSkill = Skill.FromSkillSO(skillSO);

        bool isBlocked = false;

        // 首先执行所有的移动动作
        foreach (var action in runtimeSkill.Actions)
        {
            if (action.Type == SkillType.Move)
            {
                for (int i = 0; i < action.Value; i++)
                {
                    // 检查是否还能移动
                    if (!unit.CanMoveForward())
                    {
                        Debug.Log($"{unit.unitData.unitName} 无法继续移动，动作被阻挡！");
                        isBlocked = true;
                        break;
                    }

                    unit.MoveForward();
                }

                if (isBlocked)
                {
                    break; // 停止执行后续的技能动作
                }
            }
        }

        if (!isBlocked)
        {
            // 执行攻击和防卫动作
            foreach (var action in runtimeSkill.Actions)
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
                    // 可以根据需要添加更多的技能类型
                    default:
                        Debug.LogWarning($"未处理的技能类型：{action.Type}");
                        break;
                }
            }
        }

        // 如果技能执行被阻挡，尝试应用支援技能
        if (isBlocked && unit.supportSkillSO != null)
        {
            // 找到前方的友方单位
            UnitController frontUnit = GridManager.Instance.GetFrontUnitInRow(unit);
            if (frontUnit != null)
            {
                Debug.Log($"{unit.unitData.unitName} 无法执行主技能，尝试应用支援技能到前方单位 {frontUnit.unitData.unitName}。");
                ApplySupportSkill(unit.supportSkillSO, frontUnit);
            }
            else
            {
                Debug.LogWarning($"没有找到单位 {unit.unitData.unitName} 前方的友方单位来应用支援技能。");
            }
        }
    }

    /// <summary>
    /// 应用支援技能到目标单位
    /// </summary>
    /// <param name="supportSkillSO">支援技能ScriptableObject</param>
    /// <param name="targetUnit">目标单位</param>
    public void ApplySupportSkill(SkillSO supportSkillSO, UnitController targetUnit)
    {
        if (supportSkillSO == null || targetUnit == null)
        {
            Debug.LogError("SupportSkillSO或TargetUnit为null！");
            return;
        }

        // 克隆支援技能为运行时Skill实例
        Skill supportSkill = Skill.FromSkillSO(supportSkillSO);

        // 应用支援技能的动作到目标单位的当前Skill
        foreach (var action in supportSkill.Actions)
        {
            targetUnit.currentSkill.AddAction(action);
            Debug.Log($"{targetUnit.unitData.unitName} 的主技能增加了支援技能动作：{action.Type} {action.Value}");
        }

        // 重新执行目标单位的主技能
        targetUnit.ExecuteCurrentSkill();
    }
}

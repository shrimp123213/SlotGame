using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理技能的执行
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：保持在场景切换中不被销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 执行技能，支持动作的前后顺序和阻塞
    /// </summary>
    /// <param name="skillSO">要执行的技能ScriptableObject</param>
    /// <param name="user">执行技能的用户（单位或建筑物）</param>
    public void ExecuteSkill(SkillSO skillSO, ISkillUser user)
    {
        if (skillSO == null || user == null)
        {
            Debug.LogError("SkillManager: SkillSO 或 ISkillUser 为 null！");
            return;
        }

        // 启动协程执行技能
        StartCoroutine(ExecuteSkillCoroutine(skillSO, user));
    }

    /// <summary>
    /// 协程执行技能动作
    /// </summary>
    /// <param name="skillSO">技能ScriptableObject</param>
    /// <param name="user">技能执行者</param>
    /// <returns></returns>
    private IEnumerator ExecuteSkillCoroutine(SkillSO skillSO, ISkillUser user)
    {
        // 克隆SkillSO为运行时Skill实例
        Skill runtimeSkill = Skill.FromSkillSO(skillSO);
        if (runtimeSkill == null || runtimeSkill.Actions == null)
        {
            Debug.LogError("SkillManager: 无法克隆 SkillSO！");
            yield break;
        }

        bool movementBlocked = false;

        // 按动作的顺序执行
        foreach (var action in runtimeSkill.Actions)
        {
            switch (action.Type)
            {
                case SkillType.Move:
                    for (int i = 0; i < action.Value; i++)
                    {
                        if (user.CanMoveForward())
                        {
                            user.MoveForward();
                            yield return new WaitForSeconds(0.1f); // 可根据需要调整移动间隔
                        }
                        else
                        {
                            Debug.Log("SkillManager: 用户无法继续移动，动作被阻挡！");
                            movementBlocked = true;
                            break;
                        }
                    }
                    break;

                case SkillType.Melee:
                    for (int i = 0; i < action.Value; i++)
                    {
                        user.PerformMeleeAttack();
                        yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    }
                    break;

                case SkillType.Ranged:
                    for (int i = 0; i < action.Value; i++)
                    {
                        user.PerformRangedAttack();
                        yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    }
                    break;

                case SkillType.Defense:
                    user.IncreaseDefense(action.Value);
                    yield return null; // 防御动作通常是即时的
                    break;

                default:
                    Debug.LogWarning($"SkillManager: 未处理的技能类型：{action.Type}");
                    break;
            }

            // 如果移动被阻挡，跳出动作执行循环
            if (movementBlocked)
            {
                break;
            }
        }

        if (movementBlocked)
        {
            // 查找前方的友方单位（仅适用于单位，建筑物通常不会有友方单位）
            if (user is UnitController unit)
            {
                UnitController frontUnit = GridManager.Instance.GetFrontUnitInRow(unit);
                if (frontUnit != null && frontUnit.unitData.camp == unit.unitData.camp)
                {
                    Debug.Log($"SkillManager: {unit.unitData.unitName} 被阻挡，应用支援技能到前方单位 {frontUnit.unitData.unitName}。");
                    ApplySupportSkill(unit.unitData.supportSkillSO, frontUnit);
                }
                else
                {
                    Debug.Log($"SkillManager: {unit.unitData.unitName} 被阻挡，前方无友方单位，执行主技能的剩余动作。");
                    // 执行主技能的剩余动作
                    StartCoroutine(ExecuteRemainingSkillActionsCoroutine(runtimeSkill, user));
                }
            }
            else
            {
                // 对于建筑物，直接执行剩余的技能动作
                StartCoroutine(ExecuteRemainingSkillActionsCoroutine(runtimeSkill, user));
            }
        }
        else
        {
            // 移动未被阻挡，执行主技能的非移动动作
            StartCoroutine(ExecuteRemainingSkillActionsCoroutine(runtimeSkill, user));
        }
    }

    /// <summary>
    /// 协程执行技能的非移动动作
    /// </summary>
    /// <param name="runtimeSkill">运行时Skill实例</param>
    /// <param name="user">执行技能的用户（单位或建筑物）</param>
    /// <returns></returns>
    private IEnumerator ExecuteRemainingSkillActionsCoroutine(Skill runtimeSkill, ISkillUser user)
    {
        if (runtimeSkill == null || runtimeSkill.Actions == null)
        {
            Debug.LogError("SkillManager: 运行时Skill实例或其动作列表为 null！");
            yield break;
        }

        foreach (var action in runtimeSkill.Actions)
        {
            switch (action.Type)
            {
                case SkillType.Melee:
                    for (int i = 0; i < action.Value; i++)
                    {
                        user.PerformMeleeAttack();
                        yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    }
                    break;

                case SkillType.Ranged:
                    for (int i = 0; i < action.Value; i++)
                    {
                        user.PerformRangedAttack();
                        yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    }
                    break;

                case SkillType.Defense:
                    user.IncreaseDefense(action.Value);
                    yield return null; // 防御动作通常是即时的
                    break;

                default:
                    Debug.LogWarning($"SkillManager: 未处理的技能类型：{action.Type}");
                    break;
            }
        }

        yield return null;
    }

    /// <summary>
    /// 应用支援技能到目标用户
    /// </summary>
    /// <param name="supportSkillSO">支援技能ScriptableObject</param>
    /// <param name="targetUser">目标用户（单位或建筑物）</param>
    public void ApplySupportSkill(SkillSO supportSkillSO, ISkillUser targetUser)
    {
        if (supportSkillSO == null || targetUser == null)
        {
            Debug.LogError("SkillManager: 支援技能的SkillSO或目标用户为 null！");
            return;
        }

        // 执行支援技能
        ExecuteSkill(supportSkillSO, targetUser);
    }

    /// <summary>
    /// 执行技能的非移动动作（同步方法，用于特定情况）
    /// </summary>
    /// <param name="runtimeSkill">运行时Skill实例</param>
    /// <param name="user">执行技能的用户（单位或建筑物）</param>
    private void ExecuteRemainingSkillActions(Skill runtimeSkill, ISkillUser user)
    {
        if (runtimeSkill == null || runtimeSkill.Actions == null)
        {
            Debug.LogError("SkillManager: 运行时Skill实例或其动作列表为 null！");
            return;
        }

        foreach (var action in runtimeSkill.Actions)
        {
            switch (action.Type)
            {
                case SkillType.Melee:
                    user.PerformMeleeAttack();
                    break;
                case SkillType.Ranged:
                    user.PerformRangedAttack();
                    break;
                case SkillType.Defense:
                    user.IncreaseDefense(action.Value);
                    break;
                // 可以根据需要添加更多的技能类型
                default:
                    Debug.LogWarning($"SkillManager: 未处理的技能类型：{action.Type}");
                    break;
            }
        }
    }
}

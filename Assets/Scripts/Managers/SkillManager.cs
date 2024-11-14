using System.Collections;
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

        // 验证每个SkillAction的TargetType是否与SkillType匹配
        foreach (var action in runtimeSkill.Actions)
        {
            switch (action.Type)
            {
                case SkillType.Melee:
                case SkillType.Ranged:
                    if (action.TargetType != TargetType.Enemy)
                    {
                        Debug.LogWarning($"SkillManager: SkillAction {action.Type} 应该只对敌方生效。强制设置为 Enemy。");
                        action.TargetType = TargetType.Enemy;
                    }
                    break;
                case SkillType.Defense:
                    if (action.TargetType != TargetType.Friendly && action.TargetType != TargetType.Self)
                    {
                        Debug.LogWarning($"SkillManager: SkillAction {action.Type} 应该只对友方或自身生效。强制设置为 Friendly。");
                        action.TargetType = TargetType.Friendly;
                    }
                    break;
                // 可以为更多SkillType添加验证逻辑
                default:
                    break;
            }
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
                    user.PerformMeleeAttack(action.TargetType);
                    yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    break;

                case SkillType.Ranged:
                    user.PerformRangedAttack(action.TargetType);
                    yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    break;

                case SkillType.Defense:
                    user.IncreaseDefense(action.Value, action.TargetType);
                    yield return null; // 防御动作通常是即时的
                    break;

                default:
                    Debug.LogWarning($"SkillManager: 未处理的技能类型：{action.Type}");
                    break;
            }

            // 如果移动被阻挡，跳出动作执行循环
            if (movementBlocked)
            {
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
                break; // Exit the action loop
            }
        }

        if (!movementBlocked)
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
                case SkillType.Ranged:
                case SkillType.Defense:
                    // 这些已经在主协程中处理过
                    break;
                default:
                    switch (action.Type)
                    {
                        case SkillType.Move:
                            // 已在主协程中处理
                            break;
                        default:
                            Debug.LogWarning($"SkillManager: 未处理的技能类型：{action.Type}");
                            break;
                    }
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
}

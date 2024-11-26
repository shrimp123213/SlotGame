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
                case SkillType.Breakage:
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
                case SkillType.AddToDeck:
                    // 调用处理添加到牌组的方法
                    yield return StartCoroutine(HandleAddToDeckAction(action, user));
                    yield return null;
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
                            yield return StartCoroutine(user.MoveForward()); // 等待移动完成
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
                    yield return StartCoroutine(user.PerformMeleeAttack(action.TargetType)); // 等待攻击完成
                    yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    break;

                case SkillType.Ranged:
                    yield return StartCoroutine(user.PerformRangedAttack(action.TargetType)); // 等待攻击完成
                    yield return new WaitForSeconds(0.1f); // 可根据需要调整攻击间隔
                    break;

                case SkillType.Defense:
                    yield return StartCoroutine(user.IncreaseDefense(action.Value, action.TargetType)); // 等待防御完成
                    yield return null; // 防御动作通常是即时的
                    break;
                
                case SkillType.Breakage:
                    yield return StartCoroutine(user.PerformBreakage(action.Value)); // 等待破壞完成
                    yield return new WaitForSeconds(0.1f); // 可根据需要调整破壞间隔
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
                        // 根据需要，决定是否执行剩余动作
                    }
                }
                else
                {
                    // 对于建筑物，直接继续执行
                }
                break; // 退出动作循环
            }
        }
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
    
    public IEnumerator HandleAddToDeckAction(SkillActionData action, ISkillUser user)
    {
        if (action.UnitsToAdd == null || action.UnitsToAdd.Count == 0)
        {
            Debug.LogWarning("SkillManager: AddToDeck 动作未指定任何单位！");
            yield break;
        }

        // 获取 DeckManager 实例
        DeckManager deckManager = DeckManager.Instance;
        if (deckManager == null)
        {
            Debug.LogError("SkillManager: 未找到 DeckManager 实例！");
            yield break;
        }

        // 确定要添加到哪个牌组
        Camp userCamp = user.GetCamp(); // 假设 ISkillUser 有 GetCamp() 方法
        Deck targetDeck = userCamp == Camp.Player ? deckManager.playerDeck : deckManager.enemyDeck;

        // 将指定的单位添加到牌组
        foreach (var unitToAdd in action.UnitsToAdd)
        {
            if (unitToAdd.unitData == null || unitToAdd.quantity <= 0)
            {
                Debug.LogWarning("SkillManager: AddToDeck 动作包含无效的单位或数量！");
                continue;
            }

            targetDeck.AddCard(unitToAdd.unitData, null, unitToAdd.quantity, false);
            Debug.Log($"SkillManager: 已将 {unitToAdd.quantity} 张 {unitToAdd.unitData.unitName} 添加到 {userCamp} 的牌组中。");
        }

        yield return null;
    }
}

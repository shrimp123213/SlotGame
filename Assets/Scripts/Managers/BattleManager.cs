using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理战斗流程的主控制器
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Battle Components")]
    public SlotMachineController slotMachine; // 转盘组件
    public GridManager gridManager;           // 网格管理器
    public ConnectionManager connectionManager; // 连线管理器

    [Header("Enemy Buildings")]
    public List<EnemyBuildingInfo> enemyBuildings = new List<EnemyBuildingInfo>();

    [Header("Player Buildings")]
    public List<PlayerBuildingInfo> playerBuildings = new List<PlayerBuildingInfo>();

    private bool choiceMade = false;

    private BattlePhase currentPhase;

    public enum BattlePhase
    {
        SlotMachineSpin,                     // 阶段 2.1：转盘旋转
        DefenseEffect,                       // 阶段 2.2：执行防卫效果
        SkillActivation,                     // 新的第一阶段：单位发动技能
        EffectResolution,                    // 新的第二阶段：数值与状态结算
        Movement,                            // 新的第三阶段：单位移动与触发技能
        PostMovementEffectResolution,        // 新的第四阶段：技能后的数值与状态结算
        PrepareNextTurn,                     // 新的第五阶段：准备下一回合
        ComboCalculation                     // 阶段 2.12：连线计算
    }

    private List<Effect> effectQueue = new List<Effect>();              // 数值变化队列
    private List<SkillExecution> skillExecutionQueue = new List<SkillExecution>();  // 技能执行队列

    // 用于存储技能和其执行者的类
    public class SkillExecution
    {
        public Skill skill;
        public ISkillUser user;

        public SkillExecution(Skill skill, ISkillUser user)
        {
            this.skill = skill;
            this.user = user;
        }
    }

    private void Awake()
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

    private void Start()
    {
        // 检查所有必要的组件是否已正确设置
        if (slotMachine == null)
        {
            Debug.LogError("BattleManager: 未设置 SlotMachine 组件！");
            return;
        }
        if (gridManager == null)
        {
            Debug.LogError("BattleManager: 未设置 GridManager 组件！");
            return;
        }
        if (connectionManager == null)
        {
            Debug.LogError("BattleManager: 未设置 ConnectionManager 组件！");
            return;
        }
        // 初始化建筑物
        gridManager.InitializeBuildings(playerBuildings, enemyBuildings);

        // 订阅 SlotMachine 的转动完成事件
        slotMachine.OnSpinCompleted += OnSlotMachineSpun;

        // 开始战斗流程
        StartBattleSequence();
    }

    private void OnEnable()
    {
        // 订阅选择完成事件
        if (DeckChoiceUI.Instance != null)
        {
            DeckChoiceUI.Instance.OnChoiceMade += OnPlayerChoiceMade;
        }
    }

    private void OnDisable()
    {
        // 取消订阅事件
        if (DeckChoiceUI.Instance != null)
        {
            DeckChoiceUI.Instance.OnChoiceMade -= OnPlayerChoiceMade;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
        if (slotMachine != null)
        {
            slotMachine.OnSpinCompleted -= OnSlotMachineSpun;
        }
    }

    /// <summary>
    /// 启动战斗流程
    /// </summary>
    public void StartBattleSequence()
    {
        StartCoroutine(BattleSequence());
    }

    /// <summary>
    /// 战斗流程协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator BattleSequence()
    {
        // 阶段 2.1：转盘旋转
        currentPhase = BattlePhase.SlotMachineSpin;
        yield return StartCoroutine(ExecuteSlotMachine());

        // 阶段 2.2：执行防卫效果
        currentPhase = BattlePhase.DefenseEffect;
        ExecuteDefenseEffects();

        // 新的第一阶段：单位发动技能
        currentPhase = BattlePhase.SkillActivation;
        yield return StartCoroutine(ExecuteSkillActivationPhase());

        // 新的第二阶段：数值与状态结算
        currentPhase = BattlePhase.EffectResolution;
        yield return StartCoroutine(ExecuteEffectResolutionPhase());

        // 新的第三阶段：单位移动与触发技能
        currentPhase = BattlePhase.Movement;
        yield return StartCoroutine(ExecuteMovementPhase());

        // 新的第四阶段：技能后的数值与状态结算
        currentPhase = BattlePhase.PostMovementEffectResolution;
        yield return StartCoroutine(ExecutePostMovementEffectResolutionPhase());

        // 阶段 2.12：连线计算
        currentPhase = BattlePhase.ComboCalculation;
        ExecuteComboCalculation();

        // 检查战斗结果
        if (CheckBattleOutcome())
        {
            yield break; // 战斗结束，退出协程
        }

        // 新的第五阶段：准备下一回合
        currentPhase = BattlePhase.PrepareNextTurn;
        yield return StartCoroutine(PrepareNextTurnPhase());
    }

    /// <summary>
    /// 2.1 战斗画面 转盘
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteSlotMachine()
    {
        Debug.Log("BattleManager: 开始转盘旋转！");
        slotMachine.StartSpinning();

        // 等待转盘旋转完成
        while (slotMachine.isSpinning)
        {
            yield return null;
        }

        // 转盘完成后，流程将由 OnSlotMachineSpun 回调继续
        Debug.Log("BattleManager: 转盘旋转完成！");
    }

    /// <summary>
    /// SlotMachine旋转完成后的回调
    /// </summary>
    public void OnSlotMachineSpun()
    {
        // 转盘旋转完成后，继续战斗流程
        Debug.Log("BattleManager: 转盘旋转完成！");
    }

    /// <summary>
    /// 阶段 2.2：战斗画面 防卫
    /// </summary>
    private void ExecuteDefenseEffects()
    {
        Debug.Log("BattleManager: 执行所有单位的防卫效果！");
        var allUnits = gridManager.GetAllUnits();
        foreach (var unit in allUnits)
        {
            unit.ExecuteDefense();
        }
    }

    /// <summary>
    /// 第一阶段：单位发动技能
    /// </summary>
    private IEnumerator ExecuteSkillActivationPhase()
    {
        Debug.Log("BattleManager: 第一阶段 - 单位发动技能");

        var allSkillUsers = gridManager.GetAllSkillUsers();
        foreach (var user in allSkillUsers)
        {
            user.UseSkill();
        }

        // 执行技能队列中的技能
        foreach (var skillExec in skillExecutionQueue)
        {
            yield return StartCoroutine(ExecuteSkill(skillExec.skill, skillExec.user));
        }

        // 清空技能执行队列
        skillExecutionQueue.Clear();

        yield return null;
    }

    /// <summary>
    /// 第二阶段：数值与状态结算
    /// </summary>
    private IEnumerator ExecuteEffectResolutionPhase()
    {
        Debug.Log("BattleManager: 第二阶段 - 数值与状态结算");

        foreach (var effect in effectQueue)
        {
            ApplyEffect(effect);
        }

        effectQueue.Clear(); // 清空效果队列

        yield return new WaitForSeconds(1); // 可选：等待一段时间，确保效果结算完成
    }

    /// <summary>
    /// 第三阶段：单位移动与触发技能
    /// </summary>
    private IEnumerator ExecuteMovementPhase()
    {
        Debug.Log("BattleManager: 第三阶段 - 单位移动与触发技能");

        var allUnits = gridManager.GetAllUnits();
        foreach (var unit in allUnits)
        {
            int moveCount = unit.GetMoveCount();

            if (moveCount > 0)
            {
                for (int i = 0; i < moveCount; i++)
                {
                    if (unit.CanMoveForward())
                    {
                        yield return StartCoroutine(unit.MoveForward());
                    }
                    else
                    {
                        Debug.Log($"BattleManager: 单位 {unit.name} 无法继续移动，动作被阻挡！");
                        break; // 如果不能移动，停止进一步的移动尝试
                    }
                }
            }
            else
            {
                Debug.Log($"BattleManager: 单位 {unit.name} 没有可用的移动技能或次数为 0");
            }
        }

        yield return null;
    }

    /// <summary>
    /// 第四阶段：技能后的数值与状态结算
    /// </summary>
    private IEnumerator ExecutePostMovementEffectResolutionPhase()
    {
        Debug.Log("BattleManager: 第四阶段 - 技能后的数值与状态结算");

        // 执行移动后触发的技能
        foreach (var skillExec in skillExecutionQueue)
        {
            yield return StartCoroutine(ExecuteSkill(skillExec.skill, skillExec.user));
        }

        // 清空技能执行队列
        skillExecutionQueue.Clear();

        // 结算效果队列中的效果
        foreach (var effect in effectQueue)
        {
            ApplyEffect(effect);
        }

        effectQueue.Clear(); // 清空效果队列

        yield return new WaitForSeconds(1); // 可选：等待一段时间，确保效果结算完成
    }

    /// <summary>
    /// 2.13 战斗画面 连线 COMBO
    /// </summary>
    private void ExecuteComboCalculation()
    {
        Debug.Log("BattleManager: 计算连线 COMBO！");
        connectionManager.CheckConnections();

        // 实现连线计算逻辑
        ComboCalculator.CalculateCombo(gridManager);
    }

    /// <summary>
    /// 第五阶段：准备下一回合
    /// </summary>
    private IEnumerator PrepareNextTurnPhase()
    {
        Debug.Log("BattleManager: 第五阶段 - 准备下一回合");

        // 更新单位的延迟、状态等
        var allSkillUsers = gridManager.GetAllSkillUsers();
        foreach (var user in allSkillUsers)
        {
            user.PrepareForNextTurn();
        }

        // 减少牌库中所有单位的技能延迟
        DeckManager.Instance.ReduceSkillDelaysAtStartOfTurn();

        // 显示选择面板让玩家选择增加卡牌
        choiceMade = false;
        DeckChoiceUI.Instance.ShowChoicePanel();

        // 等待玩家做出选择
        while (!choiceMade)
        {
            yield return null;
        }

        // 等待一小段时间，确保 UI 更新
        yield return new WaitForSeconds(0.5f);

        // 开始下一回合
        StartBattleSequence();
    }

    /// <summary>
    /// 检查战斗结果并结束战斗
    /// </summary>
    private bool CheckBattleOutcome()
    {
        var playerBoss = gridManager.GetBossUnit(Camp.Player);
        var enemyBoss = gridManager.GetBossUnit(Camp.Enemy);

        if (playerBoss == null || playerBoss.IsDead)
        {
            // 玩家 Boss 阵亡，敌方胜利
            GameManager.Instance.EndGame("You Lose!");
            return true;
        }
        else if (enemyBoss == null || enemyBoss.IsDead)
        {
            // 敌方 Boss 阵亡，玩家胜利
            GameManager.Instance.EndGame("You Win!");
            return true;
        }

        // 战斗未结束，继续下一回合
        return false;
    }

    /// <summary>
    /// 当玩家完成选择后触发的回调方法
    /// </summary>
    private void OnPlayerChoiceMade()
    {
        choiceMade = true;
    }

    public void OnBuildingDestroyed(BuildingController building, int row)
    {
        // 检查是否是关键建筑物被摧毁，例如保护 Boss 的建筑物
        if (building.buildingData.buildingName == "空塔" || building.buildingData.buildingName == "箭塔")
        {
            // 更新游戏状态，允许指定行的单位攻击 Boss
            AllowUnitsAttackBoss(row);
        }
    }

    public void AllowUnitsAttackBoss(int row)
    {
        // 更新游戏状态，允许指定行的单位攻击 Boss
        Debug.Log($"BattleManager: 第 {row} 行的单位现在可以攻击 Boss 了！");

        // 在 GridManager 或其他地方更新状态
        gridManager.SetRowCanAttackBoss(row, true);
    }

    public void OnBossDefeated(BossController boss)
    {
        if (boss.bossData.camp == Camp.Player)
        {
            // 玩家 BOSS 被击败，敌人胜利
            GameManager.Instance.EndGame("You Lose!");
        }
        else
        {
            // 敌人 BOSS 被击败，玩家胜利
            GameManager.Instance.EndGame("You Win!");
        }
    }

    public void AddEffectToQueue(Effect effect)
    {
        effectQueue.Add(effect);
    }

    public void ScheduleSkillExecution(Skill skill, ISkillUser user)
    {
        skillExecutionQueue.Add(new SkillExecution(skill, user));
    }

    private IEnumerator ExecuteSkill(Skill skill, ISkillUser user)
    {
        foreach (var action in skill.Actions)
        {
            user.ScheduleAction(action);
        }
        yield return null;
    }

    private void ApplyEffect(Effect effect)
    {
        if (effect.target != null)
        {
            if (effect.damage > 0)
            {
                effect.target.TakeDamage(effect.damage);
            }
            if (effect.heal > 0)
            {
                effect.target.Heal(effect.heal);
            }
            // 处理状态效果（如果有）
            /*if (effect.statusEffect != null)
            {
                effect.target.ApplyStatusEffect(effect.statusEffect);
            }*/
        }
    }
}

public class Effect
{
    public ISkillUser target;          // 目标单位
    public int damage;                 // 伤害值
    public int heal;                   // 治疗值
    //public StatusEffect statusEffect;  // 状态效果（如果有）

    // 构造函数（可选）
    public Effect(ISkillUser target, int damage = 0, int heal = 0) //StatusEffect statusEffect = null)
    {
        this.target = target;
        this.damage = damage;
        this.heal = heal;
        //this.statusEffect = statusEffect;
    }
}

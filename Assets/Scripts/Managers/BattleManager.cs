using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 管理战斗流程的主控制器
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Battle Components")]
    public SlotMachineController slotMachine; // 轉盤組件
    public SkillManager skillManager;         // 技能管理器
    public GridManager gridManager;           // 網格管理器
    public ConnectionManager connectionManager; // 連接管理器
    
    [Header("Enemy Buildings")]
    public List<EnemyBuildingInfo> enemyBuildings = new List<EnemyBuildingInfo>();

    [Header("Player Buildings")]
    public List<PlayerBuildingInfo> playerBuildings = new List<PlayerBuildingInfo>();
    
    private bool choiceMade = false;
    
    private bool playerBossAlive = false;
    private bool enemyBossAlive = false;

    private BossController playerBoss;
    private BossController enemyBoss;
    
    private BattlePhase currentPhase;
    
    public enum BattlePhase
    {
        SkillActivation,                // 第一阶段：单位发动技能
        EffectResolution,               // 第二阶段：数值与状态结算
        Movement,                       // 第三阶段：单位移动与触发技能
        PostMovementEffectResolution,   // 第四阶段：技能后的数值与状态结算
        PrepareNextTurn                 // 第五阶段：准备下一回合
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
        // 單例模式實現
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可選：保持在場景切換中不被銷毀
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 檢查所有必要的組件是否已正確設置
        if (slotMachine == null)
        {
            Debug.LogError("BattleManager: 未設置 SlotMachine 組件！");
            return;
        }
        if (skillManager == null)
        {
            Debug.LogError("BattleManager: 未設置 SkillManager 組件！");
            return;
        }
        if (gridManager == null)
        {
            Debug.LogError("BattleManager: 未設置 GridManager 組件！");
            return;
        }
        if (connectionManager == null)
        {
            Debug.LogError("BattleManager: 未設置 ConnectionManager 組件！");
            return;
        }
        // 初始化建築物
        GridManager.Instance.InitializeBuildings(playerBuildings, enemyBuildings);

        // 訂閱 SlotMachine 的轉動完成事件
        slotMachine.OnSpinCompleted += OnSlotMachineSpun;
        
        // 開始戰鬥流程
        StartBattleSequence();
    }
    
    private void OnEnable()
    {
        // 訂閱選擇完成事件
        DeckChoiceUI.Instance.OnChoiceMade += OnPlayerChoiceMade;
    }
    
    private void OnDisable()
    {
        // 取消訂閱事件
        if (DeckChoiceUI.Instance != null)
        {
            DeckChoiceUI.Instance.OnChoiceMade -= OnPlayerChoiceMade;
        }
    }

    private void OnDestroy()
    {
        // 取消訂閱事件，防止內存洩漏
        if (slotMachine != null)
        {
            slotMachine.OnSpinCompleted -= OnSlotMachineSpun;
        }
    }
    
    /// <summary>
    /// 啟動戰鬥流程
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
        while (true)
        {
            // 第一阶段：单位发动技能
            currentPhase = BattlePhase.SkillActivation;
            yield return StartCoroutine(ExecuteSkillActivationPhase());

            // 第二阶段：数值与状态结算
            currentPhase = BattlePhase.EffectResolution;
            yield return StartCoroutine(ExecuteEffectResolutionPhase());

            // 第三阶段：单位移动与触发技能
            currentPhase = BattlePhase.Movement;
            yield return StartCoroutine(ExecuteMovementPhase());

            // 第四阶段：技能后的数值与状态结算
            currentPhase = BattlePhase.PostMovementEffectResolution;
            yield return StartCoroutine(ExecutePostMovementEffectResolutionPhase());

            // 第五阶段：准备下一回合
            currentPhase = BattlePhase.PrepareNextTurn;
            yield return StartCoroutine(PrepareNextTurnPhase());

            // 战斗结束逻辑，根据连线结果决定
            CheckBattleOutcome();
            
            // // 检查游戏是否结束
            // if (CheckBattleOutcome())
            // {
            //     break;
            // }

            // 等待一小段时间，避免过快循环
            //yield return new WaitForSeconds(0.1f);
        }
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
    }

    /// <summary>
    /// SlotMachine旋转完成后的回调
    /// </summary>
    public void OnSlotMachineSpun()
    {
        StartCoroutine(ContinueBattleAfterSlotMachine());
    }

    /// <summary>
    /// 继续战斗流程协程
    /// </summary>
    /// <param name="selectedColumn">转盘选择的列</param>
    /// <returns></returns>
    private IEnumerator ContinueBattleAfterSlotMachine()
    {
        // 已经在 SlotMachineController 内部调用了 WeightedDrawAndPlaceCards，不需要再次调用
        
        // 继续后续战斗流程
        // 2.2 战斗画面 防卫
        ExecuteDefenseEffects();

        // 2.3 战斗画面 波次1 我方建筑行动
        yield return StartCoroutine(ExecutePlayerBuildingActions());

        // 2.4 - 2.7 战斗画面 波次2-波次7 单位行动
        for (int wave = 1; wave <= 6; wave++)
        {
            yield return StartCoroutine(ExecuteUnitActionsByWave(wave));
        }

        // 2.8 战斗画面 波次8 敌方部位行动
        yield return StartCoroutine(ExecuteEnemyPositionActions());

        // 2.9 战斗画面 波次9 敌方Boss行动
        yield return StartCoroutine(ExecuteBossAction());

        // 2.13 战斗画面 连线 COMBO
        ExecuteComboCalculation();

        // 战斗结束逻辑，根据连线结果决定
        CheckBattleOutcome();
    }

    /// <summary>
    /// 2.2 战斗画面 防卫
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
    /// 2.3 战斗画面 波次1 我方建筑行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecutePlayerBuildingActions()
    {
        Debug.Log("BattleManager: 执行我方建筑的行动！");
        var buildings = GridManager.Instance.GetPlayerBuildings();
        foreach (var building in buildings)
        {
            //building.ExecuteAction();
            yield return new WaitForSeconds(0.1f); // 每个建筑间隔执行
        }
    }

    /// <summary>
    /// 2.4 - 2.7 战斗画面 波次2-波次7 单位行动
    /// </summary>
    /// <param name="wave">当前波次（1-6）</param>
    /// <returns></returns>
    private IEnumerator ExecuteUnitActionsByWave(int wave)
    {
        Debug.Log($"BattleManager: 执行波次{wave}的单位行动！");
        var units = gridManager.GetUnitsByColumn(wave);
        foreach (var unit in units)
        {
            if (unit != null && unit.gameObject.activeSelf)
            {
                //yield return StartCoroutine(unit.UseSkill());
                yield return new WaitForSeconds(0.1f); // 每个单位间隔执行
            }
        }
        yield return new WaitForSeconds(0.2f); // 每个波次间隔
    }

    /// <summary>
    /// 2.8 战斗画面 波次8 敌方部位行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteEnemyPositionActions()
    {
        Debug.Log("BattleManager: 执行敌方部位的行动！");
        var enemyBuildings = gridManager.GetEnemyBuildings();
        foreach (var building in enemyBuildings)
        {
            if (building != null && building.gameObject.activeSelf)
            {
                //building.ExecuteAction();
                yield return new WaitForSeconds(0.1f); // 每个部位间隔执行
            }
        }
        yield return null;
    }

    /// <summary>
    /// 2.9 战斗画面 波次9 敌方Boss行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteBossAction()
    {
        Debug.Log("BattleManager: 执行敌方Boss的行动！");
        var boss = gridManager.GetBossUnit();
        if (boss != null && boss.gameObject.activeSelf)
        {
            //boss.ExecuteBossAbility();
            yield return new WaitForSeconds(0.2f); // 等待Boss行动完成
        }
        yield return null;
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
    /// 结束回合，移除所有单位的无敌状态
    /// </summary>
    public void EndTurn()
    {
        Debug.Log("BattleManager: 回合结束，移除无敌状态");
        var allUnits = gridManager.GetAllUnits();

        foreach (var unit in allUnits)
        {
            if (unit.HasState<InvincibleState>())
            {
                unit.RemoveState<InvincibleState>();
                Debug.Log($"BattleManager: 移除 {unit.unitData.unitName} 的无敌状态");
            }
        }

        // 继续处理其他回合结束逻辑，如计时、敌方回合等
    }

    /// <summary>
    /// 检查战斗结果并结束战斗
    /// </summary>
    private void CheckBattleOutcome()
    {
        var playerBoss = gridManager.GetBossUnit(Camp.Player);
        var enemyBoss = gridManager.GetBossUnit(Camp.Enemy);
        
        // 检查玩家 BOSS 的生命值
        if (playerBoss != null && playerBoss.currentHealth > 0)
        {
            playerBossAlive = true;
        }

        // 检查敌方 BOSS 的生命值
        if (enemyBoss != null && enemyBoss.currentHealth > 0)
        {
            enemyBossAlive = true;
        }
        
        if (playerBossAlive && enemyBossAlive)
        {
            // 戰鬥未結束，進行下一回合
            StartCoroutine(NextTurnRoutine());
            Debug.Log("BattleManager: 战斗未结束，进入下一回合！");
        }
        else if (!playerBossAlive)
        {
            // 玩家Boss阵亡，敌方胜利
            //GameManager.Instance.EndGame("You Lose!");
        }
        else
        {
            // 敌方Boss阵亡，玩家胜利
            //GameManager.Instance.EndGame("You Win!");
        }
    }
    
    /// <summary>
    /// 第一阶段：单位发动技能
    /// </summary>
    private IEnumerator ExecuteSkillActivationPhase()
    {
        Debug.Log("BattleManager: 第一阶段 - 单位发动技能");

        var allSunits = gridManager.GetAllUnits();
        foreach (var unit in allSunits)
        {
            if (unit != null)
            {
                unit.UseSkill();
            }
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

        yield return null;
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
            var skills = unit.unitData.mainSkillSO.actions;
            foreach (var action in skills)
            {
                if(action.Type == SkillType.Move)
                {
                    for (int i = 0; i < action.Value; i++)
                    {
                        if (unit.CanMoveForward())
                        {
                            yield return StartCoroutine(unit.MoveForward());
                        }
                        else
                        {
                            Debug.Log("BattleManager: 用户无法继续移动，动作被阻挡！");
                            break;
                        }
                    }
                }
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

        yield return null;
    }

    /// <summary>
    /// 第五阶段：准备下一回合
    /// </summary>
    private IEnumerator PrepareNextTurnPhase()
    {
        Debug.Log("BattleManager: 第五阶段 - 准备下一回合");

        // 更新单位的延迟、状态等
        var allUnits = gridManager.GetAllUnits();
        foreach (var unit in allUnits)
        {
            unit.PrepareForNextTurn();
        }

        // 更新建筑物的状态
        var allBuildings = gridManager.GetAllBuildings();
        foreach (var building in allBuildings)
        {
            building.PrepareForNextTurn();
        }

        yield return null;
    }
    
    /// <summary>
    /// 當玩家完成選擇後觸發的回調方法
    /// </summary>
    private void OnPlayerChoiceMade()
    {
        choiceMade = true;
    }
    
    /// <summary>
    /// 下一回合的流程協程
    /// </summary>
    /// <returns></returns>
    private IEnumerator NextTurnRoutine()
    {
        // 重置所有单位的回合标志
        var allUnits = gridManager.GetAllUnits();
        foreach (var unit in allUnits)
        {
            unit.ResetTurn();
        }
    
        // 重置所有建筑物的回合标志（如果适用）
        var allBuildings = gridManager.GetAllBuildings();
        foreach (var building in allBuildings)
        {
            building.ResetTurn(); // 需要在 BuildingController 中实现 ResetTurn 方法
        }

        // 减少所有单位的技能延迟
        Debug.Log("BattleManager: 减少所有单位的技能延迟");
        foreach (var unit in allUnits)
        {
            unit.ReduceSkillDelays();
        }

        // 减少所有建筑物的技能延迟（如果适用）
        foreach (var building in allBuildings)
        {
            building.ReduceSkillDelays(); // 需要在 BuildingController 中实现 ReduceSkillDelays 方法
        }
        
        //减少牌库中所有单位的技能延迟
        DeckManager.Instance.ReduceSkillDelaysAtStartOfTurn();
        
        // 可以添加回合開始前的邏輯，如準備階段

        // 觸發選擇面板讓玩家選擇增加卡牌
        yield return new WaitForSeconds(1f); // 延遲以確保 UI 更新順序

        choiceMade = false;
        DeckChoiceUI.Instance.ShowChoicePanel();

        // 等待玩家做出選擇
        while (!choiceMade)
        {
            yield return null;
        }

        // 進行下一回合的邏輯
        StartBattleSequence(); // 或其他相關方法
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
    
    private IEnumerator HandleDelays()
    {
        Debug.Log("BattleManager: 處理單位延遲...");

        // 獲取所有單位，包括場上和牌庫中的
        List<UnitController> allUnits = GetAllUnits();
        foreach (var unit in allUnits)
        {
            unit.ReduceDelay();
        }

        yield return null;
    }

    private List<UnitController> GetAllUnits()
    {
        List<UnitController> allUnits = new List<UnitController>();

        // 獲取場上單位
        allUnits.AddRange(gridManager.GetAllUnits());

        // 獲取牌庫中的單位（假設 DeckManager 有 GetAllDeckUnits 方法）
        if (DeckManager.Instance != null)
        {
            //allUnits.AddRange(DeckManager.Instance.GetAllDeckUnits());
        }
        else
        {
            Debug.LogError("BattleManager: DeckManager.Instance 未找到！");
        }

        return allUnits;
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

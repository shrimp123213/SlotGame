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
    public SlotMachineController slotMachine; // 转盘组件
    public SkillManager skillManager;         // 技能管理器
    public GridManager gridManager;           // 网格管理器
    public ConnectionManager connectionManager; // 连接管理器

    [Header("Battle Settings")]
    public float slotMachineSpinTime = 5f;    // 转盘旋转时间
    public float slotMachineSpinSpeed = 10f;  // 转盘旋转速度
    
    [Header("Enemy Buildings")]
    public List<EnemyBuildingInfo> enemyBuildings = new List<EnemyBuildingInfo>();

    [Header("Player Buildings")]
    public List<PlayerBuildingInfo> playerBuildings = new List<PlayerBuildingInfo>();
    
    private bool choiceMade = false;
    
    private bool playerBossAlive = false;
    private bool enemyBossAlive = false;

    private BossController playerBoss;
    private BossController enemyBoss;
    
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
        if (skillManager == null)
        {
            Debug.LogError("BattleManager: 未设置 SkillManager 组件！");
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
        GridManager.Instance.InitializeBuildings(playerBuildings, enemyBuildings);

        // 订阅 SlotMachine 的转动完成事件
        slotMachine.OnSpinCompleted += OnSlotMachineSpun;

        // 开始战斗流程
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
        // 處理延遲
        yield return StartCoroutine(HandleDelays());

        // 2.1 战斗画面 转盘
        yield return StartCoroutine(ExecuteSlotMachine());

        // 以下流程将由转盘完成后的回调继续
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
            building.ExecuteAction();
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
                yield return StartCoroutine(unit.UseMainSkillOrSupport());
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
                building.ExecuteAction();
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
        // 重置所有單位的回合標誌
        var allUnits = gridManager.GetAllUnits();
        foreach (var unit in allUnits)
        {
            unit.ResetTurn();
        }
        
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

}

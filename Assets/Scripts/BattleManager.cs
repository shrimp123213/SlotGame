using System.Collections;
using UnityEngine;

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

        // 订阅 SlotMachine 的转动完成事件
        slotMachine.OnSpinCompleted += OnSlotMachineSpun;

        // 开始战斗流程
        StartBattleSequence();
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
    /// <param name="selectedColumn">转盘选择的列</param>
    public void OnSlotMachineSpun(int selectedColumn)
    {
        Debug.Log($"BattleManager: 转盘选择的列为 {selectedColumn}");
        StartCoroutine(ContinueBattleAfterSlotMachine(selectedColumn));
    }

    /// <summary>
    /// 继续战斗流程协程
    /// </summary>
    /// <param name="selectedColumn">转盘选择的列</param>
    /// <returns></returns>
    private IEnumerator ContinueBattleAfterSlotMachine(int selectedColumn)
    {
        // 仅调用一次 WeightedDrawAndPlaceCards
        slotMachine.WeightedDrawAndPlaceCards(selectedColumn);

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
        Debug.Log("BattleManager: 执行所有防卫效果！");
        var buildings = gridManager.GetAllBuildings();
        foreach (var building in buildings)
        {
            building.ExecuteDefense();
        }
    }

    /// <summary>
    /// 2.3 战斗画面 波次1 我方建筑行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecutePlayerBuildingActions()
    {
        Debug.Log("BattleManager: 执行我方建筑的行动！");
        var buildings = gridManager.GetPlayerBuildings();
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
                unit.UseMainSkillOrSupport();
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
        var enemyPositions = gridManager.GetEnemyPositions();
        foreach (var position in enemyPositions)
        {
            var building = gridManager.GetBuildingAt(position);
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
            boss.ExecuteBossAbility();
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
    /// 检查战斗结果并结束战斗
    /// </summary>
    private void CheckBattleOutcome()
    {
        // 示例逻辑：检查是否有玩家或敌方单位存活
        bool playerAlive = gridManager.GetUnitsByCamp(Camp.Player).Count > 0 || gridManager.GetPlayerBuildings().Count > 0;
        bool enemyAlive = gridManager.GetUnitsByCamp(Camp.Enemy).Count > 0 || gridManager.GetAllBuildings().Count > 0;

        if (!playerAlive)
        {
            // 玩家全部阵亡，敌方胜利
            GameManager.Instance.EndGame("你输了！");
        }
        else if (!enemyAlive)
        {
            // 敌方全部阵亡，玩家胜利
            GameManager.Instance.EndGame("你赢了！");
        }
        else
        {
            // 战斗未结束，可以根据需要继续下一轮战斗
            Debug.Log("BattleManager: 战斗未结束，可以继续下一轮战斗！");
            // 例如，重新启动战斗流程或等待玩家操作
        }
    }
}

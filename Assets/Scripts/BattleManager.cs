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

    void Start()
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

        StartBattleSequence();
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

        // 2.2 战斗画面 防卫
        ExecuteDefenseEffects();

        // 2.3 战斗画面 波次1 我方建筑行动
        yield return StartCoroutine(ExecutePlayerBuildingActions());

        // 2.4 - 2.10 战斗画面 波次2-波次8 单位行动
        yield return StartCoroutine(ExecuteUnitActionsByWave());

        // 2.10 战斗画面 敌方部位行动
        yield return StartCoroutine(ExecuteEnemyPositionActions());

        // 2.12 战斗画面 波次9 敌方Boss行动
        yield return StartCoroutine(ExecuteBossAction());

        // 2.13 战斗画面 连线 COMBO
        ExecuteComboCalculation();

        // 战斗结束逻辑（可根据需要扩展）
        Debug.Log("战斗结束！");
    }

    /// <summary>
    /// 2.1 战斗画面 转盘
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteSlotMachine()
    {
        Debug.Log("开始转盘旋转！");
        slotMachine.StartSpinning();

        // 等待转盘旋转完成，SlotMachine 将通过回调通知 BattleManager
        // 因此在这里无需等待，可以直接返回
        yield return null;
    }

    /// <summary>
    /// SlotMachine旋转完成后的回调
    /// </summary>
    public void OnSlotMachineSpun()
    {
        // 继续战斗流程
        StartCoroutine(ContinueBattleAfterSlotMachine());
    }

    /// <summary>
    /// 继续战斗流程协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator ContinueBattleAfterSlotMachine()
    {
        // 2.2 战斗画面 防卫
        ExecuteDefenseEffects();

        // 2.3 战斗画面 波次1 我方建筑行动
        yield return StartCoroutine(ExecutePlayerBuildingActions());

        // 2.4 - 2.10 战斗画面 波次2-波次8 单位行动
        yield return StartCoroutine(ExecuteUnitActionsByWave());

        // 2.10 战斗画面 敌方部位行动
        yield return StartCoroutine(ExecuteEnemyPositionActions());

        // 2.12 战斗画面 波次9 敌方Boss行动
        yield return StartCoroutine(ExecuteBossAction());

        // 2.13 战斗画面 连线 COMBO
        ExecuteComboCalculation();

        // 战斗结束逻辑（可根据需要扩展）
        Debug.Log("战斗结束！");
    }

    /// <summary>
    /// 2.2 战斗画面 防卫
    /// </summary>
    private void ExecuteDefenseEffects()
    {
        Debug.Log("执行所有防卫效果！");
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
        Debug.Log("执行我方建筑的行动！");
        var buildings = gridManager.GetPlayerBuildings();
        foreach (var building in buildings)
        {
            building.ExecuteAction();
            yield return new WaitForSeconds(0.5f); // 每个建筑间隔执行
        }
    }

    /// <summary>
    /// 2.4 - 2.10 战斗画面 波次2-波次8 单位行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteUnitActionsByWave()
    {
        Debug.Log("开始执行单位行动波次！");
        // 假设有6列，从最左到最右
        for (int wave = 1; wave <= gridManager.columns; wave++)
        {
            Debug.Log($"执行波次{wave}的单位行动！");
            var units = gridManager.GetUnitsByColumn(wave);
            foreach (var unit in units)
            {
                if (unit != null && unit.gameObject.activeSelf)
                {
                    unit.UseMainSkillOrSupport();
                    yield return new WaitForSeconds(0.5f); // 每个单位间隔执行
                }
            }
            yield return new WaitForSeconds(1f); // 每个波次间隔
        }
    }

    /// <summary>
    /// 2.10 战斗画面 敌方部位行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteEnemyPositionActions()
    {
        Debug.Log("执行敌方部位的行动！");
        var enemyPositions = gridManager.GetEnemyPositions();
        foreach (var position in enemyPositions)
        {
            var building = gridManager.GetBuildingAt(position);
            if (building != null && building.gameObject.activeSelf)
            {
                building.ExecuteAction();
                yield return new WaitForSeconds(0.5f); // 每个部位间隔执行
            }
        }
        yield return null;
    }

    /// <summary>
    /// 2.12 战斗画面 波次9 敌方Boss行动
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteBossAction()
    {
        Debug.Log("执行敌方Boss的行动！");
        var boss = gridManager.GetBossUnit();
        if (boss != null && boss.gameObject.activeSelf)
        {
            boss.ExecuteBossAbility();
            yield return new WaitForSeconds(1f); // 等待Boss行动完成
        }
        yield return null;
    }

    /// <summary>
    /// 2.13 战斗画面 连线 COMBO
    /// </summary>
    private void ExecuteComboCalculation()
    {
        Debug.Log("计算连线 COMBO！");
        // 实现连线计算逻辑
        ComboCalculator.CalculateCombo(gridManager);
    }
}

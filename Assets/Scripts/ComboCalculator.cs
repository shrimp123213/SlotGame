using System.Collections.Generic;
using UnityEngine;

public static class ComboCalculator
{
    /// <summary>
    /// 计算连线 COMBO
    /// </summary>
    /// <param name="gridManager">GridManager 实例</param>
    public static void CalculateCombo(GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogError("ComboCalculator: GridManager 实例为空！");
            return;
        }

        int comboCount = 0;

        // 示例逻辑：统计所有连续的玩家单位组合，每个组合至少包含2个连续单位
        for (int row = 0; row < gridManager.rows; row++)
        {
            int consecutiveUnits = 0;
            for (int col = 1; col <= gridManager.columns; col++)
            {
                Vector3Int pos = new Vector3Int(col, row, 0);
                UnitController unit = gridManager.GetUnitAt(pos);
                if (unit != null && unit.unitData.camp == Camp.Player)
                {
                    consecutiveUnits++;
                }
                else
                {
                    if (consecutiveUnits >= 2)
                    {
                        comboCount++;
                    }
                    consecutiveUnits = 0;
                }
            }
            // 检查行结束时是否有未计数的组合
            if (consecutiveUnits >= 2)
            {
                comboCount++;
            }
        }

        //Debug.Log($"连线 COMBO 计数：{comboCount}");

        // 根据 comboCount 应用奖励或效果
        //ApplyComboEffects(comboCount, gridManager);
    }

    /// <summary>
    /// 根据连线数应用奖励或效果
    /// </summary>
    /// <param name="comboCount">连线数量</param>
    /// <param name="gridManager">GridManager 实例</param>
    private static void ApplyComboEffects(int comboCount, GridManager gridManager)
    {
        if (comboCount > 0)
        {
            // 获取所有玩家单位
            List<UnitController> playerUnits = gridManager.GetUnitsByCamp(Camp.Player);
            if (playerUnits == null || playerUnits.Count == 0)
            {
                Debug.LogWarning("ComboCalculator: 没有找到玩家单位来应用奖励！");
                return;
            }

            // 为每个玩家单位恢复生命值，根据 comboCount
            foreach (UnitController unit in playerUnits)
            {
                if (unit != null)
                {
                    unit.Heal(comboCount);
                }
            }

            Debug.Log($"应用了 {comboCount} 点生命值的奖励！");
        }
        else
        {
            Debug.Log("没有连线 COMBO，未应用任何奖励！");
        }
    }
}

// Scripts/LinkPattern.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 表示一个连线模式
/// </summary>
[System.Serializable]
public class LinkPattern
{
    public int Id;                          // 连线模式的唯一标识
    public List<Vector2Int> Positions;      // 连线模式包含的格子位置列表
    public bool IsUnlocked;                 // 是否已解锁

    /// <summary>
    /// 检查给定的棋盘和阵营是否匹配该模式
    /// </summary>
    /// <param name="gridCells">棋盘格子矩阵</param>
    /// <param name="isPlayer">要检查的阵营</param>
    /// <returns>是否匹配</returns>
    public bool Matches(GridCell[,] gridCells, bool isPlayer)
    {
        foreach (var pos in Positions)
        {
            if (pos.x < 0 || pos.x >= gridCells.GetLength(0) || pos.y < 0 || pos.y >= gridCells.GetLength(1))
                return false;

            GridCell cell = gridCells[pos.x, pos.y];
            if (cell == null || cell.OccupiedUnit == null || cell.OccupiedUnit.IsPlayerOwned != isPlayer)
            {
                return false;
            }
        }
        return true;
    }
}
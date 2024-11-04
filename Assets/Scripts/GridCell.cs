using UnityEngine;

/// <summary>
/// 表示棋盤上的一個格子
/// </summary>
public class GridCell
{
    /// <summary>
    /// 格子的座標位置
    /// </summary>
    public Vector2Int Position { get; private set; }

    /// <summary>
    /// 該格子上所佔據的單位，可能為null
    /// </summary>
    public Unit OccupiedUnit { get; set; }

    /// <summary>
    /// 構造函數，初始化格子的座標
    /// </summary>
    /// <param name="x">行號</param>
    /// <param name="y">列號</param>
    public GridCell(int x, int y)
    {
        Position = new Vector2Int(x, y);
        OccupiedUnit = null;
    }

    /// <summary>
    /// 檢查格子是否為空
    /// </summary>
    /// <returns>如果為空返回true，否則返回false</returns>
    public bool IsEmpty()
    {
        return OccupiedUnit == null;
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 表示一個連線模式
/// </summary>
public class LinkPattern
{
    /// <summary>
    /// 連線模式的ID，用於識別
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// 連線模式包含的格子座標列表
    /// </summary>
    public List<Vector2Int> Positions { get; private set; }

    /// <summary>
    /// 是否已解鎖該連線模式
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// 構造函數，從JSON數據初始化連線模式
    /// </summary>
    /// <param name="id">模式ID</param>
    /// <param name="patternData">模式數據</param>
    public LinkPattern(int id, List<string> patternData)
    {
        Id = id;
        Positions = new List<Vector2Int>();
        IsUnlocked = false; // 默認未解鎖

        // 解析模式數據，將"O"的位置存入Positions
        // 解析模式數據，將"O"的位置存入Positions
        for (int i = 0; i < patternData.Count; i++)
        {
            string row = patternData[i];
            for (int j = 0; j < row.Length; j++)
            {
                if (row[j] == 'O')
                {
                    Positions.Add(new Vector2Int(i, j));
                }
            }
        }
    }

    /// <summary>
    /// 檢查給定的棋盤和陣營是否匹配該模式
    /// </summary>
    /// <param name="gridCells">棋盤格子矩陣</param>
    /// <param name="isPlayer">要檢查的陣營</param>
    /// <returns>是否匹配</returns>
    public bool Matches(GridCell[,] gridCells, bool isPlayer)
    {
        foreach (var pos in Positions)
        {
            GridCell cell = gridCells[pos.x, pos.y];
            if (cell.OccupiedCharacter == null ||
                cell.OccupiedCharacter.IsPlayerOwned != isPlayer)
            {
                return false;
            }
        }
        return true;
    }
}
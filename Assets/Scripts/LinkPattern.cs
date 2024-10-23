using System.Collections.Generic;
using UnityEngine;

public class LinkPattern
{
    public int Id { get; private set; }
    public List<Vector2Int> Positions { get; private set; }
    public bool IsUnlocked { get; set; }

    public List<string> PatternData { get; private set; } // 原始的模式數據

    public LinkPattern(int id, List<string> patternData)
    {
        Id = id;
        Positions = new List<Vector2Int>();
        IsUnlocked = false; // 默認未解鎖

        PatternData = patternData; // 保存原始的模式數據

        int numRows = patternData.Count;
        int numCols = patternData[0].Length;

        // 調整循環順序，先遍歷列，再遍歷行
        for (int j = 0; j < numCols; j++)
        {
            for (int i = 0; i < numRows; i++)
            {
                if (patternData[i][j] == 'O')
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
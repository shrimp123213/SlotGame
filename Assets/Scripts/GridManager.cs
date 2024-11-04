// Scripts/GridManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 管理遊戲的棋盤格子和單位佈局
/// </summary>
public class GridManager : MonoBehaviour
{
    public int rows = 4;        // 行數
    public int columns = 6;     // 列數

    public Tilemap tilemap;                 // 參考 Tilemap，用於顯示棋盤
    public TileBase gridTile;               // 用於表示空格子的 Tile
    public TileBase playerTile;             // 用於表示玩家單位的 Tile
    public TileBase enemyTile;              // 用於表示敵方單位的 Tile

    public GridCell[,] gridCells;           // 棋盤格子矩陣

    private void Awake()
    {
        InitializeGrid(); // 初始化棋盤
    }

    /// <summary>
    /// 初始化棋盤格子
    /// </summary>
    public void InitializeGrid()
    {
        gridCells = new GridCell[rows, columns];

        // 清除 Tilemap 上的所有 Tile
        tilemap.ClearAllTiles();

        // 初始化每個格子
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                gridCells[i, j] = new GridCell(i, j);

                // 在 Tilemap 上放置空格子的 Tile
                Vector3Int tilePosition = new Vector3Int(j, -i, 0);
                tilemap.SetTile(tilePosition, gridTile);
            }
        }
    }

    /// <summary>
    /// 獲取指定位置的格子
    /// </summary>
    /// <param name="row">行號</param>
    /// <param name="column">列號</param>
    /// <returns>對應的 GridCell，如果超出範圍則返回 null</returns>
    public GridCell GetGridCell(int row, int column)
    {
        if (row >= 0 && row < rows && column >= 0 && column < columns)
        {
            return gridCells[row, column];
        }
        return null;
    }

    /// <summary>
    /// 獲取單位所在的格子
    /// </summary>
    /// <param name="unit">需要查找的單位</param>
    /// <returns>單位所在的 GridCell，如果找不到則返回 null</returns>
    public GridCell GetUnitCell(Unit unit)
    {
        foreach (var cell in gridCells)
        {
            if (cell.OccupiedUnit == unit)
            {
                return cell;
            }
        }
        return null;
    }

    /// <summary>
    /// 獲取指定格子前方的格子
    /// </summary>
    /// <param name="currentCell">當前格子</param>
    /// <param name="isPlayer">是否為玩家的單位</param>
    /// <returns>前方的 GridCell，如果超出範圍則返回 null</returns>
    public GridCell GetFrontCell(GridCell currentCell, bool isPlayer)
    {
        int row = currentCell.Position.x;
        int column = currentCell.Position.y;

        // 根據單位的陣營決定前進方向
        if (isPlayer)
        {
            column += 1; // 玩家單位向右移動
        }
        else
        {
            column -= 1; // 敵方單位向左移動
        }

        return GetGridCell(row, column);
    }

    /// <summary>
    /// 移動單位到新的格子
    /// </summary>
    /// <param name="unit">需要移動的單位</param>
    /// <param name="fromCell">當前格子</param>
    /// <param name="toCell">目標格子</param>
    public void MoveUnit(Unit unit, GridCell fromCell, GridCell toCell)
    {
        // 更新格子的 OccupiedUnit 屬性
        fromCell.OccupiedUnit = null;
        toCell.OccupiedUnit = unit;

        // 更新單位在 Tilemap 上的位置
        Vector3Int fromPosition = new Vector3Int(fromCell.Position.y, -fromCell.Position.x, 0);
        Vector3Int toPosition = new Vector3Int(toCell.Position.y, -toCell.Position.x, 0);

        // 將起始位置的 Tile 設置為空格子 Tile
        tilemap.SetTile(fromPosition, gridTile);

        // 根據單位的陣營，設置目標位置的 Tile
        if (unit.IsPlayerOwned)
        {
            tilemap.SetTile(toPosition, playerTile);
        }
        else
        {
            tilemap.SetTile(toPosition, enemyTile);
        }
    }

    /// <summary>
    /// 在指定的格子上放置單位
    /// </summary>
    /// <param name="unit">需要放置的單位</param>
    /// <param name="row">行號</param>
    /// <param name="column">列號</param>
    public void PlaceUnit(Unit unit, int row, int column)
    {
        GridCell cell = GetGridCell(row, column);
        if (cell != null && cell.IsEmpty())
        {
            cell.OccupiedUnit = unit;

            // 在 Tilemap 上設置單位的 Tile
            Vector3Int position = new Vector3Int(column, -row, 0);
            if (unit.IsPlayerOwned)
            {
                tilemap.SetTile(position, playerTile);
            }
            else
            {
                tilemap.SetTile(position, enemyTile);
            }
        }
        else
        {
            Debug.LogWarning($"無法在位置 ({row}, {column}) 放置單位，可能已被佔據或超出範圍。");
        }
    }

    /// <summary>
    /// 清除棋盤上的所有單位
    /// </summary>
    public void ClearUnits()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GridCell cell = gridCells[i, j];
                if (cell.OccupiedUnit != null)
                {
                    cell.OccupiedUnit = null;

                    // 在 Tilemap 上重置為空格子 Tile
                    Vector3Int position = new Vector3Int(j, -i, 0);
                    tilemap.SetTile(position, gridTile);
                }
            }
        }
    }

    // 其他與棋盤相關的方法，可以根據需要添加
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 管理遊戲場地的生成與狀態
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; } // 單例模式

    [Header("Tilemaps")]
    public Tilemap battleTilemap; // 戰鬥區域的Tilemap
    public Tilemap wallTilemap;   // 城牆區域的Tilemap

    [Header("Tiles")]
    public Tile battleTile;       // 戰鬥區域使用的Tile
    public Tile wallTile;         // 城牆區域使用的Tile

    [Header("Prefabs")]
    public GameObject unitPrefab;      // 單位的Prefab
    public GameObject buildingPrefab;  // 建築的Prefab

    [Header("Data")]
    public List<UnitData> playerUnits;    // 玩家單位的資料
    public List<UnitData> enemyUnits;     // 敵方單位的資料
    public List<BuildingData> buildings;  // 建築的資料

    [Header("Parent Objects")]
    public Transform unitsParent;      // 單位的父物件
    public Transform buildingsParent;  // 建築的父物件

    public int rows = 4;          // 行數
    public int columns = 6;       // 列數（戰鬥區域）

    private int totalColumns;     // 總列數 = 戰鬥區域列數 + 2（城牆）

    // 存儲單位和建築的位置
    private Dictionary<Vector3Int, UnitController> unitPositions = new Dictionary<Vector3Int, UnitController>();
    private Dictionary<Vector3Int, BuildingController> buildingPositions = new Dictionary<Vector3Int, BuildingController>();

    void Awake()
    {
        // 單例模式實現
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        totalColumns = columns + 2; // 加上兩側的城牆

        GenerateBattleArea();
        GenerateWallArea();

        // 測試生成單位和建築
        // 確保 `Units` 和 `Buildings` 父物件已經在場景中創建並設置
        if (playerUnits.Count > 0)
        {
            SpawnUnit(new Vector3Int(2, 1, 0), playerUnits[0]);
        }
        if (buildings.Count > 0)
        {
            SpawnBuilding(new Vector3Int(0, 1, 0), buildings[0]);
        }
    }

    /// <summary>
    /// 生成戰鬥區域
    /// </summary>
    void GenerateBattleArea()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 1; col <= columns; col++) // 列從1到6
            {
                Vector3Int tilePosition = new Vector3Int(col, row, 0);
                battleTilemap.SetTile(tilePosition, battleTile);
            }
        }
    }

    /// <summary>
    /// 生成城牆區域（最左和最右）
    /// </summary>
    void GenerateWallArea()
    {
        for (int row = 0; row < rows; row++)
        {
            // 最左邊一列（0）
            Vector3Int leftWallPos = new Vector3Int(0, row, 0);
            wallTilemap.SetTile(leftWallPos, wallTile);

            // 最右邊一列（7）
            Vector3Int rightWallPos = new Vector3Int(totalColumns - 1, row, 0);
            wallTilemap.SetTile(rightWallPos, wallTile);
        }
    }

    /// <summary>
    /// 生成單位
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="unitData">單位數據</param>
    public void SpawnUnit(Vector3Int position, UnitData unitData)
    {
        if (unitPositions.ContainsKey(position))
        {
            Debug.LogWarning($"位置 {position} 已經有單位存在！");
            return;
        }

        GameObject unitGO = Instantiate(unitPrefab);
        UnitController unit = unitGO.GetComponent<UnitController>();
        unit.unitData = unitData;
        unit.SetPosition(position);

        // 設置為 Units 父物件的子物件
        if (unitsParent != null)
        {
            unitGO.transform.SetParent(unitsParent);
        }

        unitPositions.Add(position, unit);
    }

    /// <summary>
    /// 生成建築
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="buildingData">建築數據</param>
    public void SpawnBuilding(Vector3Int position, BuildingData buildingData)
    {
        if (buildingPositions.ContainsKey(position))
        {
            Debug.LogWarning($"位置 {position} 已經有建築存在！");
            return;
        }

        GameObject buildingGO = Instantiate(buildingPrefab);
        BuildingController building = buildingGO.GetComponent<BuildingController>();
        building.buildingData = buildingData;
        building.SetPosition(position);

        // 設置為 Buildings 父物件的子物件
        if (buildingsParent != null)
        {
            buildingGO.transform.SetParent(buildingsParent);
        }

        buildingPositions.Add(position, building);
    }

    /// <summary>
    /// 判斷戰鬥區域上的格子是否有玩家或敵人角色
    /// </summary>
    /// <param name="position">格子的位置</param>
    /// <returns>是否有角色</returns>
    public bool HasUnitAt(Vector3Int position)
    {
        return unitPositions.ContainsKey(position);
    }

    /// <summary>
    /// 獲取指定位置的單位
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>UnitController 或 null</returns>
    public UnitController GetUnitAt(Vector3Int position)
    {
        if (unitPositions.ContainsKey(position))
        {
            return unitPositions[position];
        }
        return null;
    }

    /// <summary>
    /// 判斷城牆區域上的格子是否有建築
    /// </summary>
    /// <param name="position">格子的位置</param>
    /// <returns>是否有建築</returns>
    public bool HasBuildingAt(Vector3Int position)
    {
        return buildingPositions.ContainsKey(position);
    }

    /// <summary>
    /// 獲取格子中心的世界坐標
    /// </summary>
    /// <param name="gridPosition">格子坐標</param>
    /// <returns>世界坐標</returns>
    public Vector3 GetCellCenterWorld(Vector3Int gridPosition)
    {
        // 使用Tilemap的方法計算格子中心的世界坐標
        Vector3 cellWorldPosition = battleTilemap.GetCellCenterWorld(gridPosition);
        return cellWorldPosition;
    }

    /// <summary>
    /// 從指定位置移除單位
    /// </summary>
    /// <param name="position">格子位置</param>
    public void RemoveUnitAt(Vector3Int position)
    {
        if (unitPositions.ContainsKey(position))
        {
            unitPositions.Remove(position);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 管理游戏场地的生成与状态
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; } // 单例模式

    [Header("Tilemaps")]
    public Tilemap battleTilemap; // 战斗区域的Tilemap
    public Tilemap wallTilemap;   // 城墙区域的Tilemap

    [Header("Tiles")]
    public Tile battleTile;       // 战斗区域使用的Tile
    public Tile wallTile;         // 城墙区域使用的Tile

    [Header("Prefabs")]
    public GameObject unitPrefab;      // 单位的Prefab
    public GameObject buildingPrefab;  // 建筑的Prefab

    [Header("Data")]
    public List<UnitData> playerUnits;    // 玩家单位的数据
    public List<UnitData> enemyUnits;     // 敌方单位的数据
    public List<BuildingData> buildings;  // 建筑的数据

    [Header("Parent Objects")]
    public Transform unitsParent;      // 单位的父对象
    public Transform buildingsParent;  // 建筑的父对象

    public int rows = 4;          // 行数
    public int columns = 6;       // 列数（战斗区域）

    private int totalColumns;     // 总列数 = 战斗区域列数 + 2（城墙）

    // 存储单位和建筑的位置
    private Dictionary<Vector3Int, UnitController> unitPositions = new Dictionary<Vector3Int, UnitController>();
    private Dictionary<Vector3Int, BuildingController> buildingPositions = new Dictionary<Vector3Int, BuildingController>();

    void Awake()
    {
        // 单例模式实现
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
        totalColumns = columns + 2; // 加上两侧的城墙

        GenerateBattleArea();
        GenerateWallArea();

        // 生成玩家单位
        if (playerUnits.Count > 0)
        {
            SpawnUnit(new Vector3Int(2, 1, 0), playerUnits[0]);
        }

        // 生成建筑物
        if (buildings.Count > 0)
        {
            SpawnBuilding(new Vector3Int(0, 1, 0), buildings[0]); // 城墙
            SpawnBuilding(new Vector3Int(totalColumns - 1, 1, 0), buildings[1]); // 另一侧城墙或城镇
        }
    }

    /// <summary>
    /// 生成战斗区域
    /// </summary>
    void GenerateBattleArea()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 1; col <= columns; col++) // 列从1到6
            {
                Vector3Int tilePosition = new Vector3Int(col, row, 0);
                battleTilemap.SetTile(tilePosition, battleTile);
            }
        }
    }

    /// <summary>
    /// 生成城墙区域（最左和最右）
    /// </summary>
    void GenerateWallArea()
    {
        for (int row = 0; row < rows; row++)
        {
            // 最左边一列（0）
            Vector3Int leftWallPos = new Vector3Int(0, row, 0);
            wallTilemap.SetTile(leftWallPos, wallTile);

            // 最右边一列（columns + 1）
            Vector3Int rightWallPos = new Vector3Int(totalColumns - 1, row, 0);
            wallTilemap.SetTile(rightWallPos, wallTile);
        }
    }

    /// <summary>
    /// 生成单位
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="unitData">单位数据</param>
    public void SpawnUnit(Vector3Int position, UnitData unitData)
    {
        if (unitPositions.ContainsKey(position) || buildingPositions.ContainsKey(position))
        {
            Debug.LogWarning($"位置 {position} 已经有单位或建筑存在！");
            return;
        }

        GameObject unitGO = Instantiate(unitPrefab);
        UnitController unit = unitGO.GetComponent<UnitController>();
        unit.unitData = unitData;
        unit.SetPosition(position);

        // 设置为 Units 父对象的子对象
        if (unitsParent != null)
        {
            unitGO.transform.SetParent(unitsParent);
        }

        unitPositions.Add(position, unit);
    }

    /// <summary>
    /// 生成建筑
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="buildingData">建筑数据</param>
    public void SpawnBuilding(Vector3Int position, BuildingData buildingData)
    {
        if (buildingPositions.ContainsKey(position) || unitPositions.ContainsKey(position))
        {
            Debug.LogWarning($"位置 {position} 已经有建筑或单位存在！");
            return;
        }

        GameObject buildingGO = Instantiate(buildingPrefab);
        BuildingController building = buildingGO.GetComponent<BuildingController>();
        building.buildingData = buildingData;
        building.SetPosition(position);

        // 设置为 Buildings 父对象的子对象
        if (buildingsParent != null)
        {
            buildingGO.transform.SetParent(buildingsParent);
        }

        buildingPositions.Add(position, building);
    }

    /// <summary>
    /// 判断战斗区域上的格子是否有玩家或敌人角色
    /// </summary>
    /// <param name="position">格子的位置</param>
    /// <returns>是否有角色</returns>
    public bool HasUnitAt(Vector3Int position)
    {
        return unitPositions.ContainsKey(position);
    }

    /// <summary>
    /// 获取指定位置的单位
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
    /// 判断城墙区域上的格子是否有建筑
    /// </summary>
    /// <param name="position">格子的位置</param>
    /// <returns>是否有建筑</returns>
    public bool HasBuildingAt(Vector3Int position)
    {
        return buildingPositions.ContainsKey(position);
    }

    /// <summary>
    /// 获取指定位置的建筑
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>BuildingController 或 null</returns>
    public BuildingController GetBuildingAt(Vector3Int position)
    {
        if (buildingPositions.ContainsKey(position))
        {
            return buildingPositions[position];
        }
        return null;
    }

    /// <summary>
    /// 获取格子中心的世界坐标
    /// </summary>
    /// <param name="gridPosition">格子坐标</param>
    /// <returns>世界坐标</returns>
    public Vector3 GetCellCenterWorld(Vector3Int gridPosition)
    {
        // 使用Tilemap的方法计算格子中心的世界坐标
        Vector3 cellWorldPosition = battleTilemap.GetCellCenterWorld(gridPosition);
        return cellWorldPosition;
    }

    /// <summary>
    /// 判断一个位置是否在战斗区域内
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>是否在战斗区域内</returns>
    public bool IsWithinBattleArea(Vector3Int position)
    {
        return position.x >= 1 && position.x <= columns && position.y >= 0 && position.y < rows;
    }

    /// <summary>
    /// 从指定位置移除单位
    /// </summary>
    /// <param name="position">格子位置</param>
    public void RemoveUnitAt(Vector3Int position)
    {
        if (unitPositions.ContainsKey(position))
        {
            unitPositions.Remove(position);
        }
    }

    /// <summary>
    /// 从指定位置移除建筑
    /// </summary>
    /// <param name="position">格子位置</param>
    public void RemoveBuildingAt(Vector3Int position)
    {
        if (buildingPositions.ContainsKey(position))
        {
            buildingPositions.Remove(position);
        }
    }

    /// <summary>
    /// 获取同一行中最前面的友方单位
    /// </summary>
    /// <param name="unit">当前单位</param>
    /// <returns>前方的友方单位，若无则返回null</returns>
    public UnitController GetFrontUnitInRow(UnitController unit)
    {
        if (unit == null)
        {
            //Debug.LogError("传入的UnitController为null！");
            return null;
        }

        Vector3Int position = unit.gridPosition;
        int row = position.y;
        int frontColumn = unit.unitData.camp == Camp.Player ? position.x + 1 : position.x - 1;

        // 遍历前方的格子，找到第一个友方单位
        while ((unit.unitData.camp == Camp.Player && frontColumn <= columns) ||
               (unit.unitData.camp == Camp.Enemy && frontColumn >= 1))
        {
            Vector3Int currentPos = new Vector3Int(frontColumn, row, 0);
            UnitController frontUnit = GetUnitAt(currentPos);
            if (frontUnit != null && frontUnit.unitData.camp == unit.unitData.camp)
            {
                return frontUnit;
            }

            frontColumn += unit.unitData.camp == Camp.Player ? 1 : -1;
        }

        return null;
    }

}

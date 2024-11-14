using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 管理游戏场地的生成与状态
/// </summary>
public class GridManager : MonoBehaviour
{
    // 单例模式实现，确保全局唯一
    public static GridManager Instance { get; private set; }

    [Header("Tilemaps")]
    public Tilemap battleTilemap; // 战斗区域的Tilemap
    public Tilemap wallTilemap;   // 城墙区域的Tilemap

    [Header("Tiles")]
    public Tile battleTile;       // 战斗区域使用的Tile
    public Tile wallTile;         // 城墙区域使用的Tile

    [Header("Prefabs")]
    public GameObject unitPrefab;      // 单位的Prefab
    public GameObject bossPrefab;      // Boss的Prefab
    public GameObject buildingPrefab;  // 建筑的Prefab

    [Header("Parent Objects")]
    public Transform unitsParent;      // 单位的父对象
    public Transform buildingsParent;  // 建筑的父对象

    [Header("Grid Settings")]
    public int rows = 4;          // 行数
    public int columns = 6;       // 列数（战斗区域）

    private int totalColumns;     // 总列数 = 战斗区域列数 + 2（城墙）

    // 存储单位和建筑的位置
    private Dictionary<Vector3Int, ISkillUser> skillUsersPositions = new Dictionary<Vector3Int, ISkillUser>();

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
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        totalColumns = columns + 2; // 加上两侧的城墙

        GenerateBattleArea();
        GenerateWallArea();

        /*
        // 生成玩家单位（示例位置）
        if (playerUnits != null && playerUnits.Count > 0)
        {
            // 示例：在第1行第2列生成第一个玩家单位
            SpawnUnit(new Vector3Int(2, 1, 0), playerUnits[0]);
        }

        // 生成敌方单位（包括Boss，示例位置）
        if (enemyUnits != null && enemyUnits.Count > 0)
        {
            // 示例：在第1行第5列生成敌方Boss
            SpawnBoss(new Vector3Int(5, 1, 0), enemyUnits[0]);
        }

        // 生成建筑物（示例位置）
        if (buildings != null && buildings.Count > 0)
        {
            SpawnBuilding(new Vector3Int(0, 1, 0), buildings[0]); // 左侧城墙
            if (buildings.Count > 1)
            {
                SpawnBuilding(new Vector3Int(totalColumns - 1, 1, 0), buildings[1]); // 右侧城墙或城镇
            }
        }
        */
    }

    /// <summary>
    /// 生成战斗区域的Tile
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
    /// 生成城墙区域的Tile（最左和最右）
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
        if (skillUsersPositions.ContainsKey(position))
        {
            Debug.LogWarning($"GridManager: 位置 {position} 已经有单位或建筑存在！");
            return;
        }

        if (unitData == null)
        {
            Debug.LogError("GridManager: 尝试生成的 UnitData 为 null！");
            return;
        }

        // 直接实例化单位，无需检查 cardCount
        GameObject unitGO = Instantiate(unitPrefab);
        unitGO.name = unitData.unitName + "_" + position.x + "_" + position.y;
        UnitController unit = unitGO.GetComponent<UnitController>();
        if (unit == null)
        {
            Debug.LogError("GridManager: UnitPrefab 没有 UnitController 组件！");
            Destroy(unitGO);
            return;
        }

        unit.unitData = unitData;
        unit.SetPosition(position);

        // 设置为 Units 父对象的子对象
        if (unitsParent != null)
        {
            unitGO.transform.SetParent(unitsParent);
        }
        else
        {
            Debug.LogWarning("GridManager: unitsParent 未设置！");
        }

        skillUsersPositions.Add(position, unit);
        //Debug.Log($"GridManager: 在位置 {position} 生成单位 {unitData.unitName}");
    }


    /// <summary>
    /// 生成Boss单位
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="unitData">Boss单位数据</param>
    public void SpawnBoss(Vector3Int position, UnitData unitData)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            Debug.LogWarning($"位置 {position} 已经有单位或建筑存在！");
            return;
        }

        GameObject bossGO = Instantiate(bossPrefab);
        bossGO.name = unitData.unitName + "_" + position.x + "_" + position.y;
        BossController boss = bossGO.GetComponent<BossController>();
        if (boss == null)
        {
            Debug.LogError("GridManager: BossPrefab 没有 BossController 组件！");
            Destroy(bossGO);
            return;
        }

        boss.unitData = unitData;
        boss.SetPosition(position);

        // 设置为 Units 父对象的子对象
        if (unitsParent != null)
        {
            bossGO.transform.SetParent(unitsParent);
        }

        skillUsersPositions.Add(position, boss);
    }

    /// <summary>
    /// 生成建筑
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="buildingData">建筑数据</param>
    public void SpawnBuilding(Vector3Int position, BuildingData buildingData)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            Debug.LogWarning($"位置 {position} 已经有建筑或单位存在！");
            return;
        }

        GameObject buildingGO = Instantiate(buildingPrefab);
        buildingGO.name = buildingData.buildingName + "_" + position.x + "_" + position.y;
        BuildingController building = buildingGO.GetComponent<BuildingController>();
        if (building == null)
        {
            Debug.LogError("GridManager: BuildingPrefab 没有 BuildingController 组件！");
            Destroy(buildingGO);
            return;
        }

        building.buildingData = buildingData;
        building.SetPosition(position);

        // 设置为 Buildings 父对象的子对象
        if (buildingsParent != null)
        {
            buildingGO.transform.SetParent(buildingsParent);
        }

        skillUsersPositions.Add(position, building);
    }
    
    public void AddUnitAt(Vector3Int position, UnitController unit)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            Debug.LogWarning($"GridManager: 位置 {position} 已经有单位或建筑存在！");
            return;
        }

        skillUsersPositions.Add(position, unit);
        unit.SetPosition(position);
    }


    /// <summary>
    /// 判断战斗区域上的格子是否有玩家或敌人角色
    /// </summary>
    /// <param name="position">格子的位置</param>
    /// <returns>是否有角色</returns>
    public bool HasSkillUserAt(Vector3Int position)
    {
        return skillUsersPositions.ContainsKey(position);
    }

    /// <summary>
    /// 获取指定位置的技能用户（单位或建筑物）
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>ISkillUser 或 null</returns>
    public ISkillUser GetSkillUserAt(Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            return skillUsersPositions[position];
        }
        return null;
    }

    /// <summary>
    /// 获取指定位置的单位
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>UnitController 或 null</returns>
    public UnitController GetUnitAt(Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            return skillUsersPositions[position] as UnitController;
        }
        return null;
    }

    /// <summary>
    /// 获取指定位置的建筑
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>BuildingController 或 null</returns>
    public BuildingController GetBuildingAt(Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            return skillUsersPositions[position] as BuildingController;
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
    /// 从指定位置移除技能用户
    /// </summary>
    /// <param name="position">格子位置</param>
    public void RemoveSkillUserAt(Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            skillUsersPositions.Remove(position);
        }
    }

    /// <summary>
    /// 从指定位置移除单位
    /// </summary>
    /// <param name="position">格子位置</param>
    public void RemoveUnitAt(Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            ISkillUser user = skillUsersPositions[position];
            if (user is UnitController)
            {
                skillUsersPositions.Remove(position);
            }
            else
            {
                Debug.LogWarning($"尝试移除的位置 {position} 上的不是单位！");
            }
        }
    }

    /// <summary>
    /// 从指定位置移除建筑
    /// </summary>
    /// <param name="position">格子位置</param>
    public void RemoveBuildingAt(Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            ISkillUser user = skillUsersPositions[position];
            if (user is BuildingController)
            {
                skillUsersPositions.Remove(position);
            }
            else
            {
                Debug.LogWarning($"尝试移除的位置 {position} 上的不是建筑物！");
            }
        }
    }

    /// <summary>
    /// 移动单位从一个位置到另一个位置
    /// </summary>
    /// <param name="oldPosition">旧位置</param>
    /// <param name="newPosition">新位置</param>
    /// <returns>是否成功移动</returns>
    public bool MoveUnit(Vector3Int oldPosition, Vector3Int newPosition)
    {
        if (!skillUsersPositions.ContainsKey(oldPosition))
        {
            Debug.LogWarning($"位置 {oldPosition} 没有单位存在，无法移动！");
            return false;
        }

        if (skillUsersPositions.ContainsKey(newPosition))
        {
            Debug.LogWarning($"位置 {newPosition} 已经有单位或建筑存在，无法移动！");
            return false;
        }

        ISkillUser user = skillUsersPositions[oldPosition];

        if (!(user is UnitController unit))
        {
            Debug.LogWarning($"位置 {oldPosition} 上的技能用户不是单位，无法移动！");
            return false;
        }

        // 移动单位
        skillUsersPositions.Remove(oldPosition);
        skillUsersPositions.Add(newPosition, unit);
        unit.SetPosition(newPosition);

        return true;
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
            Debug.LogError("传入的UnitController为null！");
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
            ISkillUser frontSkillUser = GetSkillUserAt(currentPos);
            if (frontSkillUser is UnitController frontUnit && frontUnit.unitData.camp == unit.unitData.camp)
            {
                return frontUnit;
            }

            frontColumn += unit.unitData.camp == Camp.Player ? 1 : -1;
        }

        return null;
    }

    /// <summary>
    /// 获取所有玩家建筑
    /// </summary>
    /// <returns>玩家建筑列表</returns>
    public List<BuildingController> GetPlayerBuildings()
    {
        List<BuildingController> playerBuildings = new List<BuildingController>();
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is BuildingController building && building.buildingData.camp == Camp.Player)
            {
                playerBuildings.Add(building);
            }
        }
        return playerBuildings;
    }

    /// <summary>
    /// 获取所有建筑
    /// </summary>
    /// <returns>所有建筑列表</returns>
    public List<BuildingController> GetAllBuildings()
    {
        List<BuildingController> allBuildings = new List<BuildingController>();
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is BuildingController building)
            {
                allBuildings.Add(building);
            }
        }
        return allBuildings;
    }
    
    /// <summary>
    /// 获取所有單位
    /// </summary>
    /// <returns>所有單位列表</returns>
    public List<UnitController> GetAllUnits()
    {
        List<UnitController> allUnits = new List<UnitController>();
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is UnitController unit)
            {
                allUnits.Add(unit);
            }
        }
        return allUnits;
    }

    /// <summary>
    /// 获取指定波次（列）的单位
    /// </summary>
    /// <param name="column">列号（1-6）</param>
    /// <returns>单位列表</returns>
    public List<UnitController> GetUnitsByColumn(int column)
    {
        List<UnitController> units = new List<UnitController>();
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is UnitController unit && unit.unitData.camp == Camp.Player && unit.gridPosition.x == column)
            {
                units.Add(unit);
            }
        }
        return units;
    }

    /// <summary>
    /// 获取所有敌方建筑的位置
    /// </summary>
    /// <returns>敌方建筑的位置列表</returns>
    public List<Vector3Int> GetEnemyPositions()
    {
        List<Vector3Int> enemyPositions = new List<Vector3Int>();
        foreach (var kvp in skillUsersPositions)
        {
            if (kvp.Value is BuildingController building && building.buildingData.camp == Camp.Enemy)
            {
                enemyPositions.Add(kvp.Key);
            }
        }
        return enemyPositions;
    }

    /// <summary>
    /// 获取Boss单位
    /// </summary>
    /// <returns>BossController 或 null</returns>
    public BossController GetBossUnit()
    {
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is BossController boss)
            {
                return boss;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取战斗区域所有格子的位置
    /// </summary>
    /// <returns>格子位置列表</returns>
    public List<Vector3Int> GetBattleAreaPositions()
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 1; col <= columns; col++)
            {
                positions.Add(new Vector3Int(col, row, 0));
            }
        }
        return positions;
    }

    /// <summary>
    /// 获取指定阵营的所有单位
    /// </summary>
    /// <param name="camp">阵营</param>
    /// <returns>单位列表</returns>
    public List<UnitController> GetUnitsByCamp(Camp camp)
    {
        List<UnitController> units = new List<UnitController>();
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is UnitController unit && unit.unitData.camp == camp)
            {
                units.Add(unit);
            }
        }
        return units;
    }
}

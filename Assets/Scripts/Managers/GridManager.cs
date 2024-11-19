using System.Collections.Generic;
using System.Linq;
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
    
    [Header("Boss Data")]
    public BossData playerBossData; // 玩家 BOSS 的数据
    public BossData enemyBossData;  // 敌方 BOSS 的数据
    
    // 添加 BOSS 所在的列
    private int playerBossColumn; // 例如 -1
    private int enemyBossColumn;  // 例如 columns + 2


    // 存储单位和建筑的位置
    private Dictionary<Vector3Int, ISkillUser> skillUsersPositions = new Dictionary<Vector3Int, ISkillUser>();
    
    // 记录每一行是否可以攻击 Boss
    private Dictionary<int, bool> rowCanAttackBoss = new Dictionary<int, bool>();
    
    // 存储建筑物的位置和对应的控制器
    private Dictionary<Vector3Int, BuildingController> buildingPositions = new Dictionary<Vector3Int, BuildingController>();

    // 存储所有建筑物
    private List<BuildingController> allBuildings = new List<BuildingController>();

    // 分别存储玩家和敌方的建筑物
    private List<BuildingController> playerBuildings = new List<BuildingController>();
    private List<BuildingController> enemyBuildings = new List<BuildingController>();

    void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // 可选：保持在场景切换中不被销毁
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        totalColumns = columns + 2; // 原来的列数，包括建筑物的列

        playerBossColumn = -2;
        enemyBossColumn = columns + 3;

        GenerateBattleArea();
        GenerateWallArea();
        
        for (int row = 0; row < rows; row++)
        {
            rowCanAttackBoss[row] = false;
        }
    }
    
    /// <summary>
    /// 初始化建筑物
    /// </summary>
    /// <param name="playerBuildingInfos">玩家建筑物的信息列表</param>
    /// <param name="enemyBuildingInfos">敌方建筑物的信息列表</param>
    public void InitializeBuildings(List<PlayerBuildingInfo> playerBuildingInfos, List<EnemyBuildingInfo> enemyBuildingInfos)
    {
        // 初始化玩家建筑物
        foreach (var buildingInfo in playerBuildingInfos)
        {
            CreateBuilding(buildingInfo.buildingData, buildingInfo.position, Camp.Player);
        }

        // 初始化敌方建筑物
        foreach (var enemyBuildingInfo in enemyBuildingInfos)
        {
            CreateBuilding(enemyBuildingInfo.buildingData, enemyBuildingInfo.position, Camp.Enemy);
        }

        // 生成玩家 BOSS
        if (playerBossData != null)
        {
            Vector3Int bossPosition = new Vector3Int(playerBossColumn, 1, 0);
            SpawnBoss(playerBossData, bossPosition, Camp.Player);
        }

        // 生成敌方 BOSS
        if (enemyBossData != null)
        {
            Vector3Int bossPosition = new Vector3Int(enemyBossColumn, 1, 0);
            SpawnBoss(enemyBossData, bossPosition, Camp.Enemy);
        }
    }

    
    // 设置指定行是否可以攻击 Boss
    public void SetRowCanAttackBoss(int row, bool canAttack)
    {
        if (rowCanAttackBoss.ContainsKey(row))
        {
            rowCanAttackBoss[row] = canAttack;
        }
    }
    
    // 检查指定行是否可以攻击 Boss
    public bool CanRowAttackBoss(int row)
    {
        return rowCanAttackBoss.ContainsKey(row) && rowCanAttackBoss[row];
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
    public void SpawnUnit(Vector3Int position, UnitData unitData, bool isInjured)
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
        if (isInjured) unit.AddState<InjuredState>();

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
    /// <param name="bossData">Boss单位数据</param>
    /// <param name="camp">Boss陣營</param>
    public void SpawnBoss(BossData bossData, Vector3Int position, Camp camp)
    {
        if (bossData == null)
        {
            Debug.LogError("GridManager: BossData 为空，无法生成 BOSS！");
            return;
        }

        // 检查位置是否已被占用
        if (skillUsersPositions.ContainsKey(position))
        {
            Debug.LogWarning($"GridManager: 位置 {position} 已被占用，无法生成 BOSS！");
            return;
        }

        // 实例化 BOSS 预制件
        GameObject bossGO = Instantiate(bossPrefab);
        bossGO.name = bossData.bossName + "_" + position.x + "_" + position.y;

        // 获取 BossController 组件
        BossController bossController = bossGO.GetComponent<BossController>();
        if (bossController == null)
        {
            Debug.LogError("GridManager: BOSS 预制件缺少 BossController 组件！");
            Destroy(bossGO);
            return;
        }

        // 设置 BOSS 的数据
        bossController.bossData = bossData;
        bossController.bossData.camp = camp; // 设置阵营
        bossController.SetPosition(position);

        // 设置为 Units 父对象的子对象
        if (unitsParent != null)
        {
            bossGO.transform.SetParent(buildingsParent);
        }

        // 添加到 skillUsersPositions 字典
        skillUsersPositions.Add(position, bossController);
    }
    
    /// <summary>
    /// 获取指定阵营的 BOSS
    /// </summary>
    /// <param name="camp">阵营</param>
    /// <returns>BossController 或 null</returns>
    public BossController GetBossUnit(Camp camp)
    {
        foreach (var skillUser in skillUsersPositions.Values)
        {
            if (skillUser is BossController boss && boss.bossData.camp == camp)
            {
                return boss;
            }
        }
        return null;
    }
    
    public BossController GetBossUnitAt(Camp camp, Vector3Int position)
    {
        if (skillUsersPositions.ContainsKey(position))
        {
            ISkillUser skillUser = skillUsersPositions[position];
            if (skillUser is BossController boss && boss.bossData.camp == camp)
            {
                return boss;
            }
        }
        return null;
    }


    // 添加建筑物到指定位置
    public void AddBuildingAt(Vector3Int position, BuildingController building)
    {
        if (!buildingPositions.ContainsKey(position))
        {
            buildingPositions[position] = building;
        }
    }

    // 根据阵营获取建筑物
    public List<BuildingController> GetBuildingsByCamp(Camp camp)
    {
        return buildingPositions.Values.Where(b => b.buildingData.camp == camp).ToList();
    }
    
    /// <summary>
    /// 创建建筑物并添加到管理器
    /// </summary>
    /// <param name="buildingData">建筑物的数据</param>
    /// <param name="position">放置的位置</param>
    /// <param name="camp">阵营</param>
    public void CreateBuilding(BuildingData buildingData, Vector3Int position, Camp camp)
    {
        if (buildingData == null)
        {
            Debug.LogWarning("GridManager: BuildingData 为空，无法创建建筑物。");
            return;
        }

        if (buildingPositions.ContainsKey(position) || skillUsersPositions.ContainsKey(position))
        {
            Debug.LogWarning($"GridManager: 位置 {position} 已被其他建筑物或单位占用。");
            return;
        }

        // 实例化建筑物预制件
        GameObject buildingGO = Instantiate(buildingPrefab);
        buildingGO.name = buildingData.buildingName + "_" + position.x + "_" + position.y;

        // 获取 BuildingController 组件
        BuildingController buildingController = buildingGO.GetComponent<BuildingController>();
        if (buildingController == null)
        {
            Debug.LogError("GridManager: 建筑物预制件缺少 BuildingController 组件！");
            Destroy(buildingGO);
            return;
        }

        // 设置建筑物的数据
        buildingController.buildingData = buildingData;
        buildingController.buildingData.camp = camp; // 设置阵营
        buildingController.SetPosition(position);

        // 设置为 Buildings 父对象的子对象
        if (buildingsParent != null)
        {
            buildingGO.transform.SetParent(buildingsParent);
        }

        // 添加到管理器
        buildingPositions.Add(position, buildingController);
        allBuildings.Add(buildingController);

        if (camp == Camp.Player)
        {
            playerBuildings.Add(buildingController);
        }
        else if (camp == Camp.Enemy)
        {
            enemyBuildings.Add(buildingController);
        }

        // 添加到 skillUsersPositions 字典
        skillUsersPositions.Add(position, buildingController);
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
    /// 获取指定位置的建筑物
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <returns>BuildingController 或 null</returns>
    public BuildingController GetBuildingAt(Vector3Int position)
    {
        if (skillUsersPositions.TryGetValue(position, out ISkillUser skillUser) && skillUser is BuildingController building)
        {
            return building;
        }
        return null;
    }

    public bool IsWithinAccessibleArea(Vector3Int position)
    {
        return position.x >= playerBossColumn && position.x <= enemyBossColumn && position.y >= 0 && position.y < rows;
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
    public bool IsWithinBattleArea(Vector3Int position, bool includeBuildings = false)
    {
        if (includeBuildings)
        {
            return position.x >= 0 && position.x <= columns + 1 && position.y >= 0 && position.y < rows;
        }
        else
        {
            return position.x >= 1 && position.x <= columns && position.y >= 0 && position.y < rows;
        }
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
    /// 获取指定行中敌方建筑的位置
    /// </summary>
    /// <param name="row">行索引</param>
    /// <param name="camp">己方阵营</param>
    /// <returns>敌方建筑的位置</returns>
    public Vector3Int GetEnemyBuildingPositionInRow(int row, Camp camp)
    {
        int buildingColumn;

        // 假设敌方建筑位于棋盘的最右侧（对于玩家）或最左侧（对于敌人）
        if (camp == Camp.Player)
        {
            // 玩家阵营，敌方建筑在右侧
            buildingColumn = columns + 1; // 超出棋盘范围，用于放置建筑的位置
        }
        else
        {
            // 敌人阵营，敌方建筑在左侧
            buildingColumn = 0; // 或 -1，取决于您的棋盘设计
        }

        // 建筑的格子位置
        Vector3Int buildingPosition = new Vector3Int(buildingColumn, row, 0);

        return buildingPosition;
    }


    /// <summary>
    /// 获取所有玩家建筑
    /// </summary>
    /// <returns>玩家建筑列表</returns>
    public List<BuildingController> GetPlayerBuildings()
    {
        return skillUsersPositions.Values
            .OfType<BuildingController>()
            .Where(b => b.buildingData.camp == Camp.Player)
            .ToList();
    }
    
    /// <summary>
    /// 获取所有敌方建筑
    /// </summary>
    /// <returns>敌方建筑列表</returns>
    public List<BuildingController> GetEnemyBuildings()
    {
        return skillUsersPositions.Values
            .OfType<BuildingController>()
            .Where(b => b.buildingData.camp == Camp.Enemy)
            .ToList();
    }

    /// <summary>
    /// 获取所有建筑物
    /// </summary>
    /// <returns>所有建筑物列表</returns>
    public List<BuildingController> GetAllBuildings()
    {
        return skillUsersPositions.Values.OfType<BuildingController>().ToList();
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
            if (skillUser is UnitController unit && unit.gridPosition.x == column)
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

[System.Serializable]
public class EnemyBuildingInfo
{
    public BuildingData buildingData;   // 建筑物的数据
    public Vector3Int position;         // 放置的位置（网格坐标）
}

[System.Serializable]
public class PlayerBuildingInfo
{
    public BuildingData buildingData;   // 建筑物的数据
    public Vector3Int position;         // 放置的位置（网格坐标）
}

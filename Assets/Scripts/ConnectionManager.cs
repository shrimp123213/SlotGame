using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理连接方式的读取与检查
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [Header("Grid Manager")]
    public GridManager gridManager; // 引用GridManager

    [Header("Connection Patterns")]
    public TextAsset connectionPatternsJson; // 连接方式的JSON文件（从Resources中加载）

    private List<ConnectionPattern> connectionPatterns = new List<ConnectionPattern>();

    private UnitController[,] gridCells; // 棋盘格子矩阵

    void Start()
    {
        LoadConnections();
        InitializeGridCells();
    }

    /// <summary>
    /// 加载连接方式
    /// </summary>
    void LoadConnections()
    {
        if (connectionPatternsJson == null)
        {
            Debug.LogError("未指定连接方式的JSON文件！");
            return;
        }

        string json = connectionPatternsJson.text;

        ConnectionList connectionList = JsonUtility.FromJson<ConnectionList>(json);
        connectionPatterns = connectionList.connections;

        // 初始化 positions
        foreach (var pattern in connectionPatterns)
        {
            pattern.InitializePositions();
        }

        Debug.Log($"已加载 {connectionPatterns.Count} 种连接方式");
    }

    /// <summary>
    /// 初始化 GridCells
    /// </summary>
    void InitializeGridCells()
    {
        int rows = gridManager.rows;
        int cols = gridManager.columns;
        gridCells = new UnitController[rows, cols];

        // 遍历 GridManager 中的单位，填充 gridCells
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Vector3Int pos = new Vector3Int(col + 1, row, 0); // 列从1开始
                if (gridManager.HasUnitAt(pos))
                {
                    gridCells[row, col] = gridManager.GetUnitAt(pos);
                }
                else
                {
                    gridCells[row, col] = null;
                }
            }
        }
    }

    /// <summary>
    /// 检查并触发连接效果
    /// </summary>
    public void CheckConnections()
    {
        // 每次检查前更新 gridCells
        InitializeGridCells();

        bool anyConnectionTriggered = false;

        // 分别检查玩家和敌人的连接
        bool playerLinked = CheckLinksForSide(Camp.Player);
        bool enemyLinked = CheckLinksForSide(Camp.Enemy);

        if (playerLinked)
        {
            Debug.Log("玩家连接成功！");
            anyConnectionTriggered = true;
        }

        if (enemyLinked)
        {
            Debug.Log("敌人连接成功！");
            anyConnectionTriggered = true;
        }

        if (!anyConnectionTriggered)
        {
            Debug.Log("没有触发任何连接");
        }
    }

    /// <summary>
    /// 检查特定阵营的连接
    /// </summary>
    /// <param name="camp">要检查的阵营</param>
    /// <returns>是否有连接</returns>
    private bool CheckLinksForSide(Camp camp)
    {
        bool hasLink = false;

        // 遍历已解锁的连接模式
        foreach (var pattern in connectionPatterns)
        {
            if (!pattern.isUnlocked)
                continue;

            // 检查棋盘是否匹配该模式
            if (MatchesPatternForCamp(pattern, camp))
            {
                hasLink = true;

                // 绘制连接
                DrawConnection(pattern.positions);

                // 触发连接效果
                TriggerLinkEffect(pattern, camp);

                // 如果只需要检查一个连接，可以在这里break
                // break;
            }
        }

        return hasLink;
    }

    /// <summary>
    /// 检查给定的模式是否匹配指定阵营
    /// </summary>
    /// <param name="pattern">连接模式</param>
    /// <param name="camp">阵营</param>
    /// <returns>是否匹配</returns>
    private bool MatchesPatternForCamp(ConnectionPattern pattern, Camp camp)
    {
        HashSet<int> columnsWithUnit = new HashSet<int>();

        foreach (var pos in pattern.positions)
        {
            int row = pos.x;
            int col = pos.y;

            // 检查边界
            if (row < 0 || row >= gridManager.rows || col < 0 || col >= gridManager.columns)
            {
                Debug.LogWarning($"位置 ({row}, {col}) 超出棋盘范围！");
                return false;
            }

            UnitController unit = gridCells[row, col];

            if (unit == null || unit.unitData.camp != camp)
            {
                return false;
            }

            if (columnsWithUnit.Contains(col))
            {
                // 同一列中有多个单位，返回 false
                return false;
            }
            else
            {
                columnsWithUnit.Add(col);
            }
        }

        return true;
    }

    /// <summary>
    /// 绘制连接
    /// </summary>
    /// <param name="positions">连接包含的格子位置列表</param>
    private void DrawConnection(List<Vector2Int> positions)
    {
        Color lineColor = new Color(Random.value, Random.value, Random.value);

        List<Vector3> linePositions = new List<Vector3>();
        foreach (var pos in positions)
        {
            int row = pos.x;
            int col = pos.y;
            Vector3Int gridPos = new Vector3Int(col + 1, row, 0); // 列从1开始
            Vector3 worldPos = gridManager.GetCellCenterWorld(gridPos);
            linePositions.Add(worldPos);
        }

        if (linePositions.Count < 2)
            return;

        // 创建一个新的 GameObject 用于 LineRenderer
        GameObject lineGO = new GameObject("ConnectionLine");
        LineRenderer lr = lineGO.AddComponent<LineRenderer>();
        lr.positionCount = linePositions.Count;
        lr.SetPositions(linePositions.ToArray());
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor; // 可以根据需要调整颜色
        lr.endColor = lineColor;
        lr.sortingOrder = 10;

        // 使用 Debug.DrawLine 从左到右连接
        for (int i = 0; i < linePositions.Count - 1; i++)
        {
            Debug.DrawLine(linePositions[i], linePositions[i + 1], lineColor, 5f);
        }

        // 可选：自动删除连接线条
        Destroy(lineGO, 5f); // 5 秒后删除
    }

    /// <summary>
    /// 触发连接效果，例如造成伤害等
    /// </summary>
    /// <param name="pattern">匹配的连接模式</param>
    /// <param name="camp">阵营</param>
    private void TriggerLinkEffect(ConnectionPattern pattern, Camp camp)
    {
        int linkedUnitCount = pattern.positions.Count;

        // 在这里实现连接效果，例如对敌方城镇造成伤害
        if (camp == Camp.Player)
        {
            Debug.Log($"玩家触发了连接模式 {pattern.name}，连接单位数量：{linkedUnitCount}，对敌人造成伤害！");
            // 实现对敌方的具体伤害逻辑，例如调用敌方城镇的伤害方法
            // Example:
            // EnemyBuildingController enemyBuilding = FindObjectOfType<EnemyBuildingController>();
            // if (enemyBuilding != null)
            // {
            //     enemyBuilding.TakeDamage(linkedUnitCount * damagePerUnit);
            // }
        }
        else if (camp == Camp.Enemy)
        {
            Debug.Log($"敌人触发了连接模式 {pattern.name}，连接单位数量：{linkedUnitCount}，对玩家造成伤害！");
            // 实现对玩家的具体伤害逻辑，例如调用玩家城镇的伤害方法
            // Example:
            // PlayerBuildingController playerBuilding = FindObjectOfType<PlayerBuildingController>();
            // if (playerBuilding != null)
            // {
            //     playerBuilding.TakeDamage(linkedUnitCount * damagePerUnit);
            // }
        }
    }
}

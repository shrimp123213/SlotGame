using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理連線方式的讀取與檢查
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [Header("Grid Manager")]
    public GridManager gridManager; // 引用 GridManager

    [Header("Connection Patterns")]
    public TextAsset connectionPatternsJson; // 連線方式的 JSON 文件

    private List<ConnectionPattern> connectionPatterns = new List<ConnectionPattern>();

    private UnitController[,] gridCells; // 棋盤格子矩陣，索引順序為 [col, row]

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("ConnectionManager: 未指定 GridManager 引用！");
            return;
        }

        LoadConnections();
        InitializeGridCells();
    }

    /// <summary>
    /// 加載連線方式
    /// </summary>
    void LoadConnections()
    {
        if (connectionPatternsJson == null)
        {
            Debug.LogError("ConnectionManager: 未指定連線方式的 JSON 文件！");
            return;
        }

        string json = connectionPatternsJson.text;

        ConnectionList connectionList = JsonUtility.FromJson<ConnectionList>(json);
        if (connectionList == null || connectionList.connections == null)
        {
            Debug.LogError("ConnectionManager: 無法解析連線方式的 JSON 文件！");
            return;
        }

        connectionPatterns = connectionList.connections;

        // 初始化每個連線模式的位置
        foreach (var pattern in connectionPatterns)
        {
            pattern.InitializePositions();
        }

        Debug.Log($"ConnectionManager: 已加載 {connectionPatterns.Count} 種連線方式");
    }

    /// <summary>
    /// 初始化 GridCells，填充棋盤上的單位
    /// </summary>
    void InitializeGridCells()
    {
        int rows = gridManager.rows;
        int cols = gridManager.columns;
        gridCells = new UnitController[cols, rows]; // 索引順序為 [col, row]

        // 遍歷 GridManager 中的單位，填充 gridCells
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                Vector3Int pos = new Vector3Int(col + 1, row, 0); // 列從 1 開始，行從 0 開始
                if (gridManager.HasSkillUserAt(pos))
                {
                    gridCells[col, row] = gridManager.GetUnitAt(pos);
                }
                else
                {
                    gridCells[col, row] = null;
                }
            }
        }

        Debug.Log("ConnectionManager: GridCells 已初始化");
    }

    /// <summary>
    /// 檢查並觸發連線效果
    /// </summary>
    public void CheckConnections()
    {
        if (gridManager == null)
        {
            Debug.LogError("ConnectionManager: GridManager 實例為空！");
            return;
        }

        // 每次檢查前更新 gridCells
        InitializeGridCells();

        bool anyConnectionTriggered = false;

        // 分別檢查玩家和敵人的連線
        bool playerLinked = CheckLinksForSide(Camp.Player);
        bool enemyLinked = CheckLinksForSide(Camp.Enemy);

        if (playerLinked)
        {
            Debug.Log("ConnectionManager: 玩家連線成功！");
            anyConnectionTriggered = true;
        }

        if (enemyLinked)
        {
            Debug.Log("ConnectionManager: 敵人連線成功！");
            anyConnectionTriggered = true;
        }

        if (!anyConnectionTriggered)
        {
            Debug.Log("ConnectionManager: 沒有觸發任何連線");
        }
    }

    /// <summary>
    /// 檢查特定陣營的連線
    /// </summary>
    /// <param name="camp">要檢查的陣營</param>
    /// <returns>是否有連線</returns>
    private bool CheckLinksForSide(Camp camp)
    {
        bool hasLink = false;

        // 遍歷所有連線模式
        foreach (var pattern in connectionPatterns)
        {
            if (!pattern.isUnlocked)
                continue;

            // 檢查棋盤是否匹配該模式
            if (MatchesPatternForCamp(pattern, camp))
            {
                hasLink = true;

                // 繪製連線
                DrawConnection(pattern.positions);

                // 觸發連線效果
                TriggerLinkEffect(pattern, camp);

                // 如果只需要檢查一個連線，可以在此處添加 break
                // break;
            }
        }

        return hasLink;
    }

    /// <summary>
    /// 檢查給定的模式是否匹配指定陣營
    /// </summary>
    /// <param name="pattern">連線模式</param>
    /// <param name="camp">陣營</param>
    /// <returns>是否匹配</returns>
    private bool MatchesPatternForCamp(ConnectionPattern pattern, Camp camp)
    {
        if (pattern.positions == null || pattern.positions.Count == 0)
        {
            Debug.LogWarning($"ConnectionManager: 連線模式 {pattern.name} 的位置列表為空！");
            return false;
        }

        HashSet<int> columnsWithUnit = new HashSet<int>();

        foreach (var pos in pattern.positions)
        {
            int row = pos.x;     // x 表示行，假設從 0 開始
            int col = pos.y; // y 表示列，從 1 調整為 0 索引

            // 調試輸出
            //Debug.Log($"檢查位置：行 {row}, 列 {col + 1} (pos.x: {pos.x}, pos.y: {pos.y})");

            // 檢查邊界
            if (col < 0 || col >= gridManager.columns || row < 0 || row >= gridManager.rows)
            {
                Debug.LogWarning($"ConnectionManager: 位置 ({row}, {col + 1}) 超出棋盤範圍！");
                return false;
            }

            // 訪問 gridCells[col, row]
            UnitController unit = gridCells[col, row];

            if (unit == null)
            {
                //Debug.Log($"ConnectionManager: 位置 ({row}, {col + 1}) 無單位");
                return false;
            }

            if (unit.unitData.camp != camp)
            {
                //Debug.Log($"ConnectionManager: 位置 ({row}, {col + 1}) 的單位不屬於 {camp} 陣營");
                return false;
            }

            if (columnsWithUnit.Contains(col))
            {
                // 同一列中有多個單位，返回 false
                Debug.Log($"ConnectionManager: 列 {col + 1} 中已有單位，無法重複");
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
    /// 繪製連線
    /// </summary>
    /// <param name="positions">連線包含的格子位置列表</param>
    private void DrawConnection(List<Vector2Int> positions)
    {
        if (positions == null || positions.Count < 2)
            return;

        Color lineColor = new Color(Random.value, Random.value, Random.value);

        List<Vector3> linePositions = new List<Vector3>();
        foreach (var pos in positions)
        {
            int row = pos.x; // x 表示行，假設從 0 開始
            int col = pos.y; // y 表示列，從 1 開始
            Vector3Int gridPos = new Vector3Int(col + 1, row, 0);
            Vector3 worldPos = gridManager.GetCellCenterWorld(gridPos);
            linePositions.Add(worldPos);
        }

        if (linePositions.Count < 2)
            return;

        // 創建一個新的 GameObject 用於 LineRenderer
        GameObject lineGO = new GameObject("ConnectionLine");
        LineRenderer lr = lineGO.AddComponent<LineRenderer>();
        lr.positionCount = linePositions.Count;
        lr.SetPositions(linePositions.ToArray());
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor; // 可以根據需要調整顏色
        lr.endColor = lineColor;
        lr.sortingLayerName = "LR"; // 可以根據需要調整排序層
        lr.sortingOrder = 10;

        // 使用 Debug.DrawLine 從左到右連接
        for (int i = 0; i < linePositions.Count - 1; i++)
        {
            Debug.DrawLine(linePositions[i], linePositions[i + 1], lineColor, 5f);
        }

        // 可選：自動刪除連線線條
        Destroy(lineGO, 5f); // 5 秒後刪除
    }

    /// <summary>
    /// 觸發連線效果，例如造成傷害等
    /// </summary>
    /// <param name="pattern">匹配的連線模式</param>
    /// <param name="camp">陣營</param>
    private void TriggerLinkEffect(ConnectionPattern pattern, Camp camp)
    {
        if (pattern == null)
        {
            Debug.LogWarning("ConnectionManager: 匹配的連線模式為空！");
            return;
        }

        int linkedUnitCount = pattern.positions.Count;

        // 定義每個單位造成的傷害值
        int damagePerUnit = 1; // 可以根據需要調整

        // 在此實現連線效果，例如對敵方城鎮造成傷害
        if (camp == Camp.Player)
        {
            Debug.Log($"ConnectionManager: 玩家觸發了連線模式 {pattern.name}，連線單位數量：{linkedUnitCount}，對敵人造成傷害！");
            // 實現對敵方的具體傷害邏輯，例如調用敵方城鎮的傷害方法
            // 例如：
            /*
            EnemyBuildingController enemyBuilding = FindObjectOfType<EnemyBuildingController>();
            if (enemyBuilding != null)
            {
                enemyBuilding.TakeDamage(linkedUnitCount * damagePerUnit);
            }
            else
            {
                Debug.LogWarning("ConnectionManager: 未找到 EnemyBuildingController！");
            }
            */
        }
        /*else if (camp == Camp.Enemy)
        {
            Debug.Log($"ConnectionManager: 敵人觸發了連線模式 {pattern.name}，連線單位數量：{linkedUnitCount}，對玩家造成傷害！");
            // 實現對玩家的具體傷害邏輯，例如調用玩家城鎮的傷害方法
            // 例如：
            /*
            PlayerBuildingController playerBuilding = FindObjectOfType<PlayerBuildingController>();
            if (playerBuilding != null)
            {
                playerBuilding.TakeDamage(linkedUnitCount * damagePerUnit);
            }
            else
            {
                Debug.LogWarning("ConnectionManager: 未找到 PlayerBuildingController！");
            }
            
        }*/
    }
}

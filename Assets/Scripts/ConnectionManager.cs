using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理連線方式的讀取與檢查
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [Header("Grid Manager")]
    public GridManager gridManager; // 引用GridManager

    [Header("Connection Patterns")]
    public TextAsset connectionPatternsJson; // 連線方式的JSON文件（從Resources中加載）

    private List<ConnectionPattern> connectionPatterns = new List<ConnectionPattern>();

    void Start()
    {
        LoadConnections();
    }

    /// <summary>
    /// 加載連線方式
    /// </summary>
    void LoadConnections()
    {
        if (connectionPatternsJson == null)
        {
            Debug.LogError("未指定連線方式的JSON文件！");
            return;
        }

        string json = connectionPatternsJson.text;

        ConnectionList connectionList = JsonUtility.FromJson<ConnectionList>(json);
        connectionPatterns = connectionList.connections;

        Debug.Log($"已加載 {connectionPatterns.Count} 種連線方式");
    }

    /// <summary>
    /// 檢查並觸發連線效果
    /// </summary>
    public void CheckConnections()
    {
        bool anyConnectionTriggered = false;

        foreach (var pattern in connectionPatterns)
        {
            if (!pattern.isUnlocked)
            {
                // 跳過未解鎖的連線方式
                continue;
            }

            if (IsPatternMatch(pattern.pattern, pattern.name))
            {
                // 觸發連線效果，例如對敵方城鎮造成傷害
                Debug.Log($"觸發連線: {pattern.name}");
                // 根據連線方式定義具體的效果
                ApplyConnectionEffect(pattern);

                // 繪製連線
                DrawConnection(pattern.pattern);

                anyConnectionTriggered = true;
            }
        }

        if (!anyConnectionTriggered)
        {
            Debug.Log("沒有觸發任何連線");
        }
    }

    /// <summary>
    /// 檢查當前場地是否符合連線模式
    /// </summary>
    /// <param name="pattern">連線模式</param>
    /// <param name="patternName">連線模式名稱，用於調試</param>
    /// <returns>是否匹配</returns>
    bool IsPatternMatch(List<string> pattern, string patternName)
    {
        // 存儲每列的陣營
        List<Camp> campsPerColumn = new List<Camp>();

        // 遍歷戰鬥區域的格子，對比連線模式
        for (int row = 0; row < gridManager.rows; row++)
        {
            for (int col = 0; col < gridManager.columns; col++)
            {
                Vector3Int pos = new Vector3Int(col + 1, row, 0); // 戰鬥區域列從1開始
                bool hasUnit = gridManager.HasUnitAt(pos);
                char expected = pattern[row][col];

                // 調試信息
                Debug.Log($"[{patternName}] 檢查位置 ({pos.x}, {pos.y}): hasUnit = {hasUnit}, expected = {expected}");

                if ((hasUnit && expected != 'O') || (!hasUnit && expected != 'X'))
                {
                    return false; // 不匹配
                }

                if (expected == 'O' && hasUnit)
                {
                    // 獲取單位的陣營
                    UnitController unit = gridManager.GetUnitAt(pos);
                    Camp unitCamp = unit.unitData.camp;

                    campsPerColumn.Add(unitCamp);
                }
            }
        }

        // 檢查連線中是否有相同陣營的多個單位
        Dictionary<Camp, int> campCount = new Dictionary<Camp, int>();
        foreach (Camp camp in campsPerColumn)
        {
            if (campCount.ContainsKey(camp))
            {
                campCount[camp]++;
                if (campCount[camp] > 1)
                {
                    Debug.Log($"[{patternName}] 連線中有同一陣營的多個單位: {camp}");
                    return false; // 同一陣營的單位在連線中出現多次
                }
            }
            else
            {
                campCount[camp] = 1;
            }
        }

        return true; // 完全匹配，且無同陣營重複
    }

    /// <summary>
    /// 根據連線方式應用效果
    /// </summary>
    /// <param name="pattern">連線模式</param>
    void ApplyConnectionEffect(ConnectionPattern pattern)
    {
        // 根據具體的連線方式定義效果
        // 例如，對敵方城鎮造成固定傷害
        List<UnitController> connectedUnits = new List<UnitController>();

        for (int row = 0; row < gridManager.rows; row++)
        {
            for (int col = 0; col < gridManager.columns; col++)
            {
                if (pattern.pattern[row][col] == 'O')
                {
                    Vector3Int pos = new Vector3Int(col + 1, row, 0);
                    if (gridManager.HasUnitAt(pos))
                    {
                        UnitController unit = gridManager.GetUnitAt(pos);
                        connectedUnits.Add(unit);
                    }
                }
            }
        }

        foreach (var unit in connectedUnits)
        {
            if (unit.unitData.camp == Camp.Enemy)
            {
                // 對敵方單位造成傷害
                int damage = 5;
                Debug.Log($"對敵方單位 {unit.unitData.unitName} 造成 {damage} 點傷害");
                unit.TakeDamage(damage);
            }
            else if (unit.unitData.camp == Camp.Player)
            {
                // 對玩家單位造成傷害（根據需求）
                int damage = 3;
                Debug.Log($"對玩家單位 {unit.unitData.unitName} 造成 {damage} 點傷害");
                unit.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// 繪製連線
    /// </summary>
    /// <param name="pattern">連線模式</param>
    void DrawConnection(List<string> pattern)
    {
        // 使用 LineRenderer 繪製連線
        List<Vector3> linePositions = new List<Vector3>();

        for (int col = 0; col < gridManager.columns; col++)
        {
            for (int row = 0; row < gridManager.rows; row++)
            {
                if (pattern[row][col] == 'O')
                {
                    Vector3Int gridPos = new Vector3Int(col + 1, row, 0); // 戰鬥區域列從1開始
                    Vector3 worldPos = gridManager.GetCellCenterWorld(gridPos);
                    linePositions.Add(worldPos);
                    break; // 每列只有一個 "O"
                }
            }
        }

        if (linePositions.Count < 2)
        {
            Debug.LogWarning("連線點數不足，無法繪製連線");
            return;
        }

        // 創建一個新的 GameObject 用於 LineRenderer
        GameObject lineGO = new GameObject("ConnectionLine");
        LineRenderer lr = lineGO.AddComponent<LineRenderer>();
        lr.positionCount = linePositions.Count;
        lr.SetPositions(linePositions.ToArray());
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.green; // 可以根據需要調整顏色
        lr.endColor = Color.green;
        lr.sortingOrder = 10;

        // Optional: 設置連線的層級和渲染順序

        // Optional: 自動刪除連線線條
        Destroy(lineGO, 5f); // 5 秒後刪除
    }
}

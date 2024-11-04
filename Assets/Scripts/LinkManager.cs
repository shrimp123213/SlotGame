// Scripts/LinkManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// 管理连线模式和连线检测的类
/// </summary>
public class LinkManager : MonoBehaviour
{
    public GridManager gridManager;                    // 棋盘管理器的引用
    public List<LinkPattern> allLinkPatterns;          // 所有连线模式
    public List<LinkPattern> unlockedLinkPatterns;     // 已解锁的连线模式
    public int selectedPatternIndex = 0;               // 当前选择的连线模式索引

    private void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }

        LoadLinkPatternsFromJSON();    // 从 JSON 文件加载连线模式
        UnlockLinkPatterns();          // 解锁连线模式
    }

    /// <summary>
    /// 从 JSON 文件中加载连线模式
    /// </summary>
    private void LoadLinkPatternsFromJSON()
    {
        allLinkPatterns = new List<LinkPattern>();

        // 假设 JSON 文件存储在 StreamingAssets 文件夹下
        string filePath = Path.Combine(Application.streamingAssetsPath, "LinkPatterns.json");

        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);

            // 解析 JSON 数据
            LinkPatternList patternList = JsonUtility.FromJson<LinkPatternList>(jsonData);

            if (patternList != null && patternList.patterns != null)
            {
                allLinkPatterns = patternList.patterns;
            }
            else
            {
                Debug.LogError("无法解析连线模式的 JSON 数据。");
            }
        }
        else
        {
            Debug.LogError($"未找到连线模式的 JSON 文件：{filePath}");
        }
    }

    /// <summary>
    /// 用于解析 JSON 数据的辅助类
    /// </summary>
    [System.Serializable]
    private class LinkPatternList
    {
        public List<LinkPattern> patterns;
    }

    /// <summary>
    /// 解锁连线模式
    /// </summary>
    private void UnlockLinkPatterns()
    {
        // 这里简单地将所有模式都解锁，您可以根据游戏逻辑调整
        unlockedLinkPatterns = new List<LinkPattern>(allLinkPatterns);
        foreach (var pattern in unlockedLinkPatterns)
        {
            pattern.IsUnlocked = true;
        }
    }

    // ... 其余代码保持不变 ...

    // 清除棋盘上的所有单位
    public void ClearUnits()
    {
        gridManager.ClearUnits();
    }

    // 随机放置单位，供测试使用
    public void PlaceUnitsRandomly()
    {
        // 示例：随机放置玩家和敌方单位
        for (int i = 0; i < gridManager.rows; i++)
        {
            for (int j = 0; j < gridManager.columns; j++)
            {
                // 随机决定是否放置单位
                if (Random.value > 0.7f)
                {
                    UnitData unitData = GetRandomUnitData();
                    if (unitData != null)
                    {
                        Unit newUnit = new GameObject().AddComponent<Unit>();
                        bool isPlayerOwned = Random.value > 0.5f;
                        newUnit.InitializeUnit(unitData, isPlayerOwned);
                        gridManager.PlaceUnit(newUnit, i, j);
                    }
                }
            }
        }
    }

    // 获取随机的单位数据，供测试使用
    private UnitData GetRandomUnitData()
    {
        // 从 Resources/Units 文件夹中加载所有单位数据
        UnitData[] allUnits = Resources.LoadAll<UnitData>("Units");
        if (allUnits.Length > 0)
        {
            int index = Random.Range(0, allUnits.Length);
            return allUnits[index];
        }
        return null;
    }

    // 检查连线
    public void CheckForLinks()
    {
        // 分别检查玩家和敌人
        bool playerLinked = CheckLinksForSide(true);
        bool enemyLinked = CheckLinksForSide(false);

        if (playerLinked)
        {
            Debug.Log("玩家连线成功！");
        }

        if (enemyLinked)
        {
            Debug.Log("敌人连线成功！");
        }
    }

    // 检查特定阵营的连线
    private bool CheckLinksForSide(bool isPlayer)
    {
        bool hasLink = false;

        foreach (var pattern in unlockedLinkPatterns)
        {
            if (!pattern.IsUnlocked)
                continue;

            if (pattern.Matches(gridManager.gridCells, isPlayer))
            {
                hasLink = true;

                // 标记已连线的单位
                foreach (var pos in pattern.Positions)
                {
                    GridCell cell = gridManager.GetGridCell(pos.x, pos.y);
                    if (cell != null && cell.OccupiedUnit != null)
                    {
                        cell.OccupiedUnit.IsLinked = true;
                    }
                }

                // 绘制连线
                DrawLinkLine(pattern.Positions);

                // 触发连线效果
                TriggerLinkEffect(pattern, isPlayer);
            }
        }

        return hasLink;
    }

    // 绘制连线
    private void DrawLinkLine(List<Vector2Int> positions)
    {
        // 实现绘制连线的逻辑，可以使用 Gizmos 或其他方式
        // 这里为了简化，仅在控制台输出信息
        string lineInfo = "连线路径：";
        foreach (var pos in positions)
        {
            lineInfo += $"({pos.x}, {pos.y}) -> ";
        }
        Debug.Log(lineInfo.TrimEnd('-', '>'));
    }

    // 触发连线效果
    private void TriggerLinkEffect(LinkPattern pattern, bool isPlayer)
    {
        int linkedUnitCount = pattern.Positions.Count;

        if (isPlayer)
        {
            Debug.Log($"玩家触发了连线模式 {pattern.Id}，连线单位数量：{linkedUnitCount}，对敌人造成伤害！");
            // 实现对敌方造成伤害的逻辑
        }
        else
        {
            Debug.Log($"敌人触发了连线模式 {pattern.Id}，连线单位数量：{linkedUnitCount}，对玩家造成伤害！");
            // 实现对玩家造成伤害的逻辑
        }
    }
}

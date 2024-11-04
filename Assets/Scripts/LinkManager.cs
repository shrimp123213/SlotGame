using System.Collections.Generic;
using UnityEngine;

public class LinkManager : MonoBehaviour
{
    private GridManager gridManager;

    // 所有連線模式
    public List<LinkPattern> allLinkPatterns;

    // 已解鎖的連線模式
    private List<LinkPattern> unlockedLinkPatterns;

    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        LoadLinkPatterns();
        UnlockLinkPatterns();

        // 在開始時檢查連線
        CheckForLinks();
    }

    /// <summary>
    /// 從資源中加載連線模式
    /// </summary>
    private void LoadLinkPatterns()
    {
        // 需要實現從資源中加載連線模式的邏輯
        // 可以從JSON文件中加載
    }

    /// <summary>
    /// 解鎖連線模式
    /// </summary>
    private void UnlockLinkPatterns()
    {
        // 假設所有模式都已解鎖
        unlockedLinkPatterns = new List<LinkPattern>(allLinkPatterns);
    }

    /// <summary>
    /// 檢查連線
    /// </summary>
    public void CheckForLinks()
    {
        // 分別檢查玩家和敵人
        bool playerLinked = CheckLinksForSide(true);
        bool enemyLinked = CheckLinksForSide(false);

        if (playerLinked)
        {
            Debug.Log("玩家連線成功！");
        }

        if (enemyLinked)
        {
            Debug.Log("敵人連線成功！");
        }
    }

    /// <summary>
    /// 檢查特定陣營的連線
    /// </summary>
    /// <param name="isPlayer">是否為玩家</param>
    /// <returns>是否有連線</returns>
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

                // 標記已連線的單位
                foreach (var pos in pattern.Positions)
                {
                    GridCell cell = gridManager.GetGridCell(pos.x, pos.y);
                    if (cell != null && cell.OccupiedUnit != null)
                    {
                        cell.OccupiedUnit.IsLinked = true;
                    }
                }

                // 繪製連線
                DrawLinkLine(pattern.Positions);

                // 觸發連線效果
                TriggerLinkEffect(pattern, isPlayer);
            }
        }

        return hasLink;
    }

    /// <summary>
    /// 繪製連線
    /// </summary>
    /// <param name="positions">連線包含的格子位置列表</param>
    private void DrawLinkLine(List<Vector2Int> positions)
    {
        // 實現繪製連線的邏輯
    }

    /// <summary>
    /// 觸發連線效果
    /// </summary>
    /// <param name="pattern">匹配的連線模式</param>
    /// <param name="isPlayer">是否為玩家</param>
    private void TriggerLinkEffect(LinkPattern pattern, bool isPlayer)
    {
        int linkedUnitCount = pattern.Positions.Count;

        if (isPlayer)
        {
            Debug.Log($"玩家觸發了連線模式 {pattern.Id}，連線單位數量：{linkedUnitCount}，對敵人造成傷害！");
        }
        else
        {
            Debug.Log($"敵人觸發了連線模式 {pattern.Id}，連線單位數量：{linkedUnitCount}，對玩家造成傷害！");
        }
    }
}

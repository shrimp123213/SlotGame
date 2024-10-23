using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// 管理連線檢測和處理的類別
/// </summary>
public class LinkManager : MonoBehaviour
{
    private GridCell[,] gridCells; // 棋盤格子矩陣
    private int rows = 4;          // 行數
    private int cols = 6;          // 列數

    private List<LinkPattern> unlockedLinkPatterns; // 已解鎖的連線模式

    private void Start()
    {
        InitializeGrid();          // 初始化棋盤
        LoadLinkPatterns();        // 從JSON文件中讀取連線模式
        UnlockSomeLinkPatterns();  // 解鎖部分連線模式（可根據需要調整）
        PlaceCharactersRandomly(); // 隨機放置角色
        CheckForLinks();           // 檢查連線
    }

    /// <summary>
    /// 初始化棋盤格子
    /// </summary>
    private void InitializeGrid()
    {
        gridCells = new GridCell[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                gridCells[i, j] = new GridCell(i, j);
            }
        }
    }

    /// <summary>
    /// 從JSON文件中讀取連線模式
    /// </summary>
    private void LoadLinkPatterns()
    {
        unlockedLinkPatterns = new List<LinkPattern>();

        // 讀取JSON文件
        TextAsset jsonText = Resources.Load<TextAsset>("LinkPatterns");
        if (jsonText != null)
        {
            // 解析JSON數據
            LinkPatternsWrapper wrapper = JsonUtility.FromJson<LinkPatternsWrapper>(jsonText.text);

            foreach (var patternData in wrapper.patterns)
            {
                // 將JSON數據轉換為LinkPattern對象
                LinkPattern pattern = new LinkPattern(patternData.id, patternData.pattern);
                unlockedLinkPatterns.Add(pattern);
            }
        }
        else
        {
            Debug.LogError("無法載入 LinkPatterns.json");
        }
    }

    /// <summary>
    /// 解鎖部分連線模式
    /// </summary>
    private void UnlockSomeLinkPatterns()
    {
        // 這裡假設解鎖了所有模式，可以根據實際需求進行調整
        foreach (var pattern in unlockedLinkPatterns)
        {
            pattern.IsUnlocked = true;
        }
    }

    /// <summary>
    /// 隨機放置角色在棋盤上
    /// </summary>
    private void PlaceCharactersRandomly()
    {
        // 這裡簡單地隨機放置玩家和敵人角色
        // 實際應從牌組中抽取
        int totalCards = 24; // 總共放置的卡片數量
        int placedCards = 0;

        while (placedCards < totalCards)
        {
            int i = Random.Range(0, rows);
            int j = Random.Range(0, cols);

            if (gridCells[i, j].OccupiedCharacter == null)
            {
                int rand = Random.Range(0, 2);
                if (rand == 0)
                {
                    gridCells[i, j].OccupiedCharacter = new PlayerCharacter();
                }
                else
                {
                    gridCells[i, j].OccupiedCharacter = new EnemyCharacter();
                }
                placedCards++;
            }
        }
    }

    /// <summary>
    /// 檢查棋盤上的連線
    /// </summary>
    private void CheckForLinks()
    {
        // 分別檢查玩家和敵人的連線
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

        // 遍歷已解鎖的連線模式
        foreach (var pattern in unlockedLinkPatterns)
        {
            if (!pattern.IsUnlocked)
                continue;

            // 檢查棋盤是否匹配該連線模式
            if (pattern.Matches(gridCells, isPlayer))
            {
                hasLink = true;

                // 標記已連線的角色
                foreach (var pos in pattern.Positions)
                {
                    GridCell cell = gridCells[pos.x, pos.y];
                    if (cell.OccupiedCharacter != null)
                    {
                        cell.OccupiedCharacter.IsLinked = true;
                    }
                }

                // 繪製連線
                DrawLinkLine(pattern.Positions);

                // 觸發連線效果
                TriggerLinkEffect(pattern, isPlayer);

                // 如果只需要檢查一個連線，可以在這裡break
            }
        }

        return hasLink;
    }

    /// <summary>
    /// 繪製連線，使用隨機顏色
    /// </summary>
    /// <param name="positions">連線包含的格子位置列表</param>
    private void DrawLinkLine(List<Vector2Int> positions)
    {
        Color lineColor = new Color(Random.value, Random.value, Random.value);

        for (int i = 0; i < positions.Count - 1; i++)
        {
            Vector3 startPos = new Vector3(positions[i].y, -positions[i].x, 0);
            Vector3 endPos = new Vector3(positions[i + 1].y, -positions[i + 1].x, 0);

            Debug.DrawLine(startPos, endPos, lineColor, 5f);
        }
    }

    /// <summary>
    /// 觸發連線效果，例如造成傷害等
    /// </summary>
    /// <param name="pattern">匹配的連線模式</param>
    /// <param name="isPlayer">是否為玩家</param>
    private void TriggerLinkEffect(LinkPattern pattern, bool isPlayer)
    {
        // 在這裡實現連線效果，例如對敵方城鎮造成傷害
        if (isPlayer)
        {
            Debug.Log($"玩家觸發了連線模式 {pattern.Id}，對敵人造成傷害！");
        }
        else
        {
            Debug.Log($"敵人觸發了連線模式 {pattern.Id}，對玩家造成傷害！");
        }
    }

    /// <summary>
    /// 用於包裝連線模式列表，方便反序列化
    /// </summary>
    [System.Serializable]
    public class LinkPatternsWrapper
    {
        public List<LinkPatternData> patterns;
    }

    /// <summary>
    /// 連線模式的數據結構，用於反序列化JSON
    /// </summary>
    [System.Serializable]
    public class LinkPatternData
    {
        public int id;
        public List<string> pattern;
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 用於生成連線模式的工具腳本
/// </summary>
public class LinkPatternGenerator : EditorWindow
{
    private int rows = 4; // 棋盤的行數
    private int cols = 6; // 棋盤的列數

    [MenuItem("Tools/Generate Link Patterns")]
    public static void ShowWindow()
    {
        GetWindow<LinkPatternGenerator>("連線模式生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("生成連線模式", EditorStyles.boldLabel);

        rows = EditorGUILayout.IntField("行數 (Rows):", rows);
        cols = EditorGUILayout.IntField("列數 (Columns):", cols);

        if (GUILayout.Button("生成連線模式並保存到JSON"))
        {
            GenerateAndSaveLinkPatterns();
        }
    }

    /// <summary>
    /// 生成所有可能的連線模式並保存到JSON文件
    /// </summary>
    private void GenerateAndSaveLinkPatterns()
    {
        List<LinkPatternData> allPatterns = new List<LinkPatternData>();
        int patternId = 0;

        // 遍歷所有可能的起始點（第一列的每個格子）
        for (int startRow = 0; startRow < rows; startRow++)
        {
            // 對於每個起始點，生成從左到右的連線
            GeneratePatternsFromPosition(startRow, 0, new List<Vector2Int>(), ref allPatterns, ref patternId);
        }

        // 將模式列表轉換為JSON格式並保存
        string json = JsonUtility.ToJson(new LinkPatternsWrapper { patterns = allPatterns }, true);
        string path = Path.Combine(Application.dataPath, "Resources/LinkPatterns.json");
        File.WriteAllText(path, json);

        Debug.Log("連線模式已生成並保存到 " + path);
    }

    /// <summary>
    /// 遞迴生成從指定位置開始的連線模式
    /// </summary>
    private void GeneratePatternsFromPosition(int row, int col, List<Vector2Int> currentPath, ref List<LinkPatternData> allPatterns, ref int patternId)
    {
        // 將當前位置加入路徑
        currentPath.Add(new Vector2Int(row, col));

        // 如果已到達最後一列，保存當前模式
        if (col == cols - 1)
        {
            // 創建棋盤表示
            string[,] grid = new string[rows, cols];
            // 初始化為"X"
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    grid[i, j] = "X";

            // 將路徑上的位置標記為"O"
            foreach (var pos in currentPath)
            {
                grid[pos.x, pos.y] = "O";
            }

            // 將棋盤轉換為可序列化的結構
            LinkPatternData patternData = new LinkPatternData
            {
                id = patternId++,
                pattern = new List<string>()
            };

            for (int i = 0; i < rows; i++)
            {
                string rowString = "";
                for (int j = 0; j < cols; j++)
                {
                    rowString += grid[i, j];
                }
                patternData.pattern.Add(rowString);
            }

            allPatterns.Add(patternData);
        }
        else
        {
            // 獲取下一列中相鄰的格子（不同列且相鄰）
            for (int nextRow = 0; nextRow < rows; nextRow++)
            {
                if (Mathf.Abs(nextRow - row) <= 1)
                {
                    GeneratePatternsFromPosition(nextRow, col + 1, new List<Vector2Int>(currentPath), ref allPatterns, ref patternId);
                }
            }
        }
    }

    /// <summary>
    /// 用於包裝連線模式列表，方便序列化
    /// </summary>
    [System.Serializable]
    public class LinkPatternsWrapper
    {
        public List<LinkPatternData> patterns;
    }

    /// <summary>
    /// 連線模式的數據結構，用於序列化為JSON
    /// </summary>
    [System.Serializable]
    public class LinkPatternData
    {
        public int id;
        public List<string> pattern;
    }
}

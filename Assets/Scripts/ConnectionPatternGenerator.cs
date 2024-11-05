using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 自动生成所有可能的连接方式，并保存到JSON文件
/// </summary>
public class ConnectionPatternGenerator : MonoBehaviour
{
    public int rows = 4;      // 行数
    public int columns = 6;   // 列数

    public string outputFileName = "ConnectionPatterns.json"; // 输出文件名，保存在 Assets/Resources 文件夹下

    [ContextMenu("生成连接方式并保存到JSON")]
    public void GenerateAndSavePatterns()
    {
        List<ConnectionPattern> generatedPatterns = GenerateAllConnectionPatterns();

        // 将连接方式列表包装为 ConnectionList 对象
        ConnectionList connectionList = new ConnectionList
        {
            connections = generatedPatterns
        };

        // 序列化为JSON字符串
        string json = JsonUtility.ToJson(connectionList, true);

        // 确保输出目录存在
        string outputDir = Application.dataPath + "/Resources";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // 输出文件路径
        string outputPath = Path.Combine(outputDir, outputFileName);

        // 将JSON字符串写入文件
        File.WriteAllText(outputPath, json);

        Debug.Log($"连接方式已生成并保存到 {outputPath}");
    }

    /// <summary>
    /// 生成所有可能的连接方式
    /// </summary>
    List<ConnectionPattern> GenerateAllConnectionPatterns()
    {
        List<ConnectionPattern> generatedPatterns = new List<ConnectionPattern>();

        // 对每一行的每一个格子进行遍历，生成从第一列到第六列的连接
        for (int startRow = 0; startRow < rows; startRow++)
        {
            List<int> currentPath = new List<int>();
            currentPath.Add(startRow);
            GeneratePatternsRecursive(1, currentPath, generatedPatterns);
        }

        Debug.Log($"共生成了 {generatedPatterns.Count} 种连接方式");

        return generatedPatterns;
    }

    /// <summary>
    /// 递归生成连接方式
    /// </summary>
    /// <param name="currentColumn">当前列</param>
    /// <param name="currentPath">当前路径</param>
    /// <param name="patterns">存储生成的连接方式</param>
    void GeneratePatternsRecursive(int currentColumn, List<int> currentPath, List<ConnectionPattern> patterns)
    {
        if (currentColumn >= columns)
        {
            // 已经到达最后一列，保存当前的连接方式
            List<string> pattern = CreatePatternFromPath(currentPath);
            ConnectionPattern connectionPattern = new ConnectionPattern
            {
                name = $"连接方式 {patterns.Count + 1}",
                pattern = pattern,
                isUnlocked = false // 预设为未解锁
            };
            connectionPattern.InitializePositions();
            patterns.Add(connectionPattern);
            return;
        }

        // 获取当前行
        int currentRow = currentPath[currentPath.Count - 1];

        // 获取下一列可能的行（相邻或对角相邻）
        List<int> nextRows = GetAdjacentRows(currentRow);

        foreach (int nextRow in nextRows)
        {
            List<int> newPath = new List<int>(currentPath);
            newPath.Add(nextRow);
            GeneratePatternsRecursive(currentColumn + 1, newPath, patterns);
        }
    }

    /// <summary>
    /// 获取相邻或对角相邻的行索引
    /// </summary>
    /// <param name="currentRow">当前行索引</param>
    /// <returns>相邻行的列表</returns>
    List<int> GetAdjacentRows(int currentRow)
    {
        List<int> adjacentRows = new List<int>();

        // 上一行
        if (currentRow - 1 >= 0)
        {
            adjacentRows.Add(currentRow - 1);
        }

        // 当前行
        adjacentRows.Add(currentRow);

        // 下一行
        if (currentRow + 1 < rows)
        {
            adjacentRows.Add(currentRow + 1);
        }

        return adjacentRows;
    }

    /// <summary>
    /// 根据路径创建连接模式
    /// </summary>
    /// <param name="path">路径（每列的行索引）</param>
    /// <returns>连接模式的列表，每个字符串代表一行的连接状态</returns>
    List<string> CreatePatternFromPath(List<int> path)
    {
        // 初始化模式列表，所有位置设为 "X"
        List<string> pattern = new List<string>();
        for (int row = 0; row < rows; row++)
        {
            string rowPattern = "";
            for (int col = 0; col < columns; col++)
            {
                rowPattern += "X";
            }
            pattern.Add(rowPattern);
        }

        // 根据路径设置 "O"
        for (int col = 0; col < path.Count; col++)
        {
            int row = path[col];
            char[] rowChars = pattern[row].ToCharArray();
            rowChars[col] = 'O';
            pattern[row] = new string(rowChars);
        }

        return pattern;
    }
}


/// <summary>
/// 连接模式的数据结构
/// </summary>
[System.Serializable]
public class ConnectionPattern
{
    public string name;
    public List<string> pattern; // 每个字符串代表一行的连接模式，例如 "OXXXXX"
    public bool isUnlocked;      // 是否已解锁

    // 存储 'O' 的位置
    public List<Vector2Int> positions; // 格子的行和列

    /// <summary>
    /// 初始化 'O' 的位置并按列排序
    /// </summary>
    public void InitializePositions()
    {
        positions = new List<Vector2Int>();
        int numRows = pattern.Count;
        int numCols = pattern[0].Length;

        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                if (pattern[row][col] == 'O')
                {
                    positions.Add(new Vector2Int(row, col));
                }
            }
        }

        // 按照列排序，确保连接顺序从左到右
        positions.Sort((a, b) => a.y.CompareTo(b.y));
    }
}

[System.Serializable]
public class ConnectionList
{
    public List<ConnectionPattern> connections;
}


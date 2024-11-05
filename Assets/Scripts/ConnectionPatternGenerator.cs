using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 自動生成所有可能的連線方式，並保存到JSON文件
/// </summary>
public class ConnectionPatternGenerator : MonoBehaviour
{
    public int rows = 4;      // 行數
    public int columns = 6;   // 列數

    public string outputFileName = "ConnectionPatterns.json"; // 輸出文件名，保存在 Assets/Resources 資料夾下

    [ContextMenu("生成連線方式並保存到JSON")]
    public void GenerateAndSavePatterns()
    {
        List<ConnectionPattern> generatedPatterns = GenerateAllConnectionPatterns();

        // 將連線方式列表包裝為 ConnectionList 對象
        ConnectionList connectionList = new ConnectionList
        {
            connections = generatedPatterns
        };

        // 序列化為JSON字符串
        string json = JsonUtility.ToJson(connectionList, true);

        // 確保輸出目錄存在
        string outputDir = Application.dataPath + "/Resources";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // 輸出文件路徑
        string outputPath = Path.Combine(outputDir, outputFileName);

        // 將JSON字符串寫入文件
        File.WriteAllText(outputPath, json);

        Debug.Log($"連線方式已生成並保存到 {outputPath}");
    }

    /// <summary>
    /// 生成所有可能的連線方式
    /// </summary>
    List<ConnectionPattern> GenerateAllConnectionPatterns()
    {
        List<ConnectionPattern> generatedPatterns = new List<ConnectionPattern>();

        // 對每一行的每一個格子進行遍歷，生成從第一列到第六列的連線
        for (int startRow = 0; startRow < rows; startRow++)
        {
            List<int> currentPath = new List<int>();
            currentPath.Add(startRow);
            GeneratePatternsRecursive(1, currentPath, generatedPatterns);
        }

        Debug.Log($"共生成了 {generatedPatterns.Count} 種連線方式");

        return generatedPatterns;
    }

    /// <summary>
    /// 遞歸生成連線方式
    /// </summary>
    /// <param name="currentColumn">當前列</param>
    /// <param name="currentPath">當前路徑</param>
    /// <param name="patterns">存儲生成的連線方式</param>
    void GeneratePatternsRecursive(int currentColumn, List<int> currentPath, List<ConnectionPattern> patterns)
    {
        if (currentColumn >= columns)
        {
            // 已經到達最後一列，儲存當前的連線方式
            List<string> pattern = CreatePatternFromPath(currentPath);
            ConnectionPattern connectionPattern = new ConnectionPattern
            {
                name = $"連線方式 {patterns.Count + 1}",
                pattern = pattern,
                isUnlocked = false // 預設為未解鎖
            };
            patterns.Add(connectionPattern);
            return;
        }

        // 獲取當前行
        int currentRow = currentPath[currentPath.Count - 1];

        // 獲取下一列可能的行（相鄰或對角相鄰）
        List<int> nextRows = GetAdjacentRows(currentRow);

        foreach (int nextRow in nextRows)
        {
            List<int> newPath = new List<int>(currentPath);
            newPath.Add(nextRow);
            GeneratePatternsRecursive(currentColumn + 1, newPath, patterns);
        }
    }

    /// <summary>
    /// 獲取相鄰或對角相鄰的行索引
    /// </summary>
    /// <param name="currentRow">當前行索引</param>
    /// <returns>相鄰行的列表</returns>
    List<int> GetAdjacentRows(int currentRow)
    {
        List<int> adjacentRows = new List<int>();

        // 上一行
        if (currentRow - 1 >= 0)
        {
            adjacentRows.Add(currentRow - 1);
        }

        // 當前行
        adjacentRows.Add(currentRow);

        // 下一行
        if (currentRow + 1 < rows)
        {
            adjacentRows.Add(currentRow + 1);
        }

        return adjacentRows;
    }

    /// <summary>
    /// 根據路徑創建連線模式
    /// </summary>
    /// <param name="path">路徑（每列的行索引）</param>
    /// <returns>連線模式的列表，每個字符串代表一行的連線狀態</returns>
    List<string> CreatePatternFromPath(List<int> path)
    {
        // 初始化模式列表，所有位置設為 "X"
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

        // 根據路徑設置 "O"
        for (int col = 0; col < path.Count; col++)
        {
            int row = path[col];
            char[] rowChars = pattern[row].ToCharArray();
            rowChars[col] = 'O';
            pattern[row] = new string(rowChars);
        }

        return pattern;
    }

    /// <summary>
    /// 解鎖指定名稱的連線模式
    /// </summary>
    /// <param name="patternName">連線模式名稱</param>
    public void UnlockConnectionPattern(string patternName)
    {
        string outputPath = Path.Combine(Application.dataPath, "Resources", outputFileName);
        if (!File.Exists(outputPath))
        {
            Debug.LogError($"連線方式文件不存在: {outputPath}");
            return;
        }

        string json = File.ReadAllText(outputPath);
        ConnectionList connectionList = JsonUtility.FromJson<ConnectionList>(json);

        foreach (var pattern in connectionList.connections)
        {
            if (pattern.name == patternName)
            {
                pattern.isUnlocked = true;
                Debug.Log($"已解鎖連線方式: {pattern.name}");
                break;
            }
        }

        // 序列化並保存
        string updatedJson = JsonUtility.ToJson(connectionList, true);
        File.WriteAllText(outputPath, updatedJson);
    }
}



/// <summary>
/// 連線模式的數據結構
/// </summary>
[System.Serializable]
public class ConnectionPattern
{
    public string name;
    public List<string> pattern; // 每個字符串代表一行的連線模式，例如 "OXXXXX"
    public bool isUnlocked;      // 是否已解鎖
}

[System.Serializable]
public class ConnectionList
{
    public List<ConnectionPattern> connections;
}


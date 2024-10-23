using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LinkManager))]
public class LinkManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LinkManager manager = (LinkManager)target;

        // 添加一個分隔線
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("調試工具", EditorStyles.boldLabel);

        // 添加重新放置角色和檢查連線的按鈕
        if (GUILayout.Button("重新放置角色並檢查連線"))
        {
            // 調用LinkManager的方法
            manager.ClearGrid();
            manager.PlaceCharactersRandomly();
            manager.CheckForLinks();

            // 在場景中繪製連線
            SceneView.RepaintAll();
        }

        // 顯示當前選擇的連線模式
        if (manager.allLinkPatterns != null && manager.allLinkPatterns.Count > 0)
        {
            // 確保索引在有效範圍內
            manager.selectedPatternIndex = Mathf.Clamp(manager.selectedPatternIndex, 0, manager.allLinkPatterns.Count - 1);

            // 顯示選擇連線模式的滑塊
            manager.selectedPatternIndex = EditorGUILayout.IntSlider("選擇連線模式", manager.selectedPatternIndex, 0, manager.allLinkPatterns.Count - 1);

            // 添加刷新按鈕
            if (GUILayout.Button("刷新連線模式"))
            {
                // 強制重繪Scene視圖
                SceneView.RepaintAll();
            }

            // 顯示當前連線模式的ID
            EditorGUILayout.LabelField("當前連線模式ID", manager.allLinkPatterns[manager.selectedPatternIndex].Id.ToString());

            // 顯示連線模式的圖形表示
            DisplayLinkPattern(manager.allLinkPatterns[manager.selectedPatternIndex]);
        }
    }

    private void DisplayLinkPattern(LinkPattern pattern)
    {
        EditorGUILayout.LabelField("連線模式：");
        foreach (var row in pattern.PatternData)
        {
            EditorGUILayout.LabelField(row);
        }
    }
}
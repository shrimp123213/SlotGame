using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LinkManager))]
public class LinkManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LinkManager manager = (LinkManager)target;

        if (manager.allLinkPatterns != null && manager.allLinkPatterns.Count > 0)
        {
            // 顯示當前選擇的連線模式
            manager.selectedPatternIndex = EditorGUILayout.IntSlider("選擇連線模式", manager.selectedPatternIndex, 0, manager.allLinkPatterns.Count - 1);

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
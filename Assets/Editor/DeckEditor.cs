using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(Deck))]
public class DeckEditor : Editor
{
    private ReorderableList reorderableList;

    // 新增字段：用于输入设置所有卡片数量
    private string setAllQuantityInput = "1"; // 默认值
    private const string SetAllQuantityLabel = "Set All Quantities";

    private void OnEnable()
    {
        Deck deck = (Deck)target;

        reorderableList = new ReorderableList(deck.entries, typeof(DeckEntry), true, true, true, true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Deck Entries");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            Deck deckObj = (Deck)target;
            if (index >= deckObj.entries.Count)
                return;

            DeckEntry entry = deckObj.entries[index];

            float unitWidth = rect.width * 0.6f;
            float qtyWidth = rect.width * 0.4f;

            Rect unitRect = new Rect(rect.x, rect.y, unitWidth - 10, EditorGUIUtility.singleLineHeight);
            Rect qtyRect = new Rect(rect.x + unitWidth, rect.y, qtyWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            entry.unitData = (UnitData)EditorGUI.ObjectField(unitRect, entry.unitData, typeof(UnitData), false);
            if (EditorGUI.EndChangeCheck())
            {
                // 如果 unitData 被设置为 null，自动移除该 entry
                if (entry.unitData == null)
                {
                    deckObj.entries.RemoveAt(index);
                    EditorUtility.SetDirty(deckObj);
                    return;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newQuantity = EditorGUI.IntField(qtyRect, entry.quantity);
            if (EditorGUI.EndChangeCheck())
            {
                if (newQuantity < 0)
                    newQuantity = 0;
                entry.quantity = newQuantity;
                EditorUtility.SetDirty(deckObj);
            }
        };

        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            Deck deckObj = (Deck)target;
            deckObj.entries.Add(new DeckEntry(null, 1));
            EditorUtility.SetDirty(deckObj);
        };

        reorderableList.onRemoveCallback = (ReorderableList list) =>
        {
            Deck deckObj = (Deck)target;
            if (list.index >= 0 && list.index < deckObj.entries.Count)
            {
                deckObj.entries.RemoveAt(list.index);
                EditorUtility.SetDirty(deckObj);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Deck deck = (Deck)target;

        // 绘制 DeckEntry 列表
        reorderableList.DoLayoutList();

        EditorGUILayout.Space();

        // 添加分隔线
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 添加快速设置所有卡片数量的区域
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(SetAllQuantityLabel, EditorStyles.boldLabel);

        // 输入字段：设置所有卡片的数量
        EditorGUI.BeginChangeCheck();
        setAllQuantityInput = EditorGUILayout.TextField("Quantity", setAllQuantityInput);
        if (EditorGUI.EndChangeCheck())
        {
            // 可选：实时验证输入
        }

        // 按钮：设置所有卡片数量
        if (GUILayout.Button("Apply to All"))
        {
            if (int.TryParse(setAllQuantityInput, out int newQuantity))
            {
                if (newQuantity < 0)
                {
                    EditorUtility.DisplayDialog("Invalid Quantity", "Quantity cannot be negative.", "OK");
                }
                else
                {
                    SetAllDeckEntriesQuantity(deck, newQuantity);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Please enter a valid integer for quantity.", "OK");
            }
        }

        // 显示总卡片数量
        int totalQuantity = 0;
        foreach (var entry in deck.entries)
        {
            if (entry.unitData != null)
            {
                totalQuantity += entry.quantity;
            }
        }
        EditorGUILayout.LabelField($"Total Quantity: {totalQuantity}");

        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 设置所有 DeckEntry 的 quantity 为指定值
    /// </summary>
    /// <param name="deck">目标 Deck</param>
    /// <param name="newQuantity">新的数量值</param>
    private void SetAllDeckEntriesQuantity(Deck deck, int newQuantity)
    {
        Undo.RecordObject(deck, "Set All Quantities");

        foreach (var entry in deck.entries)
        {
            if (entry.unitData != null)
            {
                entry.quantity = newQuantity;
                Debug.Log($"DeckEditor: 设置 {entry.unitData.unitName} 的数量为 {newQuantity}");
            }
        }

        EditorUtility.SetDirty(deck);
    }
}

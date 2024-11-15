using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(Deck))]
public class DeckEditor : Editor
{
    private ReorderableList reorderableList;

    // 新增字段：用于输入设置所有卡片数量
    private string setAllQuantityInput = "1"; // 默认值
    private string setAllInjuredQuantityInput = "0"; // 默认值
    private const string SetAllQuantityLabel = "Set All Quantities";

    private void OnEnable()
    {
        Deck deck = (Deck)target;

        reorderableList = new ReorderableList(deck.entries, typeof(DeckEntry), true, true, true, true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            float unitWidth = rect.width * 0.4f;
            float qtyWidth = rect.width * 0.3f;
            float injuredQtyWidth = rect.width * 0.3f;

            Rect unitRect = new Rect(rect.x, rect.y, unitWidth - 10, EditorGUIUtility.singleLineHeight);
            Rect qtyRect = new Rect(rect.x + unitWidth, rect.y, qtyWidth - 10, EditorGUIUtility.singleLineHeight);
            Rect injuredQtyRect = new Rect(rect.x + unitWidth + qtyWidth, rect.y, injuredQtyWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(unitRect, "Unit Data");
            EditorGUI.LabelField(qtyRect, "Quantity");
            EditorGUI.LabelField(injuredQtyRect, "Injured Qty");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            Deck deckObj = (Deck)target;
            if (index >= deckObj.entries.Count)
                return;

            DeckEntry entry = deckObj.entries[index];

            float unitWidth = rect.width * 0.4f;
            float qtyWidth = rect.width * 0.3f;
            float injuredQtyWidth = rect.width * 0.3f;

            Rect unitRect = new Rect(rect.x, rect.y, unitWidth - 10, EditorGUIUtility.singleLineHeight);
            Rect qtyRect = new Rect(rect.x + unitWidth, rect.y, qtyWidth - 10, EditorGUIUtility.singleLineHeight);
            Rect injuredQtyRect = new Rect(rect.x + unitWidth + qtyWidth, rect.y, injuredQtyWidth, EditorGUIUtility.singleLineHeight);

            // 编辑 unitData
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

            // 编辑 quantity
            EditorGUI.BeginChangeCheck();
            int newQuantity = EditorGUI.IntField(qtyRect, entry.quantity);
            if (EditorGUI.EndChangeCheck())
            {
                if (newQuantity < 0)
                    newQuantity = 0;
                entry.quantity = newQuantity;
                EditorUtility.SetDirty(deckObj);
            }

            // 编辑 injuredQuantity
            EditorGUI.BeginChangeCheck();
            int newInjuredQuantity = EditorGUI.IntField(injuredQtyRect, entry.injuredQuantity);
            if (EditorGUI.EndChangeCheck())
            {
                if (newInjuredQuantity < 0)
                    newInjuredQuantity = 0;
                entry.injuredQuantity = newInjuredQuantity;
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

        // 输入字段：设置所有卡片的正常数量
        EditorGUI.BeginChangeCheck();
        setAllQuantityInput = EditorGUILayout.TextField("Normal Quantity", setAllQuantityInput);
        if (EditorGUI.EndChangeCheck())
        {
            // 可选：实时验证输入
        }

        // 输入字段：设置所有卡片的负伤数量
        EditorGUI.BeginChangeCheck();
        setAllInjuredQuantityInput = EditorGUILayout.TextField("Injured Quantity", setAllInjuredQuantityInput);
        if (EditorGUI.EndChangeCheck())
        {
            // 可选：实时验证输入
        }

        // 按钮：设置所有卡片数量
        if (GUILayout.Button("Apply to All"))
        {
            bool validNormal = int.TryParse(setAllQuantityInput, out int newQuantity);
            bool validInjured = int.TryParse(setAllInjuredQuantityInput, out int newInjuredQuantity);

            if (validNormal && validInjured)
            {
                if (newQuantity < 0 || newInjuredQuantity < 0)
                {
                    EditorUtility.DisplayDialog("Invalid Quantity", "Quantity cannot be negative.", "OK");
                }
                else
                {
                    SetAllDeckEntriesQuantity(deck, newQuantity, newInjuredQuantity);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Please enter valid integers for quantities.", "OK");
            }
        }

        // 显示总卡片数量
        int totalQuantity = 0;
        foreach (var entry in deck.entries)
        {
            if (entry.unitData != null)
            {
                totalQuantity += entry.quantity + entry.injuredQuantity;
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
    /// 设置所有 DeckEntry 的 quantity 和 injuredQuantity 为指定值
    /// </summary>
    /// <param name="deck">目标 Deck</param>
    /// <param name="newQuantity">新的正常数量值</param>
    /// <param name="newInjuredQuantity">新的负伤数量值</param>
    private void SetAllDeckEntriesQuantity(Deck deck, int newQuantity, int newInjuredQuantity)
    {
        Undo.RecordObject(deck, "Set All Quantities");

        foreach (var entry in deck.entries)
        {
            if (entry.unitData != null)
            {
                entry.quantity = newQuantity;
                entry.injuredQuantity = newInjuredQuantity;
                Debug.Log($"DeckEditor: 设置 {entry.unitData.unitName} 的数量为 正常：{newQuantity}，负伤：{newInjuredQuantity}");
            }
        }

        EditorUtility.SetDirty(deck);
    }
}

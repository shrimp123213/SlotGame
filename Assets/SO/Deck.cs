using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理牌组的类
/// </summary>
[CreateAssetMenu(fileName = "NewDeck", menuName = "ScriptableObjects/Deck")]
public class Deck : ScriptableObject
{
    [Header("Deck Entries")]
    [Tooltip("拖拽不同的 UnitData 进来，并在 DeckEntry 中设置对应的数量")]
    public List<DeckEntry> entries = new List<DeckEntry>();

    /// <summary>
    /// 添加卡牌到牌组
    /// </summary>
    public void AddCard(UnitData unitData, int quantity = 1)
    {
        if (unitData == null)
        {
            Debug.LogWarning("Deck: 尝试添加 null UnitData！");
            return;
        }

        DeckEntry existingEntry = entries.Find(entry => entry.unitData == unitData);
        if (existingEntry != null)
        {
            existingEntry.quantity += quantity;
        }
        else
        {
            entries.Add(new DeckEntry(unitData, quantity));
        }
    }

    /// <summary>
    /// 移除卡牌从牌组
    /// </summary>
    public void RemoveCard(UnitData unitData, int quantity = 1)
    {
        if (unitData == null)
        {
            Debug.LogWarning("Deck: 尝试移除 null UnitData！");
            return;
        }

        DeckEntry existingEntry = entries.Find(entry => entry.unitData == unitData);
        if (existingEntry != null)
        {
            existingEntry.quantity -= quantity;
            if (existingEntry.quantity < 0)
            {
                existingEntry.quantity = 0; // 保持数量不为负数
            }
            // 保留 entry，即使数量为0
        }
    }

    /// <summary>
    /// 获取当前牌组中的所有卡牌，考虑每种卡牌的数量
    /// </summary>
    public List<UnitData> GetAllCards()
    {
        List<UnitData> allCards = new List<UnitData>();
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.quantity; i++)
            {
                allCards.Add(entry.unitData);
            }
        }
        return allCards;
    }
}


[System.Serializable]
public class DeckEntry
{
    public UnitData unitData; // 卡牌类型
    public int quantity;      // 该类型卡牌的数量

    public DeckEntry(UnitData unitData, int quantity)
    {
        this.unitData = unitData;
        this.quantity = quantity;
    }
}
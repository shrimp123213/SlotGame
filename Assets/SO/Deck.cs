using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;


/// <summary>
/// 管理牌组的类
/// </summary>
[CreateAssetMenu(fileName = "NewDeck", menuName = "ScriptableObjects/Deck")]
public class Deck : ScriptableObject
{
    [Header("Deck Entries")]
    [Tooltip("拖拽不同的 UnitData 进来，并在 DeckEntry 中设置对应的数量")]
    public List<DeckEntry> entries = new List<DeckEntry>();
    
    public event Action OnDeckChanged;


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
    public void RemoveUnitQuantity(UnitData unitData, int amount = 1)
    {
        if (unitData == null)
        {
            Debug.LogWarning("Deck: 嘗試移除 null UnitData 的數量！");
            return;
        }

        DeckEntry existingEntry = entries.Find(entry => entry.unitData == unitData);
        if (existingEntry != null)
        {
            existingEntry.quantity -= amount;
            if (existingEntry.quantity < 0)
            {
                existingEntry.quantity = 0;
            }
        }

        Debug.Log($"Deck: 移除 {unitData.unitName} 的數量至 {(existingEntry != null ? existingEntry.quantity : 0)}");

        // 觸發事件
        OnDeckChanged?.Invoke();
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
    
    /// <summary>
    /// 隨機選取指定數量的不同 UnitData
    /// </summary>
    /// <param name="count">選取的數量</param>
    /// <returns>選取的 UnitData 列表</returns>
    public List<UnitData> GetRandomUnitChoices(int count)
    {
        List<UnitData> availableUnits = new List<UnitData>();

        // 收集所有可用的 UnitData（數量 > 0）
        foreach (var entry in entries)
        {
            if (entry.unitData != null && entry.quantity > 0 && !availableUnits.Contains(entry.unitData))
            {
                availableUnits.Add(entry.unitData);
            }
        }

        // 如果可用單位少於要求數量，返回所有可用的
        if (availableUnits.Count <= count)
        {
            return new List<UnitData>(availableUnits);
        }

        // 隨機選取指定數量的 UnitData
        List<UnitData> selectedUnits = new List<UnitData>();
        List<UnitData> tempList = new List<UnitData>(availableUnits);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            selectedUnits.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        return selectedUnits;
    }

    /// <summary>
    /// 增加指定 UnitData 的數量
    /// </summary>
    /// <param name="unitData">要增加數量的 UnitData</param>
    /// <param name="amount">增加的數量</param>
    public void IncreaseUnitQuantity(UnitData unitData, int amount = 1)
    {
        if (unitData == null)
        {
            Debug.LogWarning("Deck: 嘗試增加 null UnitData 的數量！");
            return;
        }

        DeckEntry existingEntry = entries.Find(entry => entry.unitData == unitData);
        if (existingEntry != null)
        {
            existingEntry.quantity += amount;
        }
        else
        {
            entries.Add(new DeckEntry(unitData, amount));
        }

        Debug.Log($"Deck: 增加 {unitData.unitName} 的數量至 {(existingEntry != null ? existingEntry.quantity : amount)}");
        OnDeckChanged?.Invoke();
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
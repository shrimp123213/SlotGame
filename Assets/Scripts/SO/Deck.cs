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
    /// 添加卡牌到牌组，并初始化技能延迟
    /// </summary>
    public void AddCard(UnitData unitData, string unitId, int quantity = 1, bool isInjured = false, Dictionary<string, int> skillDelays = null)
    {
        if (unitData == null)
        {
            Debug.LogWarning("Deck: 尝试添加 null UnitData！");
            return;
        }

        DeckEntry existingEntry = entries.Find(entry => entry.unitData == unitData);
        if (existingEntry != null)
        {
            if (isInjured)
                existingEntry.injuredQuantity += quantity;
            else
                existingEntry.quantity += quantity;

            // 仅当 unitId 和 skillDelays 都不为 null 时，才保存技能延迟
            if (!string.IsNullOrEmpty(unitId) && skillDelays != null)
            {
                existingEntry.unitSkillDelays[unitId] = new Dictionary<string, int>(skillDelays);
            }
        }
        else
        {
            DeckEntry newEntry = new DeckEntry(unitData, isInjured ? 0 : quantity);
            newEntry.injuredQuantity = isInjured ? quantity : 0;

            // 初始化 unitSkillDelays
            if (newEntry.unitSkillDelays == null)
                newEntry.unitSkillDelays = new Dictionary<string, Dictionary<string, int>>();

            // 仅当 unitId 和 skillDelays 都不为 null 时，才保存技能延迟
            if (!string.IsNullOrEmpty(unitId) && skillDelays != null)
            {
                newEntry.unitSkillDelays[unitId] = new Dictionary<string, int>(skillDelays);
            }
            entries.Add(newEntry);
        }

        // 触发事件
        OnDeckChanged?.Invoke();
        Debug.Log($"Deck: 添加了 {quantity} 张 {unitData.unitName} 到牌组，isInjured: {isInjured}");
    }

    /// <summary>
    /// 移除卡牌从牌组
    /// </summary>
    public void RemoveCard(UnitData unitData, string unitId, int quantity = 1, bool isInjured = false)
    {
        if (unitData == null)
        {
            Debug.LogWarning("Deck: 尝试移除 null UnitData 的数量！");
            return;
        }

        DeckEntry existingEntry = entries.Find(entry => entry.unitData == unitData);
        if (existingEntry != null)
        {
            if (isInjured)
            {
                existingEntry.injuredQuantity -= quantity;
                if (existingEntry.injuredQuantity < 0)
                    existingEntry.injuredQuantity = 0;
            }
            else
            {
                existingEntry.quantity -= quantity;
                if (existingEntry.quantity < 0)
                    existingEntry.quantity = 0;
            }

            // 移除技能延迟
            if (!string.IsNullOrEmpty(unitId) && existingEntry.unitSkillDelays != null)
            {
                existingEntry.unitSkillDelays.Remove(unitId);
            }
            
            // 如果数量和负伤数量都为 0，移除 DeckEntry
            if (existingEntry.quantity <= 0 && existingEntry.injuredQuantity <= 0)
            {
                entries.Remove(existingEntry);
                Debug.Log($"Deck: 已移除单位 {unitData.unitName} 的 DeckEntry，因为数量为 0");
            }
        }

        // 触发事件
        OnDeckChanged?.Invoke();
    }

    /// <summary>
    /// 获取当前牌组中的所有卡牌，包括正常和负伤的单位
    /// </summary>
    public List<UnitWithInjuryStatus> GetAllUnitsWithInjuryStatus()
    {
        List<UnitWithInjuryStatus> allUnits = new List<UnitWithInjuryStatus>();
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.quantity; i++)
            {
                allUnits.Add(new UnitWithInjuryStatus(entry.unitData, false));
            }
            for (int i = 0; i < entry.injuredQuantity; i++)
            {
                allUnits.Add(new UnitWithInjuryStatus(entry.unitData, true));
            }
        }
        return allUnits;
    }
    
    /// <summary>
    /// 隨機選取指定數量的不同 UnitData
    /// </summary>
    /// <param name="count">選取的數量</param>
    /// <returns>選取的 UnitData 列表</returns>
    public List<UnitData> GetRandomUnitChoices(int count)
    {
        List<UnitData> availableUnits = new List<UnitData>();

        // 收集所有的 UnitData
        foreach (var entry in entries)
        {
            if (entry.unitData != null && entry.quantity >= 0 && !availableUnits.Contains(entry.unitData))
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
    
    /// <summary>
    /// 获取特定单位的技能延迟
    /// </summary>
    public Dictionary<string, int> GetUnitSkillDelays(UnitData unitData, string unitId)
    {
        if (entries == null)
        {
            Debug.LogError("Deck: entries 列表未初始化！");
            return null;
        }

        DeckEntry entry = entries.Find(e => e.unitData == unitData);
        if (entry == null)
        {
            Debug.LogWarning($"Deck: 未找到匹配的 DeckEntry，unitData: {unitData?.unitName ?? "null"}");
            return null;
        }

        if (entry.unitSkillDelays == null)
        {
            Debug.LogWarning($"Deck: DeckEntry 的 unitSkillDelays 未初始化，unitData: {entry.unitData.unitName}");
            entry.unitSkillDelays = new Dictionary<string, Dictionary<string, int>>();
        }

        if (string.IsNullOrEmpty(unitId))
        {
            Debug.LogWarning("Deck: unitId 为空或 null！");
            return null;
        }

        if (entry.unitSkillDelays.ContainsKey(unitId))
        {
            return entry.unitSkillDelays[unitId];
        }
        else
        {
            Debug.Log($"Deck: unitSkillDelays 不包含 unitId: {unitId}");
        }
        return null;
    }

    /// <summary>
    /// 保存单位的技能延迟
    /// </summary>
    public void SaveUnitSkillDelays(UnitData unitData, string unitId, Dictionary<string, int> skillDelays)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            Debug.LogError("Deck.SaveUnitSkillDelays: unitId 不能为 null 或空！");
            return;
        }
        
        DeckEntry entry = entries.Find(e => e.unitData == unitData);
        if (entry != null)
        {
            if (entry.unitSkillDelays == null)
                entry.unitSkillDelays = new Dictionary<string, Dictionary<string, int>>();

            entry.unitSkillDelays[unitId] = new Dictionary<string, int>(skillDelays);
        }
        else
        {
            Debug.LogWarning($"Deck: No DeckEntry found for UnitData {unitData?.unitName ?? "null"}.");
        }
    }
    
    /// <summary>
    /// 在每回合开始时减少所有单位的技能延迟
    /// </summary>
    public void ReduceSkillDelays()
    {
        if (entries == null)
        {
            Debug.LogError("Deck.ReduceSkillDelays: entries 列表未初始化！");
            return;
        }

        foreach (var entry in entries)
        {
            if (entry == null)
            {
                Debug.LogWarning("Deck.ReduceSkillDelays: entries 列表中存在 null 的 DeckEntry！");
                continue;
            }

            if (entry.unitSkillDelays == null)
            {
                Debug.LogWarning($"Deck.ReduceSkillDelays: DeckEntry 的 unitSkillDelays 未初始化，unitData: {entry.unitData?.unitName ?? "null"}");
                entry.unitSkillDelays = new Dictionary<string, Dictionary<string, int>>();
                continue;
            }
            
            Debug.Log($"Deck: 单位 {entry.unitData.unitName} 的 unitSkillDelays 中有 {entry.unitSkillDelays.Count} 个 unitId");

            foreach (var unitDelays in entry.unitSkillDelays)
            {
                if (unitDelays.Value == null)
                {
                    Debug.LogWarning($"Deck.ReduceSkillDelays: unitDelays.Value 为 null，unitId: {unitDelays.Key}");
                    continue;
                }

                var skillDelays = unitDelays.Value;
                List<string> skills = new List<string>(skillDelays.Keys);
                foreach (var skillName in skills)
                {
                    if (skillDelays[skillName] > 0)
                    {
                        skillDelays[skillName]--;
                        Debug.Log($"Deck: 单位 {entry.unitData.unitName} 技能 {skillName} 的延迟减少到 {skillDelays[skillName]} 回合");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查单位的技能是否准备就绪（延迟为0）
    /// </summary>
    public bool IsSkillReady(UnitData unitData, string skillName)
    {
        foreach (var entry in entries)
        {
            if (entry.unitData == unitData)
            {
                foreach (var unitDelays in entry.unitSkillDelays)
                {
                    if (unitDelays.Value.ContainsKey(skillName) && unitDelays.Value[skillName] <= 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false; // 默认认为未准备就绪
    }

    /// <summary>
    /// 重置单位的技能延迟
    /// </summary>
    public void ResetSkillDelay(UnitData unitData, string skillName, SkillSO skillSO)
    {
        if (skillSO == null)
        {
            Debug.LogWarning("Deck.ResetSkillDelay: skillSO is null.");
            return;
        }

        foreach (var entry in entries)
        {
            if (entry.unitData == unitData)
            {
                foreach (var unitDelays in entry.unitSkillDelays)
                {
                    if (unitDelays.Value.ContainsKey(skillName))
                    {
                        SkillActionData firstAction = skillSO.actions != null && skillSO.actions.Count > 0 ? skillSO.actions[0] : null;
                        if (firstAction == null)
                        {
                            Debug.LogWarning($"Deck.ResetSkillDelay: No actions found in SkillSO {skillSO.skillName}.");
                            unitDelays.Value[skillName] = 0; // 或其他默认值
                        }
                        else
                        {
                            int delay = firstAction.Delay;
                            unitDelays.Value[skillName] = delay;
                            Debug.Log($"Deck: 单位 {unitData.unitName} 技能 {skillName} 的延迟已重置为 {delay} 回合");
                        }
                    }
                }
            }
        }
    }
    
    public void Clear()
    {
        entries.Clear();
        OnDeckChanged?.Invoke();
    }
    
    public void RemoveUnitSkillDelays(UnitData unitData, string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            Debug.LogError("Deck.RemoveUnitSkillDelays: unitId 不能为 null 或空！");
            return;
        }
        
        DeckEntry entry = entries.Find(e => e.unitData == unitData);
        if (entry != null && entry.unitSkillDelays != null)
        {
            if (entry.unitSkillDelays.ContainsKey(unitId))
            {
                entry.unitSkillDelays.Remove(unitId);
                Debug.Log($"Deck: 已移除单位 {unitData.unitName} 的 unitId {unitId} 的技能延迟数据");
            }
            else
            {
                Debug.LogWarning($"Deck: unitSkillDelays 中不存在 unitId {unitId}，无法移除");
            }
        }
    }

}


[System.Serializable]
public class DeckEntry
{
    public UnitData unitData;      // 卡牌类型
    public int quantity;           // 正常状态的卡牌数量
    public int injuredQuantity;    // 负伤状态的卡牌数量
    
    // 保存每个单位实例的技能延迟，使用唯一标识符（如GUID）作为键
    [SerializeField]
    public Dictionary<string, Dictionary<string, int>> unitSkillDelays;

    public DeckEntry(UnitData unitData, int quantity)
    {
        this.unitData = unitData;
        this.quantity = quantity;
        this.injuredQuantity = 0; // 初始为0
        this.unitSkillDelays = new Dictionary<string, Dictionary<string, int>>();
    }
    
    // 添加一个默认构造函数，确保在反序列化时初始化字段
    public DeckEntry()
    {
        this.unitSkillDelays = new Dictionary<string, Dictionary<string, int>>();
    }
}

// 定义一个类，用于存储单位和其负伤状态
[System.Serializable]
public class UnitWithInjuryStatus
{
    public UnitData unitData;
    public bool isInjured;

    public UnitWithInjuryStatus(UnitData unitData, bool isInjured)
    {
        this.unitData = unitData;
        this.isInjured = isInjured;
    }
}
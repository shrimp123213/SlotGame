// DeckManager.cs
using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Decks")]
    public Deck playerDeck; // 玩家牌組
    public Deck enemyDeck;  // 敵人牌組

    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 根據需要設置
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 添加卡牌到玩家牌组
    /// </summary>
    public void AddCardToPlayerDeck(UnitData unitData, string unitId, int quantity = 1, bool isInjured = false, Dictionary<string, int> skillDelays = null)
    {
        if (playerDeck != null)
        {
            playerDeck.AddCard(unitData, unitId, quantity, isInjured, skillDelays);
        }
    }
    
    /// <summary>
    /// 添加卡牌到敌人牌组
    /// </summary>
    public void AddCardToEnemyDeck(UnitData unitData, string unitId, int quantity = 1, bool isInjured = false, Dictionary<string, int> skillDelays = null)
    {
        if (enemyDeck != null)
        {
            enemyDeck.AddCard(unitData, unitId, quantity, isInjured, skillDelays);
        }
    }

    /// <summary>
    /// 从玩家牌组中移除卡牌
    /// </summary>
    public void RemoveCardFromPlayerDeck(UnitData unitData, string unitId, int quantity = 1, bool isInjured = false)
    {
        if (playerDeck != null)
        {
            playerDeck.RemoveCard(unitData, unitId, quantity, isInjured);
        }
    }

    /// <summary>
    /// 从敌人牌组中移除卡牌
    /// </summary>
    public void RemoveCardFromEnemyDeck(UnitData unitData, string unitId, int quantity = 1, bool isInjured = false)
    {
        if (enemyDeck != null)
        {
            enemyDeck.RemoveCard(unitData, unitId, quantity, isInjured);
        }
    }

    /// <summary>
    /// 处理单位死亡，将卡牌移至墓地
    /// </summary>
    public void HandleUnitDeath(UnitData unitData, string unitId, bool isInjured, bool isPlayerUnit)
    {
        // 从战场移除单位
        Vector3Int unitPosition = GridManager.Instance.GetUnitPosition(unitId);
        GridManager.Instance.RemoveSkillUserAt(unitPosition);

        if (isPlayerUnit)
        {
            if (isInjured)
            {
                // 单位已处于负伤状态，再次死亡，进入墓地
                RemoveCardFromPlayerDeck(unitData, unitId, 1, isInjured: true);
                GraveyardManager.Instance.AddToPlayerGraveyard(unitData, unitId);
                Debug.Log($"DeckManager: 玩家单位 {unitData.unitName} 在负伤状态下死亡，进入墓地。");
            }
            else
            {
                // 单位第一次死亡，进入负伤状态并返回牌库
                RemoveCardFromPlayerDeck(unitData, unitId, 1, isInjured: false);
                AddCardToPlayerDeck(unitData, unitId, 1, isInjured: true);
                Debug.Log($"DeckManager: 玩家单位 {unitData.unitName} 第一次死亡，进入负伤状态并返回牌库。");
            }
        }
        else
        {
            if (isInjured)
            {
                // 敌方单位已处于负伤状态，再次死亡，进入墓地
                RemoveCardFromEnemyDeck(unitData, unitId, 1, isInjured: true);
                GraveyardManager.Instance.AddToEnemyGraveyard(unitData, unitId);
                Debug.Log($"DeckManager: 敌方单位 {unitData.unitName} 在负伤状态下死亡，进入墓地。");
            }
            else
            {
                // 敌方单位第一次死亡，进入负伤状态并返回牌库
                RemoveCardFromEnemyDeck(unitData, unitId, 1, isInjured: false);
                AddCardToEnemyDeck(unitData, unitId, 1, isInjured: true);
                Debug.Log($"DeckManager: 敌方单位 {unitData.unitName} 第一次死亡，进入负伤状态并返回牌库。");
            }
        }

        // 可以在这里触发相关的事件，例如更新 UI 等
    }
    
    public void RemoveUnitSkillDelays(UnitData unitData, string unitId)
    {
        if (unitData.camp == Camp.Player)
        {
            playerDeck?.RemoveUnitSkillDelays(unitData, unitId);
        }
        else
        {
            enemyDeck?.RemoveUnitSkillDelays(unitData, unitId);
        }
    }

    
    /// <summary>
    /// 在每回合开始时减少牌库中所有单位的技能延迟
    /// </summary>
    public void ReduceSkillDelaysAtStartOfTurn()
    {
        if (playerDeck != null)
        {
            playerDeck.ReduceSkillDelays();
        }

        if (enemyDeck != null)
        {
            enemyDeck.ReduceSkillDelays();
        }

        Debug.Log("DeckManager: 所有牌库中单位的技能延迟已减少");
    }

    /// <summary>
    /// 检查牌库中单位的技能是否准备就绪
    /// </summary>
    public bool IsSkillReadyInDeck(UnitData unitData, string skillName)
    {
        if (playerDeck != null && playerDeck.IsSkillReady(unitData, skillName))
        {
            return true;
        }

        if (enemyDeck != null && enemyDeck.IsSkillReady(unitData, skillName))
        {
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// 重置牌库中单位的技能延迟
    /// </summary>
    public void ResetSkillDelayInDeck(UnitData unitData, string skillName, SkillSO skillSO, bool isPlayerUnit)
    {
        if (unitData == null || string.IsNullOrEmpty(skillName) || skillSO == null)
        {
            Debug.LogWarning("DeckManager.ResetSkillDelayInDeck: Invalid parameters.");
            return;
        }

        if (isPlayerUnit && playerDeck != null)
        {
            playerDeck.ResetSkillDelay(unitData, skillName, skillSO);
        }

        if (!isPlayerUnit && enemyDeck != null)
        {
            enemyDeck.ResetSkillDelay(unitData, skillName, skillSO);
        }
    }
    
    /// <summary>
    /// 获取单位的技能延迟
    /// </summary>
    public Dictionary<string, int> GetUnitSkillDelays(UnitData unitData, string unitId)
    {
        if (playerDeck != null)
        {
            var delays = playerDeck.GetUnitSkillDelays(unitData, unitId);
            if (delays != null)
                return delays;
        }

        if (enemyDeck != null)
        {
            var delays = enemyDeck.GetUnitSkillDelays(unitData, unitId);
            if (delays != null)
                return delays;
        }

        return null;
    }
    
    /// <summary>
    /// 保存单位的技能延迟
    /// </summary>
    public void SaveUnitSkillDelays(UnitData unitData, string unitId, Dictionary<string, int> skillDelays)
    {
        if (playerDeck != null && IsPlayerUnit(unitData))
        {
            playerDeck.SaveUnitSkillDelays(unitData, unitId, skillDelays);
            return;
        }

        if (enemyDeck != null && IsEnemyUnit(unitData))
        {
            enemyDeck.SaveUnitSkillDelays(unitData, unitId, skillDelays);
            return;
        }

        Debug.LogWarning("DeckManager.SaveUnitSkillDelays: 无法确定单位所属的牌组。");
    }
    
    /// <summary>
    /// 判断单位是否属于玩家
    /// </summary>
    private bool IsPlayerUnit(UnitData unitData)
    {
        // 根据您的游戏逻辑实现
        return unitData.camp == Camp.Player;
    }

    /// <summary>
    /// 判断单位是否属于敌人
    /// </summary>
    private bool IsEnemyUnit(UnitData unitData)
    {
        // 根据您的游戏逻辑实现
        return unitData.camp == Camp.Enemy;
    }
    
    // 可以在這裡添加更多與牌組相關的方法
}
// DeckManager.cs
using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Decks")]
    public Deck playerDeck; // 玩家牌組
    public Deck enemyDeck;  // 敵人牌組
    
    // 墓地列表
    private List<UnitData> graveyardCards = new List<UnitData>();
    private List<UnitData> enemyGraveyardCards = new List<UnitData>();

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
    public void AddCardToPlayerDeck(UnitData unitData, int quantity = 1, bool isInjured = false)
    {
        if (playerDeck != null)
        {
            playerDeck.AddCard(unitData, quantity, isInjured);
        }
    }
    
    /// <summary>
    /// 添加卡牌到敵人牌组
    /// </summary>
    public void AddCardToEnemyDeck(UnitData unitData, int quantity = 1, bool isInjured = false)
    {
        if (enemyDeck != null)
        {
            enemyDeck.AddCard(unitData, quantity, isInjured);
        }
    }


    /// <summary>
    /// 从玩家牌组中移除卡牌
    /// </summary>
    public void RemoveCardFromPlayerDeck(UnitData unitData, int quantity = 1, bool isInjured = false)
    {
        if (playerDeck != null)
        {
            playerDeck.RemoveCard(unitData, quantity, isInjured);
        }
    }
    
    public void RemoveCardFromEnemyDeck(UnitData unitData, int quantity = 1, bool isInjured = false)
    {
        if (enemyDeck != null)
        {
            enemyDeck.RemoveCard(unitData, quantity, isInjured);
        }
    }


    /// <summary>
    /// 处理单位死亡，将卡牌移至墓地
    /// </summary>
    public void HandleUnitDeath(UnitData unitData, bool isInjured, bool isPlayerUnit)
    {
        if (isPlayerUnit)
        {
            // 从玩家牌组中移除卡牌
            RemoveCardFromPlayerDeck(unitData, 1, isInjured);

            // 将卡牌添加到玩家的墓地
            AddCardToPlayerGraveyard(unitData);
        }
        else
        {
            // 从敌人牌组中移除卡牌
            RemoveCardFromEnemyDeck(unitData, 1, isInjured);

            // 将卡牌添加到敌人的墓地
            AddCardToEnemyGraveyard(unitData);
        }

        // 通知 DeckUI 更新墓地显示（如果需要）
        DeckUI deckUI = FindObjectOfType<DeckUI>();
        if (deckUI != null && isPlayerUnit)
        {
            deckUI.AddToGraveyard(unitData);
        }
    }


    /// <summary>
    /// 添加卡牌到墓地
    /// </summary>
    private void AddCardToPlayerGraveyard(UnitData unitData)
    {
        if (unitData != null)
        {
            graveyardCards.Add(unitData);
        }
    }
    
    private void AddCardToEnemyGraveyard(UnitData unitData)
    {
        if (unitData != null)
        {
            enemyGraveyardCards.Add(unitData);
        }
    }

    /// <summary>
    /// 获取墓地中的卡牌列表
    /// </summary>
    public List<UnitData> GetGraveyardCards()
    {
        return graveyardCards;
    }

    // 可以在這裡添加更多與牌組相關的方法
}
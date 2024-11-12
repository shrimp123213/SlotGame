using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckTester : MonoBehaviour
{
    [Header("Decks")]
    public Deck playerDeck; // 玩家牌组
    public Deck enemyDeck;  // 敌人牌组

    [Header("UI Elements")]
    public Button increaseAllButton; // 增加所有卡片按钮
    public Button decreaseAllButton; // 减少所有卡片按钮
    public TMP_InputField amountInputField; // 数量输入字段（可选）

    [Header("Amount Settings")]
    public int defaultAmount = 1; // 默认增加或减少的数量

    void Start()
    {
        if (increaseAllButton != null)
        {
            increaseAllButton.onClick.AddListener(IncreaseAllCards);
        }
        else
        {
            Debug.LogWarning("DeckTester: 未分配 IncreaseAllButton！");
        }

        if (decreaseAllButton != null)
        {
            decreaseAllButton.onClick.AddListener(DecreaseAllCards);
        }
        else
        {
            Debug.LogWarning("DeckTester: 未分配 DecreaseAllButton！");
        }
    }

    /// <summary>
    /// 增加所有卡片的数量
    /// </summary>
    public void IncreaseAllCards()
    {
        int amount = GetAmount();
        Debug.Log($"DeckTester: 增加所有卡片数量 +{amount}");
        ModifyAllCardsQuantity(amount);
    }

    /// <summary>
    /// 减少所有卡片的数量
    /// </summary>
    public void DecreaseAllCards()
    {
        int amount = GetAmount();
        Debug.Log($"DeckTester: 减少所有卡片数量 -{amount}");
        ModifyAllCardsQuantity(-amount);
    }

    /// <summary>
    /// 获取用户输入的数量，若无输入则使用默认值
    /// </summary>
    /// <returns>数量</returns>
    private int GetAmount()
    {
        if (amountInputField != null && !string.IsNullOrEmpty(amountInputField.text))
        {
            if (int.TryParse(amountInputField.text, out int parsedAmount))
            {
                return parsedAmount;
            }
            else
            {
                Debug.LogWarning("DeckTester: 输入的数量无效，使用默认值！");
            }
        }
        return defaultAmount;
    }

    /// <summary>
    /// 修改所有卡片的数量
    /// </summary>
    /// <param name="amount">增加或减少的数量</param>
    private void ModifyAllCardsQuantity(int amount)
    {
        if (playerDeck != null)
        {
            foreach (var entry in playerDeck.entries)
            {
                if (entry.unitData != null)
                {
                    entry.quantity += amount;
                    if (entry.quantity < 0)
                        entry.quantity = 0;
                    Debug.Log($"DeckTester: 玩家牌组 - {entry.unitData.unitName} 数量现在为 {entry.quantity}");
                }
            }
        }
        else
        {
            Debug.LogWarning("DeckTester: PlayerDeck 未分配！");
        }

        if (enemyDeck != null)
        {
            foreach (var entry in enemyDeck.entries)
            {
                if (entry.unitData != null)
                {
                    entry.quantity += amount;
                    if (entry.quantity < 0)
                        entry.quantity = 0;
                    Debug.Log($"DeckTester: 敌人牌组 - {entry.unitData.unitName} 数量现在为 {entry.quantity}");
                }
            }
        }
        else
        {
            Debug.LogWarning("DeckTester: EnemyDeck 未分配！");
        }
    }
}

// DeckUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckUI : MonoBehaviour
{
    [Header("Deck Entries UI")]
    public Transform deckEntriesParent; // 用於放置牌組條目的父對象
    public GameObject deckEntryPrefab; // 牌組條目的預製體

    private Deck playerDeck;

    private void Start()
    {
        // 獲取玩家牌組
        playerDeck = DeckManager.Instance.playerDeck;

        if (playerDeck == null)
        {
            Debug.LogError("DeckUI: 玩家牌組未設置！");
            return;
        }

        // 設置牌組變更事件
        playerDeck.OnDeckChanged += RefreshDeckUI;

        // 初始刷新
        RefreshDeckUI();
    }

    private void OnDestroy()
    {
        if (playerDeck != null)
        {
            playerDeck.OnDeckChanged -= RefreshDeckUI;
        }
    }

    /// <summary>
    /// 刷新牌組的 UI 顯示
    /// </summary>
    private void RefreshDeckUI()
    {
        // 清空現有的條目
        foreach (Transform child in deckEntriesParent)
        {
            Destroy(child.gameObject);
        }

        // 創建新的條目
        foreach (var entry in playerDeck.entries)
        {
            if (entry.unitData != null)
            {
                GameObject entryGO = Instantiate(deckEntryPrefab, deckEntriesParent);
                DeckEntryUI entryUI = entryGO.GetComponent<DeckEntryUI>();
                if (entryUI != null)
                {
                    entryUI.Setup(entry.unitData, entry.quantity);
                }
                else
                {
                    Debug.LogError("DeckUI: DeckEntryPrefab 沒有 DeckEntryUI 組件！");
                }
            }
        }
    }
}
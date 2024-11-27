// DeckUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class DeckUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Deck Entries UI")]
    public Transform deckEntriesParent; // 用于放置牌组条目的父对象
    public GameObject deckEntryPrefab;  // 牌组条目的预制体

    [Header("Deck Stats UI")]
    public TextMeshProUGUI totalCardCountText; // 总卡牌数
    public TextMeshProUGUI graveyardCountText; // 墓地卡牌数
    
    [Header("Graveyard Tooltip")]
    public GameObject graveyardTooltipPanel;
    public TextMeshProUGUI graveyardTooltipText;

    private Deck playerDeck;
    private List<UnitData> graveyardCards = new List<UnitData>(); // 墓地卡牌列表

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
        
        // 设置墓地变更事件
        GraveyardManager.Instance.OnPlayerGraveyardUpdated += RefreshGraveyardUI;

        // 初始化墓地卡牌列表
        graveyardCards = GraveyardManager.Instance.GetPlayerGraveyard();

        // 初始刷新
        RefreshDeckUI();
        RefreshGraveyardUI();
    }

    private void OnDestroy()
    {
        if (playerDeck != null)
        {
            playerDeck.OnDeckChanged -= RefreshDeckUI;
        }

        if (GraveyardManager.Instance != null)
        {
            GraveyardManager.Instance.OnPlayerGraveyardUpdated -= RefreshGraveyardUI;
        }
    }

    /// <summary>
    /// 刷新牌组的 UI 显示
    /// </summary>
    private void RefreshDeckUI()
    {
        // 清空现有的条目
        foreach (Transform child in deckEntriesParent)
        {
            Destroy(child.gameObject);
        }

        int totalCardCount = 0;

        // 创建新的条目
        foreach (var entry in playerDeck.entries)
        {
            if (entry.unitData != null)
            {
                // 正常状态的卡片
                if (entry.quantity > 0)
                {
                    CreateDeckEntryUI(entry.unitData, entry.quantity, false);
                    totalCardCount += entry.quantity;
                }

                // 负伤状态的卡片
                if (entry.injuredQuantity > 0)
                {
                    CreateDeckEntryUI(entry.unitData, entry.injuredQuantity, true);
                    totalCardCount += entry.injuredQuantity;
                }
            }
        }

        // 更新总卡牌数
        if (totalCardCountText != null)
            totalCardCountText.text = $"{totalCardCount}";
    }
    
    /// <summary>
    /// 刷新墓地的 UI 显示
    /// </summary>
    private void RefreshGraveyardUI()
    {
        graveyardCards = GraveyardManager.Instance.GetPlayerGraveyard();

        // 更新墓地卡牌数显示
        if (graveyardCountText != null)
            graveyardCountText.text = graveyardCards.Count.ToString();
    }
    
    private void CreateDeckEntryUI(UnitData unitData, int quantity, bool isInjured)
    {
        GameObject entryGO = Instantiate(deckEntryPrefab, deckEntriesParent);
        DeckEntryUI entryUI = entryGO.GetComponent<DeckEntryUI>();
        if (entryUI != null)
        {
            entryUI.Setup(unitData, quantity, isInjured);
        }
        else
        {
            Debug.LogError("DeckUI: DeckEntryPrefab 没有 DeckEntryUI 组件！");
        }
    }

    /// <summary>
    /// 添加卡牌到墓地
    /// </summary>
    public void AddToGraveyard(UnitData unitData)
    {
        if (unitData == null)
            return;

        // 更新墓地卡牌数显示
        if (graveyardCountText != null)
            graveyardCountText.text = graveyardCards.Count.ToString();
    }

    /// <summary>
    /// 获取墓地卡牌详情
    /// </summary>
    public string GetGraveyardDetails()
    {
        string details = "墓地中的卡片：\n";
        foreach (var unit in graveyardCards)
        {
            details += $"{unit.unitName}\n";
        }
        return details;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 显示墓地卡片列表
        if (eventData.pointerEnter == graveyardCountText.gameObject)
        {
            if (graveyardTooltipPanel != null)
            {
                graveyardTooltipPanel.SetActive(true);
                graveyardTooltipText.text = GetGraveyardDetails();
                Debug.Log("显示墓地卡片列表");
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 隐藏墓地卡片列表
        if (eventData.pointerEnter == graveyardCountText.gameObject)
        {
            if (graveyardTooltipPanel != null)
            {
                graveyardTooltipPanel.SetActive(false);
                Debug.Log("隐藏墓地卡片列表");
            }
        }
    }
}
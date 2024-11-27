// DeckChoiceUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DeckChoiceUI : MonoBehaviour
{
    public static DeckChoiceUI Instance;

    [Header("UI Elements")]
    public GameObject choicePanel; // 整個選擇面板
    public List<Button> choiceButtons; // 三個選擇按鈕
    public Button closeButton; // 關閉按鈕（可選）

    [Header("Button Components")]
    public List<Image> choiceImages; // 按鈕上的圖片
    public List<TextMeshProUGUI> choiceTexts; // 按鈕上的文字

    [Header("Toggle Button")]
    public Button toggleButton; // 開關按鈕
    public Sprite openIcon; // 選單打開時的圖標
    public Sprite closeIcon; // 選單關閉時的圖標

    private Deck playerDeck; // 玩家牌組

    // 新增事件
    public event Action OnChoiceMade;

    private List<UnitData> currentChoices = new List<UnitData>();
    private bool isPanelOpen = false; // 追踪選單的狀態

    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 如果需要跨場景存在
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 確保面板初始時為隱藏
        choicePanel.SetActive(false);
        isPanelOpen = false;
        toggleButton.gameObject.SetActive(false);

        // 設置選擇按鈕事件
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            int index = i; // 避免閉包問題
            choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
        }

        // 設置關閉按鈕事件（如果有）
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => CloseChoicePanel(toggleButton));
        }

        // 設置 Toggle Button 事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleChoicePanel);
            // 初始化 Toggle Button 的圖標
            UpdateToggleButtonIcon();
        }
        else
        {
            Debug.LogError("DeckChoiceUI: Toggle Button 未設置！");
        }

        // 獲取玩家牌組
        if (DeckManager.Instance != null)
        {
            playerDeck = DeckManager.Instance.playerDeck; // 假設有 DeckManager 管理牌組
        }
        else
        {
            Debug.LogError("DeckChoiceUI: DeckManager 實例未找到！");
        }
    }

    /// <summary>
    /// 顯示選擇面板，並隨機選取三個 UnitData 作為選項
    /// </summary>
    public void ShowChoicePanel()
    {
        if (playerDeck == null)
        {
            Debug.LogError("DeckChoiceUI: 玩家牌組未設置！");
            return;
        }

        // 只有當 currentChoices 為空時才生成新的選項
        if (currentChoices.Count == 0)
        {
            currentChoices = playerDeck.GetRandomUnitChoices(3);
            if (currentChoices.Count == 0)
            {
                Debug.LogWarning("DeckChoiceUI: 玩家牌組中沒有可選的 UnitData！");
                return;
            }

            // 設置按鈕顯示
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (i < currentChoices.Count)
                {
                    choiceImages[i].sprite = currentChoices[i].unitSprite;
                    choiceTexts[i].text = currentChoices[i].unitName;
                    choiceButtons[i].interactable = true;
                }
                else
                {
                    choiceImages[i].sprite = null;
                    choiceTexts[i].text = "";
                    choiceButtons[i].interactable = false;
                }
            }
        }

        // 顯示面板
        choicePanel.SetActive(true);
        isPanelOpen = true;
        toggleButton.gameObject.SetActive(true);
        UpdateToggleButtonIcon();
    }

    /// <summary>
    /// 隱藏選擇面板
    /// </summary>
    public void CloseChoicePanel()
    {
        choicePanel.SetActive(false);
        isPanelOpen = false;
        UpdateToggleButtonIcon();
    }

    /// <summary>
    /// 隱藏選擇面板並重置選項
    /// </summary>
    /// <param name="_toggleButton">Toggle Button 引用</param>
    public void CloseChoicePanel(Button _toggleButton)
    {
        OnChoiceMade?.Invoke();
        Debug.Log("DeckChoiceUI: 玩家選擇完成！");

        choicePanel.SetActive(false);
        isPanelOpen = false;
        UpdateToggleButtonIcon();
        _toggleButton.gameObject.SetActive(false);

        // 清空選項以便下次生成新的選項
        currentChoices.Clear();
    }

    /// <summary>
    /// 切換選擇面板的顯示狀態
    /// </summary>
    private void ToggleChoicePanel()
    {
        if (isPanelOpen)
        {
            CloseChoicePanel();
        }
        else
        {
            ShowChoicePanel();
        }
    }

    /// <summary>
    /// 更新 Toggle Button 的圖標根據選單的狀態
    /// </summary>
    private void UpdateToggleButtonIcon()
    {
        if (toggleButton == null)
            return;

        Image toggleButtonImage = toggleButton.GetComponent<Image>();
        if (toggleButtonImage != null)
        {
            toggleButtonImage.sprite = isPanelOpen ? openIcon : closeIcon;
        }
        else
        {
            Debug.LogError("DeckChoiceUI: Toggle Button 沒有 Image 組件！");
        }
    }

    /// <summary>
    /// 處理玩家的選擇
    /// </summary>
    /// <param name="index">選擇的按鈕索引</param>
    private void OnChoiceSelected(int index)
    {
        if (index >= currentChoices.Count)
        {
            Debug.LogWarning("DeckChoiceUI: 選擇的索引超出範圍！");
            return;
        }

        UnitData selectedUnit = currentChoices[index];
        playerDeck.IncreaseUnitQuantity(selectedUnit, 1); // 增加一張

        Debug.Log($"DeckChoiceUI: 玩家選擇增加 {selectedUnit.unitName} 的數量");

        // 關閉面板並重置選項
        CloseChoicePanel(toggleButton);
    }
}

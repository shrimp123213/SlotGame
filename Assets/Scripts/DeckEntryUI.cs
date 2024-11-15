// DeckEntryUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class DeckEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image unitImage;
    public TextMeshProUGUI quantityText;
    public GameObject injuredIcon; // 负伤标识

    // 悬停提示面板
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    private UnitData unitData;
    private int quantity;
    private bool isInjured;

    public void Setup(UnitData unitData, int quantity, bool isInjured)
    {
        this.unitData = unitData;
        this.quantity = quantity;
        this.isInjured = isInjured;

        if (unitImage != null)
        {
            unitImage.sprite = unitData.unitSprite;
        }

        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
        }

        if (injuredIcon != null)
        {
            injuredIcon.SetActive(isInjured);
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 显示悬停提示
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = GetUnitDetails();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 隐藏悬停提示
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private string GetUnitDetails()
    {
        // 根据 unitData 获取详细信息
        string status = isInjured ? "Injured" : "";
        return $"{unitData.unitName}\nHP:{unitData.maxHealth}\n{status}\n";
    }
}

// DeckEntryUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckEntryUI : MonoBehaviour
{
    public Image unitImage;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI quantityText;

    /// <summary>
    /// 設置牌組條目的 UI 元素
    /// </summary>
    /// <param name="unitData">UnitData</param>
    /// <param name="quantity">數量</param>
    public void Setup(UnitData unitData, int quantity)
    {
        if (unitImage != null)
        {
            unitImage.sprite = unitData.unitSprite;
        }

        if (unitNameText != null)
        {
            unitNameText.text = unitData.unitName;
        }

        if (quantityText != null)
        {
            quantityText.text = $"x{quantity}";
        }
    }
}
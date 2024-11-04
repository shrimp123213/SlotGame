using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item")]
public class ItemData : BaseData
{
    public ItemType itemType;          // 道具類型
    public int effectValue;            // 效果數值
    public string effectDescription;   // 效果描述
}
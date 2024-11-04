using UnityEngine;

public abstract class BaseData : ScriptableObject
{
    public string dataName;        // 名稱
    public Sprite icon;            // 圖標
    public Rarity rarity;          // 稀有度
    public int price;              // 價格
    public string description;     // 描述
}
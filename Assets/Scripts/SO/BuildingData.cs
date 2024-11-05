using UnityEngine;

/// <summary>
/// 建築的數據定義
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "ScriptableObjects/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;        // 建築名稱
    public string description;         // 建築描述
    public Sprite buildingSprite;      // 建築圖片
    public BuildingType buildingType;  // 建築類型
    public int health;                 // 建築生命值
    // 其他屬性如加成、攻擊等可根據需求添加
}

public enum BuildingType
{
    Wall,
    Resource,
    // 其他建築類型...
}
using UnityEngine;

/// <summary>
/// 建築的數據定義
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "ScriptableObjects/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;    // 建筑物名称
    public Sprite buildingSprite;  // 建筑物的Sprite
    public int health;             // 建筑物生命值
    // 可以根据需要添加更多属性，如建筑物类型、功能等
}

public enum BuildingType
{
    Wall,
    Resource,
    // 其他建築類型...
}
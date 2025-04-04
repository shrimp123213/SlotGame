using UnityEngine;

/// <summary>
/// 建築的數據定義
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "ScriptableObjects/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;         // 建筑物名称
    public Sprite buildingSprite;       // 建筑物的Sprite
    public int maxHealth;               // 建筑物的最大生命值

    [Header("Skills")]
    public SkillSO actionSkillSO;       // 建筑物的其他行动技能（如攻击）

    [Header("Camp")]
    public Camp camp;                   // 建筑物的阵营（玩家或敌人）
    
    //[Header("Prefab")]
    //public GameObject buildingPrefab;   // 建筑物的预制件

    // 可以根据需要添加更多属性，如建筑物类型、功能等
}
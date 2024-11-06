using UnityEngine;

/// <summary>
/// 單位的數據定義
/// </summary>
[CreateAssetMenu(fileName = "NewUnit", menuName = "ScriptableObjects/Unit")]
public class UnitData : ScriptableObject
{
    public string unitName;               // 单位名称
    public Sprite unitSprite;             // 单位的Sprite
    public int maxHealth;                 // 单位的最大生命值
    public Camp camp;                     // 单位的阵营（玩家或敌人）

    [Header("Skills")]
    public SkillSO mainSkillSO;           // 主技能
    public SkillSO supportSkillSO;        // 支援技能

    // 可以根据需要添加更多属性，如速度、攻击范围等
}

public enum Camp
{
    Player,
    Enemy
}
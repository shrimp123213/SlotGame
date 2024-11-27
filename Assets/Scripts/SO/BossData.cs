using UnityEngine;

[CreateAssetMenu(fileName = "NewBoss", menuName = "ScriptableObjects/Boss")]
public class BossData : ScriptableObject
{
    public string bossName;
    public Sprite bossSprite;
    public int maxHealth;
    
    [Header("Skills")]
    public SkillSO bossSkillSO;
    
    public Camp camp; // 玩家或敌人
    // 可以根据需要添加其他属性
}
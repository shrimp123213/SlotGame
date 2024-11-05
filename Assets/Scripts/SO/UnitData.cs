using UnityEngine;

/// <summary>
/// 單位的數據定義
/// </summary>
[CreateAssetMenu(fileName = "NewUnit", menuName = "ScriptableObjects/Unit")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public Sprite unitSprite;
    public int health;
    public Camp camp; // 使用枚舉
}

public enum Camp
{
    Player,
    Enemy
}
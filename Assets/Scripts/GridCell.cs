using UnityEngine;

/// <summary>
/// 表示棋盤上的一個格子
/// </summary>
public class GridCell
{
    /// <summary>
    /// 格子的座標位置
    /// </summary>
    public Vector2Int Position { get; private set; }

    /// <summary>
    /// 該格子上所佔據的角色，可能為null
    /// </summary>
    public CharacterBase OccupiedCharacter { get; set; }

    /// <summary>
    /// 構造函數，初始化格子的座標
    /// </summary>
    /// <param name="x">行號</param>
    /// <param name="y">列號</param>
    public GridCell(int x, int y)
    {
        Position = new Vector2Int(x, y);
        OccupiedCharacter = null;
    }
}

/// <summary>
/// 角色的基類，包含基本屬性
/// </summary>
public abstract class CharacterBase
{
    /// <summary>
    /// 角色是否屬於玩家
    /// </summary>
    public bool IsPlayerOwned { get; protected set; }

    /// <summary>
    /// 角色是否已經參與連線
    /// </summary>
    public bool IsLinked { get; set; }

    /// <summary>
    /// 構造函數，初始化角色的擁有者
    /// </summary>
    /// <param name="isPlayerOwned">是否屬於玩家</param>
    protected CharacterBase(bool isPlayerOwned)
    {
        IsPlayerOwned = isPlayerOwned;
        IsLinked = false;
    }
}

public class PlayerCharacter : CharacterBase
{
    public PlayerCharacter() : base(true)
    {
    }
}

public class EnemyCharacter : CharacterBase
{
    public EnemyCharacter() : base(false)
    {
    }
}

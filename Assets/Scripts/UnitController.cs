using UnityEngine;

/// <summary>
/// 控制单位的行为
/// </summary>
public class UnitController : MonoBehaviour
{
    public UnitData unitData;       // 单位的数据

    private Vector3Int gridPosition; // 单位在格子上的位置

    void Start()
    {
        // 初始化单位属性
        InitializeUnit();
    }

    /// <summary>
    /// 初始化单位
    /// </summary>
    void InitializeUnit()
    {
        // 设置单位的图片
        if (unitData.unitSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = unitData.unitSprite;
        }

        // 根据单位数据设置其他属性
        // 例如，设置速度、攻击范围等
    }

    /// <summary>
    /// 设置单位的位置，将单位放置在格子的中心
    /// </summary>
    /// <param name="position">格子位置</param>
    public void SetPosition(Vector3Int position)
    {
        gridPosition = position;

        // 计算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(position);

        transform.position = cellWorldPosition;

        Debug.Log($"单位 {unitData.unitName} 放置在格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 移动单位到新的位置
    /// </summary>
    /// <param name="newPosition">新的格子位置</param>
    public void MoveTo(Vector3Int newPosition)
    {
        gridPosition = newPosition;

        // 计算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(newPosition);

        transform.position = cellWorldPosition;

        Debug.Log($"单位 {unitData.unitName} 移动到格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        unitData.health -= damage;
        Debug.Log($"单位 {unitData.unitName} 接受 {damage} 点伤害，当前生命值: {unitData.health}");

        if (unitData.health <= 0)
        {
            DestroyUnit();
        }
    }

    /// <summary>
    /// 销毁单位
    /// </summary>
    void DestroyUnit()
    {
        // 根据需求，可以添加单位销毁的动画或效果
        Debug.Log($"单位 {unitData.unitName} 被销毁");
        Destroy(gameObject);
        GridManager.Instance.RemoveUnitAt(gridPosition);
    }

    // 其他行为如攻击、使用技能等可根据需求添加
}

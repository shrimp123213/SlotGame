using UnityEngine;

/// <summary>
/// 控制單位的行為
/// </summary>
public class UnitController : MonoBehaviour
{
    public UnitData unitData;       // 單位的數據

    [SerializeField]
    private Vector3Int gridPosition; // 單位在格子上的位置

    void Start()
    {
        // 初始化單位屬性
        InitializeUnit();
    }

    /// <summary>
    /// 初始化單位
    /// </summary>
    void InitializeUnit()
    {
        // 設置單位的圖片
        if (unitData.unitSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = unitData.unitSprite;
        }

        // 根據單位數據設置其他屬性
        // 例如，設置速度、攻擊範圍等
    }

    /// <summary>
    /// 設置單位的位置，將單位放置在格子的中心
    /// </summary>
    /// <param name="position">格子位置</param>
    public void SetPosition(Vector3Int position)
    {
        gridPosition = position;

        // 計算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(position);

        transform.position = cellWorldPosition;

        //Debug.Log($"單位 {unitData.unitName} 放置在格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 移動單位到新的位置
    /// </summary>
    /// <param name="newPosition">新的格子位置</param>
    public void MoveTo(Vector3Int newPosition)
    {
        gridPosition = newPosition;

        // 計算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(newPosition);

        transform.position = cellWorldPosition;

        Debug.Log($"單位 {unitData.unitName} 移動到格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 接受傷害
    /// </summary>
    /// <param name="damage">傷害值</param>
    public void TakeDamage(int damage)
    {
        unitData.health -= damage;
        Debug.Log($"單位 {unitData.unitName} 接受 {damage} 點傷害，當前生命值: {unitData.health}");

        if (unitData.health <= 0)
        {
            DestroyUnit();
        }
    }

    /// <summary>
    /// 銷毀單位
    /// </summary>
    void DestroyUnit()
    {
        // 根據需求，可以添加單位銷毀的動畫或效果
        Debug.Log($"單位 {unitData.unitName} 被銷毀");
        Destroy(gameObject);
        GridManager.Instance.RemoveUnitAt(gridPosition);
    }

    // 其他行為如攻擊、使用技能等可根據需求添加
}

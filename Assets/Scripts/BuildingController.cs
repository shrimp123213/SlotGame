using UnityEngine;

/// <summary>
/// 控制建筑物的行为
/// </summary>
public class BuildingController : MonoBehaviour
{
    public BuildingData buildingData; // 建筑物的数据

    private Vector3Int gridPosition;  // 建筑物在格子上的位置

    void Start()
    {
        InitializeBuilding();
    }

    /// <summary>
    /// 初始化建筑物
    /// </summary>
    void InitializeBuilding()
    {
        // 设置建筑物的图片或其他属性
        if (buildingData.buildingSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = buildingData.buildingSprite;
        }

        // 根据建筑物数据设置其他属性
        // 例如，生命值、功能等
    }

    /// <summary>
    /// 设置建筑物的位置，将建筑物放置在格子的中心
    /// </summary>
    /// <param name="position">格子位置</param>
    public void SetPosition(Vector3Int position)
    {
        gridPosition = position;

        // 计算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(position);

        transform.position = cellWorldPosition;

        Debug.Log($"建筑物 {buildingData.buildingName} 放置在格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        buildingData.health -= damage;
        Debug.Log($"建筑物 {buildingData.buildingName} 接受 {damage} 点伤害，当前生命值: {buildingData.health}");

        if (buildingData.health <= 0)
        {
            DestroyBuilding();
        }
    }

    /// <summary>
    /// 销毁建筑物
    /// </summary>
    void DestroyBuilding()
    {
        // 根据需求，可以添加建筑物销毁的动画或效果
        Debug.Log($"建筑物 {buildingData.buildingName} 被摧毁");
        Destroy(gameObject);
        GridManager.Instance.RemoveBuildingAt(gridPosition);
    }

    // 其他行为如建筑物的功能实现等可根据需求添加
}
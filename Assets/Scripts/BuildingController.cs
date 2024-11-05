using UnityEngine;

/// <summary>
/// 控制建築的行為
/// </summary>
public class BuildingController : MonoBehaviour
{
    public BuildingData buildingData; // 建築的數據

    private Vector3Int gridPosition;   // 建築在格子上的位置
    private int currentHealth;        // 當前生命值

    void Start()
    {
        InitializeBuilding();
    }

    /// <summary>
    /// 初始化建築
    /// </summary>
    void InitializeBuilding()
    {
        // 設置建築的圖片
        if (buildingData.buildingSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = buildingData.buildingSprite;
        }

        // 設置建築的生命值
        currentHealth = buildingData.health;
    }

    /// <summary>
    /// 設置建築的位置，將建築放置在格子的中心
    /// </summary>
    /// <param name="position">格子位置</param>
    public void SetPosition(Vector3Int position)
    {
        gridPosition = position;

        // 計算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(position);

        transform.position = cellWorldPosition;
    }

    /// <summary>
    /// 接受傷害
    /// </summary>
    /// <param name="damage">傷害值</param>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    /// <summary>
    /// 銷毀建築
    /// </summary>
    void DestroyBuilding()
    {
        // 根據需求，可以添加建築破壞的動畫或效果
        Destroy(gameObject);
    }

    // 其他行為如回復生命、提供加成等可根據需求添加
}
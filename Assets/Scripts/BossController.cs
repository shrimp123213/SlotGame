using UnityEngine;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    public BossData bossData;
    public int currentHealth;

    public Image healthBar;       // 生命值条的填充部分
    public Image healthBarFrame;  // 生命值条的框架

    private void Start()
    {
        InitializeBoss();
        InitializeHealthBar();
    }

    private void InitializeBoss()
    {
        // 设置当前生命值为最大生命值
        currentHealth = bossData.maxHealth;

        // 设置 BOSS 的精灵
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && bossData.bossSprite != null)
        {
            spriteRenderer.sprite = bossData.bossSprite;
        }
        else
        {
            Debug.LogWarning("BossController: 未设置 SpriteRenderer 或 bossSprite。");
        }

        // 初始化生命值条
        UpdateHealthBar();
    }
    
    private void InitializeHealthBar()
    {
        // 从 Resources 文件夹加载生命值条预制件
        GameObject healthBarPrefab = Resources.Load<GameObject>("HealthBarPrefab");
        if (healthBarPrefab != null)
        {
            // 实例化生命值条预制件，作为建筑物的子对象
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
            healthBar = healthBarInstance.transform.Find("HealthBarFill").GetComponent<Image>();
            healthBarFrame = healthBarInstance.transform.Find("HealthBarFrame").GetComponent<Image>();

            // 设置生命值条的位置
            RectTransform rt = healthBarInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -128); // 根据需要调整位置
        }
        else
        {
            Debug.LogWarning("BuildingController: 在 Resources 中未找到 HealthBarPrefab。");
        }

        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            OnBossDefeated();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercent = (float)currentHealth / bossData.maxHealth;
            healthBar.fillAmount = healthPercent;
        }
    }

    private void OnBossDefeated()
    {
        // 处理 BOSS 被击败的逻辑
        Debug.Log($"{bossData.bossName} 被击败了！");

        // 通知 BattleManager 或 GameManager
        BattleManager.Instance.OnBossDefeated(this);
    }
}
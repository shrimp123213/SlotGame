using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BossController : MonoBehaviour, ISkillUser
{
    public BossData bossData;
    public int currentHealth;
    public int maxHealth;
    
    public Vector3Int gridPosition; // 单位在格子上的位置
    
    public Skill currentSkill;

    public Image healthBar;       // 生命值条的填充部分
    public Image healthBarFrame;  // 生命值条的框架
    
    private SpriteRenderer spriteRenderer;
    
    // 添加对 Sprite 子对象的引用
    private Transform spriteTransform;

    private void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        InitializeBoss();
        InitializeHealthBar();
        InitializeUnitSprite();
        
        // 如果 BOSS 有初始技能，可以在这里赋值
        //if (bossData.initialSkill != null)
        {
            //currentSkill = Skill.FromSkillSO(bossData.initialSkill);
        }
    }

    private void InitializeBoss()
    {
        // 设置当前生命值为最大生命值
        currentHealth = bossData.maxHealth;
        maxHealth = bossData.maxHealth;

        // 设置 BOSS 的精灵
        if (spriteRenderer != null && bossData.bossSprite != null)
        {
            spriteRenderer.sprite = bossData.bossSprite;
        }
        else
        {
            Debug.LogWarning("BossController: 未设置 SpriteRenderer 或 bossSprite。");
        }
        
        if(spriteRenderer != null)
        {
            spriteTransform = spriteRenderer.transform;
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
            rt.anchoredPosition = new Vector2(0, -1.1f); // 根据需要调整位置
            rt.localScale = Vector3.one * 0.01f;
        }
        else
        {
            Debug.LogWarning("BuildingController: 在 Resources 中未找到 HealthBarPrefab。");
        }

        UpdateHealthBar();
    }
    
    public void SetPosition(Vector3Int position)
    {
        gridPosition = position;

        // 计算格子中心的位置
        Vector3 cellWorldPosition = GridManager.Instance.GetCellCenterWorld(position);

        transform.position = cellWorldPosition;

        //Debug.Log($"UnitController: 单位 {unitData.unitName} 放置在格子中心: {cellWorldPosition}");
    }

    public bool CanMoveForward()
    {
        return false;
    }

    public void MoveForward()
    {
        
    }

    public void PerformMeleeAttack(TargetType targetType)
    {
        
    }

    public void PerformRangedAttack(TargetType targetType)
    {
        
    }

    public void IncreaseDefense(int value, TargetType targetType)
    {
        
    }

    public void TakeDamage(int damage)
    {
        PlayHitAnimation(() =>
        {
            currentHealth -= damage;
            Debug.Log($"BossController: BOSS {bossData.bossName} 受到 {damage} 点伤害，当前生命值：{currentHealth}");
            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            UpdateHealthBar();

            if (currentHealth <= 0)
            {
                OnBossDefeated();
            }
        });
    }
    
    /// <summary>
    /// 播放受击动画
    /// </summary>
    public void PlayHitAnimation(Action onComplete = null)
    {
        if (spriteTransform == null)
        {
            Debug.LogWarning("UnitController: spriteTransform 未设置，无法播放受击动画！");
            onComplete?.Invoke();
            return;
        }

        float moveDistance = 0.2f; // 向后移动的距离
        float animationDuration = 0.1f; // 动画持续时间

        // 计算移动方向
        Vector3 direction = bossData.camp == Camp.Player ? Vector3.left : Vector3.right;

        // 停止当前动画
        spriteTransform.DOKill();

        // 动画序列
        Sequence hitSequence = DOTween.Sequence();

        // 向后移动
        hitSequence.Append(spriteTransform.DOMove(spriteTransform.position + direction * moveDistance, animationDuration));

        // 返回原位
        hitSequence.Append(spriteTransform.DOMove(spriteTransform.position, animationDuration));

        // 动画完成回调
        if (onComplete != null)
        {
            hitSequence.OnComplete(() => onComplete());
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"BossController: BOSS {bossData.bossName} 恢复了 {amount} 点生命值，当前生命值：{currentHealth}");
        UpdateHealthBar();
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
    
    public void ExecuteSkill()
    {
        if (currentSkill != null)
        {
            // 执行技能逻辑
            // 类似于 UnitController 中的 ExecuteCurrentSkill
        }
    }
    
    public void InitializeUnitSprite()
    {
        // 设置单位的图片
        if (bossData.bossSprite != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = bossData.bossSprite;
            }
        }

        // 设置朝向
        var scale = spriteTransform.localScale;
        scale.x = bossData.camp == Camp.Player ? 1 : -1;
        spriteTransform.localScale = scale;
    }
}
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class BossController : MonoBehaviour, ISkillUser
{
    public BossData bossData;
    public int currentHealth;
    public int maxHealth;
    public int defensePoints;
    
    public Vector3Int gridPosition; // 单位在格子上的位置
    
    public Skill currentSkill;

    public Image healthBar;       // 生命值条的填充部分
    public Image healthBarFrame;  // 生命值条的框架
    
    private SpriteRenderer spriteRenderer;
    
    // 添加对 Sprite 子对象的引用
    private Transform spriteTransform;
    
    private bool isDead = false;
    public bool IsDead => isDead;

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

    public IEnumerator MoveForward()
    {
        yield break;
    }

    public IEnumerator PerformMeleeAttack(TargetType targetType)
    {
        // 执行近战攻击动画
        yield return StartCoroutine(PlayMeleeAttackAnimation());

        // 执行攻击逻辑
        PerformMeleeAttackLogic(targetType);
    }

    public IEnumerator PerformRangedAttack(TargetType targetType)
    {
        // 执行远程攻击动画
        yield return StartCoroutine(PlayRangedAttackAnimation());

        // 执行攻击逻辑
        PerformRangedAttackLogic(targetType);
    }

    public IEnumerator IncreaseDefense(int value, TargetType targetType)
    {
        if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            // 执行防御增加动画
            yield return StartCoroutine(PlayIncreaseDefenseAnimation());

            defensePoints += value;
            Debug.Log($"BossController: Boss {bossData.bossName} 防御点数增加 {value}，当前防御点数：{defensePoints}");
        }
        else
        {
            Debug.LogWarning($"BossController: Boss {bossData.bossName} 尝试对非友方进行防卫！");
        }
    }

    public IEnumerator PerformBreakage(int breakagePoints)
    {
        // 执行破壞动画
        yield return StartCoroutine(PlayBreakageAnimation());

        // 执行破壞逻辑
        PerformBreakageLogic(breakagePoints);
    }
    
    // 示例动画协程
    private IEnumerator PlayMeleeAttackAnimation()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("MeleeAttack");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator PlayRangedAttackAnimation()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("RangedAttack");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator PlayIncreaseDefenseAnimation()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("IncreaseDefense");
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator PlayBreakageAnimation()
    {
        // 假设有一个破壞动画
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Breakage");
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return null;
        }
    }
    
    // 实现动作逻辑
    private void PerformMeleeAttackLogic(TargetType targetType)
    {
        // 近战攻击逻辑
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = bossData.camp == Camp.Player ? Vector3Int.left : Vector3Int.right;
            Vector3Int targetPosition = gridPosition + attackDirection;

            UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

            if (targetUnit != null && targetUnit.unitData.camp != bossData.camp)
            {
                targetUnit.TakeDamage(1);
                Debug.Log($"BossController: BOSS {bossData.bossName} 对 {targetUnit.unitData.unitName} 进行近战攻击，造成1点伤害！");
            }
            else if (targetBuilding != null && targetBuilding.buildingData.camp != bossData.camp && !targetBuilding.isRuin)
            {
                targetBuilding.TakeDamage(1);
                Debug.Log($"BossController: BOSS {bossData.bossName} 对建筑物 {targetBuilding.buildingData.buildingName} 进行近战攻击，造成1点伤害！");
            }
            else
            {
                Debug.Log($"BossController: BOSS {bossData.bossName} 近战攻击无目标或目标为友方！");
            }
        }
    }

    private void PerformRangedAttackLogic(TargetType targetType)
    {
        // 远程攻击逻辑
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = bossData.camp == Camp.Player ? Vector3Int.left : Vector3Int.right;
            Vector3Int currentPos = gridPosition + attackDirection;

            bool hasAttacked = false;

            while (GridManager.Instance.IsWithinBattleArea(currentPos))
            {
                UnitController targetUnit = GridManager.Instance.GetUnitAt(currentPos);
                BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(currentPos);

                if (targetUnit != null && targetUnit.unitData.camp != bossData.camp)
                {
                    targetUnit.TakeDamage(1);
                    Debug.Log($"BossController: BOSS {bossData.bossName} 对 {targetUnit.name} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }
                else if (targetBuilding != null && targetBuilding.buildingData.camp != bossData.camp && !targetBuilding.isRuin)
                {
                    targetBuilding.TakeDamage(1);
                    Debug.Log($"BossController: BOSS {bossData.bossName} 对建筑物 {targetBuilding.name} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }

                currentPos += attackDirection;
            }

            if (!hasAttacked)
            {
                Debug.Log($"BossController: BOSS {bossData.bossName} 远程攻击无目标或目标为友方！");
            }
        }
    }

    private void IncreaseDefenseLogic(int value, TargetType targetType)
    {
        if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            defensePoints += value;
            Debug.Log($"BossController: Boss {bossData.bossName} 防御点数增加 {value}，当前防御点数：{defensePoints}");
        }
        else
        {
            Debug.LogWarning($"BossController: Boss {bossData.bossName} 尝试对非友方进行防卫！");
        }
    }
    
    private void PerformBreakageLogic(int breakagePoints)
    {
        // 破壞逻辑已经在 PerformBreakage 方法中实现
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="source">伤害来源</param>
    public virtual void TakeDamage(int damage, DamageSource source = DamageSource.Normal)
    {
        if (isDead)
            return;
        
        StartCoroutine(HandleTakeDamage(damage, source));
        /*PlayHitAnimation(() =>
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
        });*/
    }
    
    private IEnumerator HandleTakeDamage(int damage, DamageSource source = DamageSource.Normal)
    {
        // 执行受击动画
        yield return StartCoroutine(PlayHitAnimation());

        // 首先扣除防御点数
        int remainingDamage = damage - defensePoints;
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            Debug.Log($"BossController: BOSS {bossData.bossName} 接受 {remainingDamage} 点伤害，当前生命值: {currentHealth}");
        }
        else
        {
            defensePoints -= damage;
            Debug.Log($"BossController: BOSS {bossData.bossName} 防御点数抵消了 {damage} 点伤害，剩余防御点数: {defensePoints}");
        }

        // 更新生命值条
        UpdateHealthBar();

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnBossDefeated();
        }
    }
    
    /// <summary>
    /// 播放受击动画
    /// </summary>
    public IEnumerator PlayHitAnimation(Action onComplete = null)
    {
        if (spriteTransform == null)
        {
            Debug.LogWarning("UnitController: spriteTransform 未设置，无法播放受击动画！");
            onComplete?.Invoke();
            yield break;
        }

        float moveDistance = 0.2f; // 向后移动的距离
        float animationDuration = 0.1f; // 动画持续时间

        // 计算移动方向
        Vector3 direction = bossData.camp == Camp.Player ? Vector3.left : Vector3.right;

        // 停止当前动画
        spriteTransform.DOKill();

        // 动画序列
        Sequence hitSequence = DOTween.Sequence();

        // 向后移动（使用本地坐标）
        hitSequence.Append(spriteTransform.DOLocalMove(spriteTransform.localPosition + direction * moveDistance, animationDuration));

        // 返回原位（使用本地坐标）
        hitSequence.Append(spriteTransform.DOLocalMove(Vector3.zero, animationDuration)); // 假设原位为本地坐标的 (0,0,0)

        // 动画完成回调
        if (onComplete != null)
        {
            hitSequence.OnComplete(() => onComplete());
        }
        
        // 等待动画完成
        yield return hitSequence.WaitForCompletion();
    }

    public void Heal(int amount)
    {
        if (isDead)
        {
            Debug.Log($"BossController: Boss {bossData.bossName} 已被摧毁，无法恢复生命值！");
            return;
        }
        
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
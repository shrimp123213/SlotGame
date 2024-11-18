using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制建筑物的行为
/// </summary>
public class BuildingController : MonoBehaviour, ISkillUser
{
    public BuildingData buildingData; // 建筑物的静态数据

    public Vector3Int gridPosition;   // 建筑物在格子上的位置

    public int currentHealth;        // 建筑物的当前生命值
    public int defensePoints = 0;    // 防卫点数
    
    public bool isRuin = false; // 是否是废墟状态
    public Sprite ruinSprite;   // 废墟状态的精灵
    
    public Image healthBar;       // 生命值条的填充部分
    public Image healthBarFrame;  // 生命值条的框架

    [HideInInspector]
    public Skill currentSkill;        // 当前技能的运行时实例

    void Start()
    {
        // 初始化建筑物属性
        InitializeBuilding();

        // 获取 SkillManager 引用
        if (SkillManager.Instance == null)
        {
            Debug.LogError("BuildingController: 未找到 SkillManager！");
        }

        // 初始化当前技能为行动技能的克隆
        if (buildingData.actionSkillSO != null)
        {
            currentSkill = Skill.FromSkillSO(buildingData.actionSkillSO);
        }
        else
        {
            Debug.LogWarning($"BuildingController: 建筑物 {buildingData.buildingName} 没有配置行动技能！");
        }

        // 初始化实例变量
        currentHealth = buildingData.maxHealth;

        // 调用初始化方法
        Init();
    }

    /// <summary>
    /// 初始化建筑物
    /// </summary>
    void InitializeBuilding()
    {
        // 设置建筑物的图片
        if (buildingData.buildingSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = buildingData.buildingSprite;
            }
            else
            {
                Debug.LogWarning("BuildingController: 未找到 SpriteRenderer 组件！");
            }
        }

        // 根据建筑物数据设置其他属性
        // 例如，设置防御值、功能等
    }

    /// <summary>
    /// 虚拟初始化方法，允许派生类重写
    /// </summary>
    public void Init()
    {
        // 初始化生命值条
        InitializeHealthBar();

        // 其他需要的初始化逻辑
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
            rt.anchoredPosition = new Vector2(0, -0.45f); // 根据需要调整位置
            rt.localScale = Vector3.one * 0.01f; // 根据需要调整大小
        }
        else
        {
            Debug.LogWarning("BuildingController: 在 Resources 中未找到 HealthBarPrefab。");
        }

        UpdateHealthBar();
    }
    
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercent = (float)currentHealth / buildingData.maxHealth;
            healthBar.fillAmount = healthPercent;
        }
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
    /// 检查建筑物是否可以执行行动
    /// </summary>
    /// <returns>是否可以执行行动</returns>
    public virtual bool CanExecuteAction()
    {
        // 定义建筑物是否可以执行行动的条件
        // 例如，是否有敌人接近，或者根据其他游戏机制
        // 这里以始终可以执行行动为例
        return true;
    }

    /// <summary>
    /// 执行建筑物的行动
    /// </summary>
    public virtual void ExecuteAction()
    {
        if (isRuin)
        {
            Debug.Log($"BuildingController: 废墟无法执行行动！");
            return;
        }
        
        if (CanExecuteAction())
        {
            if (buildingData.actionSkillSO != null)
            {
                // 初始化当前技能为行动技能的克隆
                currentSkill = Skill.FromSkillSO(buildingData.actionSkillSO);

                // 执行当前技能
                ExecuteCurrentSkill();

                Debug.Log($"建筑物 {buildingData.buildingName} 执行了行动技能！");
            }
            else
            {
                Debug.LogWarning($"建筑物 {buildingData.buildingName} 没有配置行动技能！");
            }
        }
        else
        {
            Debug.Log($"建筑物 {buildingData.buildingName} 无法执行行动！");
        }
    }

    /// <summary>
    /// 执行防卫效果
    /// </summary>
    public virtual void ExecuteDefense()
    {
        if (isRuin)
        {
            Debug.Log($"BuildingController: 废墟无法执行防卫技能！");
            return;
        }
        
        if (buildingData.defenseSkillSO != null)
        {
            // 初始化当前技能为防卫技能的克隆
            currentSkill = Skill.FromSkillSO(buildingData.defenseSkillSO);

            // 执行当前技能
            ExecuteCurrentSkill();

            Debug.Log($"建筑物 {buildingData.buildingName} 执行了防卫技能！");
        }
        else
        {
            Debug.LogWarning($"建筑物 {buildingData.buildingName} 没有配置防卫技能！");
        }
    }

    /// <summary>
    /// 执行近战攻击
    /// </summary>
    public virtual void PerformMeleeAttack(TargetType targetType)
    {
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
            Vector3Int targetPosition = gridPosition + attackDirection;

            UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

            if (targetUnit != null && targetUnit.unitData.camp != buildingData.camp)
            {
                targetUnit.TakeDamage(1);
                Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对 {targetUnit.unitData.unitName} 进行近战攻击，造成1点伤害！");
            }
            else if (targetBuilding != null && targetBuilding.buildingData.camp != buildingData.camp)
            {
                targetBuilding.TakeDamage(1);
                Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对建筑物 {targetBuilding.buildingData.buildingName} 进行近战攻击，造成1点伤害！");
            }
            else
            {
                Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 近战攻击无目标或目标为友方！");
            }
        }
        else if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            // 防卫技能，只对自身或友方生效
            //IncreaseDefense(1, targetType);
        }
        else
        {
            Debug.LogWarning($"BuildingController: 未处理的 TargetType：{targetType}");
        }
    }

    /// <summary>
    /// 执行远程攻击
    /// </summary>
    public virtual void PerformRangedAttack(TargetType targetType)
    {
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
            Vector3Int currentPos = gridPosition + attackDirection;

            bool hasAttacked = false;

            while (GridManager.Instance.IsWithinBattleArea(currentPos))
            {
                UnitController targetUnit = GridManager.Instance.GetUnitAt(currentPos);
                BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(currentPos);

                if (targetUnit != null && targetUnit.unitData.camp != buildingData.camp)
                {
                    targetUnit.TakeDamage(1);
                    Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对 {targetUnit.unitData.unitName} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }
                else if (targetBuilding != null && targetBuilding.buildingData.camp != buildingData.camp)
                {
                    targetBuilding.TakeDamage(1);
                    Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对建筑物 {targetBuilding.buildingData.buildingName} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }

                currentPos += attackDirection;
            }

            if (!hasAttacked)
            {
                Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 远程攻击无目标或目标为友方！");
            }
        }
        else if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            // 防卫技能，只对自身或友方生效
            //IncreaseDefense(1, targetType);
        }
        else
        {
            Debug.LogWarning($"BuildingController: 未处理的 TargetType：{targetType}");
        }
    }

    /// <summary>
    /// 增加防卫点数
    /// </summary>
    /// <param name="value">增加的防卫点数</param>
    /// <param name="targetType">目标类型</param>
    public virtual void IncreaseDefense(int value, TargetType targetType)
    {
        if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            defensePoints += value;
            Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 防卫点数增加 {value}，当前防卫点数：{defensePoints}");
        }
        else
        {
            Debug.LogWarning($"BuildingController: 建筑物 {buildingData.buildingName} 尝试对非友方进行防卫！");
        }
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public virtual void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 受到 {damage} 点伤害，当前生命值：{currentHealth}");

        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            DestroyBuilding();
            // 通知 GridManager 更新行的状态
            //int row = gridPosition.y;
            //GridManager.Instance.SetRowCanAttackBoss(row, true);

            Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 被摧毁，变为废墟！");
        }
    }

    /// <summary>
    /// 治疗建筑物
    /// </summary>
    /// <param name="amount">治疗量</param>
    public virtual void Heal(int amount)
    {
        if (buildingData.maxHealth > 0)
        {
            currentHealth = Mathf.Min(currentHealth + amount, buildingData.maxHealth);
        }
        else
        {
            currentHealth += amount;
        }

        UpdateHealthBar();
        Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 恢复了 {amount} 点生命值，当前生命值: {currentHealth}");
    }

    /// <summary>
    /// 销毁建筑物
    /// </summary>
    void DestroyBuilding()
    {
        if (!isRuin)
        {
            // 切换为废墟状态
            isRuin = true;

            // 更改建筑物的外观
            if (ruinSprite != null)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = ruinSprite;
                }
            }

            // 禁用建筑物的功能（如行动和防卫）
            // 可以设置一个标志位，或者根据 isRuin 状态在其他方法中判断

            // 通知 BattleManager，建筑物已变为废墟
            int row = gridPosition.y;
            GridManager.Instance.SetRowCanAttackBoss(row, true);
            //BattleManager.Instance.OnBuildingDestroyed(this, row);
        }
        /*else
        {
            // 已经是废墟状态，彻底销毁
            Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 的废墟被移除");
            GridManager.Instance.RemoveSkillUserAt(gridPosition);
            Destroy(gameObject);
        }*/
    }

    /// <summary>
    /// 使用行动技能
    /// </summary>
    public virtual void UseActionSkill()
    {
        if (buildingData.actionSkillSO != null)
        {
            buildingData.actionSkillSO.Execute(this);
        }
        else
        {
            Debug.LogWarning($"BuildingController: 建筑物 {buildingData.buildingName} 没有配置行动技能！");
        }
    }

    /// <summary>
    /// 使用防卫技能
    /// </summary>
    public virtual void UseDefenseSkill()
    {
        if (buildingData.defenseSkillSO != null)
        {
            buildingData.defenseSkillSO.Execute(this);
        }
        else
        {
            Debug.LogWarning($"BuildingController: 建筑物 {buildingData.buildingName} 没有配置防卫技能！");
        }
    }

    /// <summary>
    /// 重新执行当前技能
    /// </summary>
    public virtual void ExecuteCurrentSkill()
    {
        if (currentSkill != null)
        {
            // 执行当前技能的所有动作
            foreach (var action in currentSkill.Actions)
            {
                switch (action.Type)
                {
                    case SkillType.Move:
                        for (int i = 0; i < action.Value; i++)
                        {
                            if (!CanMoveForward())
                            {
                                Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 无法继续移动，技能执行被阻挡！");
                                break;
                            }
                            MoveForward();
                        }
                        break;
                    case SkillType.Melee:
                        PerformMeleeAttack(action.TargetType);
                        break;
                    case SkillType.Ranged:
                        PerformRangedAttack(action.TargetType);
                        break;
                    case SkillType.Defense:
                        //IncreaseDefense(action.Value, action.TargetType);
                        break;
                    default:
                        Debug.LogWarning($"BuildingController: 未处理的技能类型：{action.Type}");
                        break;
                }
            }

            // 清空当前技能动作，防止重复执行
            currentSkill.Actions.Clear();
        }
    }

    public virtual void MoveForward()
    {
        Debug.Log("建築物無法移動！");
    }

    public virtual bool CanMoveForward()
    {
        return false;
    }
}

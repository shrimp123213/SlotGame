using UnityEngine;

public class UnitController : MonoBehaviour, ISkillUser
{
    public UnitData unitData;       // 单位的静态数据
    public Vector3Int gridPosition; // 单位在格子上的位置

    [SerializeField]
    protected int currentHealth;      // 单位的当前生命值

    [SerializeField]
    private int defensePoints = 0;  // 防御点数

    public Skill currentSkill;       // 当前技能的运行时实例

    // 新增一個公共變量，用於引用子物件的 SpriteRenderer
    public SpriteRenderer spriteRenderer;


    void Awake()
    {
        // 如果沒有在 Inspector 中手動設置 spriteRenderer，則自動查找
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("UnitController: 未找到子物件的 SpriteRenderer！");
            }
        }
    }

    void Start()
    {
        // 初始化单位属性
        InitializeUnit();

        // 获取 SkillManager 引用
        if (SkillManager.Instance == null)
        {
            Debug.LogError("UnitController: 未找到 SkillManager！");
        }

        // 初始化当前技能为主技能的克隆
        if (unitData.mainSkillSO != null)
        {
            currentSkill = Skill.FromSkillSO(unitData.mainSkillSO);
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {unitData.unitName} 没有配置主技能！");
        }

        // 初始化实例变量
        currentHealth = unitData.maxHealth;

        // 调用初始化方法
        Init();
    }

    /// <summary>
    /// 初始化单位
    /// </summary>
    void InitializeUnit()
    {
        // 设置单位的图片
        if (unitData.unitSprite != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = unitData.unitSprite;
            }
        }

        // 根据单位数据设置其他属性
        // 例如，设置速度、攻击范围等
    }

    /// <summary>
    /// 虚拟初始化方法，允许派生类重写
    /// </summary>
    protected virtual void Init()
    {
        // 基类的初始化逻辑（如果有的话）
        var scale = spriteRenderer.GetComponent<Transform>().localScale;
        scale.x = unitData.camp == Camp.Player ? 1 : -1;
        spriteRenderer.GetComponent<Transform>().localScale = scale;
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

        Debug.Log($"UnitController: 单位 {unitData.unitName} 放置在格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 检查单位是否可以继续向前移动
    /// </summary>
    /// <returns>是否可以移动</returns>
    public virtual bool CanMoveForward()
    {
        Vector3Int direction = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int newPosition = gridPosition + direction;

        // 检查新位置是否在战斗区域
        if (!GridManager.Instance.IsWithinBattleArea(newPosition))
        {
            return false;
        }

        // 检查新位置是否被占据
        if (GridManager.Instance.HasSkillUserAt(newPosition))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 移动单位到前方一格
    /// </summary>
    public virtual void MoveForward()
    {
        Vector3Int direction = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int newPosition = gridPosition + direction;

        // 尝试通过 GridManager 移动单位
        bool moved = GridManager.Instance.MoveUnit(gridPosition, newPosition);
        if (moved)
        {
            gridPosition = newPosition;
            Debug.Log($"UnitController: 单位 {unitData.unitName} 向前移动到 {newPosition}");
        }
        else
        {
            Debug.Log($"UnitController: 单位 {unitData.unitName} 无法移动到 {newPosition}");
        }
    }

    /// <summary>
    /// 执行近战攻击
    /// </summary>
    public virtual void PerformMeleeAttack(TargetType targetType)
    {
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
            Vector3Int targetPosition = gridPosition + attackDirection;

            UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

            if (targetUnit != null && targetUnit.unitData.camp != unitData.camp)
            {
                targetUnit.TakeDamage(1);
                Debug.Log($"UnitController: 单位 {unitData.unitName} 对 {targetUnit.unitData.unitName} 进行近战攻击，造成1点伤害！");
            }
            else if (targetBuilding != null && targetBuilding.buildingData.camp != unitData.camp)
            {
                targetBuilding.TakeDamage(1);
                Debug.Log($"UnitController: 单位 {unitData.unitName} 对建筑 {targetBuilding.buildingData.buildingName} 进行近战攻击，造成1点伤害！");
            }
            else
            {
                Debug.Log($"UnitController: 单位 {unitData.unitName} 近战攻击无目标或目标为友方！");
            }
        }
        else if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            // 防卫技能，只对自身或友方生效
            IncreaseDefense(1, targetType);
        }
        else
        {
            Debug.LogWarning($"UnitController: 未处理的 TargetType：{targetType}");
        }
    }

    /// <summary>
    /// 执行远程攻击
    /// </summary>
    public virtual void PerformRangedAttack(TargetType targetType)
    {
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
            Vector3Int currentPos = gridPosition + attackDirection;

            bool hasAttacked = false;

            while (GridManager.Instance.IsWithinBattleArea(currentPos))
            {
                UnitController targetUnit = GridManager.Instance.GetUnitAt(currentPos);
                BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(currentPos);

                if (targetUnit != null && targetUnit.unitData.camp != unitData.camp)
                {
                    targetUnit.TakeDamage(1);
                    Debug.Log($"UnitController: 单位 {unitData.unitName} 对 {targetUnit.unitData.unitName} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }
                else if (targetBuilding != null && targetBuilding.buildingData.camp != unitData.camp)
                {
                    targetBuilding.TakeDamage(1);
                    Debug.Log($"UnitController: 单位 {unitData.unitName} 对建筑 {targetBuilding.buildingData.buildingName} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }

                currentPos += attackDirection;
            }

            if (!hasAttacked)
            {
                Debug.Log($"UnitController: 单位 {unitData.unitName} 远程攻击无目标或目标为友方！");
            }
        }
        else if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            // 防卫技能，只对自身或友方生效
            IncreaseDefense(1, targetType);
        }
        else
        {
            Debug.LogWarning($"UnitController: 未处理的 TargetType：{targetType}");
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
            Debug.Log($"UnitController: 单位 {unitData.unitName} 防卫点数增加 {value}，当前防卫点数：{defensePoints}");
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {unitData.unitName} 尝试对非友方进行防卫！");
        }
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public virtual void TakeDamage(int damage)
    {
        // 首先扣除防卫点数
        int remainingDamage = damage - defensePoints;
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            Debug.Log($"UnitController: 单位 {unitData.unitName} 接受 {remainingDamage} 点伤害，当前生命值: {currentHealth}");
        }
        else
        {
            // 防卫点数足以抵消所有伤害
            defensePoints -= damage;
            Debug.Log($"UnitController: 单位 {unitData.unitName} 防卫点数抵消了 {damage} 点伤害，剩余防卫点数: {defensePoints}");
        }

        if (currentHealth <= 0)
        {
            DestroyUnit();
        }
    }

    /// <summary>
    /// 治疗单位
    /// </summary>
    /// <param name="amount">治疗量</param>
    public virtual void Heal(int amount)
    {
        if (unitData.maxHealth > 0)
        {
            currentHealth = Mathf.Min(currentHealth + amount, unitData.maxHealth);
        }
        else
        {
            currentHealth += amount;
        }
        Debug.Log($"UnitController: 单位 {unitData.unitName} 恢复了 {amount} 点生命值，当前生命值: {currentHealth}");
    }

    /// <summary>
    /// 销毁单位
    /// </summary>
    void DestroyUnit()
    {
        // 根据需求，可以添加单位销毁的动画或效果
        Debug.Log($"UnitController: 单位 {unitData.unitName} 被销毁");
        Destroy(gameObject);
        GridManager.Instance.RemoveUnitAt(gridPosition);
    }

    /// <summary>
    /// 使用主技能或支援技能
    /// </summary>
    public virtual void UseMainSkillOrSupport()
    {
        if (CanUseMainSkill())
        {
            UseMainSkill();
        }
        else if (CanUseSupportSkill())
        {
            UseSupportSkill();
        }
        else
        {
            Debug.Log($"UnitController: 单位 {unitData.unitName} 无法使用任何技能！");
        }
    }

    /// <summary>
    /// 判断是否可以使用主技能
    /// </summary>
    /// <returns></returns>
    private bool CanUseMainSkill()
    {
        // 判断条件，例如是否有目标
        Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int targetPosition = gridPosition + attackDirection;

        UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
        BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

        return targetUnit != null || targetBuilding != null;
    }

    /// <summary>
    /// 判断是否可以使用支援技能
    /// </summary>
    /// <returns></returns>
    private bool CanUseSupportSkill()
    {
        // 判断条件，例如前方有友方单位
        UnitController frontUnit = GridManager.Instance.GetFrontUnitInRow(this);
        return frontUnit != null && frontUnit.unitData.camp == this.unitData.camp;
    }

    /// <summary>
    /// 使用主技能
    /// </summary>
    public virtual void UseMainSkill()
    {
        if (unitData.mainSkillSO != null)
        {
            unitData.mainSkillSO.Execute(this);
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {unitData.unitName} 没有配置主技能！");
        }
    }

    /// <summary>
    /// 使用支援技能
    /// </summary>
    public virtual void UseSupportSkill()
    {
        if (unitData.supportSkillSO != null)
        {
            unitData.supportSkillSO.Execute(this);
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {unitData.unitName} 没有配置支援技能！");
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
                                Debug.Log($"UnitController: 单位 {unitData.unitName} 无法继续移动，技能执行被阻挡！");
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
                        IncreaseDefense(action.Value, action.TargetType);
                        break;
                    default:
                        Debug.LogWarning($"UnitController: 未处理的技能类型：{action.Type}");
                        break;
                }
            }

            // 清空当前技能动作，防止重复执行
            currentSkill.Actions.Clear();
        }
    }
}

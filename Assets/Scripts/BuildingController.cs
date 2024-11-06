using UnityEngine;

/// <summary>
/// 控制建筑物的行为
/// </summary>
public class BuildingController : MonoBehaviour, ISkillUser
{
    public BuildingData buildingData; // 建筑物的静态数据

    public Vector3Int gridPosition;   // 建筑物在格子上的位置

    private int currentHealth;        // 建筑物的当前生命值
    private int defensePoints = 0;    // 防卫点数

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
    protected virtual void Init()
    {
        // 基类的初始化逻辑（如果有的话）
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
        if (CanExecuteAction())
        {
            if (buildingData.actionSkillSO != null)
            {
                SkillManager.Instance.ExecuteSkill(buildingData.actionSkillSO, this);
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
        if (buildingData.defenseSkillSO != null)
        {
            SkillManager.Instance.ExecuteSkill(buildingData.defenseSkillSO, this);
            Debug.Log($"建筑物 {buildingData.buildingName} 执行了防卫技能！");
        }
        else
        {
            Debug.LogWarning($"建筑物 {buildingData.buildingName} 没有配置防卫技能！");
        }
    }

    /// <summary>
    /// 增加防卫点数
    /// </summary>
    /// <param name="value">增加的防卫点数</param>
    public virtual void IncreaseDefense(int value)
    {
        defensePoints += value;
        Debug.Log($"建筑物 {buildingData.buildingName} 防卫点数增加 {value}，当前防卫点数：{defensePoints}");
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
            Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 接受 {remainingDamage} 点伤害，当前生命值: {currentHealth}");
        }
        else
        {
            // 防卫点数足以抵消所有伤害
            defensePoints -= damage;
            Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 防卫点数抵消了 {damage} 点伤害，剩余防卫点数: {defensePoints}");
        }

        if (currentHealth <= 0)
        {
            DestroyBuilding();
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
        Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 恢复了 {amount} 点生命值，当前生命值: {currentHealth}");
    }

    /// <summary>
    /// 销毁建筑物
    /// </summary>
    void DestroyBuilding()
    {
        // 根据需求，可以添加建筑物销毁的动画或效果
        Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 被销毁");
        Destroy(gameObject);
        GridManager.Instance.RemoveSkillUserAt(gridPosition);
    }

    /// <summary>
    /// 使用行动技能
    /// </summary>
    public virtual void UseActionSkill()
    {
        if (buildingData.actionSkillSO != null)
        {
            SkillManager.Instance.ExecuteSkill(buildingData.actionSkillSO, this);
        }
        else
        {
            Debug.LogWarning($"建筑物 {buildingData.buildingName} 没有配置行动技能！");
        }
    }

    /// <summary>
    /// 使用防卫技能
    /// </summary>
    public virtual void UseDefenseSkill()
    {
        if (buildingData.defenseSkillSO != null)
        {
            SkillManager.Instance.ExecuteSkill(buildingData.defenseSkillSO, this);
        }
        else
        {
            Debug.LogWarning($"建筑物 {buildingData.buildingName} 没有配置防卫技能！");
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
                        // 建筑物通常不会移动，忽略此类技能
                        Debug.LogWarning($"建筑物 {buildingData.buildingName} 无法执行移动技能！");
                        break;
                    case SkillType.Melee:
                        PerformMeleeAttack();
                        break;
                    case SkillType.Ranged:
                        PerformRangedAttack();
                        break;
                    case SkillType.Defense:
                        IncreaseDefense(action.Value);
                        break;
                    // 可以根据需要添加更多的技能类型
                    default:
                        Debug.LogWarning($"未处理的技能类型：{action.Type}");
                        break;
                }
            }

            // 清空当前技能动作，防止重复执行
            currentSkill.Actions.Clear();
        }
    }

    public virtual bool CanMoveForward()
    {
        return false;
    }

    public virtual void MoveForward()
    {
        // 建筑物通常不会移动，忽略此方法
        Debug.LogWarning($"建筑物 {buildingData.buildingName} 无法移动！");
    }

    /// <summary>
    /// 执行近战攻击（如果适用）
    /// </summary>
    public virtual void PerformMeleeAttack()
    {
        // 定义建筑物的近战攻击逻辑
        // 例如，攻击附近的单位或建筑
        Debug.Log($"建筑物 {buildingData.buildingName} 执行了近战攻击！");
        // 示例：攻击前方单位
        Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int targetPosition = gridPosition + attackDirection;

        UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
        BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

        if (targetUnit != null)
        {
            targetUnit.TakeDamage(1);
            Debug.Log($"建筑物 {buildingData.buildingName} 对单位 {targetUnit.unitData.unitName} 进行近战攻击，造成1点伤害！");
        }
        else if (targetBuilding != null)
        {
            targetBuilding.TakeDamage(1);
            Debug.Log($"建筑物 {buildingData.buildingName} 对建筑物 {targetBuilding.buildingData.buildingName} 进行近战攻击，造成1点伤害！");
        }
        else
        {
            Debug.Log($"建筑物 {buildingData.buildingName} 近战攻击无目标！");
        }
    }

    /// <summary>
    /// 执行远程攻击（如果适用）
    /// </summary>
    public virtual void PerformRangedAttack()
    {
        // 定义建筑物的远程攻击逻辑
        // 例如，攻击更远范围内的单位或建筑
        Debug.Log($"建筑物 {buildingData.buildingName} 执行了远程攻击！");
        Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int currentPos = gridPosition + attackDirection;

        bool hasAttacked = false;

        while (GridManager.Instance.IsWithinBattleArea(currentPos))
        {
            UnitController targetUnit = GridManager.Instance.GetUnitAt(currentPos);
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(currentPos);

            if (targetUnit != null)
            {
                targetUnit.TakeDamage(1);
                Debug.Log($"建筑物 {buildingData.buildingName} 对单位 {targetUnit.unitData.unitName} 进行远程攻击，造成1点伤害！");
                hasAttacked = true;
                break; // 只攻击第一个目标
            }
            else if (targetBuilding != null)
            {
                targetBuilding.TakeDamage(1);
                Debug.Log($"建筑物 {buildingData.buildingName} 对建筑物 {targetBuilding.buildingData.buildingName} 进行远程攻击，造成1点伤害！");
                hasAttacked = true;
                break; // 只攻击第一个目标
            }

            currentPos += attackDirection;
        }

        if (!hasAttacked)
        {
            Debug.Log($"建筑物 {buildingData.buildingName} 远程攻击无目标！");
        }
    }

    /// <summary>
    /// 使用当前技能或其他逻辑
    /// </summary>
    public virtual void UseCurrentSkill()
    {
        ExecuteCurrentSkill();
    }
}

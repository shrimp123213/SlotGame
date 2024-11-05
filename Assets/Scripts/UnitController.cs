using UnityEngine;

public class UnitController : MonoBehaviour
{
    public UnitData unitData;       // 单位的数据

    private Vector3Int gridPosition; // 单位在格子上的位置

    private int defensePoints = 0;  // 防卫点数

    // 引用 SkillManager
    private SkillManager skillManager;

    void Start()
    {
        // 初始化单位属性
        InitializeUnit();

        // 获取 SkillManager 引用
        skillManager = FindObjectOfType<SkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("未找到 SkillManager！");
        }
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
    /// 移动单位到前方一格
    /// </summary>
    public void MoveForward()
    {
        Vector3Int direction = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int newPosition = gridPosition + direction;

        // 检查新位置是否在战斗区域
        if (!GridManager.Instance.IsWithinBattleArea(newPosition))
        {
            Debug.Log($"{unitData.unitName} 无法移动，超出战斗区域！");
            return;
        }

        // 检查新位置是否被占据
        if (GridManager.Instance.HasUnitAt(newPosition))
        {
            Debug.Log($"{unitData.unitName} 无法移动，前方已被占据！");
            return;
        }

        // 移动单位
        GridManager.Instance.RemoveUnitAt(gridPosition);
        SetPosition(newPosition);
        GridManager.Instance.SpawnUnit(newPosition, unitData);

        Debug.Log($"{unitData.unitName} 向前移动到 {newPosition}");
    }

    /// <summary>
    /// 执行近战攻击
    /// </summary>
    public void PerformMeleeAttack()
    {
        Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int targetPosition = gridPosition + attackDirection;

        // 获取目标单位
        UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);

        if (targetUnit != null)
        {
            targetUnit.TakeDamage(1);
            Debug.Log($"{unitData.unitName} 对 {targetUnit.unitData.unitName} 进行近战攻击，造成1点伤害！");
        }
        else
        {
            Debug.Log($"{unitData.unitName} 近战攻击无目标！");
        }
    }

    /// <summary>
    /// 执行远程攻击
    /// </summary>
    public void PerformRangedAttack()
    {
        Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int currentPos = gridPosition + attackDirection;

        bool hasAttacked = false;

        while (GridManager.Instance.IsWithinBattleArea(currentPos))
        {
            UnitController targetUnit = GridManager.Instance.GetUnitAt(currentPos);
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(1);
                Debug.Log($"{unitData.unitName} 对 {targetUnit.unitData.unitName} 进行远程攻击，造成1点伤害！");
                hasAttacked = true;

                // 如果远程攻击让单位离开战场，继续攻击下一个目标
                // 这里假设攻击不会导致单位离开战场，除非有特殊效果
                break; // 根据需求决定是否继续攻击
            }

            currentPos += attackDirection;
        }

        if (!hasAttacked)
        {
            Debug.Log($"{unitData.unitName} 远程攻击无目标！");
        }
    }

    /// <summary>
    /// 增加防卫点数
    /// </summary>
    /// <param name="value">增加的防卫点数</param>
    public void IncreaseDefense(int value)
    {
        defensePoints += value;
        Debug.Log($"{unitData.unitName} 防卫点数增加 {value}，当前防卫点数：{defensePoints}");
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        // 首先扣除防卫点数
        int remainingDamage = damage - defensePoints;
        if (remainingDamage > 0)
        {
            unitData.health -= remainingDamage;
            Debug.Log($"单位 {unitData.unitName} 接受 {remainingDamage} 点伤害，当前生命值: {unitData.health}");
        }
        else
        {
            // 防卫点数足以抵消所有伤害
            defensePoints -= damage;
            Debug.Log($"单位 {unitData.unitName} 防卫点数抵消了 {damage} 点伤害，剩余防卫点数: {defensePoints}");
        }

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

    /// <summary>
    /// 使用技能
    /// </summary>
    /// <param name="skillString">技能字符串，例如 "2[Move] 2[Melee]"</param>
    public void UseSkill(string skillString)
    {
        if (skillManager == null)
        {
            Debug.LogError("SkillManager 未初始化！");
            return;
        }

        Skill skill = skillManager.ParseSkill(skillString);
        skillManager.ExecuteSkill(skill, this);
    }
    
    /// <summary>
    /// 检查单位是否可以继续向前移动
    /// </summary>
    /// <returns>是否可以移动</returns>
    public bool CanMoveForward()
    {
        Vector3Int direction = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int newPosition = gridPosition + direction;

        // 检查新位置是否在战斗区域
        if (!GridManager.Instance.IsWithinBattleArea(newPosition))
        {
            return false;
        }

        // 检查新位置是否被占据
        if (GridManager.Instance.HasUnitAt(newPosition))
        {
            return false;
        }

        return true;
    }

    // 其他行为如攻击、使用技能等可根据需求添加
}

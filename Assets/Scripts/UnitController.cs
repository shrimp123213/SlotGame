using UnityEngine;
using System.Collections.Generic;

public class UnitController : MonoBehaviour, ISkillUser
{
    public UnitData unitData;       // 单位的静态数据
    public Vector3Int gridPosition; // 单位在格子上的位置

    [SerializeField]
    protected int currentHealth;      // 单位的当前生命值

    [SerializeField]
    private int defensePoints = 0;  // 防御点数

    public Skill currentSkill;       // 当前技能的运行时实例

    // 新增一个公共变量，用于引用子物件的 SpriteRenderer
    public SpriteRenderer spriteRenderer;
    
    // 新增状态管理字段
    private List<IUnitState> currentStates = new List<IUnitState>();

    [Header("UI Elements")]
    public Transform statusIconsParent; // 状态图标的父对象
    public GameObject statusIconPrefab; // 状态图标预制体

    private Dictionary<string, GameObject> activeStatusIcons = new Dictionary<string, GameObject>();



    void Awake()
    {
        // 如果没有在 Inspector 中手动设置 spriteRenderer，则自动查找
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

        // 初始化实例变量
        currentHealth = unitData.maxHealth;

        // 调用初始化方法
        Init();
        
        // 添加初始状态
        //AddState<InvincibleState>();
        //AddState<InjuredState>();
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

        //Debug.Log($"UnitController: 单位 {unitData.unitName} 放置在格子中心: {cellWorldPosition}");
    }

    /// <summary>
    /// 判断是否可以使用主技能，依据技能的TargetType检查是否有有效目标
    /// </summary>
    /// <returns>是否可以使用主技能</returns>
    public bool CanUseMainSkill()
    {
        if (currentSkill == null || currentSkill.Actions == null)
        {
            Debug.LogWarning("UnitController: currentSkill 或其动作列表为 null！");
            return false;
        }

        foreach (var action in currentSkill.Actions)
        {
            switch (action.Type)
            {
                case SkillType.Melee:
                case SkillType.Ranged:
                    if (action.TargetType == TargetType.Enemy)
                    {
                        // 检查相应方向是否有敌方单位或建筑
                        Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
                        Vector3Int targetPosition = gridPosition + attackDirection;

                        UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
                        BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

                        if ((targetUnit != null && targetUnit.unitData.camp != unitData.camp) ||
                            (targetBuilding != null && targetBuilding.buildingData.camp != unitData.camp))
                        {
                            // 有有效的敌方目标
                            return true;
                        }
                    }
                    break;

                case SkillType.Defense:
                    if (action.TargetType == TargetType.Friendly)
                    {
                        // 检查是否有友方单位需要防卫（例如，低生命值）
                        List<UnitController> friendlyUnits = GridManager.Instance.GetUnitsByCamp(unitData.camp);
                        foreach (var friendly in friendlyUnits)
                        {
                            if (friendly.currentHealth < friendly.unitData.maxHealth)
                            {
                                return true;
                            }
                        }
                    }
                    else if (action.TargetType == TargetType.Self)
                    {
                        // 自身防卫，总是可以使用
                        return true;
                    }
                    break;

                // 处理其他 SkillType，如需要

                default:
                    break;
            }
        }

        // 如果所有动作都没有找到有效目标，则不能使用主技能
        return false;
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
        if (HasState<InvincibleState>())
        {
            Debug.Log($"{unitData.unitName} 处于无敌状态，免疫伤害");
            return;
        }
        
        // 首先扣除防卫点数
        int remainingDamage = damage - defensePoints;
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            Debug.Log($"UnitController: 单位 {unitData.unitName} 接受 {remainingDamage} 点伤害，当前生命值: {currentHealth}");
            if(!HasState<InjuredState>())
                AddState<InjuredState>();
        }
        else
        {
            // 防卫点数足以抵消所有伤害
            defensePoints -= damage;
            Debug.Log($"UnitController: 单位 {unitData.unitName} 防卫点数抵消了 {damage} 点伤害，剩余防卫点数: {defensePoints}");
        }

        if (currentHealth <= 0)
        {
            if (HasState<InjuredState>())
            {
                // 负伤状态下再次死亡，进入墓地
                Debug.Log($"UnitController: 单位 {unitData.unitName} 在负伤状态下再次死亡，进入墓地");
                MoveToGraveyard();
            }
            else
            {
                // 第一次死亡，进入负伤状态并返回牌库
                Debug.Log($"UnitController: 单位 {unitData.unitName} 第一次死亡，进入负伤状态并返回牌库");
                AddState<InjuredState>();
                MoveToDeck();
            }
        }
    }
    
    /// <summary>
    /// 将单位移动回牌库
    /// </summary>
    private void MoveToDeck()
    {
        // 从战场移除
        GridManager.Instance.RemoveSkillUserAt(gridPosition);

        // 将单位返回到牌库（具体实现根据您的牌库管理方式）
        // 例如，将单位对象禁用或移动到牌库位置
        gameObject.SetActive(false);
        Debug.Log($"{unitData.unitName} 返回牌库");
    }

    /// <summary>
    /// 将单位移动到墓地
    /// </summary>
    private void MoveToGraveyard()
    {
        // 从战场移除
        GridManager.Instance.RemoveSkillUserAt(gridPosition);

        // 将单位移动到墓地（具体实现根据您的墓地管理方式）
        // 例如，将单位对象禁用或移动到墓地位置
        gameObject.SetActive(false);
        Debug.Log($"{unitData.unitName} 移动到墓地");
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

        // 如果单位处于负伤状态，触发治疗
        if (HasState<InjuredState>())
        {
            AddState<HealedState>();
        }
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
                Debug.Log($"Action Type: {action.Type}, TargetType: {action.TargetType}, Value: {action.Value}");
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
    
    
    /// <summary>
    /// 添加一個狀態到單位
    /// </summary>
    /// <typeparam name="T">狀態類型</typeparam>
    public void AddState<T>() where T : ScriptableObject, IUnitState
    {
        // 先檢查是否有互斥的狀態
        if (typeof(T) == typeof(InvincibleState))
        {
            // 移除所有可能與無敵狀態互斥的狀態
            //RemoveState<InjuredState>();
            //RemoveState<HealedState>();
        }
        
        // 查找是否已經存在該類型的狀態
        foreach (var state in currentStates)
        {
            if (state.GetType() == typeof(T))
            {
                Debug.LogWarning($"{unitData.unitName} 已經擁有 {state.StateName} 狀態");
                return;
            }
        }

        // 加載狀態資源
        T newState = Resources.Load<T>($"UnitStates/{typeof(T).Name}");
        if (newState != null)
        {
            currentStates.Add(newState);
            newState.OnEnter(this);
            AddStatusIcon(newState);
        }
        else
        {
            Debug.LogError($"無法加載狀態類型: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 移除一個狀態從單位
    /// </summary>
    /// <typeparam name="T">狀態類型</typeparam>
    public void RemoveState<T>() where T : ScriptableObject, IUnitState
    {
        IUnitState stateToRemove = null;
        foreach (var state in currentStates)
        {
            if (state.GetType() == typeof(T))
            {
                stateToRemove = state;
                break;
            }
        }

        if (stateToRemove != null)
        {
            stateToRemove.OnExit(this);
            currentStates.Remove(stateToRemove);
            RemoveStatusIcon(stateToRemove);
        }
        else
        {
            Debug.LogWarning($"{unitData.unitName} 不存在 {typeof(T).Name} 狀態");
        }
    }

    /// <summary>
    /// 檢查單位是否擁有某個狀態
    /// </summary>
    /// <typeparam name="T">狀態類型</typeparam>
    /// <returns>是否擁有該狀態</returns>
    public bool HasState<T>() where T : ScriptableObject, IUnitState
    {
        foreach (var state in currentStates)
        {
            if (state.GetType() == typeof(T))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 添加狀態圖標到 Unit 的子物件
    /// </summary>
    /// <param name="state">要添加的狀態</param>
    private void AddStatusIcon(IUnitState state)
    {
        if (statusIconsParent != null && StatusIconPool.Instance != null)
        {
            // 從對象池中獲取一個狀態圖標
            GameObject iconGO = StatusIconPool.Instance.GetStatusIcon();
            SpriteRenderer iconSpriteRenderer = iconGO.GetComponent<SpriteRenderer>();
            if (iconSpriteRenderer != null)
            {
                iconSpriteRenderer.sprite = state.Icon;
                iconSpriteRenderer.enabled = true;

                // 設置圖標的父對象為 statusIconsParent
                iconGO.transform.SetParent(statusIconsParent, false);

                // 設置圖標的位置，根據當前活躍圖標數量
                int iconCount = activeStatusIcons.Count;
                iconGO.transform.localPosition = new Vector3(iconCount * 0.2f, 0, 0); // 水平排列，每個圖標間隔0.2單位
            }
            else
            {
                Debug.LogError("StatusIconPrefab 沒有 SpriteRenderer 組件！");
            }
            activeStatusIcons[state.StateName] = iconGO;
        }
        else
        {
            Debug.LogError("UnitController: statusIconsParent 或 StatusIconPool.Instance 未設置！");
        }
    }

    /// <summary>
    /// 從 Unit 的子物件移除狀態圖標
    /// </summary>
    /// <param name="state">要移除的狀態</param>
    private void RemoveStatusIcon(IUnitState state)
    {
        if (activeStatusIcons.ContainsKey(state.StateName))
        {
            GameObject iconGO = activeStatusIcons[state.StateName];
            // 將圖標返回對象池
            StatusIconPool.Instance.ReturnStatusIcon(iconGO);
            activeStatusIcons.Remove(state.StateName);

            // 重新排列剩餘的圖標位置
            RepositionStatusIcons();
        }
        else
        {
            Debug.LogWarning($"UnitController: 狀態圖標 {state.StateName} 不存在於 activeStatusIcons 中");
        }
    }

    /// <summary>
    /// 重新排列狀態圖標的位置，避免重疊
    /// </summary>
    private void RepositionStatusIcons()
    {
        int index = 0;
        foreach (var kvp in activeStatusIcons)
        {
            GameObject iconGO = kvp.Value;
            iconGO.transform.localPosition = new Vector3(index * 0.2f, 0, 0); // 水平排列，每個圖標間隔0.2單位
            index++;
        }
    }

    /// <summary>
    /// 更新單位的 UI（如狀態圖標）
    /// </summary>
    public void UpdateUnitUI()
    {
        // 此方法可用於更新其他 UI 元素，如狀態圖標
        // 目前已在 AddState 和 RemoveState 中處理了狀態圖標
    }
    
    public void InitializeUnitSprite()
    {
        // 设置单位的图片
        if (unitData.unitSprite != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = unitData.unitSprite;
            }
        }

        // 设置朝向
        var scale = spriteRenderer.GetComponent<Transform>().localScale;
        scale.x = unitData.camp == Camp.Player ? 1 : -1;
        spriteRenderer.GetComponent<Transform>().localScale = scale;
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
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
    
    private bool hasActedThisTurn = false;
    
    // 跟踪每个技能的剩余延迟回合数
    private Dictionary<string, int> skillDelays = new Dictionary<string, int>();
    
    private List<ScheduledAction> scheduledActions = new List<ScheduledAction>();

    [System.Serializable]
    public class ScheduledAction
    {
        public SkillActionData action;
        public int remainingDelay;
    }
    
    // 唯一标识符，用于在 DeckEntry 中保存技能延迟
    [SerializeField]
    private string _buildingId = "";

    public string buildingId
    {
        get
        {
            if (string.IsNullOrEmpty(_buildingId))
            {
                _buildingId = Guid.NewGuid().ToString();
            }
            return _buildingId;
        }
    }
    
    private void Awake()
    {
        // 生成唯一标识符
        _buildingId = Guid.NewGuid().ToString();
    }
    
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
    
    private void OnEnable()
    {
        // 不再在这里调用 InitializeSkillDelays()
        // 因为此时 buildingData 可能还未赋值
    }

    private void OnDisable()
    {
        // 移除对 SaveSkillDelays 的调用
        // SaveSkillDelays();
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

    public void Initialize()
    {
        InitializeSkillDelays();
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
    /// 从牌库加载技能延迟
    /// </summary>
    private void LoadSkillDelays()
    {
        // 建筑物不需要从 DeckManager 加载技能延迟，直接初始化技能延迟
        InitializeSkillDelays();
        Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 的技能延迟已初始化。");
    }

    /// <summary>
    /// 保存技能延迟到牌库
    /// </summary>
    private void SaveSkillDelays()
    {
        
    }

    /// <summary>
    /// 初始化技能延迟
    /// </summary>
    private void InitializeSkillDelays()
    {
        if (buildingData == null)
        {
            Debug.LogError("BuildingController: buildingData 未赋值，无法初始化技能延迟！");
            return;
        }
        
        if (buildingData.actionSkillSO != null)
        {
            foreach (var action in buildingData.actionSkillSO.actions)
            {
                if (!skillDelays.ContainsKey(buildingData.actionSkillSO.skillName))
                {
                    skillDelays[buildingData.actionSkillSO.skillName] = action.Delay;
                }
            }
        }
    }

    /// <summary>
    /// 在每回合开始时减少所有技能的延迟值
    /// </summary>
    public void ReduceSkillDelays()
    {
        List<string> keys = new List<string>(skillDelays.Keys);
        foreach (var skillName in keys)
        {
            if (skillDelays.ContainsKey(skillName))
            {
                if (skillDelays[skillName] > 0)
                {
                    skillDelays[skillName]--;
                    Debug.Log($"BuildingController: 建筑物 {name} 技能 {skillName} 的延迟减少到 {skillDelays[skillName]} 回合");
                }
            }
        }
    }

    /// <summary>
    /// 检查技能是否准备就绪（延迟为0）
    /// </summary>
    private bool IsSkillReady(string skillName)
    {
        if (skillDelays.ContainsKey(skillName))
        {
            return skillDelays[skillName] <= 0;
        }
        return true; // 如果没有记录延迟，则认为已准备就绪
    }
    
    /// <summary>
    /// 判断建筑物是否可以使用行动技能
    /// </summary>
    public bool CanUseActionSkill()
    {
        if (buildingData.actionSkillSO == null || buildingData.actionSkillSO.actions == null)
        {
            Debug.LogWarning($"{name}: actionSkillSO 或其动作列表为 null！");
            return false;
        }

        return IsSkillReady(buildingData.actionSkillSO.skillName);
    }

    /// <summary>
    /// 执行建筑物的行动
    /// </summary>
    public IEnumerator ExecuteAction(SkillActionData action)
    {
        if (hasActedThisTurn)
        {
            yield break; // 單位已經行動過，跳過
        }

        if (isRuin)
        {
            Debug.Log($"BuildingController: 废墟无法执行行动！");
            yield break;
        }

        if (buildingData.actionSkillSO != null)
        {
            // 初始化当前技能为行动技能的克隆
            currentSkill = Skill.FromSkillSO(buildingData.actionSkillSO);

            // 初始化技能延迟
            InitializeSkillDelay(currentSkill);

            if (currentSkill != null)
            {
                // 执行当前技能的所有动作
                switch (action.Type)
                {
                    case SkillType.Melee:
                        ISkillUser meleeTarget = GetMeleeTarget(action.TargetType);
                        if (meleeTarget != null)
                        {
                            Effect effect = new Effect(meleeTarget, damage: action.Value);
                            BattleManager.Instance.AddEffectToQueue(effect);
                            Debug.Log($"{name} 计划对 {meleeTarget} 造成 {action.Value} 点伤害");
                        }
                        else
                        {
                            Debug.Log($"{name} 没有找到近战目标");
                        }

                        break;

                    case SkillType.Ranged:
                        ISkillUser rangedTarget = GetRangedTarget(action.TargetType);
                        if (rangedTarget != null)
                        {
                            Effect effect = new Effect(rangedTarget, damage: action.Value);
                            BattleManager.Instance.AddEffectToQueue(effect);
                            Debug.Log($"{name} 计划对 {rangedTarget} 造成 {action.Value} 点远程伤害");
                        }
                        else
                        {
                            Debug.Log($"{name} 没有找到远程目标");
                        }

                        break;
                    case SkillType.Defense:
                        //IncreaseDefense(action.Value, action.TargetType);
                        yield return null;
                        break;
                    case SkillType.AddToDeck:
                        // 调用处理添加到牌组的方法
                        yield return StartCoroutine(HandleAddToDeckAction(action));
                        break;
                    default:
                        Debug.LogWarning($"BuildingController: 未处理的技能类型：{action.Type}");
                        yield return null;
                        break;
                }

                Debug.Log($"{name}: 当前技能执行完毕！");

                // 清空当前技能动作，防止重复执行
                currentSkill.Actions.Clear();
            }

            Debug.Log($"建筑物 {buildingData.buildingName} 执行了行动技能！");
        }
        else
        {
            Debug.LogWarning($"建筑物 {buildingData.buildingName} 没有配置行动技能！");
        }

        hasActedThisTurn = true; // 标记为已行动
    }
    
    private ISkillUser GetMeleeTarget(TargetType targetType)
    {
        Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int targetPosition = gridPosition + attackDirection;

        ISkillUser target = GridManager.Instance.GetSkillUserAt(targetPosition);

        if (target != null && target.GetCamp() != buildingData.camp)
        {
            return target;
        }
        return null;
    }

    private ISkillUser GetRangedTarget(TargetType targetType)
    {
        Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int currentPos = gridPosition + attackDirection;

        while (GridManager.Instance.IsWithinBattleArea(currentPos))
        {
            ISkillUser target = GridManager.Instance.GetSkillUserAt(currentPos);

            if (target != null && target.GetCamp() != buildingData.camp)
            {
                return target;
            }

            currentPos += attackDirection;
        }

        return null;
    }
    
    /// <summary>
    /// 初始化技能的延迟
    /// </summary>
    private void InitializeSkillDelay(Skill skill)
    {
        if (skill != null && !string.IsNullOrEmpty(skill.skillName))
        {
            foreach (var action in skill.Actions)
            {
                // 假设同一个技能的所有动作共享相同的延迟
                if (!skillDelays.ContainsKey(skill.skillName))
                {
                    skillDelays[skill.skillName] = action.Delay;
                }
            }
        }
    }

    /// <summary>
    /// 执行近战攻击
    /// </summary>
    public virtual IEnumerator PerformMeleeAttack(TargetType targetType)
    {
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = buildingData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
            Vector3Int targetPosition = gridPosition + attackDirection;

            UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

            if (targetUnit != null && targetUnit.unitData.camp != buildingData.camp)
            {
                // 执行攻击动画
                yield return StartCoroutine(PlayMeleeAttackAnimation());

                targetUnit.TakeDamage(1);
                Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对 {targetUnit.unitData.unitName} 进行近战攻击，造成1点伤害！");
            }
            else if (targetBuilding != null && targetBuilding.buildingData.camp != buildingData.camp && !targetBuilding.isRuin)
            {
                // 执行攻击动画
                yield return StartCoroutine(PlayMeleeAttackAnimation());

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
            //yield return StartCoroutine(IncreaseDefense(value, targetType));
        }
        else
        {
            Debug.LogWarning($"BuildingController: 未处理的 TargetType：{targetType}");
        }
    }

    /// <summary>
    /// 执行远程攻击
    /// </summary>
    public virtual IEnumerator PerformRangedAttack(TargetType targetType)
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
                    // 执行攻击动画
                    yield return StartCoroutine(PlayRangedAttackAnimation());

                    targetUnit.TakeDamage(1);
                    Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对 {targetUnit.name} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }
                else if (targetBuilding != null && targetBuilding.buildingData.camp != buildingData.camp && !targetBuilding.isRuin)
                {
                    // 执行攻击动画
                    yield return StartCoroutine(PlayRangedAttackAnimation());

                    targetBuilding.TakeDamage(1);
                    Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 对建筑物 {targetBuilding.name} 进行远程攻击，造成1点伤害！");
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
            //yield return StartCoroutine(IncreaseDefense(value, targetType));
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
    public virtual IEnumerator IncreaseDefense(int value, TargetType targetType)
    {
        if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            // 执行防御动画
            yield return StartCoroutine(PlayIncreaseDefenseAnimation());

            defensePoints += value;
            Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 防卫点数增加 {value}，当前防卫点数：{defensePoints}");
        }
        else
        {
            Debug.LogWarning($"BuildingController: 建筑物 {buildingData.buildingName} 尝试对非友方进行防卫！");
        }
    }

    public virtual IEnumerator PerformBreakage(int breakagePoints)
    {
        // 破壞建筑物时的逻辑
        Debug.Log($"BuildingController: 建筑物 {buildingData.buildingName} 被破壞，减少 {breakagePoints} 点生命值！");
        TakeDamage(breakagePoints);
        yield break;
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public virtual void TakeDamage(int damage, DamageSource source = DamageSource.Normal)
    {
        if(isRuin)
            return;
        
        int remainingDamage = damage - defensePoints;
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            defensePoints = 0;
            Debug.Log($"BuildingController: 建筑物 {name} 接受 {remainingDamage} 点伤害，当前生命值: {currentHealth}");
        }
        else
        {
            defensePoints -= damage;
            Debug.Log($"BuildingController: 建筑物 {name} 防卫点数抵消了 {damage} 点伤害，剩余防卫点数: {defensePoints}");
        }

        if (currentHealth <= 0)
        {
            DestroyBuilding();
        }
        
        currentHealth = Mathf.Clamp(currentHealth, 0, buildingData.maxHealth);
    }

    /// <summary>
    /// 治疗建筑物
    /// </summary>
    /// <param name="amount">治疗量</param>
    public virtual void Heal(int amount)
    {
        /*if (buildingData.maxHealth > 0)
        {
            currentHealth = Mathf.Min(currentHealth + amount, buildingData.maxHealth);
        }
        else
        {
            currentHealth += amount;
        }*/
        if (isRuin)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, buildingData.maxHealth);
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
            Debug.Log($"BuildingController: 建筑物 {name} 被摧毁，变为废墟！");
        }
    }

    /// <summary>
    /// 使用行动技能
    /// </summary>
    public void UseSkill()
    {
        if(isRuin)
            return;
        
        if (CanUseActionSkill())
        {
            Skill currentSkill = Skill.FromSkillSO(buildingData.actionSkillSO);
            ScheduleSkillExecution(currentSkill);
            hasActedThisTurn = true; // 标记为已行动
        }
        else
        {
            Debug.Log($"BuildingController: 建筑物 {name} 的行动技能还未准备好！");
        }
    }

    private void ScheduleSkillExecution(Skill skill)
    {
        BattleManager.Instance.ScheduleSkillExecution(skill, this);
    }

    public virtual IEnumerator MoveForward()
    {
        Debug.Log("建築物無法移動！");
        yield break;
    }

    public virtual bool CanMoveForward()
    {
        // 建筑物无法移动
        return false;
    }
    
    // 示例动画协程
    private IEnumerator PlayMeleeAttackAnimation()
    {
        // 假设有一个动画组件
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("MeleeAttack");
            // 等待动画完成，这里假设动画持续时间为0.5秒
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
    
    public void ResetTurn()
    {
        hasActedThisTurn = false;
    }
    
    public Camp GetCamp()
    {
        return buildingData != null ? buildingData.camp : Camp.Player; // 默认返回玩家阵营
    }
    
    private IEnumerator HandleAddToDeckAction(SkillActionData action)
    {
        Debug.Log("BuildingController: 处理 AddToDeck 动作");
        
        if (action.UnitsToAdd == null || action.UnitsToAdd.Count == 0)
        {
            Debug.LogWarning("BuildingController: AddToDeck 动作未指定任何单位！");
            yield break;
        }

        // 获取 DeckManager 实例
        DeckManager deckManager = DeckManager.Instance;
        if (deckManager == null)
        {
            Debug.LogError("BuildingController: 未找到 DeckManager 实例！");
            yield break;
        }

        // 确定要添加到哪个牌组
        Camp buildingCamp = GetCamp();
        Deck targetDeck = buildingCamp == Camp.Player ? deckManager.playerDeck : deckManager.enemyDeck;

        // 将指定的单位添加到牌组
        foreach (var unitToAdd in action.UnitsToAdd)
        {
            if (unitToAdd.unitData == null || unitToAdd.quantity <= 0)
            {
                Debug.LogWarning("BuildingController: AddToDeck 动作包含无效的单位或数量！");
                continue;
            }

            targetDeck.AddCard(unitToAdd.unitData, null, unitToAdd.quantity, false);
            Debug.Log($"BuildingController: 已将 {unitToAdd.quantity} 张 {unitToAdd.unitData.unitName} 添加到 {buildingCamp} 的牌组中。");
        }

        yield return null;
    }
    
    public void ScheduleAction(SkillActionData action)
    {
        if (action.Delay > 0)
        {
            scheduledActions.Add(new ScheduledAction { action = action, remainingDelay = action.Delay });
            Debug.Log($"UnitController: {name} 计划执行技能 {action.Type}，延迟 {action.Delay} 回合");
        }
        else
        {
            StartCoroutine(ExecuteAction(action));
        }
    }

    /// <summary>
    /// 在每个回合结束时调用，处理延迟的动作
    /// </summary>
    public void ReduceDelay()
    {
        List<ScheduledAction> actionsToExecute = new List<ScheduledAction>();

        foreach (var scheduled in scheduledActions)
        {
            scheduled.remainingDelay--;
            if (scheduled.remainingDelay <= 0)
            {
                actionsToExecute.Add(scheduled);
            }
        }

        foreach (var action in actionsToExecute)
        {
            scheduledActions.Remove(action);
            StartCoroutine(ExecuteAction(action.action));
        }
    }
    
    /*public void ApplyStatusEffect(StatusEffect statusEffect)
    {
        // 根据状态效果的类型，添加状态
        //AddState(statusEffect);
    }*/

    
    public void PrepareForNextTurn()
    {
        // 减少技能延迟
        ReduceSkillDelays();

        // 处理延迟的动作
        ReduceDelay();

        // 重置回合标志
        hasActedThisTurn = false;

        // 处理状态效果的持续时间等
    }

}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.InputSystem.XR;

public class UnitController : MonoBehaviour, ISkillUser
{
    public UnitData unitData;       // 单位的静态数据
    public Vector3Int gridPosition; // 单位在格子上的位置
    [HideInInspector]
    public int columnIndex; // 新增：单位所在的列索引
    
    public int currentHealth;      // 单位的当前生命值
    [SerializeField]
    private int defensePoints = 0;  // 防御点数

    public Skill currentSkill;       // 当前技能的运行时实例

    // 新增一个公共变量，用于引用子物件的 SpriteRenderer
    public SpriteRenderer spriteRenderer;
    // 添加对 Sprite 子对象的引用
    private Transform spriteTransform;
    
    // 新增状态管理字段
    private List<UnitStateBase> currentStates = new List<UnitStateBase>();

    [Header("UI Elements")]
    public Transform statusIconsParent; // 状态图标的父对象
    public GameObject statusIconPrefab; // 状态图标预制体
    
    private Dictionary<string, GameObject> activeStatusIcons = new Dictionary<string, GameObject>();
    
    // 跟踪每个技能的剩余延迟回合数
    private Dictionary<string, int> skillDelays = new Dictionary<string, int>();
    
    // 唯一标识符，用于在 DeckEntry 中保存技能延迟
    [SerializeField]
    public string unitId { get; private set; }

    public bool isInjured => HasState<InjuredState>();
    public bool isFragile => HasState<FragileState>();
    
    private bool isDead = false;
    public bool IsDead => isDead;
    
    // 新增：疾病层数管理
    private int diseaseLayers = 0;
    public int DiseaseLayers => diseaseLayers;
    
    private bool hasActedThisTurn = false;

    private List<ScheduledAction> scheduledActions = new List<ScheduledAction>();

    [System.Serializable]
    public class ScheduledAction
    {
        public SkillActionData action;
        public int remainingDelay;
    }
    
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
        
        // 获取 Sprite 子对象的 Transform
        if (spriteRenderer != null)
        {
            spriteTransform = spriteRenderer.transform;
        }
        else
        {
            Debug.LogError("UnitController: 无法获取 SpriteRenderer 的 Transform！");
        }
        
    }

    void Start()
    {
        // 初始化单位属性
        InitializeUnit();

        // 初始化实例变量
        currentHealth = unitData.maxHealth;

        // 调用初始化方法
        Init();
        
        // 添加初始状态
        if (unitData.initialStates != null)
        {
            foreach(var state in unitData.initialStates)
            {
                AddState(state);
            }
        }
    }
    
    private void OnEnable()
    {
        // 当单位进入场地时，加载其技能延迟
        //LoadSkillDelays();
    }

    private void OnDisable()
    {
        // 当单位离开场地时，保存其技能延迟
        if (!string.IsNullOrEmpty(unitId))
        {
            SaveSkillDelays();
        }
        else
        {
            Debug.LogWarning($"UnitController.OnDisable: unitId 为 null 或空，无法保存技能延迟。Unit: {unitData?.unitName ?? "未指定"}");
        }
    }
    
    /// <summary>
    /// 获取技能的当前延迟回合数（深拷贝，防止外部修改）
    /// </summary>
    public Dictionary<string, int> GetSkillDelays()
    {
        return new Dictionary<string, int>(skillDelays);
    }
    
    /// <summary>
    /// 从牌库加载技能延迟
    /// </summary>
    private void LoadSkillDelays()
    {
        if (unitData == null)
        {
            Debug.LogError("UnitController: unitData 未赋值，无法加载技能延迟！");
            return;
        }
        
        Dictionary<string, int> loadedDelays = DeckManager.Instance.GetUnitSkillDelays(unitData, unitId);
        if (loadedDelays != null)
        {
            skillDelays = new Dictionary<string, int>(loadedDelays);
            Debug.Log($"UnitController: 单位 {unitData.name} 的技能延迟已加载。");
        }
        else
        {
            // 初始化技能延迟，如果没有保存的数据
            InitializeSkillDelays();
            Debug.Log($"UnitController: 单位 {unitData.name} 的技能延迟已初始化。");
        }
    }

    /// <summary>
    /// 保存技能延迟到牌库
    /// </summary>
    private void SaveSkillDelays()
    {
        if (unitData != null && !string.IsNullOrEmpty(unitId))
        {
            DeckManager.Instance.SaveUnitSkillDelays(unitData, unitId, skillDelays);
            Debug.Log($"UnitController: 已保存 {unitData.unitName} 的技能延迟。unitId: {unitId}");
        }
        else
        {
            Debug.LogWarning($"UnitController.SaveSkillDelays: unitData 或 unitId 为 null 或空，无法保存技能延迟。");
        }
    }
    
    public void ResetTurn()
    {
        hasActedThisTurn = false;
    }

    public void Initialize()
    {
        if (unitData == null)
        {
            Debug.LogError("UnitController: unitData 未赋值，无法初始化！");
            return;
        }

        if (string.IsNullOrEmpty(unitId))
        {
            unitId = Guid.NewGuid().ToString();
            Debug.Log($"UnitController: 为 {unitData.unitName} 生成了新的 unitId: {unitId}");
        }
        else
        {
            Debug.Log($"UnitController: {unitData.unitName} 已有 unitId: {unitId}");
        }
    
        // 初始化技能延迟
        LoadSkillDelays();
    }

    /// <summary>
    /// 初始化单位
    /// </summary>
    void InitializeUnit()
    {
        if (unitData == null)
        {
            Debug.LogError("UnitController: unitData 未赋值，无法初始化！");
            return;
        }
        
        InitializeUnitSprite();
        
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

        name = unitData.unitName + "_" + position.x + "_" + position.y;

        // 确保子物件的本地位置为零（保持相对位置不变）
        if (spriteTransform != null)
        {
            spriteTransform.localPosition = Vector3.zero;
        }

        //Debug.Log($"UnitController: 单位 {unitData.unitName} 放置在格子中心: {cellWorldPosition}");
    }
    
    /// <summary>
    /// 初始化技能延迟
    /// </summary>
    private void InitializeSkillDelays()
    {
        if (unitData == null)
        {
            Debug.LogError("UnitController: unitData 未赋值，无法初始化技能延迟！");
            return;
        }
        
        foreach (var skillSO in new List<SkillSO> { unitData.mainSkillSO, unitData.supportSkillSO })
        {
            if (skillSO != null)
            {
                foreach (var action in skillSO.actions)
                {
                    if (!skillDelays.ContainsKey(skillSO.skillName))
                    {
                        skillDelays[skillSO.skillName] = action.Delay;
                    }
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
                    Debug.Log($"UnitController: 单位 {name} 技能 {skillName} 的延迟减少到 {skillDelays[skillName]} 回合");
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
    /// 判断是否可以使用主技能
    /// </summary>
    public bool CanUseMainSkill()
    {
        if (unitData.mainSkillSO == null || unitData.mainSkillSO.actions == null)
        {
            Debug.LogWarning($"{name}: mainSkillSO 或其动作列表为 null！");
            return false;
        }

        return IsSkillReady(unitData.mainSkillSO.skillName);
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
    public virtual IEnumerator MoveForward()
    {
        Vector3Int direction = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int newPosition = gridPosition + direction;

        // 尝试通过 GridManager 移动单位
        bool moved = GridManager.Instance.MoveUnit(gridPosition, newPosition);
        if (moved)
        {
            gridPosition = newPosition;
            Debug.Log($"UnitController: 单位 {name} 向前移动到 {newPosition}");

            // 播放移动动画（假设有相应的方法）
            yield return StartCoroutine(PlayMoveAnimation());
            
        }
        else
        {
            Debug.Log($"UnitController: 单位 {name} 无法移动到 {newPosition}");
            yield return null;
        }
    }
    
    private ISkillUser GetMeleeTarget(TargetType targetType)
    {
        Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int targetPosition = gridPosition + attackDirection;

        ISkillUser target = GridManager.Instance.GetSkillUserAt(targetPosition);


        if (target != null && target.GetCamp() != unitData.camp)
        {
            if (target is BuildingController { isRuin: true })
            {
                TryAttackBoss(1);
            }
            else
            {
                return target;
            }
        }

        return null;
    }

    private ISkillUser GetRangedTarget(TargetType targetType)
    {
        Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        Vector3Int currentPos = gridPosition + attackDirection;

        while (GridManager.Instance.IsWithinBattleArea(currentPos))
        {
            ISkillUser target = GridManager.Instance.GetSkillUserAt(currentPos);

            if (target != null && target.GetCamp() != unitData.camp)
            {
                if (target is  BuildingController { isRuin: true })
                {
                    TryAttackBoss(1);
                }
                else
                {
                    return target;
                }
            }

            currentPos += attackDirection;
        }
        
        return null;
    }

    /// <summary>
    /// 执行近战攻击
    /// </summary>
    public virtual IEnumerator PerformMeleeAttack(TargetType targetType)
    {
        // 执行近战攻击动画
        yield return StartCoroutine(PlayMeleeAttackAnimation());

        // 执行攻击逻辑
        PerformMeleeAttackLogic(targetType);
        
        //Debug.Log("UnitController: 执行近战攻击");
    }
    
    /// <summary>
    /// 执行远程攻击
    /// </summary>
    public virtual IEnumerator PerformRangedAttack(TargetType targetType)
    {
        // 执行远程攻击动画
        yield return StartCoroutine(PlayRangedAttackAnimation());

        // 执行攻击逻辑
        PerformRangedAttackLogic(targetType);
    }

    /// <summary>
    /// 增加防卫点数
    /// </summary>
    /// <param name="value">增加的防卫点数</param>
    /// <param name="targetType">目标类型</param>
    public virtual IEnumerator IncreaseDefense(int value, TargetType targetType)
    {
        // 执行防御增加动画（如果有）
        //yield return StartCoroutine(PlayIncreaseDefenseAnimation());
        
        yield return null; // 暂时不播放动画
        
        // 增加防御点数
        IncreaseDefenseLogic(value, targetType);
    }
    
    // 实现 PerformBreakage 方法
    public virtual IEnumerator PerformBreakage(int breakagePoints)
    {
        // 执行破壞动画（如果有）
        yield return StartCoroutine(PlayBreakageAnimation());
        
        // 执行破壞逻辑
        PerformBreakageLogic(breakagePoints);
    }
    
    private bool TryAttackBoss(int damage)
    {
        int row = gridPosition.y;
        if (GridManager.Instance.CanRowAttackBoss(row))
        {
            BossController boss = GridManager.Instance.GetBossUnit(unitData.camp == Camp.Player ? Camp.Enemy : Camp.Player);
            if (boss != null)
            {
                boss.TakeDamage(damage, DamageSource.Normal); // 以正常伤害来源攻击Boss);
                Debug.Log($"UnitController: {name} 对 Boss 进行攻击，造成 {damage} 点伤害！");
                return true;
            }
        }
        return false;
    }

    // 示例动画协程
    private IEnumerator PlayMoveAnimation()
    {
        // 假设有一个动画组件
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Move");
            // 等待动画完成，这里假设动画持续时间为0.5秒
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return null;
        }
    }
    
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
            //yield return StartCoroutine(PlayAttackAnimation());
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
            //yield return StartCoroutine(PlayAttackAnimation());
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
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Breakage");
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return null;
            //yield return StartCoroutine(PlayAttackAnimation());
        }
    }
    
    private void PerformMeleeAttackLogic(TargetType targetType)
    {
        if (isDead)
        {
            Debug.Log($"UnitController: 单位 {name} 已死亡，无法进行近战攻击！");
            return;
        }
        
        // 近战攻击逻辑
        if (targetType == TargetType.Enemy)
        {
            Vector3Int attackDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
            Vector3Int targetPosition = gridPosition + attackDirection;

            UnitController targetUnit = GridManager.Instance.GetUnitAt(targetPosition);
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(targetPosition);

            if (targetUnit != null && targetUnit.unitData.camp != unitData.camp)
            {
                targetUnit.TakeDamage(1);
                Debug.Log($"UnitController: 单位 {name} 对 {targetUnit.name} 进行近战攻击，造成1点伤害！");
            }
            else if (targetBuilding != null && targetBuilding.buildingData.camp != unitData.camp && !targetBuilding.isRuin)
            {
                targetBuilding.TakeDamage(1);
                Debug.Log($"UnitController: 单位 {name} 对建筑物 {targetBuilding.buildingData.name} 进行近战攻击，造成1点伤害！");
            }
            else
            {
                if (targetBuilding != null && targetBuilding.buildingData.camp != unitData.camp && !TryAttackBoss(1))
                {
                    Debug.Log($"UnitController: 单位 {name} 近战攻击无目标或目标为友方！");
                }
            }
        }
    }

    private void PerformRangedAttackLogic(TargetType targetType)
    {
        if (isDead)
        {
            Debug.Log($"UnitController: 单位 {name} 已死亡，无法进行远程攻击！");
            return;
        }
        
        // 远程攻击逻辑
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
                    Debug.Log($"UnitController: 单位 {name} 对 {targetUnit.name} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }
                else if (targetBuilding != null && targetBuilding.buildingData.camp != unitData.camp && !targetBuilding.isRuin)
                {
                    targetBuilding.TakeDamage(1);
                    Debug.Log($"UnitController: 单位 {name} 对建筑物 {targetBuilding.name} 进行远程攻击，造成1点伤害！");
                    hasAttacked = true;
                    break; // 只攻击第一个目标
                }

                currentPos += attackDirection;
            }

            if (!hasAttacked)
            {
                if (!TryAttackBoss(1))
                {
                    Debug.Log($"UnitController: 单位 {name} 远程攻击无目标或目标为友方！");
                }
            }
        }
    }

    private void IncreaseDefenseLogic(int value, TargetType targetType)
    {
        if (isDead)
        {
            Debug.Log($"UnitController: 单位 {name} 已死亡，无法增加防卫点数！");
            return;
        }
        
        if (targetType == TargetType.Friendly || targetType == TargetType.Self)
        {
            defensePoints += value;
            Debug.Log($"UnitController: 单位 {name} 防卫点数增加 {value}，当前防卫点数：{defensePoints}");
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {name} 尝试对非友方进行防卫！");
        }
    }
    
    private void PerformBreakageLogic(int breakagePoints)
    {
        if (isDead)
        {
            Debug.Log($"UnitController: 单位 {name} 已死亡，无法执行破壞！");
            return;
        }

        if (breakagePoints <= 0)
        {
            Debug.LogWarning($"UnitController: 单位 {name} 执行破壞时，破壞点数无效：{breakagePoints}");
            return;
        }

        // 查找相邻的敌方建筑或Boss
        List<ISkillUser> adjacentTargets = GetAdjacentTargets();

        if (adjacentTargets.Count == 0)
        {
            Debug.Log($"UnitController: 单位 {name} 执行破壞时，周围没有可破壞的目标！");
            return;
        }

        foreach (var target in adjacentTargets)
        {
            if (breakagePoints <= 0)
                break;

            // 对目标造成1点破壞伤害
            target.TakeDamage(1);
            breakagePoints--;

            Debug.Log($"UnitController: 单位 {name} 对目标 {target} 造成了1点破壞！");
        }
    }
    
    public virtual void RepairRuin(int value, TargetType targetType)
    {
        // 根据目标类型确定搜索方向
        Vector3Int searchDirection;
        if (targetType == TargetType.Enemy)
        {
            // 攻击敌方建筑物，搜索前方
            searchDirection = unitData.camp == Camp.Player ? Vector3Int.right : Vector3Int.left;
        }
        else if (targetType == TargetType.Friendly)
        {
            // 修复友方建筑物，搜索后方
            searchDirection = unitData.camp == Camp.Player ? Vector3Int.left : Vector3Int.right;
        }
        else
        {
            Debug.LogWarning($"UnitController: 未处理的 TargetType：{targetType}");
            return;
        }

        Vector3Int currentPos = gridPosition + searchDirection;
        bool actionPerformed = false;

        while (GridManager.Instance.IsWithinBattleArea(currentPos, includeBuildings: true))
        {
            BuildingController targetBuilding = GridManager.Instance.GetBuildingAt(currentPos);

            if (targetBuilding != null)
            {
                if (targetType == TargetType.Enemy && targetBuilding.buildingData.camp != unitData.camp)
                {
                    // 攻击敌方建筑物
                    int damage = value; // 您可以根据需要设置伤害值
                    targetBuilding.TakeDamage(damage);
                    Debug.Log($"UnitController: {name} 对敌方建筑 {targetBuilding.buildingData.name} 造成了 {damage} 点伤害！");
                    actionPerformed = true;
                    break; // 攻击后停止搜索
                }
                else if (targetType == TargetType.Friendly && targetBuilding.buildingData.camp == unitData.camp)
                {
                    if (!targetBuilding.isRuin)
                    {
                        // 修复友方建築
                        targetBuilding.currentHealth += value;
                        
                        // 如果生命值超过最大值，将其设置为最大值
                        targetBuilding.currentHealth = Mathf.Min(targetBuilding.currentHealth, targetBuilding.buildingData.maxHealth);
                        Debug.Log($"UnitController: {name} 修复了废墟 {targetBuilding.buildingData.name}！");
                        actionPerformed = true;
                    }
                    else
                    {
                        BossController boss = GridManager.Instance.GetBossUnit();
                        if (boss != null && boss.bossData.camp == unitData.camp)
                        {
                            boss.currentHealth += value;
                            // 如果生命值超过最大值，将其设置为最大值
                            boss.currentHealth = Mathf.Min(boss.currentHealth, boss.bossData.maxHealth);
                            Debug.Log($"UnitController: {name} 修复了 Boss！");
                            actionPerformed = true;
                        }
                    }
                    break; // 修复后停止搜索
                }
            }

            // 前进到下一个位置
            currentPos += searchDirection;
        }

        if (!actionPerformed)
        {
            string action = targetType == TargetType.Enemy ? "攻击的敌方建筑" : "修复的友方建筑";
            Debug.Log($"UnitController: {name} 没有找到可以{action}。");
        }
    }
    
    /// <summary>
    /// 接受伤害逻辑
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="source">伤害来源</param>
    public virtual void TakeDamage(int damage, DamageSource source = DamageSource.Normal)
    {
        if (isDead)
            return;
        
        if (HasState<InvincibleState>())
        {
            Debug.Log($"{name} 处于无敌状态，免疫伤害");
            return;
        }

        // 如果单位处于脆弱状态，且伤害来源不是疾病，则立即死亡
        if (HasState<FragileState>() && source == DamageSource.Normal)
        {
            Debug.Log($"UnitController: 单位 {name} 处于脆弱状态，受到正常伤害，将立即死亡！");
            //MoveToGraveyard();
            Die();
            return;
        }

        // 首先扣除防卫点数
        int remainingDamage = damage - defensePoints;
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            Debug.Log($"UnitController: 单位 {name} 接受 {remainingDamage} 点伤害，当前生命值: {currentHealth}");
        }
        else
        {
            // 防卫点数足以抵消所有伤害
            defensePoints -= damage;
            Debug.Log($"UnitController: 单位 {name} 防卫点数抵消了 {damage} 点伤害，剩余防卫点数: {defensePoints}");
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        else
        {
            // 如果未死亡，播放受击动画
            PlayHitAnimation();
        }
    }
    
    /// <summary>
    /// 处理单位死亡逻辑
    /// </summary>
    private void Die()
    {
        if (isDead)
            return;
        
        isDead = true; // 设置死亡标志

        // 调用 DeckManager 的 HandleUnitDeath 方法
        DeckManager.Instance.HandleUnitDeath(unitData, unitId, isInjured, unitData.camp == Camp.Player, isFragile);
        
        // 从 DeckManager 中移除技能延迟
        if (!string.IsNullOrEmpty(unitId))
        {
            DeckManager.Instance.RemoveUnitSkillDelays(unitData, unitId);
        }
        else
        {
            Debug.LogWarning("UnitController.Die: unitId 为 null，无法移除技能延迟！");
        }
        
        // 清空所有已排程的延遲動作
        scheduledActions.Clear();

        // 播放死亡动画后销毁
        PlayHitAnimation(() =>
        {
            Destroy(gameObject);
        });
    }
    
    /// <summary>
    /// 将单位移动回牌库
    /// </summary>
    private void MoveToDeck()
    {
        // 从战场移除
        GridManager.Instance.RemoveSkillUserAt(gridPosition);

        // 将单位返回到牌库，并更新牌组数量
        if (unitData.camp == Camp.Player)
        {
            // 更新玩家牌组
            DeckManager.Instance.AddCardToPlayerDeck(unitData, unitId, 1, isInjured);
            Debug.Log($"{name} 负伤，返回玩家牌库");
        }
        else if (unitData.camp == Camp.Enemy)
        {
            // 更新敌人牌组
            DeckManager.Instance.AddCardToEnemyDeck(unitData, unitId, 1, isInjured);
            Debug.Log($"{name} 负伤，返回敌人牌库");
        }
        else
        {
            Debug.LogWarning($"{name} 的阵营未知，无法返回牌库");
        }
    }

    /// <summary>
    /// 将单位移动到墓地
    /// </summary>
    private void MoveToGraveyard()
    {
        isDead = true; // 设置死亡标志
        
        // 从战场移除
        GridManager.Instance.RemoveSkillUserAt(gridPosition);
        
        // 将单位移动到墓地
        if (unitData.camp == Camp.Player)
        {
            GraveyardManager.Instance.AddToPlayerGraveyard(unitData, unitId);
        }
        else if (unitData.camp == Camp.Enemy)
        {
            GraveyardManager.Instance.AddToEnemyGraveyard(unitData, unitId);
        }
        
        // 从 DeckManager 中移除技能延迟
        if (!string.IsNullOrEmpty(unitId))
        {
            DeckManager.Instance.RemoveUnitSkillDelays(unitData, unitId);
        }
        else
        {
            Debug.LogWarning("UnitController.Die: unitId 为 null，无法移除技能延迟！");
        }

        // 销毁单位的游戏对象
        Destroy(gameObject);
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
        Debug.Log($"UnitController: 单位 {name} 恢复了 {amount} 点生命值，当前生命值: {currentHealth}");

        // 如果单位处于负伤状态，触发治疗
        if (HasState<InjuredState>())
        {
            AddState<HealedState>();
        }

        // 如果单位有疾病，优先治疗疾病
        if (diseaseLayers > 0)
        {
            HealDisease();
        }
    }
    
    /// <summary>
    /// 治疗疾病，每次调用减少一层
    /// </summary>
    public void HealDisease()
    {
        if (diseaseLayers > 0)
        {
            diseaseLayers--;
            Debug.Log($"UnitController: 单位 {name} 疾病层数减少到: {diseaseLayers}");
            if (diseaseLayers == 0)
            {
                RemoveState<DiseaseState>();
            }
            else
            {
                UpdateUnitUI();
            }
        }
    }
    
    /// <summary>
    /// 每回合结束时调用，减少一层疾病
    /// </summary>
    public void OnEndTurn()
    {
        if (diseaseLayers > 0)
        {
            diseaseLayers--;
            Debug.Log($"UnitController: 单位 {name} 疾病层数减少到: {diseaseLayers}");
            if (diseaseLayers == 0)
            {
                RemoveState<DiseaseState>();
            }
            else
            {
                UpdateUnitUI();
            }
        }
    }
    
    /// <summary>
    /// 添加疾病
    /// </summary>
    public void AddDisease()
    {
        diseaseLayers++;
        if (diseaseLayers == 1)
        {
            AddState<DiseaseState>();
        }
        Debug.Log($"UnitController: 单位 {name} 受到了疾病，目前疾病层数: {diseaseLayers}");
    }

    /// <summary>
    /// 销毁单位
    /// </summary>
    void DestroyUnit()
    {
        // 根据需求，可以添加单位销毁的动画或效果
        Debug.Log($"UnitController: 单位 {name} 被销毁");
        Destroy(gameObject);
        GridManager.Instance.RemoveUnitAt(gridPosition);
    }
    
    public virtual void ExecuteDefense()
    {
        if (unitData.mainSkillSO!= null)
        {
            var action = unitData.mainSkillSO.actions.Find(action => action.Type == SkillType.Defense);
            // 执行当前技能
            if (action != null)
            {
                IncreaseDefense(action.Value, action.TargetType);
                Debug.Log($"单位 {name} 执行了防卫技能！");
            }
        }
        else
        {
            Debug.LogWarning($"单位 {name} 没有配置防卫技能！");
        }
    }
    
    /// <summary>
    /// 获取与当前单位相邻的敌方建筑或Boss
    /// </summary>
    /// <returns>相邻的ISkillUser列表</returns>
    private List<ISkillUser> GetAdjacentTargets()
    {
        List<ISkillUser> targets = new List<ISkillUser>();

        // 定义相邻位置（左、右、上、下）
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
            gridPosition + Vector3Int.left,
            gridPosition + Vector3Int.right,
            gridPosition + Vector3Int.up,
            gridPosition + Vector3Int.down
        };

        foreach (var pos in adjacentPositions)
        {
            if (GridManager.Instance.HasSkillUserAt(pos))
            {
                ISkillUser user = GridManager.Instance.GetSkillUserAt(pos);

                // 检查目标是否为敌方建筑或Boss
                if (user is BuildingController building && building.buildingData.camp != unitData.camp)
                {
                    targets.Add(building);
                }
                else if (user is BossController boss && boss.bossData.camp != unitData.camp)
                {
                    targets.Add(boss);
                }
            }
        }

        return targets;
    }

    /// <summary>
    /// 使用主技能或支援技能
    /// </summary>
    public void UseSkill()
    {
        if (hasActedThisTurn)
        {
            return; // 单位已经行动过，跳过
        }

        if (isDead)
        {
            Debug.Log($"UnitController: 单位 {name} 已死亡，无法使用任何技能！");
            return;
        }

        if (CanUseMainSkill())
        {
            currentSkill = Skill.FromSkillSO(unitData.mainSkillSO);
            ScheduleSkillExecution(currentSkill);
        }
        else if (CanUseSupportSkill())
        {
            currentSkill = Skill.FromSkillSO(unitData.supportSkillSO);
            ScheduleSkillExecution(currentSkill);
        }

        hasActedThisTurn = true; // 标记为已行动
    }
    
    private void ScheduleSkillExecution(Skill skill)
    {
        BattleManager.Instance.ScheduleSkillExecution(skill, this);
    }
    
    /// <summary>
    /// 获取技能的当前延迟回合数
    /// </summary>
    private int GetSkillDelay(string skillName)
    {
        if (skillDelays.ContainsKey(skillName))
        {
            return skillDelays[skillName];
        }
        return 0;
    }
    
    /// <summary>
    /// 判断是否可以使用支援技能
    /// </summary>
    public bool CanUseSupportSkill()
    {
        if (unitData.supportSkillSO == null || unitData.supportSkillSO.actions == null)
        {
            Debug.LogWarning($"{name}: supportSkillSO 或其动作列表为 null！");
            return false;
        }

        return IsSkillReady(unitData.supportSkillSO.skillName);
    }

    /// <summary>
    /// 重置技能延迟
    /// </summary>
    private void ResetSkillDelay(string skillName, SkillSO skillSO)
    {
        if (skillSO == null)
            return;

        SkillActionData firstAction = skillSO.actions != null && skillSO.actions.Count > 0 ? skillSO.actions[0] : null;
        if (firstAction == null)
        {
            Debug.LogWarning($"UnitController: 技能 {skillName} 没有配置任何动作，无法重置延迟。");
            skillDelays[skillName] = 0;
        }
        else
        {
            int delay = firstAction.Delay;
            skillDelays[skillName] = delay;
            Debug.Log($"UnitController: 单位 {name} 技能 {skillName} 的延迟已重置为 {delay} 回合");
        }
    }

    /// <summary>
    /// 使用主技能
    /// </summary>
    public virtual IEnumerator UseMainSkill()
    {
        if (unitData.mainSkillSO != null)
        {
            // 检查技能是否准备就绪
            if (IsSkillReady(unitData.mainSkillSO.skillName))
            {
                // 为 currentSkill 赋值
                currentSkill = Skill.FromSkillSO(unitData.mainSkillSO);

                // 执行当前技能
                yield return StartCoroutine(ExecuteCurrentSkill());

                // 重置技能延迟
                ResetSkillDelay(unitData.mainSkillSO.skillName, unitData.mainSkillSO);
            }
            else
            {
                Debug.Log($"UnitController: 单位 {name} 的主技能 {unitData.mainSkillSO.skillName} 还在延迟中（剩余 {skillDelays[unitData.mainSkillSO.skillName]} 回合）");
            }
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {name} 没有配置主技能！");
            yield break;
        }
    }

    /// <summary>
    /// 使用支援技能
    /// </summary>
    public virtual IEnumerator UseSupportSkill()
    {
        if (unitData.supportSkillSO != null)
        {
            // 检查技能是否准备就绪
            if (IsSkillReady(unitData.supportSkillSO.skillName))
            {
                // 为 currentSkill 支援技能赋值
                currentSkill = Skill.FromSkillSO(unitData.supportSkillSO);

                // 执行当前技能
                yield return StartCoroutine(ExecuteCurrentSkill());

                // 重置技能延迟
                ResetSkillDelay(unitData.supportSkillSO.skillName, unitData.supportSkillSO);
            }
            else
            {
                Debug.Log($"UnitController: 单位 {name} 的支援技能 {unitData.supportSkillSO.skillName} 还在延迟中（剩余 {skillDelays[unitData.supportSkillSO.skillName]} 回合）");
            }
        }
        else
        {
            Debug.LogWarning($"UnitController: 单位 {name} 没有配置支援技能！");
            yield break;
        }
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
    /// 重新执行当前技能
    /// </summary>
    public virtual IEnumerator ExecuteCurrentSkill()
    {
        if (currentSkill != null)
        {
            foreach (var action in currentSkill.Actions)
            {
                if (action.Delay > 0)
                {
                    ScheduleAction(action);
                }
                else
                {
                    yield return StartCoroutine(ExecuteAction(action));
                }
            }

            // 清空當前技能動作，防止重複執行
            currentSkill.Actions.Clear();
        }

        #region Old Code
        /*
        if (currentSkill != null)
        {
            foreach (var action in currentSkill.Actions)
            {
                switch (action.Type)
                {
                    case SkillType.Move:
                        for (int i = 0; i < action.Value; i++)
                        {
                            if (!CanMoveForward())
                            {
                                Debug.Log($"UnitController: 单位 {name} 无法继续移动，技能执行被阻挡！");
                                break;
                            }
                            yield return StartCoroutine(MoveForward());
                        }
                        break;
                    case SkillType.Melee:
                        yield return StartCoroutine(PerformMeleeAttack(action.TargetType));
                        break;
                    case SkillType.Ranged:
                        yield return StartCoroutine(PerformRangedAttack(action.TargetType));
                        break;
                    case SkillType.Defense:
                        // Handle Defense if needed
                        //yield return StartCoroutine(IncreaseDefense(action.Value, action.TargetType));
                        yield return null;
                        break;
                    case SkillType.Breakage:
                        yield return StartCoroutine(PerformBreakage(action.Value));
                        break;
                    case SkillType.Repair:
                        RepairRuin(action.Value, action.TargetType);
                        yield return null;
                        break;
                    // 其他 SkillType
                    default:
                        Debug.LogWarning($"UnitController: 未处理的技能类型：{action.Type}");
                        yield return null;
                        break;
                }
            }

            // 清空当前技能动作，防止重复执行
            currentSkill.Actions.Clear();
        }
        */
        #endregion
    }

    
    /// <summary>
    /// 添加一个状态到单位
    /// </summary>
    /// <param name="state">要添加的状态实例</param>
    public void AddState(UnitStateBase state)
    {
        if(state == null)
        {
            Debug.LogError("UnitController: 尝试添加空的状态");
            return;
        }

        // 查找是否已存在该状态
        foreach(var existingState in currentStates)
        {
            if(existingState.GetType() == state.GetType())
            {
                Debug.LogWarning($"{name} 已经拥有 {existingState.StateName} 状态");
                return;
            }
        }

        // 添加状态
        currentStates.Add(state);
        state.OnEnter(this);
        AddStatusIcon(state);
    }
    
    /// <summary>
    /// 添加一個狀態到單位
    /// </summary>
    /// <typeparam name="T">狀態類型</typeparam>
    public void AddState<T>() where T : UnitStateBase
    {
        // 检查是否已拥有该状态
        foreach(var existingState in currentStates)
        {
            if(existingState.GetType() == typeof(T))
            {
                Debug.LogWarning($"{name} 已经拥有 {existingState.StateName} 状态");
                return;
            }
        }

        // 加载状态资源
        T newState = Resources.Load<T>($"UnitStates/{typeof(T).Name}");
        if(newState != null)
        {
            currentStates.Add(newState);
            newState.OnEnter(this);
            AddStatusIcon(newState);
        }
        else
        {
            Debug.LogError($"无法加载状态类型: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 移除一個狀態從單位
    /// </summary>
    /// <typeparam name="T">狀態類型</typeparam>
    public void RemoveState<T>() where T : UnitStateBase
    {
        UnitStateBase stateToRemove = null;
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
            Debug.LogWarning($"{name} 不存在 {typeof(T).Name} 状态");
        }
    }

    /// <summary>
    /// 检查单位是否拥有某个状态
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <returns>是否拥有该状态</returns>
    public bool HasState<T>() where T : UnitStateBase
    {
        foreach (var state in currentStates)
        {
            if (state.GetType() == typeof(T))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 添加状态图标到 Unit 的子物件
    /// </summary>
    /// <param name="state">要添加的状态</param>
    private void AddStatusIcon(UnitStateBase state)
    {
        if (statusIconsParent != null && StatusIconPool.Instance != null)
        {
            // 从对象池中获取一个状态图标
            GameObject iconGO = StatusIconPool.Instance.GetStatusIcon();
            SpriteRenderer iconSpriteRenderer = iconGO.GetComponent<SpriteRenderer>();
            if (iconSpriteRenderer != null)
            {
                iconSpriteRenderer.sprite = state.Icon;
                iconSpriteRenderer.enabled = true;

                // 设置图标的父对象为 statusIconsParent
                iconGO.transform.SetParent(statusIconsParent, false);

                // 设置图标的位置，依据当前活跃图标数量
                int iconCount = activeStatusIcons.Count;
                iconGO.transform.localPosition = new Vector3(iconCount * 0.2f, 0, 0); // 水平排列，每个图标间隔0.2单位
            }
            else
            {
                Debug.LogError("StatusIconPrefab 没有 SpriteRenderer 组件！");
            }
            activeStatusIcons[state.StateName] = iconGO;
        }
        else
        {
            Debug.LogError("UnitController: statusIconsParent 或 StatusIconPool.Instance 未设置！");
        }
    }

    /// <summary>
    /// 从 Unit 的子物件移除状态图标
    /// </summary>
    /// <param name="state">要移除的状态</param>
    private void RemoveStatusIcon(UnitStateBase state)
    {
        if (activeStatusIcons.ContainsKey(state.StateName))
        {
            GameObject iconGO = activeStatusIcons[state.StateName];
            // 将图标返回对象池
            StatusIconPool.Instance.ReturnStatusIcon(iconGO);
            activeStatusIcons.Remove(state.StateName);

            // 重新排列剩余的图标位置
            RepositionStatusIcons();
        }
        else
        {
            Debug.LogWarning($"UnitController: 状态图标 {state.StateName} 不存在于 activeStatusIcons 中");
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
    
    /// <summary>
    /// 播放攻击动画
    /// </summary>
    public IEnumerator PlayAttackAnimation(Action onComplete = null)
    {
        if (spriteTransform == null)
        {
            Debug.LogWarning("UnitController: spriteTransform 未设置，无法播放攻击动画！");
            onComplete?.Invoke();
            yield break;
        }

        float moveDistance = 0.5f; // 向前移动的距离
        float animationDuration = 0.2f; // 动画持续时间

        // 计算移动方向
        Vector3 direction = unitData.camp == Camp.Player ? Vector3.right : Vector3.left;

        // 停止当前动画
        spriteTransform.DOKill();

        // 动画序列
        Sequence attackSequence = DOTween.Sequence();

        // 向前移动（使用本地坐标）
        attackSequence.Append(spriteTransform.DOLocalMove(spriteTransform.localPosition + direction * moveDistance, animationDuration));

        // 返回原位（使用本地坐标）
        attackSequence.Append(spriteTransform.DOLocalMove(Vector3.zero, animationDuration)); // 假设原位为本地坐标的 (0,0,0)

        // 动画完成回调
        if (onComplete != null)
        {
            attackSequence.OnComplete(() => onComplete());
        }
        
        Debug.Log($"UnitController: {name} 播放攻击动画");
        yield return attackSequence.WaitForCompletion();
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
        Vector3 direction = unitData.camp == Camp.Player ? Vector3.left : Vector3.right;

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


    public IEnumerator ExecuteAction(SkillActionData action)
    {
        switch (action.Type)
        {
            case SkillType.Move:
                scheduledActions.Add(new ScheduledAction { action = action, remainingDelay = 0 });
                break;

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
                yield return StartCoroutine(IncreaseDefense(action.Value, action.TargetType));
                break;
            case SkillType.Breakage:
                yield return StartCoroutine(PerformBreakage(action.Value));
                break;
            case SkillType.Repair:
                RepairRuin(action.Value, action.TargetType);
                yield return null;
                break;
            // 其他 SkillType
            default:
                Debug.LogWarning($"UnitController: 未處理的技能類型：{action.Type}");
                yield return null;
                break;
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

    public bool IsOnField()
    {
        return GridManager.Instance.HasSkillUserAt(gridPosition);
    }

    public Camp GetCamp()
    {
        return unitData != null ? unitData.camp : Camp.Player; // 默认返回玩家阵营
    }

    /*public void ApplyStatusEffect(StatusEffect statusEffect)
    {
        // 根据状态效果的类型，添加状态
        AddState(statusEffect);
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
    
    public int GetMoveCount()
    {
        // 遍历单位的技能，查找移动技能并获取其移动次数
        int totalMoveCount = 0;

        // 假设单位的技能存储在 unitData 中
        if (unitData != null && unitData.mainSkillSO != null)
        {
            foreach (var action in unitData.mainSkillSO.actions)
            {
                if (action.Type == SkillType.Move)
                {
                    totalMoveCount += action.Value; // 累加移动次数
                }
            }
        }

        return totalMoveCount;
    }


}

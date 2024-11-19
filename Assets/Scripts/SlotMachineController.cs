using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;

/// <summary>
/// 控制拉霸机（老虎机）的行为
/// </summary>
public class SlotMachineController : MonoBehaviour
{
    [Header("Grid Manager")]
    public GridManager gridManager; // 引用 GridManager

    [Header("Connection Manager")]
    public ConnectionManager connectionManager; // 引用 ConnectionManager

    [Header("Decks")]
    public Deck playerDeck; // 玩家牌组
    public Deck enemyDeck;  // 敌人牌组

    [Header("Slot Machine Settings")]
    public float spinDuration = 5f; // 旋转时间
    public int maxCards = 24;        // 最大卡片数
    public float spinSpeed = 10f;    // 旋转速度
    public int spinLoopCount = 3;         // 旋转的循环次数
    public int extraRows = 2;             // 额外的行数，用于旋转效果
    public Ease spinEaseType = Ease.Linear; // 旋转的缓动类型
    
    [Header("Visual Settings")]
    public Tile[] spinTiles;         // 转盘使用的 Tile 集合

    private List<GameObject> spinningUnits = new List<GameObject>(); // 存储正在旋转的单位

    private List<Vector3Int> battlePositions = new List<Vector3Int>(); // 战斗区域的所有格子位置
    private List<UnitWithInjuryStatus> selectedCards = new List<UnitWithInjuryStatus>();        // 抽取的卡片包含负伤状态

    public bool isSpinning = false;
    
    private Vector3 cellSize;


    // 定义转盘完成的事件
    public delegate void SpinCompleted();
    public event SpinCompleted OnSpinCompleted;

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("SlotMachineController: GridManager 未设置！");
            return;
        }

        if (connectionManager == null)
        {
            Debug.LogError("SlotMachineController: ConnectionManager 未设置！");
            return;
        }

        if (playerDeck == null || enemyDeck == null)
        {
            Debug.LogError("SlotMachineController: 玩家牌组或敌人牌组未设置！");
            return;
        }

        InitializeBattlePositions();
        
        // 获取格子大小
        cellSize = gridManager.battleTilemap.cellSize;

        // 可以在这里调用 StartSpinning() 进行测试
        // StartSpinning();
    }

    /// <summary>
    /// 初始化战斗区域的所有格子位置
    /// </summary>
    void InitializeBattlePositions()
    {
        battlePositions.Clear();
        for (int row = 0; row < gridManager.rows; row++)
        {
            // 将列索引从 1 开始
            for (int col = 1; col <= gridManager.columns; col++) // 列从1到6
            {
                battlePositions.Add(new Vector3Int(col, row, 0));
            }
        }
    }

    /// <summary>
    /// 开始转动拉霸机
    /// </summary>
    public void StartSpinning()
    {
        if (isSpinning) 
        {
            Debug.LogWarning("SlotMachineController: 已经在旋转中！");
            return;
        }
        
        // 清空之前的单位
        ClearBattleAreaUnits();

        // 开始旋转
        StartCoroutine(SpinRoutine());
    }

    /// <summary>
    /// 拉霸机的转动协程
    /// </summary>
    private IEnumerator SpinRoutine()
    {
        isSpinning = true;

        // 初始化旋转单位
        InitializeSpinningUnits();

        // 计算总的移动距离
        float totalMovementDistance = cellSize.y * (gridManager.rows + extraRows); //*2

        // 计算单次循环的时间
        float singleSpinTime = totalMovementDistance / spinSpeed;

        // 设置每列之间的延迟（您可以调整此值）
        float columnDelay = 0.2f; // 每列延迟0.2秒

        // 使用 DOTween 控制旋转
        List<Tweener> tweens = new List<Tweener>();

        foreach (var unitGO in spinningUnits)
        {
            UnitController unitController = unitGO.GetComponent<UnitController>();

            // 计算单位的延迟时间
            float delay = (unitController.columnIndex - 1) * columnDelay;

            // 移动单位向下
            Tweener tween = unitGO.transform.DOMoveY(unitGO.transform.position.y - totalMovementDistance, singleSpinTime)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetDelay(delay); // 添加延迟

            tweens.Add(tween);
        }

        // 等待旋转持续时间加上最大延迟
        float totalDelay = (gridManager.columns - 1) * columnDelay;
        yield return new WaitForSeconds(spinDuration + totalDelay);

        // 停止所有旋转
        foreach (var tween in tweens)
        {
            tween.Kill();
        }

        // 将单位固定到格子上
        PlaceSpunUnits();
        
        // 执行加权抽取并放置卡牌
        WeightedDrawAndPlaceCards();

        isSpinning = false;

        // 触发旋转完成事件
        OnSpinCompleted?.Invoke();
    }

    private void InitializeSpinningUnits()
    {
        // 清理之前的单位
        foreach (var unitGO in spinningUnits)
        {
            Destroy(unitGO);
        }
        spinningUnits.Clear();

        // 获取格子大小
        Vector3 cellSize = gridManager.battleTilemap.cellSize;

        // 计算开始和结束的行索引
        int startRow = gridManager.rows - 1 + extraRows;
        int endRow = -extraRows;

        // 遍历每一列和每一行（包括额外的行）
        for (int col = 1; col <= gridManager.columns; col++)
        {
            for (int row = startRow; row >= endRow; row--)
            {
                Vector3Int gridPos = new Vector3Int(col, row, 0);
                Vector3 worldPos = gridManager.GetCellCenterWorld(gridPos);

                // 创建单位实例，使用随机的单位数据
                UnitData unitData = GetRandomUnitData();
                if (unitData == null)
                    continue;

                GameObject unitGO = Instantiate(gridManager.unitPrefab, worldPos, Quaternion.identity, gridManager.unitsParent);
                UnitController unitController = unitGO.GetComponent<UnitController>();
                unitController.unitData = unitData;
                unitController.InitializeUnitSprite();

                // 设置单位的列索引
                unitController.columnIndex = col;

                spinningUnits.Add(unitGO);
            }
        }
    }


    
    private UnitData GetRandomUnitData()
    {
        List<DeckEntry> allEntries = new List<DeckEntry>();
        allEntries.AddRange(playerDeck.entries);
        allEntries.AddRange(enemyDeck.entries);

        // 过滤掉数量为0的卡牌
        allEntries.RemoveAll(entry => entry.quantity <= 0);

        if (allEntries.Count == 0)
            return null;

        int index = Random.Range(0, allEntries.Count);
        return allEntries[index].unitData;
    }


    private void PlaceSpunUnits()
    {
        // 清空战斗区域内的单位
        //ClearBattleAreaUnits();

        // 如果需要，在此销毁所有旋转的单位
        foreach (var unitGO in spinningUnits)
        {
            Destroy(unitGO);
        }
        // 清空旋转单位列表
        spinningUnits.Clear();
    }

    // 随机获取一个 DeckEntry
    private DeckEntry GetRandomDeckEntry()
    {
        List<DeckEntry> allEntries = new List<DeckEntry>();
        allEntries.AddRange(playerDeck.entries);
        allEntries.AddRange(enemyDeck.entries);

        // 过滤掉数量为0的
        allEntries.RemoveAll(entry => entry.quantity <= 0);

        if (allEntries.Count == 0)
            return null;

        int index = Random.Range(0, allEntries.Count);
        return allEntries[index];
    }


    /// <summary>
    /// 执行加权抽取并放置卡片到战斗区域
    /// </summary>
    public void WeightedDrawAndPlaceCards()
    {
        Debug.Log("WeightedDrawAndPlaceCards: 开始抽取并放置卡牌");

        // 定义权重映射，可以根据需要调整权重值
        var weightMap = new Dictionary<PreferredPosition, int>
        {
            { PreferredPosition.Left, 4 },
            { PreferredPosition.Any, 2 },
            { PreferredPosition.Right, 1 }
        };

        // 获取所有可抽取的单位，包括正常和负伤的
        var availableUnits = new List<UnitWithQuantity>();

        // 获取玩家和敌人的可用单位
        var playerUnits = playerDeck.GetAllUnitsWithInjuryStatus();
        var enemyUnits = enemyDeck.GetAllUnitsWithInjuryStatus();

        // 合并所有可用单位
        var allUnits = new List<UnitWithInjuryStatus>();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);

        // 统计每种单位的数量
        foreach (var unit in allUnits)
        {
            var existingUnit = availableUnits.Find(u => u.unitData == unit.unitData && u.isInjured == unit.isInjured);
            if (existingUnit != null)
            {
                existingUnit.quantity += 1;
            }
            else
            {
                availableUnits.Add(new UnitWithQuantity(unit.unitData, 1, unit.isInjured));
            }
        }

        // 计算可用的总卡片数量
        var totalAvailableCards = 0;
        foreach (var unit in availableUnits)
        {
            totalAvailableCards += unit.quantity;
        }

        Debug.Log($"WeightedDrawAndPlaceCards: 总可用卡片数量: {totalAvailableCards}");

        // 如果总可用卡片数量小于 maxCards，调整 cardsToDraw
        var cardsToDraw = Mathf.Min(maxCards, totalAvailableCards);

        // 开始抽取过程
        for (var i = 0; i < cardsToDraw; i++)
        {
            // 更新当前可用的单位列表
            var currentAvailableUnits = new List<UnitWithQuantity>(availableUnits);

            if (currentAvailableUnits.Count == 0)
            {
                Debug.Log("WeightedDrawAndPlaceCards: 没有更多可抽取的卡牌！");
                break;
            }

            // 计算总权重
            var totalWeight = 0;
            foreach (var unit in currentAvailableUnits)
            {
                var pos = unit.unitData.preferredPosition;
                var entryWeight = weightMap.ContainsKey(pos) ? weightMap[pos] :
                    weightMap.ContainsKey(PreferredPosition.Any) ? weightMap[PreferredPosition.Any] : 1;

                totalWeight += unit.quantity * entryWeight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning("WeightedDrawAndPlaceCards: 总权重为0，无法继续抽取！");
                break;
            }

            // 生成一个随机数
            var randomWeight = Random.Range(0, totalWeight);

            // 选择对应的单位
            var cumulativeWeight = 0;
            UnitWithQuantity selectedUnit = null;
            foreach (var unit in currentAvailableUnits)
            {
                var pos = unit.unitData.preferredPosition;
                var entryWeight = weightMap.ContainsKey(pos) ? weightMap[pos] :
                    weightMap.ContainsKey(PreferredPosition.Any) ? weightMap[PreferredPosition.Any] : 1;

                var unitWeight = unit.quantity * entryWeight;
                cumulativeWeight += unitWeight;

                if (randomWeight < cumulativeWeight)
                {
                    selectedUnit = unit;
                    break;
                }
            }

            if (selectedUnit == null)
            {
                Debug.LogWarning("WeightedDrawAndPlaceCards: 没有选中的单位！");
                break;
            }

            // 添加到 selectedCards 并减少数量
            selectedCards.Add(new UnitWithInjuryStatus(selectedUnit.unitData, selectedUnit.isInjured));
            selectedUnit.quantity -= 1;

            if (selectedUnit.quantity <= 0)
            {
                availableUnits.Remove(selectedUnit);
            }

            // 从对应的牌组中减少数量
            DeckEntry correspondingEntry = null;
            if (selectedUnit.unitData.camp == Camp.Player)
            {
                correspondingEntry = playerDeck.entries.Find(entry => entry.unitData == selectedUnit.unitData);
            }
            else if (selectedUnit.unitData.camp == Camp.Enemy)
            {
                correspondingEntry = enemyDeck.entries.Find(entry => entry.unitData == selectedUnit.unitData);
            }

            if (correspondingEntry != null)
            {
                if (selectedUnit.isInjured)
                    correspondingEntry.injuredQuantity -= 1;
                else
                    correspondingEntry.quantity -= 1;
            }
        }

        // 放置卡片到战斗区域
        ShuffleAndPlaceCards();
    }
    
    // 定义一个类，用于存储单位、数量和负伤状态
    public class UnitWithQuantity
    {
        public UnitData unitData;
        public int quantity;
        public bool isInjured;

        public UnitWithQuantity(UnitData unitData, int quantity, bool isInjured)
        {
            this.unitData = unitData;
            this.quantity = quantity;
            this.isInjured = isInjured;
        }
    }
    
    /// <summary>
    /// 随机放置卡片到战斗区域的格子上，若不足 maxCards 张，用空白格填充
    /// </summary>
    void ShuffleAndPlaceCards()
    {
        Debug.Log("ShuffleAndPlaceCards: 开始放置卡牌到战斗区域");

        // 清空战斗区域内的单位
        ClearBattleAreaUnits();

        // 随机排列 battlePositions
        List<Vector3Int> shuffledPositions = new List<Vector3Int>(battlePositions);
        for (int i = shuffledPositions.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            Vector3Int temp = shuffledPositions[i];
            shuffledPositions[i] = shuffledPositions[rnd];
            shuffledPositions[rnd] = temp;
        }

        int positionCount = Mathf.Min(maxCards, shuffledPositions.Count);
        Debug.Log($"ShuffleAndPlaceCards: 需要放置的格子数量: {positionCount}");

        for (int i = 0; i < positionCount; i++)
        {
            Vector3Int selectedPos = shuffledPositions[i];

            if (i < selectedCards.Count)
            {
                // 放置卡片
                UnitWithInjuryStatus unitToSpawn = selectedCards[i];
                gridManager.SpawnUnit(selectedPos, unitToSpawn.unitData, unitToSpawn.isInjured);
            }
            else
            {
                // 用空白格填充（可选择不做任何操作或显示空白图）
                // 此处暂不处理
            }
        }
    }

    /// <summary>
    /// 清空战斗区域上的所有单位
    /// </summary>
    void ClearBattleAreaUnits()
    {
        Debug.Log("ClearBattleAreaUnits: 开始清空战斗区域的单位");

        // 遍历所有战斗区域格子，销毁存在的单位
        foreach (var pos in battlePositions)
        {
            if (gridManager.HasSkillUserAt(pos))
            {
                ISkillUser user = gridManager.GetSkillUserAt(pos);
                if (user != null)
                {
                    if (user is MonoBehaviour monoUser)
                    {
                        Debug.Log($"ClearBattleAreaUnits: 销毁位置 {pos} 上的 {monoUser.gameObject.name}");
                        Destroy(monoUser.gameObject);
                    }
                    gridManager.RemoveSkillUserAt(pos); // 确保从字典中移除
                }
            }
        }

        Debug.Log("ClearBattleAreaUnits: 战斗区域的单位已清空");
    }

    /// <summary>
    /// 更新转盘上的 Tile 以模拟旋转
    /// </summary>
    private void UpdateSpinTiles()
    {
        Tilemap battleTilemap = gridManager.battleTilemap;
        if (battleTilemap == null)
        {
            Debug.LogError("SlotMachineController: battleTilemap 未设置！");
            return;
        }

        foreach (var position in battlePositions)
        {
            if (spinTiles.Length > 0)
            {
                int randomIndex = Random.Range(0, spinTiles.Length);
                //battleTilemap.SetTile(position, spinTiles[randomIndex]);
            }
        }
    }

    /// <summary>
    /// 清除转盘上的 Tile
    /// </summary>
    private void ClearSpinTiles()
    {
        Tilemap battleTilemap = gridManager.battleTilemap;
        if (battleTilemap == null)
        {
            Debug.LogError("SlotMachineController: battleTilemap 未设置！");
            return;
        }

        foreach (var position in battlePositions)
        {
            //battleTilemap.SetTile(position, null);
        }
    }
}

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
    private List<UnitData> selectedCards = new List<UnitData>();        // 抽取的卡片

    public bool isSpinning = false;
    
    private Vector3 cellSize;


    // 定义转盘完成的事件
    public delegate void SpinCompleted(int selectedColumn);
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

        // 抽取卡片
        WeightedDrawCards();

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
        float totalMovementDistance = cellSize.y * (gridManager.rows + extraRows * 2);

        // 计算单次循环的时间
        float singleSpinTime = totalMovementDistance / spinSpeed;

        // 使用 DOTween 控制旋转
        List<Tweener> tweens = new List<Tweener>();

        foreach (var unitGO in spinningUnits)
        {
            // 移动单位向下
            Tweener tween = unitGO.transform.DOMoveY(unitGO.transform.position.y - totalMovementDistance, singleSpinTime)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);

            tweens.Add(tween);
        }

        // 等待旋转持续时间
        yield return new WaitForSeconds(spinDuration);

        // 停止所有旋转
        foreach (var tween in tweens)
        {
            tween.Kill();
        }

        // 将单位固定到格子上
        PlaceSpunUnits();

        isSpinning = false;

        // 触发旋转完成事件
        OnSpinCompleted?.Invoke(0);
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

                // 创建单位实例
                UnitData unitData = GetRandomUnitData();
                GameObject unitGO = Instantiate(gridManager.unitPrefab, worldPos, Quaternion.identity, gridManager.unitsParent);
                UnitController unitController = unitGO.GetComponent<UnitController>();
                unitController.unitData = unitData;
                unitController.InitializeUnitSprite();

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
        ClearBattleAreaUnits();

        // 遍历所有旋转的单位
        foreach (var unitGO in spinningUnits)
        {
            UnitController unitController = unitGO.GetComponent<UnitController>();

            // 获取单位当前位置对应的格子坐标
            Vector3 worldPos = unitGO.transform.position;
            Vector3Int gridPos = gridManager.battleTilemap.WorldToCell(worldPos);

            // 调整格子坐标以匹配战斗区域
            gridPos.x += gridManager.battleTilemap.cellBounds.xMin;
            gridPos.y += gridManager.battleTilemap.cellBounds.yMin;

            // 检查位置是否在战斗区域内
            if (gridManager.IsWithinBattleArea(gridPos))
            {
                unitController.SetPosition(gridPos);

                // 将单位添加到 GridManager
                gridManager.AddUnitAt(gridPos, unitController);
            }
            else
            {
                // 销毁不在战斗区域内的单位
                Destroy(unitGO);
            }
        }

        // 清空旋转单位列表
        spinningUnits.Clear();
    }
    
    private void WeightedDrawCards()
    {
        // 您可以保留现有的抽卡逻辑，但不立即放置卡片
        // 这里简化为随机选择一些单位数据
        selectedCards.Clear();

        // 假设每列一个单位，总共 columns 个单位
        int cardsToDraw = gridManager.columns;

        for (int i = 0; i < cardsToDraw; i++)
        {
            // 从玩家和敌人牌组中随机抽取
            DeckEntry entry = GetRandomDeckEntry();
            if (entry != null)
            {
                selectedCards.Add(entry.unitData);
            }
        }
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
    /// 执行加权抽取，根据选中的列和单元的偏好位置
    /// </summary>
    /// <param name="selectedColumn">转盘选择的列</param>
    public void WeightedDrawAndPlaceCards(int selectedColumn)
    {
        Debug.Log($"WeightedDrawAndPlaceCards: 开始，选中的列: {selectedColumn}");

        // 根据选中的列确定加权策略
        var isLeftSide = selectedColumn >= 1 && selectedColumn <= 3;

        // 定义权重
        var weightMap = new Dictionary<PreferredPosition, int>();

        if (isLeftSide)
        {
            weightMap[PreferredPosition.Left] = 4;
            weightMap[PreferredPosition.Any] = 2;
            weightMap[PreferredPosition.Right] = 1;
        }
        else // selectedColumn 4-6
        {
            weightMap[PreferredPosition.Right] = 4;
            weightMap[PreferredPosition.Any] = 2;
            weightMap[PreferredPosition.Left] = 1;
        }

        // 获取所有可抽取的单位
        var availableEntries = new List<DeckEntry>();
        availableEntries.AddRange(playerDeck.entries);
        availableEntries.AddRange(enemyDeck.entries);

        Debug.Log($"WeightedDrawAndPlaceCards: 可用的 DeckEntry 数量: {availableEntries.Count}");

        // 初始化抽取的卡片列表
        selectedCards = new List<UnitData>();

        // 计算可用的总卡片数量
        var totalAvailableCards = 0;
        foreach (var entry in availableEntries)
            if (entry.unitData != null && entry.quantity > 0)
                totalAvailableCards += entry.quantity;

        Debug.Log($"WeightedDrawAndPlaceCards: 总可用卡片数量: {totalAvailableCards}");

        // 如果总可用卡片数量小于 maxCards，调整 cardsToDraw
        var cardsToDraw = Mathf.Min(maxCards, totalAvailableCards);

        // 开始抽取过程
        for (var i = 0; i < cardsToDraw; i++)
        {
            // 更新当前可用的 DeckEntry（quantity > 0）
            var currentAvailableEntries = new List<DeckEntry>();
            foreach (var entry in availableEntries)
                if (entry.unitData != null && entry.quantity > 0)
                    currentAvailableEntries.Add(entry);

            if (currentAvailableEntries.Count == 0)
            {
                Debug.Log("WeightedDrawAndPlaceCards: 没有更多可抽取的卡牌！");
                break;
            }

            // 计算总权重
            var totalWeight = 0;
            foreach (var entry in currentAvailableEntries)
            {
                var pos = entry.unitData.preferredPosition;
                if (weightMap.ContainsKey(pos))
                    totalWeight += weightMap[pos];
                else
                    // 默认权重，如果没有指定
                    totalWeight += weightMap.ContainsKey(PreferredPosition.Any) ? weightMap[PreferredPosition.Any] : 1;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning("WeightedDrawAndPlaceCards: 总权重为0，无法继续抽取！");
                break;
            }

            // 生成一个随机数
            var randomWeight = Random.Range(0, totalWeight);
            //Debug.Log($"WeightedDrawAndPlaceCards: 随机数: {randomWeight}，总权重: {totalWeight}");

            // 选择对应的 DeckEntry
            var cumulativeWeight = 0;
            DeckEntry selectedEntry = null;
            foreach (var entry in currentAvailableEntries)
            {
                var pos = entry.unitData.preferredPosition;
                var entryWeight = weightMap.ContainsKey(pos) ? weightMap[pos] :
                    weightMap.ContainsKey(PreferredPosition.Any) ? weightMap[PreferredPosition.Any] : 1;
                cumulativeWeight += entryWeight;
                if (randomWeight < cumulativeWeight)
                {
                    selectedEntry = entry;
                    break;
                }
            }

            if (selectedEntry == null)
            {
                Debug.LogWarning("WeightedDrawAndPlaceCards: 没有选中的 DeckEntry！");
                break;
            }

            // 添加到 selectedCards 并减少 quantity
            selectedCards.Add(selectedEntry.unitData);
            //Debug.Log($"WeightedDrawAndPlaceCards: 抽取卡牌: {selectedEntry.unitData.unitName}");

            selectedEntry.quantity -= 1;
            //Debug.Log($"WeightedDrawAndPlaceCards: {selectedEntry.unitData.unitName} 剩余数量: {selectedEntry.quantity}");
        }

        //Debug.Log($"WeightedDrawAndPlaceCards: 抽取的卡牌数量: {selectedCards.Count}");

        // 放置卡片到战斗区域
        ShuffleAndPlaceCards();
    }



    /// <summary>
    /// 随机放置卡片到战斗区域的格子上，若不足 maxCards 张，用空白格填充
    /// </summary>
    void ShuffleAndPlaceCards()
    {
        Debug.Log("ShuffleAndPlaceCards: 开始放置卡牌到战斗区域");

        // 不再调用 ClearBattleAreaUnits()
        // ClearBattleAreaUnits();

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
                UnitData unitDataToSpawn = selectedCards[i];
                //Debug.Log($"ShuffleAndPlaceCards: 在位置 {selectedPos} 放置单位 {unitDataToSpawn.unitName}");
                gridManager.SpawnUnit(selectedPos, unitDataToSpawn);
            }
            else
            {
                // 用空白格填充（可选择不做任何操作或显示空白图）
                // 此处暂不处理
                //Debug.Log($"ShuffleAndPlaceCards: 在位置 {selectedPos} 放置空白格");
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

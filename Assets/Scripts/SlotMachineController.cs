using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    [Header("Visual Settings")]
    public Tile[] spinTiles;         // 转盘使用的 Tile 集合

    private List<Vector3Int> battlePositions = new List<Vector3Int>(); // 战斗区域的所有格子位置
    private List<UnitData> selectedCards = new List<UnitData>();        // 抽取的卡片

    public bool isSpinning = false;

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

        StartCoroutine(SpinRoutine());
    }

    /// <summary>
    /// 拉霸机的转动协程
    /// </summary>
    IEnumerator SpinRoutine()
    {
        isSpinning = true;

        float elapsed = 0f;
        float spinInterval = 1f / spinSpeed;

        // 清空之前的单位和 Tile
        ClearBattleAreaUnits();
        //ClearSpinTiles();

        // 开始旋转视觉效果
        while (elapsed < spinDuration)
        {
            //UpdateSpinTiles(); // 更新 Tile 以模拟旋转
            elapsed += spinInterval;
            yield return new WaitForSeconds(spinInterval);
        }

        isSpinning = false;

        // 停止旋转视觉效果
        //ClearSpinTiles();

        // 选取一列作为转盘选择的列（示例逻辑）
        int selectedColumn = Random.Range(1, gridManager.columns + 1);
        Debug.Log($"SlotMachineController: 选择的列为 {selectedColumn}");

        // 触发转盘完成事件
        OnSpinCompleted?.Invoke(selectedColumn);

        // 不再在这里调用 WeightedDrawAndPlaceCards
        // 因为 BattleManager 会通过事件处理
        Debug.Log("SlotMachineController: 拉霸机停止，等待 BattleManager 处理后续流程");
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

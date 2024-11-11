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

    [Header("Cards")]
    public List<UnitData> playerDeck; // 玩家牌组
    public List<UnitData> enemyDeck;  // 敌人牌组

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
        if (isSpinning) return;

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

        // 进行游戏逻辑
        selectedCards = DrawCards();
        ShuffleAndPlaceCards();

        // 选取一列作为转盘选择的列（示例逻辑）
        int selectedColumn = Random.Range(1, gridManager.columns + 1);
        Debug.Log($"SlotMachineController: 选择的列为 {selectedColumn}");

        // 触发转盘完成事件
        OnSpinCompleted?.Invoke(selectedColumn);

        // 检查连线
        Debug.Log("SlotMachineController: 拉霸机停止，进入连线阶段");
        // connectionManager.CheckConnections(); // 已由 BattleManager 负责
    }

    /// <summary>
    /// 抽取卡片，数量不超过 maxCards 张，不重复
    /// </summary>
    /// <returns>抽取的卡片列表</returns>
    List<UnitData> DrawCards()
    {
        List<UnitData> deck = new List<UnitData>();
        deck.AddRange(playerDeck);
        deck.AddRange(enemyDeck);

        List<UnitData> drawnCards = new List<UnitData>();
        int drawCount = Mathf.Min(maxCards, deck.Count);

        // 洗牌
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            UnitData temp = deck[i];
            deck[i] = deck[rnd];
            deck[rnd] = temp;
        }

        // 抽取前 maxCards 张
        for (int i = 0; i < drawCount; i++)
        {
            drawnCards.Add(deck[i]);
        }

        return drawnCards;
    }

    /// <summary>
    /// 随机放置卡片到战斗区域的格子上，卡片不重复，若不足 maxCards 张，用空白格填充
    /// </summary>
    void ShuffleAndPlaceCards()
    {
        // 清空之前的单位
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

        for (int i = 0; i < positionCount; i++)
        {
            Vector3Int selectedPos = shuffledPositions[i];

            if (i < selectedCards.Count)
            {
                // 放置卡片
                gridManager.SpawnUnit(selectedPos, selectedCards[i]);
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
        // 遍历所有战斗区域格子，销毁存在的单位
        foreach (var pos in battlePositions)
        {
            if (gridManager.HasSkillUserAt(pos))
            {
                UnitController unit = gridManager.GetUnitAt(pos);
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
                gridManager.RemoveUnitAt(pos); // 确保从字典中移除
            }
        }
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

    /// <summary>
    /// 执行加权抽取，根据选中的列和单元的偏好位置
    /// </summary>
    /// <param name="selectedColumn">转盘选择的列</param>
    public void WeightedDrawAndPlaceCards(int selectedColumn)
    {
        // 根据选中的列确定加权策略
        bool isLeftSide = selectedColumn >= 1 && selectedColumn <= 3;
        // bool isRightSide = selectedColumn >=4 && selectedColumn <=6;

        // 定义权重
        Dictionary<PreferredPosition, int> weightMap = new Dictionary<PreferredPosition, int>();

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
        List<UnitData> availableUnits = new List<UnitData>();
        availableUnits.AddRange(playerDeck);
        availableUnits.AddRange(enemyDeck);

        // 创建加权列表
        List<UnitData> weightedList = new List<UnitData>();
        foreach (var unit in availableUnits)
        {
            if (weightMap.ContainsKey(unit.preferredPosition))
            {
                int weight = weightMap[unit.preferredPosition];
                for (int w = 0; w < weight; w++)
                {
                    weightedList.Add(unit);
                }
            }
            else
            {
                // 如果单位没有指定偏好位置，默认为 Any
                int weight = weightMap[PreferredPosition.Any];
                for (int w = 0; w < weight; w++)
                {
                    weightedList.Add(unit);
                }
            }
        }

        // 随机抽取 maxCards 张
        selectedCards = new List<UnitData>();
        for (int i = 0; i < maxCards && weightedList.Count > 0; i++)
        {
            int rndIndex = Random.Range(0, weightedList.Count);
            UnitData selectedUnit = weightedList[rndIndex];
            selectedCards.Add(selectedUnit);
            // 移除所有与 selectedUnit 相同的实例，以避免重复
            weightedList.RemoveAll(u => u == selectedUnit);
        }

        // 如果抽取的卡片少于 maxCards，用空白格填充（可选择不做任何操作或显示空白图）
        // 此处暂不处理

        // 放置卡片到战斗区域
        ShuffleAndPlaceCards();
    }
}

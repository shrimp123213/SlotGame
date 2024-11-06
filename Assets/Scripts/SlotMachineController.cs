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

    private bool isSpinning = false;

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
        StartSpinning();
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
            for (int col = 1; col <= gridManager.columns; col++) // 列从 1 到 columns
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

        // 检查连线
        Debug.Log("拉霸机停止，进入连线阶段");
        connectionManager.CheckConnections();
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
}

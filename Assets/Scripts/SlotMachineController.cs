using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制拉霸機（老虎機）的行為
/// </summary>
public class SlotMachineController : MonoBehaviour
{
    [Header("Grid Manager")]
    public GridManager gridManager; // 引用GridManager

    [Header("Connection Manager")]
    public ConnectionManager connectionManager; // 引用ConnectionManager

    [Header("Cards")]
    public List<UnitData> playerDeck; // 玩家牌組
    public List<UnitData> enemyDeck;  // 敵人牌組

    [Header("Slot Machine Settings")]
    public float spinDuration = 5f; // 轉動時間
    public int maxCards = 24;        // 最大卡片數

    private List<Vector3Int> battlePositions = new List<Vector3Int>(); // 戰鬥區域的所有格子位置
    private List<UnitData> selectedCards = new List<UnitData>();        // 抽取的卡片

    private bool isSpinning = false;

    void Start()
    {
        InitializeBattlePositions();
        // 您可以在此處調用 StartSpinning() 進行測試
         StartSpinning();
    }

    /// <summary>
    /// 初始化戰鬥區域的所有格子位置
    /// </summary>
    void InitializeBattlePositions()
    {
        for (int row = 0; row < gridManager.rows; row++)
        {
            for (int col = 1; col <= gridManager.columns; col++) // 列從1到6
            {
                battlePositions.Add(new Vector3Int(col, row, 0));
            }
        }
    }

    /// <summary>
    /// 開始轉動拉霸機
    /// </summary>
    public void StartSpinning()
    {
        if (isSpinning) return;

        StartCoroutine(SpinRoutine());
    }

    /// <summary>
    /// 拉霸機的轉動協程
    /// </summary>
    /// <returns></returns>
    IEnumerator SpinRoutine()
    {
        isSpinning = true;

        // 抽取卡片
        selectedCards = DrawCards();

        // 隨機放置卡片
        ShuffleAndPlaceCards();

        // 轉動持續時間
        yield return new WaitForSeconds(spinDuration);

        isSpinning = false;

        // 停止轉動後的處理
        Debug.Log("拉霸機停止，進入連線階段");

        // 檢查連線
        connectionManager.CheckConnections();
    }

    /// <summary>
    /// 抽取卡片，總數不超過24張，不重複
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

        // 抽取前maxCards張
        for (int i = 0; i < drawCount; i++)
        {
            drawnCards.Add(deck[i]);
        }

        return drawnCards;
    }

    /// <summary>
    /// 隨機放置卡片到戰鬥區域的格子上，卡片不重複，若不足24張，用空白格填充
    /// </summary>
    void ShuffleAndPlaceCards()
    {
        // 清空之前的單位
        ClearBattleAreaUnits();

        // 隨機選擇24個位置
        List<Vector3Int> availablePositions = new List<Vector3Int>(battlePositions);
        for (int i = 0; i < maxCards; i++)
        {
            if (availablePositions.Count == 0) break;

            int rndIndex = Random.Range(0, availablePositions.Count);
            Vector3Int selectedPos = availablePositions[rndIndex];
            availablePositions.RemoveAt(rndIndex);

            if (i < selectedCards.Count)
            {
                // 放置卡片
                gridManager.SpawnUnit(selectedPos, selectedCards[i]);
            }
            else
            {
                // 用空白格填充（可選擇不做任何操作或顯示空白圖）
                // 例如，可以在此處設置一個空的Tile或顯示空白圖
            }
        }
    }

    /// <summary>
    /// 清空戰鬥區域上的所有單位
    /// </summary>
    void ClearBattleAreaUnits()
    {
        // 遍歷所有戰鬥區域格子，銷毀存在的單位
        foreach (var pos in battlePositions)
        {
            if (gridManager.HasUnitAt(pos))
            {
                UnitController unit = gridManager.GetUnitAt(pos);
                Destroy(unit.gameObject);
                gridManager.RemoveUnitAt(pos); // 確保從字典中移除
            }
        }
    }
}

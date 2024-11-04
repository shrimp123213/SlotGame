using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public GridManager gridManager;      // 棋盤管理器
    public Unit playerUnitPrefab;        // 玩家單位的預製體
    public Unit enemyUnitPrefab;         // 敵方單位的預製體

    private void Start()
    {
        // 初始化遊戲
        gridManager.InitializeGrid();

        // 根據遊戲流程進行其他初始化和流程控制
        StartCoroutine(GameFlow());
    }

    private IEnumerator GameFlow()
    {
        // 0. 起始畫面（ProtoType 版本跳過）

        // 1. 選擇領袖（ProtoType 版本跳過）

        // 2.0 戰鬥開始前
        PreBattleEffects();

        // 2.1 戰鬥畫面 轉盤
        yield return SpinSlotMachine();

        // 2.2 戰鬥畫面 防衛
        ExecuteDefenseEffects();

        // 2.3 - 2.9 波次行動
        for (int wave = 1; wave <= 9; wave++)
        {
            yield return ExecuteWave(wave);
        }

        // 2.10 連線計算
        CalculateCombo();

        // 2.11 回合結束效果
        EndTurnEffects();

        // 3.1 三選一（ProtoType 版本可跳過或實現簡單版本）

        // 重複流程或結束遊戲
    }

    private void PreBattleEffects()
    {
        // 執行戰鬥開始前的效果
    }

    private IEnumerator SpinSlotMachine()
    {
        // 實現老虎機轉盤的效果
        // 可以模擬轉動過程，並隨機選擇結果
        yield return new WaitForSeconds(2f); // 模擬轉盤時間
    }

    private void ExecuteDefenseEffects()
    {
        // 執行所有防衛效果
    }

    private IEnumerator ExecuteWave(int waveNumber)
    {
        // 根據波次執行相應的單位行動
        switch (waveNumber)
        {
            case 1:
                // 我方建築行動
                break;
            case 2:
                // 第1列的單位行動
                break;
            // ...其他波次
            case 9:
                // 敵方Boss行動
                break;
        }

        yield return null; // 等待一幀或指定時間
    }

    private void CalculateCombo()
    {
        // 計算連線COMBO
    }

    private void EndTurnEffects()
    {
        // 執行回合結束效果
    }
}
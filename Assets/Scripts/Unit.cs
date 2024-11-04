using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitData unitData;    // 引用 UnitData 資源

    public bool IsPlayerOwned { get; set; }      // 是否屬於玩家
    public bool IsLinked { get; set; }           // 是否已經參與連線

    // 初始化單位
    public void InitializeUnit(UnitData data, bool isPlayerOwned)
    {
        unitData = data;

        IsPlayerOwned = isPlayerOwned;
        IsLinked = false;

        // 根據數據設置單位的屬性，例如名稱、圖標等
        gameObject.name = data.dataName;
        // 設置圖標、模型等
    }

    // 單位的行動
    public void PerformAction(GridManager gridManager)
    {
        // 行動邏輯
        UnitAction action = new UnitAction(this, gridManager);
        action.Execute();
    }
}
// GraveyardManager.cs
using System.Collections.Generic;
using UnityEngine;

public class GraveyardManager : MonoBehaviour
{
    public static GraveyardManager Instance { get; private set; }

    [Header("Graveyards")]
    private Deck playerGraveyard;
    private Deck enemyGraveyard;
    // 事件，当墓地有新卡牌时触发
    public delegate void GraveyardUpdated();
    public event GraveyardUpdated OnPlayerGraveyardUpdated;
    public event GraveyardUpdated OnEnemyGraveyardUpdated;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 根据需要保持在场景切换中不被销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 确保 DeckManager 已初始化
        if (DeckManager.Instance == null)
        {
            Debug.LogError("GraveyardManager: DeckManager 实例未找到！");
            return;
        }
        
        // 初始化墓地
        playerGraveyard = ScriptableObject.CreateInstance<Deck>();
        enemyGraveyard = ScriptableObject.CreateInstance<Deck>();
    }

    /// <summary>
    /// 添加单位到玩家墓地
    /// </summary>
    public void AddToPlayerGraveyard(UnitData unitData, string unitId)
    {
        if (unitData == null)
        {
            Debug.LogError("GraveyardManager: unitData 为 null，无法添加到玩家墓地！");
            return;
        }

        // 添加到玩家墓地牌组
        playerGraveyard.AddCard(unitData, unitId, 1, false, null);
        OnPlayerGraveyardUpdated?.Invoke();
    }

    public void AddToEnemyGraveyard(UnitData unitData, string unitId)
    {
        if (unitData == null)
        {
            Debug.LogError("GraveyardManager: unitData 为 null，无法添加到敌人墓地！");
            return;
        }

        // 添加到敌人墓地牌组
        enemyGraveyard.AddCard(unitData, unitId, 1, false, null);
        OnEnemyGraveyardUpdated?.Invoke();
    }

    /// <summary>
    /// 获取玩家墓地的所有卡牌
    /// </summary>
    public List<UnitData> GetPlayerGraveyard()
    {
        if (playerGraveyard != null)
        {
            List<UnitData> unitDatas = new List<UnitData>();
            foreach (var unitsWithInjuryStatu in playerGraveyard.GetAllUnitsWithInjuryStatus())
            {
                UnitData unitData = unitsWithInjuryStatu.unitData;
                if (unitData != null)
                {
                    unitDatas.Add(unitData);
                }
            }
            
            return unitDatas;
        }
        else
        {
            Debug.LogError("GraveyardManager: playerGraveyard 未赋值！");
            return new List<UnitData>();
        }
    }

    /// <summary>
    /// 获取敌人墓地的所有卡牌
    /// </summary>
    public List<UnitData> GetEnemyGraveyard()
    {
        if (enemyGraveyard != null)
        {
            List<UnitData> unitDatas = new List<UnitData>();
            foreach (var unitsWithInjuryStatu in enemyGraveyard.GetAllUnitsWithInjuryStatus())
            {
                UnitData unitData = unitsWithInjuryStatu.unitData;
                if (unitData != null)
                {
                    unitDatas.Add(unitData);
                }
            }
            
            return unitDatas;
        }
        else
        {
            Debug.LogError("GraveyardManager: enemyGraveyard 未赋值！");
            return new List<UnitData>();
        }
    }

    /// <summary>
    /// 清空玩家墓地
    /// </summary>
    public void ClearPlayerGraveyard()
    {
        playerGraveyard.Clear();
        OnPlayerGraveyardUpdated?.Invoke();
    }

    /// <summary>
    /// 清空敌人墓地
    /// </summary>
    public void ClearEnemyGraveyard()
    {
        enemyGraveyard.Clear();
        OnEnemyGraveyardUpdated?.Invoke();
    }
}

// GraveyardManager.cs
using System.Collections.Generic;
using UnityEngine;

public class GraveyardManager : MonoBehaviour
{
    public static GraveyardManager Instance { get; private set; }

    [Header("Graveyards")]
    private List<UnitData> playerGraveyard = new List<UnitData>();
    private List<UnitData> enemyGraveyard = new List<UnitData>();

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
    }

    /// <summary>
    /// 添加卡牌到玩家墓地
    /// </summary>
    public void AddToPlayerGraveyard(UnitData unitData)
    {
        if (unitData != null)
        {
            playerGraveyard.Add(unitData);
            OnPlayerGraveyardUpdated?.Invoke();
            Debug.Log($"GraveyardManager: 添加 {unitData.unitName} 到玩家墓地");
        }
    }

    /// <summary>
    /// 添加卡牌到敌人墓地
    /// </summary>
    public void AddToEnemyGraveyard(UnitData unitData)
    {
        if (unitData != null)
        {
            enemyGraveyard.Add(unitData);
            OnEnemyGraveyardUpdated?.Invoke();
            Debug.Log($"GraveyardManager: 添加 {unitData.unitName} 到敌人墓地");
        }
    }

    /// <summary>
    /// 获取玩家墓地的所有卡牌
    /// </summary>
    public List<UnitData> GetPlayerGraveyard()
    {
        return new List<UnitData>(playerGraveyard);
    }

    /// <summary>
    /// 获取敌人墓地的所有卡牌
    /// </summary>
    public List<UnitData> GetEnemyGraveyard()
    {
        return new List<UnitData>(enemyGraveyard);
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

using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [SerializeField]
    private List<UnitController> units = new List<UnitController>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // 根据需要设置
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化游戏单位列表
        units.AddRange(FindObjectsOfType<UnitController>());
    }

    /// <summary>
    /// 开始新回合
    /// </summary>
    public void StartNewTurn()
    {
        Debug.Log("TurnManager: 新回合开始");
        foreach (var unit in units)
        {
            if (unit != null)
            {
                unit.OnEndTurn();
            }
        }

        // 其他回合开始逻辑
    }

    /// <summary>
    /// 结束当前回合
    /// </summary>
    public void EndTurn()
    {
        Debug.Log("TurnManager: 当前回合结束");
        // 其他回合结束逻辑

        // 开始新回合
        StartNewTurn();
    }
}
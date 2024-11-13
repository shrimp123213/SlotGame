using System.Collections.Generic;
using UnityEngine;

public class StatusIconPool : MonoBehaviour
{
    public static StatusIconPool Instance;

    [Header("Pool Settings")]
    public GameObject statusIconPrefab; // 狀態圖標預製體
    public int poolSize = 20;           // 初始池大小

    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    void Awake()
    {
        // 確保只有一個池實例
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化對象池，生成預製體實例並隱藏
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(statusIconPrefab);
            obj.SetActive(false);
            poolQueue.Enqueue(obj);
        }
    }

    /// <summary>
    /// 從池中獲取一個狀態圖標
    /// </summary>
    /// <returns>狀態圖標 GameObject</returns>
    public GameObject GetStatusIcon()
    {
        if (poolQueue.Count > 0)
        {
            GameObject obj = poolQueue.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // 池空了，創建新的圖標並返回
            GameObject obj = Instantiate(statusIconPrefab);
            return obj;
        }
    }

    /// <summary>
    /// 將狀態圖標返回池中
    /// </summary>
    /// <param name="obj">要返回的圖標 GameObject</param>
    public void ReturnStatusIcon(GameObject obj)
    {
        obj.SetActive(false);
        poolQueue.Enqueue(obj);
    }
}
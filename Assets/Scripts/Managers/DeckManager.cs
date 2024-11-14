// DeckManager.cs
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Decks")]
    public Deck playerDeck; // 玩家牌組
    public Deck enemyDeck;  // 敵人牌組

    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 根據需要設置
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 可以在這裡添加更多與牌組相關的方法
}
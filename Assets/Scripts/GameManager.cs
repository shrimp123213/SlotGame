using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 管理整個遊戲的流程和狀態
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    // 單例模式
    public static GameManager Instance { get; private set; }

    // 當前遊戲狀態
    public GameState CurrentState { get; private set; }

    // 遊戲結束原因
    public string GameOverReason { get; private set; }

    private void Awake()
    {
        // 確保單例唯一性
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 在場景切換時不銷毀
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始狀態設置為主菜單
        ChangeState(GameState.MainMenu);
    }

    /// <summary>
    /// 改變遊戲狀態
    /// </summary>
    /// <param name="newState">新的遊戲狀態</param>
    /// <param name="reason">遊戲結束原因（可選）</param>
    public void ChangeState(GameState newState, string reason = "")
    {
        CurrentState = newState;
        Debug.Log($"GameManager: 遊戲狀態變更為: {newState}");

        switch (newState)
        {
            case GameState.MainMenu:
                // 加載主菜單場景
                //SceneManager.LoadScene("MainMenu");
                break;

            case GameState.Playing:
                // 開始遊戲，載入戰鬥場景
                //SceneManager.LoadScene("BattleScene");
                break;

            case GameState.Paused:
                // 暫停遊戲，例如顯示暫停菜單
                Time.timeScale = 0f;
                UIManager.Instance.ShowPauseMenu();
                break;

            case GameState.GameOver:
                // 結束遊戲，顯示遊戲結束畫面
                GameOverReason = reason;
                Time.timeScale = 0f;
                UIManager.Instance.ShowGameOverScreen(reason);
                break;
        }
    }

    /// <summary>
    /// 暫停遊戲
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
    }

    /// <summary>
    /// 恢復遊戲
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            UIManager.Instance.HidePauseMenu();
            Debug.Log("GameManager: 遊戲恢復");
        }
    }

    /// <summary>
    /// 結束遊戲並顯示遊戲結束畫面
    /// </summary>
    /// <param name="reason">結束原因</param>
    public void EndGame(string reason)
    {
        ChangeState(GameState.GameOver, reason);
    }
}

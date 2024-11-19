using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理遊戲中的所有 UI 元件
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button quitButton;

    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可選，根據需求決定是否保持單例
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 設置按鈕的事件
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(GameManager.Instance.ResumeGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        HideGameOverScreen();
        //HidePauseMenu();
        //HideMainMenu();
    }

    /// <summary>
    /// 顯示暫停菜單
    /// </summary>
    public void ShowPauseMenu()
    {
        pauseMenuPanel.SetActive(true);
    }

    /// <summary>
    /// 隱藏暫停菜單
    /// </summary>
    public void HidePauseMenu()
    {
        pauseMenuPanel.SetActive(false);
    }

    /// <summary>
    /// 顯示遊戲結束畫面
    /// </summary>
    /// <param name="reason">遊戲結束原因</param>
    public void ShowGameOverScreen(string reason)
    {
        gameOverPanel.SetActive(true);
        // 假設有一個 Text 元件來顯示原因
        gameOverPanel.transform.Find("ReasonText").GetComponent<TMP_Text>().text = reason;
    }

    /// <summary>
    /// 隱藏遊戲結束畫面
    /// </summary>
    public void HideGameOverScreen()
    {
        gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// 顯示主菜單
    /// </summary>
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// 隱藏主菜單
    /// </summary>
    public void HideMainMenu()
    {
        mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// 結束遊戲
    /// </summary>
    private void QuitGame()
    {
        Debug.Log("UIManager: 退出遊戲");
        Application.Quit();
    }
}


using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("主選單面板")]
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        // 預設隱藏設定面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // 開始遊戲
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // 開啟設定
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // 返回主選單
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}

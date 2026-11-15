
using UnityEngine;

public class InfoPage : MonoBehaviour
{
    [Header("衛教資訊面板")]
    [SerializeField] private GameObject infoPagePanel;

    // 進入主選單時隱藏衛教資訊頁
    private void Start()
    {
        if (infoPagePanel != null)
        {
            infoPagePanel.SetActive(false);
        }
    }

    // 開啟衛教資訊頁
    public void OpenInfoPage()
    {
        if (infoPagePanel != null)
        {
            infoPagePanel.SetActive(true);
        }
    }

    // 返回主選單
    public void CloseInfoPage()
    {
        if (infoPagePanel != null)
        {
            infoPagePanel.SetActive(false);
        }
    }
}

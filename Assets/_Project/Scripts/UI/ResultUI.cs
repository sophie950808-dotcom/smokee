using TMPro;
using UnityEngine;

/// <summary>
/// 結算畫面：遊戲結束（GameManager.OnGameEnded）時顯示 C5 的結算面板，填入勝敗、用時、血量、暴露時間、總分、評級。
/// [I1 整合] 由 A 在整合時依 C5 版面補上邏輯；版面與文字樣式仍由 C 負責。
/// </summary>
public class ResultUI : MonoBehaviour
{
    [Header("版面")]
    [Tooltip("結算面板（C5 的 ResultPanel）。遊戲進行中隱藏，結束時顯示。")]
    [SerializeField] private GameObject _panel;
    [Tooltip("顯示「淨化成功／淨化失敗」的標題文字。")]
    [SerializeField] private TMP_Text _titleText;
    [Tooltip("顯示用時、剩餘血量、二手菸暴露時間、總分、評級的文字。")]
    [SerializeField] private TMP_Text _statsText;

    [Header("標題文字")]
    [Tooltip("勝利時的標題。")]
    [SerializeField] private string _winTitle = "淨化完成";
    [Tooltip("失敗時的標題。")]
    [SerializeField] private string _loseTitle = "任務結束";

    private GameManager _gameManager;

    private void Awake()
    {
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        BindGameManager(GameManager.Instance);
    }

    private void Update()
    {
        // GameManager 可能比這個元件晚啟用，所以每幀確認一次
        GameManager manager = GameManager.Instance;
        if (manager != _gameManager)
            BindGameManager(manager);
    }

    private void OnDisable()
    {
        BindGameManager(null);
    }

    private void BindGameManager(GameManager manager)
    {
        if (_gameManager != null)
            _gameManager.OnGameEnded -= ShowResult;

        _gameManager = manager;

        if (manager != null)
        {
            manager.OnGameEnded += ShowResult;

            // 如果綁定時遊戲已經結束（錯過事件），直接顯示最後結果
            if (manager.State == GameState.Ended)
                ShowResult(manager.LastResult);
        }
    }

    /// <summary>依結算結果填入文字並顯示結算面板。</summary>
    public void ShowResult(ScoreResult result)
    {
        if (_titleText != null)
            _titleText.text = result.IsWin ? _winTitle : _loseTitle;

        if (_statsText != null)
        {
            int clearSeconds = Mathf.Max(0, Mathf.RoundToInt(result.ClearTimeSeconds));
            int smokeSeconds = Mathf.Max(0, Mathf.RoundToInt(result.SmokeSeconds));
            _statsText.text =
                $"用時  {clearSeconds / 60:00}:{clearSeconds % 60:00}\n" +
                $"剩餘血量  {Mathf.RoundToInt(result.RemainingHp)}\n" +
                $"二手菸暴露時間 {smokeSeconds} 秒\n" +
                $"總分  {Mathf.RoundToInt(result.TotalScore)}\n" +
                $"等級  {result.Grade}";
        }

        SetPanelVisible(true);
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (_panel != null)
            _panel.SetActive(isVisible);
    }

#if UNITY_EDITOR
    [ContextMenu("C5/模擬勝利結算")]
    private void PreviewWin()
    {
        if (Application.isPlaying)
            ShowResult(new ScoreResult { IsWin = true, RemainingHp = 80f, ClearTimeSeconds = 270f, SmokeSeconds = 35f, TotalScore = 81.3f, Grade = "A" });
    }

    [ContextMenu("C5/模擬失敗結算")]
    private void PreviewLose()
    {
        if (Application.isPlaying)
            ShowResult(new ScoreResult { IsWin = false, RemainingHp = 0f, ClearTimeSeconds = 200f, SmokeSeconds = 120f, TotalScore = 20f, Grade = "D" });
    }
#endif
}

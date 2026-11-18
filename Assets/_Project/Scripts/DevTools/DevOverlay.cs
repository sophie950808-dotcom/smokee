using UnityEngine;

/// <summary>
/// 開發者面板（只在測試用）：用 OnGUI 在畫面左上角顯示遊戲狀態與快捷鍵說明。
/// 只有 GameManager 勾選 devMode 時才會顯示。
/// 找不到 PlayerHealth／ExposureSystem 時該欄顯示 N/A。
/// 遊戲結束時另外顯示完整的 ScoreResult（A8）。
/// </summary>
public class DevOverlay : MonoBehaviour
{
    [Tooltip("文字大小")]
    [SerializeField] private int _fontSize = 18;

    private PlayerHealth _playerHealth;
    private ExposureSystem _exposureSystem;
    private GUIStyle _style;

    private void Start()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        _exposureSystem = FindFirstObjectByType<ExposureSystem>();
    }

    private void OnGUI()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.DevMode)
        {
            return;
        }

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.box)
            {
                fontSize = _fontSize,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8),
            };
            _style.normal.textColor = Color.white;
        }

        float remaining = Mathf.Max(gameManager.RemainingTime, 0f);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        string hpText = _playerHealth != null
            ? _playerHealth.CurrentHp.ToString("F0") + " / " + _playerHealth.MaxHp.ToString("F0")
            : "N/A";
        string exposureText = _exposureSystem != null
            ? Mathf.RoundToInt(_exposureSystem.Exposure).ToString()
              + "（警示：" + (_exposureSystem.IsWarning ? "是" : "否")
              + "，滿值：" + (_exposureSystem.IsMaxed ? "是" : "否") + "）\n"
              + "煙霧中累計：" + Mathf.RoundToInt(_exposureSystem.SmokeSeconds).ToString() + " 秒"
            : "N/A";

        string routeText = RouteManager.Instance != null
            ? RouteManager.Instance.ChosenRoute.ToString()
            : "N/A（沒有 RouteManager）";

        string text =
            "[DEV] 狀態：" + gameManager.State + "\n" +
            "路線：" + routeText + "\n" +
            "剩餘時間：" + minutes.ToString("00") + ":" + seconds.ToString("00") + "\n" +
            "淨化進度：" + gameManager.PurificationProgress.ToString("F0") + " / 100\n" +
            "HP：" + hpText + "\n" +
            "暴露值：" + exposureText + "\n" +
            GetResultText(gameManager) +
            "──────────\n" +
            DevHotkeys.HelpText;

        GUIContent content = new GUIContent(text);
        Vector2 size = _style.CalcSize(content);
        GUI.Box(new Rect(10f, 10f, size.x, size.y), content, _style);
    }

    /// <summary>
    /// 遊戲結束（Ended）時回傳完整的結算結果文字（直接讀 GameManager.LastResult）；其他狀態回傳空字串。
    /// 正式結算畫面由 C 的 ResultUI 負責，這裡只是測試用。
    /// </summary>
    private static string GetResultText(GameManager gameManager)
    {
        if (gameManager.State != GameState.Ended)
        {
            return "";
        }

        ScoreResult r = gameManager.LastResult;
        return
            "══ 結算結果 ══\n" +
            "結果：" + (r.IsWin ? "勝利" : "失敗") + "\n" +
            "結算 HP：" + r.RemainingHp.ToString("F1") + "\n" +
            "剩餘秒數：" + r.RemainingSeconds.ToString("F1") + "\n" +
            "通關秒數：" + r.ClearTimeSeconds.ToString("F1") + "\n" +
            "煙霧中秒數：" + r.SmokeSeconds.ToString("F1") + "\n" +
            "血量分：" + r.HpScore.ToString("F1") + "\n" +
            "時間分：" + r.TimeScore.ToString("F1") + "\n" +
            "總分：" + r.TotalScore.ToString("F1") + "\n" +
            "評級：" + r.Grade + "\n";
    }
}

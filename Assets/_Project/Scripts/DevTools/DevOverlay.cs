using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 開發者面板（只在測試用）：用 OnGUI 在畫面左上角顯示遊戲狀態與快捷鍵說明。
/// 只有 GameManager 勾選 devMode 時才會顯示。
/// 顯示：狀態、路線、階段、故事事件、空檔倒數、淨化（整數與百分比）、用時、計時是否暫停（與申請的 key）、
/// 目前任務提示、淨化里程碑、第三階段快照、HP、暴露值；遊戲結束時另外顯示完整的 ScoreResult。
/// 找不到 PlayerHealth／ExposureSystem／PhaseManager 時該欄顯示 N/A。
/// </summary>
public class DevOverlay : MonoBehaviour
{
    [Tooltip("文字大小")]
    [SerializeField] private int _fontSize = 16;

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

        string hpText = _playerHealth != null
            ? _playerHealth.CurrentHp.ToString("F0") + " / " + _playerHealth.MaxHp.ToString("F0")
            : "N/A";
        string exposureText = _exposureSystem != null
            ? Mathf.RoundToInt(_exposureSystem.Exposure).ToString()
              + "（警示：" + (_exposureSystem.IsWarning ? "是" : "否")
              + "，滿值：" + (_exposureSystem.IsMaxed ? "是" : "否")
              + "，煙霧中累計 " + Mathf.RoundToInt(_exposureSystem.SmokeSeconds) + " 秒）"
            : "N/A";

        string routeText = RouteManager.Instance != null
            ? RouteManager.Instance.ChosenRoute.ToString()
            : "N/A（沒有 RouteManager）";

        string timerPauseText = gameManager.IsTimerPaused
            ? "暫停中（" + string.Join("、", new List<string>(gameManager.TimerPauseKeys)) + "）"
            : "計時中";

        string currentTask = TaskHint.CurrentTask;

        string text =
            "[DEV] 狀態：" + gameManager.State + "　路線：" + routeText + "\n" +
            GetPhaseText() +
            "淨化：" + gameManager.PurificationUnits + " / " + GameManager.PurificationMax
                + "（" + gameManager.PurificationProgress.ToString("F1") + "%）"
                + (gameManager.IsPurificationLocked ? "【第三階段鎖定】" : "") + "\n" +
            "用時：" + FormatTime(gameManager.ElapsedTime) + "　" + timerPauseText + "\n" +
            "任務提示：" + (string.IsNullOrEmpty(currentTask) ? "（無）" : currentTask) + "\n" +
            "第三階段快照：" + (Phase3Checkpoint.HasSnapshot ? Phase3Checkpoint.Snapshot.ToString() : "（無）") + "\n" +
            "HP：" + hpText + "\n" +
            "暴露值：" + exposureText + "\n" +
            GetResultText(gameManager) +
            "──────────\n" +
            DevHotkeys.HelpText;

        GUIContent content = new GUIContent(text);
        Vector2 size = _style.CalcSize(content);
        GUI.Box(new Rect(10f, 10f, size.x, size.y), content, _style);
    }

    private static string GetPhaseText()
    {
        PhaseManager phaseManager = PhaseManager.Instance;
        if (phaseManager == null)
        {
            return "階段：N/A（沒有 PhaseManager）\n";
        }

        string eventText;
        if (phaseManager.CurrentEventIndex < 0)
        {
            eventText = "—";
        }
        else
        {
            string stage = phaseManager.IsInEventGap ? "已完成，空檔中"
                : phaseManager.IsHelpRefused ? "拒絕中"
                : phaseManager.IsEventInProgress ? "進行中"
                : "等待接受";
            eventText = "事件 " + (phaseManager.CurrentEventIndex + 1) + "（" + stage + "）";
        }

        string gapText = phaseManager.IsInEventGap
            ? phaseManager.EventGapRemaining.ToString("F1") + " 秒"
            : "—";

        string milestones =
            "20%" + (phaseManager.HasReachedMilestone(20) ? "已達" : "未達") + "　" +
            "60%" + (phaseManager.HasReachedMilestone(60) ? "已達" : "未達") + "　" +
            "100%" + (phaseManager.HasReachedMilestone(100) ? "已達" : "未達");

        return "階段：" + phaseManager.CurrentPhase + "　" + eventText + "　空檔倒數：" + gapText + "\n" +
               "藏寶圖：" + (phaseManager.HasTreasureMap ? "有" : "無") + "　里程碑：" + milestones + "\n";
    }

    private static string FormatTime(float seconds)
    {
        float clamped = Mathf.Max(seconds, 0f);
        int minutes = Mathf.FloorToInt(clamped / 60f);
        float rest = clamped - minutes * 60f;
        return minutes.ToString("00") + ":" + rest.ToString("00.0");
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
            "結果：" + (r.IsWin ? "勝利" : "失敗") + "　結束階段：" + r.EndPhase + "\n" +
            "結算 HP：" + r.RemainingHp.ToString("F1") + "\n" +
            "用時：" + r.ClearTimeSeconds.ToString("F1") + " 秒\n" +
            "煙霧中秒數：" + r.SmokeSeconds.ToString("F1") + "\n" +
            "血量分：" + r.HpScore.ToString("F1") + "　用時分：" + r.TimeScore.ToString("F1") + "\n" +
            "總分：" + r.TotalScore.ToString("F1") + "　評級：" + r.Grade + "\n" +
            "可從第三階段重新挑戰：" + (r.CanRetryFromPhase3 ? "是" : "否") + "\n";
    }
}

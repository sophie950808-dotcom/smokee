using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 開發者快捷鍵（只在測試用）。只有 GameManager 勾選 devMode 時才有效。
/// 遊戲進行中（Playing）：F1 淨化進度 +10、F2 剩餘時間設為 65 秒、F3 立即勝利、F4 立即失敗。
/// 開場中（Opening，需要場景裡有 RouteManager）：F5 接受香菸、F6 拒絕香菸、F7 開場演出結束。
/// （F5～F7 是在 C 的對話框、B 的開場演出完成前，先用按鍵模擬）
/// 之後的任務會在這裡繼續加鍵，記得同步更新 HelpText。
/// </summary>
public class DevHotkeys : MonoBehaviour
{
    /// <summary>按鍵說明文字，DevOverlay 會顯示。</summary>
    public const string HelpText =
        "F1 淨化進度 +10\n" +
        "F2 剩餘時間設為 65 秒\n" +
        "F3 立即勝利\n" +
        "F4 立即失敗\n" +
        "F5 選擇接受香菸（開場）\n" +
        "F6 選擇拒絕香菸（開場）\n" +
        "F7 開場演出結束（開場）";

    [Tooltip("F1 每次增加的淨化進度")]
    [SerializeField] private float _progressStep = 10f;

    [Tooltip("F2 把剩餘時間設成幾秒（比 60 秒多一點，可以測最後一分鐘事件）")]
    [SerializeField] private float _setTimeSeconds = 65f;

    private void Update()
    {
        GameManager gameManager = GameManager.Instance;
        Keyboard keyboard = Keyboard.current;
        if (gameManager == null || keyboard == null || !gameManager.DevMode)
        {
            return;
        }

        if (gameManager.State == GameState.Opening)
        {
            HandleOpeningKeys(keyboard);
        }
        else if (gameManager.State == GameState.Playing)
        {
            HandlePlayingKeys(gameManager, keyboard);
        }
    }

    private void HandleOpeningKeys(Keyboard keyboard)
    {
        RouteManager routeManager = RouteManager.Instance;
        if (routeManager == null)
        {
            return;
        }

        if (keyboard.f5Key.wasPressedThisFrame)
        {
            routeManager.ChooseRoute(OpeningRoute.AcceptSmoke);
        }
        else if (keyboard.f6Key.wasPressedThisFrame)
        {
            routeManager.ChooseRoute(OpeningRoute.RefuseSmoke);
        }
        else if (keyboard.f7Key.wasPressedThisFrame)
        {
            routeManager.NotifyOpeningFinished();
        }
    }

    private void HandlePlayingKeys(GameManager gameManager, Keyboard keyboard)
    {
        if (keyboard.f1Key.wasPressedThisFrame)
        {
            gameManager.AddPurificationProgress(_progressStep);
        }
        else if (keyboard.f2Key.wasPressedThisFrame)
        {
            gameManager.DevSetRemainingTime(_setTimeSeconds);
        }
        else if (keyboard.f3Key.wasPressedThisFrame)
        {
            gameManager.DevForceEnd(true);
        }
        else if (keyboard.f4Key.wasPressedThisFrame)
        {
            gameManager.DevForceEnd(false);
        }
    }
}

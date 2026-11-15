using System;
using UnityEngine;

/// <summary>
/// 管理開場路線選擇（路線 A 接受香菸／路線 B 拒絕香菸）。
/// 用 RouteManager.Instance 取得。
/// （A2 空殼：路線 A 重新載入場景在 A9 實作）
/// </summary>
public class RouteManager : MonoBehaviour
{
    /// <summary>場景中唯一的 RouteManager。</summary>
    public static RouteManager Instance { get; private set; }

    private OpeningRoute _chosenRoute = OpeningRoute.None;

    /// <summary>玩家選擇的路線。</summary>
    public OpeningRoute ChosenRoute => _chosenRoute;

    /// <summary>玩家選好路線時觸發（B 的 OpeningSequence 監聽，播放對應演出）。</summary>
    public event Action<OpeningRoute> OnRouteChosen;

    /// <summary>開場演出結束時觸發。</summary>
    public event Action<OpeningRoute> OnOpeningFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RouteManager] 場景中有超過一個 RouteManager，請只保留一個。", this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 選擇路線（C 的開場對話框按鈕呼叫）。
    /// </summary>
    public void ChooseRoute(OpeningRoute route)
    {
        _chosenRoute = route;
        OnRouteChosen?.Invoke(route);
    }

    /// <summary>
    /// 開場演出結束（B 的 OpeningSequence 呼叫）。
    /// </summary>
    public void NotifyOpeningFinished()
    {
        OnOpeningFinished?.Invoke(_chosenRoute);
        // TODO（A9）：AcceptSmoke → 重新載入 GameScene；RefuseSmoke → 恢復玩家操作
    }

    /// <summary>
    /// 玩家走出死巷（B 在死巷出口的 Trigger 呼叫）→ 開始倒數。
    /// </summary>
    public void NotifyPlayerLeftAlley()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[RouteManager] 找不到 GameManager，無法開始倒數。", this);
            return;
        }
        // 只在還沒開始倒數時有效，避免玩家來回穿過出口重複重設時間
        if (GameManager.Instance.State != GameState.Opening)
        {
            return;
        }
        GameManager.Instance.StartRound();
    }
}

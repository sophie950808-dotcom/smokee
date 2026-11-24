using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 管理開場路線選擇（路線 A 接受香菸／路線 B 拒絕香菸）。
/// 用 RouteManager.Instance 取得。規格見 docs/INTERFACES.md 第 4 節。
/// 流程：場景開始停用玩家控制 → ChooseRoute → NotifyOpeningFinished
/// → 路線 A 等待後重新載入場景；路線 B 恢復控制，走出死巷（NotifyPlayerLeftAlley）才開始倒數。
/// </summary>
public class RouteManager : MonoBehaviour
{
    /// <summary>場景中唯一的 RouteManager。</summary>
    public static RouteManager Instance { get; private set; }

    [Header("路線 A")]
    [Tooltip("路線 A 開場演出結束後，等幾秒再重新載入場景（暫停中不會計時）")]
    [SerializeField] private float _routeAReloadDelay = 1f;

    [Header("玩家引用（留空會自動在場景裡找）")]
    [Tooltip("玩家控制。場景開始時停用（讓玩家點開場對話框），路線 B 開場結束後恢復")]
    [SerializeField] private PlayerController _playerController;

    private OpeningRoute _chosenRoute = OpeningRoute.None;
    private bool _openingFinished;
    private bool _leftAlley;
    private bool _isReloading;

    /// <summary>玩家選擇的路線。</summary>
    public OpeningRoute ChosenRoute => _chosenRoute;

    /// <summary>玩家選好路線時觸發（B 的 OpeningSequence 監聽，播放對應演出）。</summary>
    public event Action<OpeningRoute> OnRouteChosen;

    /// <summary>開場演出結束時觸發。</summary>
    public event Action<OpeningRoute> OnOpeningFinished;

    /// <summary>
    /// 每次進入 Play 時先清掉 static 的 Instance。
    /// 保險用：就算專案關閉 Domain Reload，也不會留下上一次 Play 的舊值。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RouteManager] 場景中有超過一個 RouteManager，請只保留一個。", this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        if (_playerController == null)
        {
            _playerController = FindFirstObjectByType<PlayerController>();
        }

        // 開場：等待玩家在對話框選擇，先停用玩家控制並解鎖滑鼠
        if (_playerController != null)
        {
            _playerController.SetControlEnabled(false);
        }
        else
        {
            Debug.LogWarning("[RouteManager] 找不到 PlayerController，無法停用／恢復玩家控制。", this);
        }

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Opening)
        {
            Debug.LogWarning("[RouteManager] 場景開始時 GameManager 不是 Opening 狀態（目前是 "
                             + GameManager.Instance.State + "）。", this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 選擇路線（C 的開場對話框按鈕呼叫）。只接受第一次選擇，之後的呼叫會被忽略。
    /// </summary>
    public void ChooseRoute(OpeningRoute route)
    {
        if (route == OpeningRoute.None)
        {
            Debug.LogWarning("[RouteManager] ChooseRoute 不能傳 None，已忽略。", this);
            return;
        }
        if (_chosenRoute != OpeningRoute.None)
        {
            Debug.Log("[RouteManager] 已經選過路線（" + _chosenRoute + "），忽略這次選擇（" + route + "）。", this);
            return;
        }

        _chosenRoute = route;
        Debug.Log("[RouteManager] 選擇路線：" + route, this);
        OnRouteChosen?.Invoke(route);
    }

    /// <summary>
    /// 開場演出結束（B 的 OpeningSequence 呼叫）。只有選過路線後的第一次呼叫有效。
    /// 路線 A：等待後重新載入目前場景；路線 B：恢復玩家控制。
    /// </summary>
    public void NotifyOpeningFinished()
    {
        if (_chosenRoute == OpeningRoute.None)
        {
            Debug.LogWarning("[RouteManager] 還沒選擇路線，NotifyOpeningFinished 已忽略。", this);
            return;
        }
        if (_openingFinished)
        {
            return;
        }

        _openingFinished = true;
        Debug.Log("[RouteManager] 開場演出結束（" + _chosenRoute + "）", this);
        OnOpeningFinished?.Invoke(_chosenRoute);

        if (_chosenRoute == OpeningRoute.AcceptSmoke)
        {
            StartCoroutine(ReloadSceneAfterDelay());
        }
        else if (_chosenRoute == OpeningRoute.RefuseSmoke)
        {
            if (_playerController != null)
            {
                _playerController.SetControlEnabled(true);
            }
        }
    }

    /// <summary>
    /// 【即將移除】玩家走出死巷（死巷出口的 AlleyExitTrigger 呼叫）→ 開始倒數。
    /// 只有「選了 RefuseSmoke 且開場已結束」時有效，而且只會觸發一次。
    /// v3.1 改成：路線 B 開場結束 → PhaseManager 開始新手教學。
    /// </summary>
    [Obsolete("v3.1 改由 PhaseManager 在開場結束時開始新手教學。A11 刪除。")]
    public void NotifyPlayerLeftAlley()
    {
        if (_leftAlley || _chosenRoute != OpeningRoute.RefuseSmoke || !_openingFinished)
        {
            return;
        }

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[RouteManager] 找不到 GameManager，無法開始倒數。", this);
            return;
        }
        // 暫停中（Paused）不算數，避免 StartRound 失敗卻被記成已觸發
        if (gameManager.State != GameState.Opening)
        {
            return;
        }

        _leftAlley = true;
        Debug.Log("[RouteManager] 玩家走出死巷，開始倒數。", this);
        gameManager.StartRound();
    }

    private IEnumerator ReloadSceneAfterDelay()
    {
        if (_isReloading)
        {
            yield break;
        }
        _isReloading = true;

        // 用一般的 WaitForSeconds：暫停（timeScale = 0）時會停下來等
        if (_routeAReloadDelay > 0f)
        {
            yield return new WaitForSeconds(_routeAReloadDelay);
        }

        Scene scene = SceneManager.GetActiveScene();
        Debug.Log("[RouteManager] 路線 A：重新載入場景 " + scene.name, this);

        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            yield break;
        }

#if UNITY_EDITOR
        // 場景沒有加進 Build Settings（例如 TestScene_A）時，編輯器裡改用這個方法載入
        Debug.LogWarning("[RouteManager] " + scene.name + " 不在 Build Settings 裡，"
                         + "編輯器中改用 EditorSceneManager.LoadSceneInPlayMode 載入。正式版請把場景加進 Build Settings。", this);
        EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("[RouteManager] " + scene.name + " 不在 Build Settings 裡，無法重新載入。", this);
#endif
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 管理整局遊戲：狀態、倒數時間、淨化進度、暫停與結算。
/// 場景裡只有一個，放在 GameScene，用 GameManager.Instance 取得。
/// 規格見 docs/INTERFACES.md 第 3 節。
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>場景中唯一的 GameManager。</summary>
    public static GameManager Instance { get; private set; }

    [Header("回合設定")]
    [Tooltip("一局的秒數（預設 420 秒，展示模式約 90 秒）")]
    [SerializeField] private float _roundDuration = 420f;

    [Tooltip("剩幾秒時進入最後階段（觸發 OnFinalMinute，小地圖顯示菸怪）")]
    [SerializeField] private float _finalPhaseSeconds = 60f;

    [Header("玩家引用（留空會自動在場景裡找）")]
    [Tooltip("玩家控制。遊戲結束時用來停止操作、解鎖滑鼠")]
    [SerializeField] private PlayerController _playerController;

    [Tooltip("玩家血量。HP 歸零時判定失敗，結算時讀取 HP")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Tooltip("暴露值系統。結算時讀取待在煙霧裡的秒數")]
    [SerializeField] private ExposureSystem _exposureSystem;

    [Header("開發者模式（正式版請取消勾選）")]
    [Tooltip("勾選後 DevOverlay（左上角狀態面板）與 DevHotkeys（F1～F4）才會生效")]
    [SerializeField] private bool _devMode = false;

    [Tooltip("開發者模式下，進入場景就直接開始倒數。場景裡有 RouteManager 時會被忽略（改由走出死巷開始）")]
    [SerializeField] private bool _autoStartRound = false;

    private GameState _state = GameState.Opening;
    private GameState _stateBeforePause = GameState.Opening;
    private float _remainingTime;
    private float _purificationProgress;
    private bool _finalMinuteTriggered;
    private bool _gameEnded;
    private ScoreResult _lastResult;

    /// <summary>目前遊戲狀態。</summary>
    public GameState State => _state;

    /// <summary>一局的總秒數（Inspector 可調，預設 420）。</summary>
    public float RoundDuration => _roundDuration;

    /// <summary>剩餘秒數。HUD 每幀直接讀取。</summary>
    public float RemainingTime => _remainingTime;

    /// <summary>淨化進度 0–100。</summary>
    public float PurificationProgress => _purificationProgress;

    /// <summary>上一局的結算結果，遊戲結束後才有值。</summary>
    public ScoreResult LastResult => _lastResult;

    /// <summary>【只給開發者工具用，B、C 不要使用】是否開啟開發者模式。</summary>
    public bool DevMode => _devMode;

    /// <summary>淨化進度改變時觸發。參數：目前進度 0–100。</summary>
    public event Action<float> OnProgressChanged;

    /// <summary>倒數開始時觸發。</summary>
    public event Action OnRoundStarted;

    /// <summary>剩 60 秒時觸發一次（小地圖顯示菸怪）。</summary>
    public event Action OnFinalMinute;

    /// <summary>暫停狀態改變時觸發。參數：是否暫停。</summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>勝利或失敗結算時觸發（每局只觸發一次）。</summary>
    public event Action<ScoreResult> OnGameEnded;

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
            Debug.LogWarning("[GameManager] 場景中有超過一個 GameManager，請只保留一個。", this);
            return;
        }
        Instance = this;

        // 上一局暫停中重新載入場景時，timeScale 可能還是 0，這裡重設
        Time.timeScale = 1f;
        _remainingTime = _roundDuration;
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
        if (_playerHealth == null)
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        if (_exposureSystem == null)
        {
            _exposureSystem = FindFirstObjectByType<ExposureSystem>();
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnDied += HandlePlayerDied;
        }
        else
        {
            Debug.LogWarning("[GameManager] 找不到 PlayerHealth：HP 歸零不會判定失敗，結算時 HP 以 100 計算。", this);
        }

        if (_devMode && _autoStartRound)
        {
            // 所有 Awake 都比 Start 早執行，所以這裡 RouteManager.Instance 已經設定好了
            if (RouteManager.Instance != null)
            {
                Debug.Log("[GameManager] 場景裡有 RouteManager，已忽略 autoStartRound（改由玩家走出死巷開始倒數）。", this);
            }
            else
            {
                StartRound();
            }
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied -= HandlePlayerDied;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        HandleEscapeKey();

        if (_state == GameState.Playing)
        {
            UpdateCountdown();
        }
    }

    /// <summary>
    /// 開始一局的倒數（由 RouteManager 在玩家走出死巷時呼叫）。只在 Opening 狀態有效。
    /// </summary>
    public void StartRound()
    {
        if (_state != GameState.Opening)
        {
            Debug.LogWarning("[GameManager] 只有在 Opening 狀態才能開始倒數，目前是 " + _state + "。", this);
            return;
        }

        _state = GameState.Playing;
        _remainingTime = _roundDuration;
        _purificationProgress = 0f;
        _finalMinuteTriggered = false;
        Time.timeScale = 1f;
        OnProgressChanged?.Invoke(_purificationProgress);
        OnRoundStarted?.Invoke();
    }

    /// <summary>
    /// 增加淨化進度（B 的 SmokeMonster 被擊敗時呼叫）。結果會限制在 0–100，到 100 就勝利。
    /// 只在遊戲進行中（Playing）有效。
    /// </summary>
    public void AddPurificationProgress(float amount)
    {
        if (_state != GameState.Playing)
        {
            return;
        }

        float newProgress = Mathf.Clamp(_purificationProgress + amount, 0f, 100f);
        if (Mathf.Approximately(newProgress, _purificationProgress))
        {
            return;
        }

        _purificationProgress = newProgress;
        OnProgressChanged?.Invoke(_purificationProgress);

        if (_purificationProgress >= 100f)
        {
            EndGame(true);
        }
    }

    /// <summary>
    /// 暫停或繼續遊戲。暫停時 Time.timeScale = 0。
    /// 可在 Opening、Playing 時暫停，繼續後回到暫停前的狀態；遊戲結束後無效。
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (paused && (_state == GameState.Opening || _state == GameState.Playing))
        {
            _stateBeforePause = _state;
            _state = GameState.Paused;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }
        else if (!paused && _state == GameState.Paused)
        {
            _state = _stateBeforePause;
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// 【只給開發者工具用，B、C 不要使用】直接設定剩餘秒數。開發者模式關閉時沒有作用。
    /// </summary>
    public void DevSetRemainingTime(float seconds)
    {
        if (!_devMode || _state != GameState.Playing)
        {
            return;
        }
        _remainingTime = Mathf.Max(seconds, 0f);
    }

    /// <summary>
    /// 【只給開發者工具用，B、C 不要使用】立即勝利或失敗。開發者模式關閉時沒有作用。
    /// </summary>
    public void DevForceEnd(bool isWin)
    {
        if (!_devMode || _state != GameState.Playing)
        {
            return;
        }
        EndGame(isWin);
    }

    private void HandleEscapeKey()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        // Ended 狀態下 SetPaused 本身不會有作用
        SetPaused(_state != GameState.Paused);
    }

    private void UpdateCountdown()
    {
        _remainingTime = Mathf.Max(_remainingTime - Time.deltaTime, 0f);

        if (!_finalMinuteTriggered && _remainingTime <= _finalPhaseSeconds)
        {
            _finalMinuteTriggered = true;
            OnFinalMinute?.Invoke();
        }

        if (_remainingTime <= 0f && _purificationProgress < 100f)
        {
            EndGame(false);
        }
    }

    private void HandlePlayerDied()
    {
        // HP 歸零 → 立即失敗（沒有倒地）。暫停中或還沒開始時不判定
        if (_state == GameState.Playing)
        {
            EndGame(false);
        }
    }

    private void EndGame(bool isWin)
    {
        if (_gameEnded)
        {
            return;
        }
        _gameEnded = true;
        _state = GameState.Ended;

        float hp = _playerHealth != null ? _playerHealth.CurrentHp : 100f;
        float maxHp = _playerHealth != null ? _playerHealth.MaxHp : 100f;
        float smokeSeconds = _exposureSystem != null ? _exposureSystem.SmokeSeconds : 0f;

        _lastResult = ScoreCalculator.Calculate(isWin, hp, maxHp, _remainingTime, _roundDuration, smokeSeconds);

        if (_playerController != null)
        {
            // 這個方法會同時解鎖滑鼠
            _playerController.SetControlEnabled(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Debug.Log("[GameManager] 遊戲結束：" + (isWin ? "勝利" : "失敗")
                  + "，總分 " + _lastResult.TotalScore.ToString("F1") + "，評級 " + _lastResult.Grade, this);

        OnGameEnded?.Invoke(_lastResult);
    }
}

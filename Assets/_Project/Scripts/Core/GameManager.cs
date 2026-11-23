using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 管理整局遊戲：狀態、用時、淨化進度、暫停與結算。
/// 場景裡只有一個，放在 GameScene，用 GameManager.Instance 取得。
/// 規格見 docs/INTERFACES.md 第 3 節（v3.1）。
/// 【A10】v3.1 的新成員已加入；倒數相關成員標記 Obsolete、仍照舊運作，A11 改成用時並刪除倒數。
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>場景中唯一的 GameManager。</summary>
    public static GameManager Instance { get; private set; }

    /// <summary>淨化進度的滿分（內部整數，1000 = 100%）。</summary>
    public const int PurificationMax = 1000;

    [Header("回合設定（舊倒數，A11 刪除）")]
    [Tooltip("一局的秒數（預設 420 秒）。v3.1 取消倒數，A11 刪除")]
    [SerializeField] private float _roundDuration = 420f;

    [Tooltip("剩幾秒時觸發 OnFinalMinute。v3.1 取消，A11 刪除")]
    [SerializeField] private float _finalPhaseSeconds = 60f;

    [Header("場景")]
    [Tooltip("「返回主選單」要載入的場景名稱（要加進 Build Settings）")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("玩家引用（留空會自動在場景裡找）")]
    [Tooltip("玩家控制。遊戲結束時用來停止操作、解鎖滑鼠")]
    [SerializeField] private PlayerController _playerController;

    [Tooltip("玩家血量。HP 歸零時判定失敗，結算時讀取 HP")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Tooltip("暴露值系統。結算時讀取待在煙霧裡的秒數")]
    [SerializeField] private ExposureSystem _exposureSystem;

    [Header("開發者模式（正式版請取消勾選）")]
    [Tooltip("勾選後 DevOverlay（左上角狀態面板）與 DevHotkeys 才會生效")]
    [SerializeField] private bool _devMode = false;

    [Tooltip("開發者模式下，進入場景就直接開始。場景裡有 RouteManager 時會被忽略")]
    [SerializeField] private bool _autoStartRound = false;

    private GameState _state = GameState.Opening;
    private GameState _stateBeforePause = GameState.Opening;
    private float _remainingTime;
    private float _elapsedTime;
    private int _purificationUnits;
    private bool _finalMinuteTriggered;
    private bool _gameEnded;
    private ScoreResult _lastResult;
    private readonly HashSet<string> _timerPauseKeys = new HashSet<string>();

    /// <summary>目前遊戲狀態。</summary>
    public GameState State => _state;

    /// <summary>【即將移除】一局的總秒數。v3.1 取消倒數。</summary>
    [Obsolete("v3.1 取消倒數，改用 ElapsedTime。A11 刪除。")]
    public float RoundDuration => _roundDuration;

    /// <summary>【即將移除】剩餘秒數。v3.1 取消倒數，HUD 請改讀 ElapsedTime。</summary>
    [Obsolete("v3.1 取消倒數，請改讀 ElapsedTime（用時）。A11 刪除。")]
    public float RemainingTime => _remainingTime;

    /// <summary>用時（秒）。暫停選單、「暫停計時」期間不增加。HUD 每幀直接讀取。</summary>
    public float ElapsedTime => _elapsedTime;

    /// <summary>目前有沒有任何系統申請「暫停計時」（對話、過場、傳送、視窗）。</summary>
    public bool IsTimerPaused => _timerPauseKeys.Count > 0;

    /// <summary>淨化進度（整數 0–1000）。寶箱等判斷一律用這個。</summary>
    public int PurificationUnits => _purificationUnits;

    /// <summary>淨化進度 0–100，給 UI 顯示（= PurificationUnits ÷ 10）。</summary>
    public float PurificationProgress => _purificationUnits / 10f;

    /// <summary>上一局的結算結果，遊戲結束後才有值。</summary>
    public ScoreResult LastResult => _lastResult;

    /// <summary>
    /// 結算時是否可以「從第三階段重新挑戰」。
    /// 【A10 空殼】目前固定 false，A19 完成。
    /// </summary>
    public bool CanRetryFromPhase3 => false;

    /// <summary>【只給開發者工具用，B、C 不要使用】是否開啟開發者模式。</summary>
    public bool DevMode => _devMode;

    /// <summary>淨化進度改變時觸發。參數：目前進度 0–100。</summary>
    public event Action<float> OnProgressChanged;

    /// <summary>遊戲進入 Playing 時觸發（v3.1：新手教學開始時；A11 之前仍是走出死巷、倒數開始時）。</summary>
    public event Action OnRoundStarted;

    /// <summary>【即將移除】剩 60 秒時觸發一次。v3.1 取消倒數。</summary>
    [Obsolete("v3.1 取消倒數與「剩 60 秒顯示菸怪」。A11 刪除。")]
    public event Action OnFinalMinute;

    /// <summary>暫停選單開關時觸發。參數：是否暫停。</summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>「暫停計時」狀態改變時觸發。參數：是否暫停計時中。</summary>
    public event Action<bool> OnTimerPausedChanged;

#pragma warning disable 67 // A11、A19 才會觸發
    /// <summary>新手教學結束、第一階段開始時觸發：用時歸零、HP 補滿、暴露值與淨化歸零。【A11 完成】</summary>
    public event Action OnRunReset;

    /// <summary>
    /// 從第三階段重新挑戰開始時觸發。收到要自己重設：BOSS、吸菸 NPC、菸圈（A）；BOSS 特效（B）；
    /// 背包恢復快照、結算畫面與 BOSS 血條隱藏（C）。【A19 完成】
    /// </summary>
    public event Action OnPhase3Retry;
#pragma warning restore 67

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
                Debug.Log("[GameManager] 場景裡有 RouteManager，已忽略 autoStartRound。", this);
            }
            else
            {
                BeginRun();
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
            if (!IsTimerPaused)
            {
                _elapsedTime += Time.deltaTime;
            }
            UpdateCountdown();
        }
    }

    /// <summary>
    /// 申請或取消「暫停計時」。每個系統用自己的 key，例如 ("Dialogue", true)、("Teleport", true)；
    /// 所有 key 都放開才繼續計時，不會互相覆蓋。暫停計時期間暴露值、HP 也不會變。
    /// </summary>
    public void SetTimerPaused(string key, bool paused)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        bool wasPaused = IsTimerPaused;
        if (paused)
        {
            _timerPauseKeys.Add(key);
        }
        else
        {
            _timerPauseKeys.Remove(key);
        }

        if (wasPaused != IsTimerPaused)
        {
            OnTimerPausedChanged?.Invoke(IsTimerPaused);
        }
    }

    /// <summary>
    /// 【只給 PhaseManager 使用】新手教學開始：進入 Playing，HP 與用時開始計算。只在 Opening 狀態有效。
    /// </summary>
    public void BeginRun()
    {
        if (_state != GameState.Opening)
        {
            Debug.LogWarning("[GameManager] 只有在 Opening 狀態才能開始，目前是 " + _state + "。", this);
            return;
        }

        _state = GameState.Playing;
        _remainingTime = _roundDuration;
        _elapsedTime = 0f;
        _purificationUnits = 0;
        _finalMinuteTriggered = false;
        Time.timeScale = 1f;
        OnProgressChanged?.Invoke(PurificationProgress);
        OnRoundStarted?.Invoke();
    }

    /// <summary>
    /// 【即將移除】開始一局的倒數（舊流程：RouteManager 在玩家走出死巷時呼叫）。現在等於 BeginRun()。
    /// </summary>
    [Obsolete("v3.1 改由 PhaseManager 呼叫 BeginRun()。A11 刪除。")]
    public void StartRound()
    {
        BeginRun();
    }

    /// <summary>
    /// 【只給 PhaseManager 使用】新手教學結束、第一階段開始：用時歸零、HP 補滿、暴露值與淨化歸零，觸發 OnRunReset。
    /// 【A10 空殼】A11 完成，目前沒有作用。
    /// </summary>
    public void ResetRunForPhase1()
    {
        Debug.Log("[GameManager] （A10 空殼）ResetRunForPhase1 在 A11 完成。", this);
    }

    /// <summary>
    /// 增加淨化進度（整數，1000 = 100%）。B 的 SmokeMonster 被擊敗時呼叫，例如第一階段菸怪 40、事件菸怪 33。
    /// 結果限制在 0–1000。只在遊戲進行中（Playing）有效。
    /// （A11 之前到 1000 仍會直接勝利；A11 起改成打倒 BOSS → 結局動畫 → 勝利）
    /// </summary>
    public void AddPurification(int units)
    {
        if (_state != GameState.Playing || units == 0)
        {
            return;
        }

        int newUnits = Mathf.Clamp(_purificationUnits + units, 0, PurificationMax);
        if (newUnits == _purificationUnits)
        {
            return;
        }

        _purificationUnits = newUnits;
        OnProgressChanged?.Invoke(PurificationProgress);

        if (_purificationUnits >= PurificationMax)
        {
            EndGame(true);
        }
    }

    /// <summary>
    /// 【即將移除】增加淨化進度（0–100 的小數）。現在會換算成整數（× 10 四捨五入）後呼叫 AddPurification。
    /// </summary>
    [Obsolete("請改用 AddPurification(int)，1000 = 100%。B10 改完後 A 在 I3 刪除。")]
    public void AddPurificationProgress(float amount)
    {
        AddPurification(Mathf.RoundToInt(amount * 10f));
    }

    /// <summary>
    /// 暫停或繼續遊戲（Esc 暫停選單）。暫停時 Time.timeScale = 0。
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
    /// 重新開始：重新載入目前場景（GameScene），回到開場選擇。
    /// C 的暫停選單、結算畫面的「重新開始」按鈕呼叫。B、C 不要自己 LoadScene。
    /// </summary>
    public void RestartToOpening()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        // 場景沒有加進 Build Settings（例如 TestScene_A）時，編輯器裡改用這個方法載入
        EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("[GameManager] " + scene.name + " 不在 Build Settings 裡，無法重新載入。", this);
#endif
    }

    /// <summary>
    /// 返回主選單：載入 MainMenu 場景。C 的暫停選單「返回主選單」按鈕呼叫。B、C 不要自己 LoadScene。
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    /// <summary>
    /// 從第三階段重新挑戰（C 的結算畫面按鈕呼叫）：HP 恢復成第一次進入第三階段時的數值、
    /// 用時繼續累計、觸發 OnPhase3Retry、重播 BOSS 登場。
    /// 【A10 空殼】A19 完成，目前沒有作用。
    /// </summary>
    public void RetryFromPhase3()
    {
        Debug.LogWarning("[GameManager] （A10 空殼）RetryFromPhase3 在 A19 完成，目前沒有作用。", this);
    }

    /// <summary>【只給 PhaseManager 使用】結局動畫播完 → 勝利結算。</summary>
    public void FinishWithVictory()
    {
        if (_state == GameState.Playing)
        {
            EndGame(true);
        }
    }

    /// <summary>
    /// 【即將移除】【只給開發者工具用，B、C 不要使用】直接設定剩餘秒數。開發者模式關閉時沒有作用。
    /// </summary>
    [Obsolete("v3.1 取消倒數。A11 刪除。")]
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

    /// <summary>舊的倒數（A11 刪除）。</summary>
    private void UpdateCountdown()
    {
        _remainingTime = Mathf.Max(_remainingTime - Time.deltaTime, 0f);

        if (!_finalMinuteTriggered && _remainingTime <= _finalPhaseSeconds)
        {
            _finalMinuteTriggered = true;
#pragma warning disable 618 // A11 之前沿用舊事件
            OnFinalMinute?.Invoke();
#pragma warning restore 618
        }

        if (_remainingTime <= 0f && _purificationUnits < PurificationMax)
        {
            EndGame(false);
        }
    }

    private void HandlePlayerDied()
    {
        // HP 歸零 → 立即失敗（沒有倒地）。暫停中或還沒開始時不判定
        // （A12 起：新手教學期間由 TutorialManager 處理，不進結算）
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

#pragma warning disable 618 // A11 之前沿用舊的評分（倒數）
        _lastResult = ScoreCalculator.Calculate(isWin, hp, maxHp, _remainingTime, _roundDuration, smokeSeconds);
#pragma warning restore 618
        _lastResult.Title = "";
        _lastResult.CanRetryFromPhase3 = CanRetryFromPhase3;
        _lastResult.EndPhase = PhaseManager.Instance != null ? PhaseManager.Instance.CurrentPhase : GamePhase.Opening;

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

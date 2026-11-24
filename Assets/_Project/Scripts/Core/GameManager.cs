using System;
using System.Collections;
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
/// 規格見 docs/INTERFACES.md 第 3 節（v3.1，A11 更新）。
/// - 沒有倒數；唯一的失敗條件是 HP 歸零（新手教學期間除外，由 TutorialManager 處理）。
/// - 用時：新手教學開始時開始跑，第一階段開始時（ResetRunForPhase1）歸零重算；BOSS 被打倒時停止。
/// - 第三階段：淨化進度固定 600（60%），菸怪等任何來源都不能改變；BOSS 被打倒（NotifyBossDefeated）才變成 1000。
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>開發者模式下，沒有 RouteManager 時進入場景要從哪裡開始。</summary>
    public enum DevAutoStartMode
    {
        /// <summary>不自動開始。</summary>
        Off,
        /// <summary>直接開始新手教學。</summary>
        Tutorial,
        /// <summary>直接從第一階段開始（跳過教學）。</summary>
        Phase1
    }

    /// <summary>場景中唯一的 GameManager。</summary>
    public static GameManager Instance { get; private set; }

    /// <summary>淨化進度的滿分（內部整數，1000 = 100%）。</summary>
    public const int PurificationMax = 1000;

    /// <summary>進入第三階段時的淨化進度（60%）。整場 BOSS 戰都停在這個值。</summary>
    public const int Phase3PurificationUnits = 600;

    private const string BossDefeatedTimerKey = "BossDefeated";

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

    [Tooltip("開發者模式下，進入場景就自動開始：Tutorial = 從新手教學開始；Phase1 = 跳過教學直接從第一階段開始。" +
             "場景裡有 RouteManager 時會被忽略（請用 F6、F7 跑開場）")]
    [SerializeField] private DevAutoStartMode _devAutoStart = DevAutoStartMode.Off;

    private GameState _state = GameState.Opening;
    private GameState _stateBeforePause = GameState.Opening;
    private float _elapsedTime;
    private int _purificationUnits;
    private bool _purificationLocked;
    private bool _bossDefeated;
    private float _victoryHp;
    private bool _gameEnded;
    private ScoreResult _lastResult;
    private readonly HashSet<string> _timerPauseKeys = new HashSet<string>();

    /// <summary>目前遊戲狀態。</summary>
    public GameState State => _state;

    /// <summary>
    /// 【即將移除】舊的剩餘秒數。v3.1 取消倒數；C10 改讀 ElapsedTime 之前，暫時回傳「用時」，
    /// 讓舊 HUD 顯示往上數的時間。C10 完成後刪除。
    /// </summary>
    [Obsolete("v3.1 取消倒數，請改讀 ElapsedTime（用時）。目前暫時回傳用時，C10 改完後刪除。")]
    public float RemainingTime => _elapsedTime;

    /// <summary>用時（秒）。暫停選單、「暫停計時」期間不增加；BOSS 被打倒後停止。HUD 每幀直接讀取。</summary>
    public float ElapsedTime => _elapsedTime;

    /// <summary>目前有沒有任何系統申請「暫停計時」（對話、過場、傳送、視窗）。</summary>
    public bool IsTimerPaused => _timerPauseKeys.Count > 0;

    /// <summary>【開發者工具用】目前申請「暫停計時」的所有 key。</summary>
    public IReadOnlyCollection<string> TimerPauseKeys => _timerPauseKeys;

    /// <summary>淨化進度（整數 0–1000）。寶箱等判斷一律用這個。</summary>
    public int PurificationUnits => _purificationUnits;

    /// <summary>淨化進度 0–100，給 UI 顯示（= PurificationUnits ÷ 10）。</summary>
    public float PurificationProgress => _purificationUnits / 10f;

    /// <summary>淨化進度是否被鎖住（第三階段、結局）：鎖住時 AddPurification 沒有作用。</summary>
    public bool IsPurificationLocked => _purificationLocked;

    /// <summary>BOSS 是否已被打倒（NotifyBossDefeated 呼叫過）。</summary>
    public bool IsBossDefeated => _bossDefeated;

    /// <summary>上一局的結算結果，遊戲結束後才有值。</summary>
    public ScoreResult LastResult => _lastResult;

    /// <summary>
    /// 結算時是否可以「從第三階段重新挑戰」。
    /// 【A19 完成】重新挑戰還沒做好，目前固定 false（避免 C 的按鈕出現但按了沒反應）。
    /// </summary>
    public bool CanRetryFromPhase3 => false;

    /// <summary>【只給開發者工具用，B、C 不要使用】是否開啟開發者模式。</summary>
    public bool DevMode => _devMode;

    /// <summary>淨化進度改變時觸發。參數：目前進度 0–100。</summary>
    public event Action<float> OnProgressChanged;

    /// <summary>遊戲進入 Playing 時觸發：新手教學開始（HP、用時開始跑）。</summary>
    public event Action OnRoundStarted;

    /// <summary>暫停選單開關時觸發。參數：是否暫停。</summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>「暫停計時」狀態改變時觸發。參數：是否暫停計時中。</summary>
    public event Action<bool> OnTimerPausedChanged;

    /// <summary>新手教學結束、第一階段開始時觸發：用時歸零、HP 補滿、暴露值與淨化歸零。</summary>
    public event Action OnRunReset;

#pragma warning disable 67 // A19 才會觸發
    /// <summary>
    /// 從第三階段重新挑戰開始時觸發。【A19 完成】詳細時機見 docs/INTERFACES.md「第三階段起點」。
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

        if (_devMode && _devAutoStart != DevAutoStartMode.Off)
        {
            // 所有 Awake 都比 Start 早執行，所以這裡 RouteManager.Instance 已經設定好了
            if (RouteManager.Instance != null)
            {
                Debug.Log("[GameManager] 場景裡有 RouteManager，已忽略 devAutoStart（請用 F6、F7 跑開場）。", this);
            }
            else
            {
                // 等一個 frame，讓所有物件的 Start 都跑完（PhaseManager、B、C 的監聽都接好）再開始
                StartCoroutine(DevAutoStartNextFrame());
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

        if (_state == GameState.Playing && !IsTimerPaused)
        {
            _elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// 申請或取消「暫停計時」。每個系統用自己的 key，例如 ("Dialogue", true)、("Teleport", true)；
    /// 所有 key 都放開才繼續計時，不會互相覆蓋。暫停計時期間暴露值、HP、40 秒空檔也不會變。
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
        _elapsedTime = 0f;
        _purificationUnits = 0;
        Time.timeScale = 1f;
        OnProgressChanged?.Invoke(PurificationProgress);
        OnRoundStarted?.Invoke();
    }

    /// <summary>
    /// 【只給 PhaseManager 使用】新手教學結束、第一階段開始：用時歸零、HP 補滿、暴露值與淨化歸零，觸發 OnRunReset。
    /// </summary>
    public void ResetRunForPhase1()
    {
        if (_state != GameState.Playing)
        {
            Debug.LogWarning("[GameManager] ResetRunForPhase1 只在 Playing 時有效，目前是 " + _state + "。", this);
            return;
        }

        _elapsedTime = 0f;
        if (_playerHealth != null)
        {
            _playerHealth.ResetHp(_playerHealth.MaxHp);
        }
        if (_exposureSystem != null)
        {
            _exposureSystem.ResetExposure();
        }

        _purificationUnits = 0;
        OnProgressChanged?.Invoke(PurificationProgress);
        OnRunReset?.Invoke();
    }

    /// <summary>
    /// 增加淨化進度（整數，1000 = 100%）。B 的 SmokeMonster 被擊敗時呼叫，例如第一階段菸怪 40、事件菸怪 33。
    /// 結果限制在 0–1000。只在遊戲進行中（Playing）有效；第三階段（淨化鎖住）時沒有作用。
    /// 到 1000 不會直接勝利（勝利 = 打倒 BOSS → 結局動畫）。
    /// </summary>
    public void AddPurification(int units)
    {
        if (_state != GameState.Playing || units == 0)
        {
            return;
        }
        if (_purificationLocked)
        {
            Debug.Log("[GameManager] 第三階段淨化進度固定 60%，已忽略 AddPurification(" + units + ")。", this);
            return;
        }

        SetPurificationUnits(_purificationUnits + units);
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
    /// 【只給 PhaseManager 使用】進入第三階段：淨化進度設為 600（60%）並鎖住，之後任何來源都不能改變，
    /// 直到 NotifyBossDefeated。
    /// </summary>
    public void LockPurificationForPhase3()
    {
        if (_purificationLocked)
        {
            return;
        }

        SetPurificationUnits(Phase3PurificationUnits);
        _purificationLocked = true;
    }

    /// <summary>
    /// 【給 A14 的 BOSS 呼叫】BOSS 被打倒：淨化進度設為 1000（100%）、停止用時、記下這時的 HP（勝利的血量分用）。
    /// 之後 PhaseManager 會進入結局（OnEndingRequested），結局動畫播完才勝利結算。
    /// 只在第三階段（淨化已鎖住）、遊戲進行中有效，而且只有第一次呼叫有效。
    /// </summary>
    public void NotifyBossDefeated()
    {
        if (_state != GameState.Playing || _bossDefeated)
        {
            return;
        }
        if (!_purificationLocked)
        {
            Debug.LogWarning("[GameManager] 還沒進入第三階段，NotifyBossDefeated 已忽略。", this);
            return;
        }

        _bossDefeated = true;
        _victoryHp = _playerHealth != null ? _playerHealth.CurrentHp : 100f;

        // 用「暫停計時」停止用時：結局動畫期間用時、HP、暴露值都不會再變
        SetTimerPaused(BossDefeatedTimerKey, true);

        _purificationUnits = PurificationMax;
        OnProgressChanged?.Invoke(PurificationProgress);
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
    /// 重新開始：重新載入目前場景（GameScene），回到開場選擇。會清掉第三階段快照（重新開始一整局）。
    /// C 的暫停選單、結算畫面的「重新開始」按鈕呼叫。B、C 不要自己 LoadScene。
    /// </summary>
    public void RestartToOpening()
    {
        Phase3Checkpoint.Clear();
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
    /// 返回主選單：載入 MainMenu 場景。會清掉第三階段快照。C 的暫停選單「返回主選單」按鈕呼叫。B、C 不要自己 LoadScene。
    /// </summary>
    public void ReturnToMainMenu()
    {
        Phase3Checkpoint.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    /// <summary>
    /// 從第三階段重新挑戰（C 的結算畫面按鈕呼叫）：記下目前用時 → 重新載入 GameScene → 直接從第三階段起點開始。
    /// 【A19 完成】目前沒有作用。
    /// </summary>
    public void RetryFromPhase3()
    {
        Debug.LogWarning("[GameManager] RetryFromPhase3 尚未實作（A19）。", this);
    }

    /// <summary>【只給 PhaseManager 使用】結局動畫播完 → 勝利結算（血量分用打倒 BOSS 當下的 HP）。</summary>
    public void FinishWithVictory()
    {
        if (_state != GameState.Playing)
        {
            return;
        }
        if (!_bossDefeated)
        {
            Debug.LogWarning("[GameManager] BOSS 還沒被打倒，FinishWithVictory 已忽略。", this);
            return;
        }
        EndGame(true);
    }

    /// <summary>
    /// 【只給開發者工具用，B、C 不要使用】立即勝利或失敗（不經過正常流程）。開發者模式關閉時沒有作用。
    /// </summary>
    public void DevForceEnd(bool isWin)
    {
        if (!_devMode || _state != GameState.Playing)
        {
            return;
        }
        EndGame(isWin);
    }

    /// <summary>
    /// 【只給開發者工具用，B、C 不要使用】直接設定淨化進度（整數，0–1000）。
    /// 開發者模式關閉、或第三階段（淨化鎖住）時沒有作用。
    /// </summary>
    public void DevSetPurification(int units)
    {
        if (!_devMode || _state != GameState.Playing)
        {
            return;
        }
        if (_purificationLocked)
        {
            Debug.Log("[GameManager] 第三階段淨化進度固定 60%，已忽略 DevSetPurification(" + units + ")。", this);
            return;
        }
        SetPurificationUnits(units);
    }

    private void SetPurificationUnits(int units)
    {
        int newUnits = Mathf.Clamp(units, 0, PurificationMax);
        if (newUnits == _purificationUnits)
        {
            return;
        }

        _purificationUnits = newUnits;
        OnProgressChanged?.Invoke(PurificationProgress);
    }

    private IEnumerator DevAutoStartNextFrame()
    {
        yield return null;

        if (_state != GameState.Opening)
        {
            yield break;
        }

        PhaseManager phaseManager = PhaseManager.Instance;
        if (phaseManager == null)
        {
            Debug.LogWarning("[GameManager] devAutoStart：場景裡沒有 PhaseManager，只進入 Playing（沒有階段流程）。", this);
            BeginRun();
            yield break;
        }

        phaseManager.BeginTutorialPhase();
        if (_devAutoStart == DevAutoStartMode.Phase1)
        {
            phaseManager.NotifyTutorialFinished();
        }
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

    private void HandlePlayerDied()
    {
        // HP 歸零 → 立即失敗（沒有倒地）。暫停中或還沒開始時不判定
        if (_state != GameState.Playing || _bossDefeated)
        {
            return;
        }

        // 新手教學期間不進結算：由 TutorialManager（A12）補滿 HP、從當下那一步繼續
        if (PhaseManager.Instance != null && PhaseManager.Instance.CurrentPhase == GamePhase.Tutorial)
        {
            Debug.Log("[GameManager] 新手教學中 HP 歸零，不進結算（補滿與重來由 TutorialManager 處理，A12）。", this);
            return;
        }

        EndGame(false);
    }

    private void EndGame(bool isWin)
    {
        if (_gameEnded)
        {
            return;
        }
        _gameEnded = true;
        _state = GameState.Ended;

        float currentHp = _playerHealth != null ? _playerHealth.CurrentHp : 100f;
        float hp = isWin && _bossDefeated ? _victoryHp : currentHp;
        float maxHp = _playerHealth != null ? _playerHealth.MaxHp : 100f;
        float smokeSeconds = _exposureSystem != null ? _exposureSystem.SmokeSeconds : 0f;

        _lastResult = ScoreCalculator.Calculate(isWin, hp, maxHp, _elapsedTime, smokeSeconds);
        _lastResult.Title = ""; // A18 填頭銜
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
                  + "，用時 " + _elapsedTime.ToString("F1") + " 秒，總分 " + _lastResult.TotalScore.ToString("F1")
                  + "，評級 " + _lastResult.Grade, this);

        OnGameEnded?.Invoke(_lastResult);
    }
}

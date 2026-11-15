using System;
using UnityEngine;

/// <summary>
/// 管理整局遊戲：狀態、倒數時間、淨化進度、暫停與結算。
/// 場景裡只有一個，放在 GameScene，用 GameManager.Instance 取得。
/// （A2 空殼：倒數、Esc 暫停、勝敗判定在 A5 實作）
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>場景中唯一的 GameManager。</summary>
    public static GameManager Instance { get; private set; }

    [Header("回合設定")]
    [Tooltip("一局的秒數（預設 420 秒，展示模式約 90 秒）")]
    [SerializeField] private float _roundDuration = 420f;

    private GameState _state = GameState.Opening;
    private float _remainingTime;
    private float _purificationProgress;
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

    /// <summary>淨化進度改變時觸發。參數：目前進度 0–100。</summary>
    public event Action<float> OnProgressChanged;

    /// <summary>倒數開始時觸發。</summary>
    public event Action OnRoundStarted;

    /// <summary>剩 60 秒時觸發一次（小地圖顯示菸怪）。</summary>
    public event Action OnFinalMinute;

    /// <summary>暫停狀態改變時觸發。參數：是否暫停。</summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>勝利或失敗結算時觸發。</summary>
    public event Action<ScoreResult> OnGameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] 場景中有超過一個 GameManager，請只保留一個。", this);
            return;
        }
        Instance = this;
        _remainingTime = _roundDuration;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 開始一局的倒數（由 RouteManager 在玩家走出死巷時呼叫）。
    /// </summary>
    public void StartRound()
    {
        _state = GameState.Playing;
        _remainingTime = _roundDuration;
        _purificationProgress = 0f;
        Time.timeScale = 1f;
        OnProgressChanged?.Invoke(_purificationProgress);
        OnRoundStarted?.Invoke();
    }

    /// <summary>
    /// 增加淨化進度（B 的 SmokeMonster 被擊敗時呼叫）。結果會限制在 0–100。
    /// </summary>
    public void AddPurificationProgress(float amount)
    {
        _purificationProgress = Mathf.Clamp(_purificationProgress + amount, 0f, 100f);
        OnProgressChanged?.Invoke(_purificationProgress);
    }

    /// <summary>
    /// 暫停或繼續遊戲。暫停時 Time.timeScale = 0。只在遊戲進行中或暫停中有效。
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (paused && _state == GameState.Playing)
        {
            _state = GameState.Paused;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }
        else if (!paused && _state == GameState.Paused)
        {
            _state = GameState.Playing;
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }
    }
}

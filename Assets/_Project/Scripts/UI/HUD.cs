using TMPro;
using UnityEngine;

/// <summary>顯示玩家血量、回合剩餘時間與淨化進度。</summary>
public class HUD : MonoBehaviour
{
    [Header("場景資料")]
    [Tooltip("拖入場景中的玩家血量元件。")]
    [SerializeField] private PlayerHealth _playerHealth;
    [Tooltip("拖入玩家身上的互動元件。留空會自動在場景裡找。")]          // [I1 整合]
    [SerializeField] private PlayerInteractor _playerInteractor;

    [Header("介面文字")]
    [Tooltip("顯示目前血量與最大血量。")]
    [SerializeField] private TMP_Text _hpText;
    [Tooltip("以分、秒顯示剩餘時間。")]
    [SerializeField] private TMP_Text _timeText;
    [Tooltip("顯示目前淨化百分比與目標。")]
    [SerializeField] private TMP_Text _progressText;
    [Tooltip("顯示互動提示，例如「按 E 開啟寶箱」。沒有可互動物件時隱藏。")]  // [I1 整合]
    [SerializeField] private TMP_Text _interactPromptText;

    private GameManager _gameManager;
    private int _displayedSeconds = -1;

    private void OnEnable()
    {
        if (_playerHealth == null)                                   // [I1 整合] 欄位空著時自動找玩家
            _playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (_playerHealth != null)
        {
            _playerHealth.OnHpChanged += ShowHealth;
            ShowHealth(_playerHealth.CurrentHp, _playerHealth.MaxHp);
        }

        BindGameManager(GameManager.Instance);

        if (_playerInteractor == null)                               // [I1 整合] 互動提示
            _playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        if (_playerInteractor != null)
        {
            _playerInteractor.OnFocusChanged += ShowInteractPrompt;
            ShowInteractPrompt(_playerInteractor.CurrentTarget);
        }
        else
            ShowInteractPrompt(null);
    }

    private void Start()
    {
        // 場景中所有物件完成 Awake 後，讀取初始血量。
        if (_playerHealth != null)
            ShowHealth(_playerHealth.CurrentHp, _playerHealth.MaxHp);

        if (_playerInteractor != null)                               // [I1 整合] 啟動時先讀一次目前的互動目標
            ShowInteractPrompt(_playerInteractor.CurrentTarget);
    }

    private void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager != _gameManager)
            BindGameManager(manager);

        if (manager == null || _timeText == null)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(manager.RemainingTime));
        if (seconds == _displayedSeconds)
            return;

        _displayedSeconds = seconds;
        _timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnHpChanged -= ShowHealth;

        if (_playerInteractor != null)                               // [I1 整合]
            _playerInteractor.OnFocusChanged -= ShowInteractPrompt;

        BindGameManager(null);
    }

    private void BindGameManager(GameManager manager)
    {
        if (_gameManager != null)
            _gameManager.OnProgressChanged -= ShowProgress;

        _gameManager = manager;
        _displayedSeconds = -1;

        if (manager != null)
        {
            manager.OnProgressChanged += ShowProgress;
            ShowProgress(manager.PurificationProgress);
        }
    }

    private void ShowHealth(float current, float maximum)
    {
        if (_hpText != null)
            _hpText.text = $"{current:0.#} / {maximum:0.#}";
    }

    // [I1 整合] 有互動目標時顯示它的提示文字，沒有時隱藏
    private void ShowInteractPrompt(IInteractable target)
    {
        if (_interactPromptText == null)
            return;

        bool hasPrompt = target != null && !string.IsNullOrEmpty(target.InteractPrompt);
        _interactPromptText.enabled = hasPrompt;
        if (hasPrompt)
            _interactPromptText.text = target.InteractPrompt;
    }

    private void ShowProgress(float progress)
    {
        if (_progressText != null)
            _progressText.text = $"{progress:0.#}% / 100%";
    }
}

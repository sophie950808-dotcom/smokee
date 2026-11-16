using TMPro;
using UnityEngine;

/// <summary>顯示玩家血量、回合剩餘時間與淨化進度。</summary>
public class HUD : MonoBehaviour
{
    [Header("場景資料")]
    [Tooltip("拖入場景中的玩家血量元件。")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("介面文字")]
    [Tooltip("顯示目前血量與最大血量。")]
    [SerializeField] private TMP_Text _hpText;
    [Tooltip("以分、秒顯示剩餘時間。")]
    [SerializeField] private TMP_Text _timeText;
    [Tooltip("顯示目前淨化百分比與目標。")]
    [SerializeField] private TMP_Text _progressText;

    private GameManager _gameManager;
    private int _displayedSeconds = -1;

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHpChanged += ShowHealth;
            ShowHealth(_playerHealth.CurrentHp, _playerHealth.MaxHp);
        }

        BindGameManager(GameManager.Instance);
    }

    private void Start()
    {
        // 場景中所有物件完成 Awake 後，讀取初始血量。
        if (_playerHealth != null)
            ShowHealth(_playerHealth.CurrentHp, _playerHealth.MaxHp);
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

    private void ShowProgress(float progress)
    {
        if (_progressText != null)
            _progressText.text = $"{progress:0.#}% / 100%";
    }
}

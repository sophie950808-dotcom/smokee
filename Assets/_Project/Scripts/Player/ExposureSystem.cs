using System;
using UnityEngine;

/// <summary>
/// 玩家的二手菸暴露值（0–100）。掛在 Player 身上。
/// 規格見 docs/INTERFACES.md 第 7 節、docs/DECISIONS.md 數值表。
/// - 在「正在冒煙」的 SmokeSource 半徑內：每秒上升 10 ×（1 − 距離 ÷ 半徑）× RiseMultiplier；
///   同時在多個範圍內時，取上升速度最大的那一個（不加總）。
/// - 不在任何範圍內：每秒下降 3。
/// - 距離只算水平距離（忽略高度），玩家跳躍時數值不會亂跳。
/// - 只在 GameManager 為 Opening 或 Playing 時計算；場景裡沒有 GameManager 時照樣計算（方便單獨測試）。
/// - SmokeSeconds（吸入二手菸量）只在 Playing（倒數中）累加，開場死巷裡的時間不算；
///   沒有 GameManager 時照樣累加。
/// </summary>
public class ExposureSystem : MonoBehaviour
{
    private const string MaxExposureSpeedKey = "MaxExposure";
    private const float MaxExposure = 100f;

    [Header("暴露值變化")]
    [Tooltip("在煙霧範圍內的上升速度係數。實際每秒上升 = 這個值 ×（1 − 距離 ÷ 半徑）")]
    [SerializeField] private float _riseRate = 54f;

    [Tooltip("不在任何煙霧範圍內時，每秒下降多少")]
    [SerializeField] private float _decayRate = 18f;

    [Header("門檻")]
    [Tooltip("暴露值大於或等於這個數字時顯示警示文字")]
    [SerializeField] private float _warningThreshold = 80f;

    [Header("暴露值 100 時")]
    [Tooltip("暴露值 100 時的移動速度倍率，0.7 = 移速 −30%")]
    [SerializeField] private float _maxedSpeedMultiplier = 0.7f;

    private float _exposure;
    private float _smokeSeconds;
    private float _riseMultiplier = 1f;
    private bool _isVisualSuppressed;
    private bool _isWarning;
    private bool _isMaxed;
    private PlayerController _playerController;

    /// <summary>實際暴露值 0–100。</summary>
    public float Exposure => _exposure;

    /// <summary>畫面效果用的暴露值（B 讀這個）。v3.1 起一般都等於 Exposure。</summary>
    public float VisualExposure => _isVisualSuppressed ? 0f : _exposure;

    /// <summary>暴露值 ≥ 警示門檻（預設 80）時為 true。</summary>
    public bool IsWarning => _isWarning;

    /// <summary>暴露值 ≥ 100 時為 true。</summary>
    public bool IsMaxed => _isMaxed;

    /// <summary>累計待在（正在冒煙的）煙霧範圍內的秒數。只算倒數中（Playing），開場時間不算。</summary>
    public float SmokeSeconds => _smokeSeconds;

    /// <summary>
    /// 【即將移除】上升速度倍率，預設 1。v3.1 的道具不再使用。不會小於 0。
    /// </summary>
    [Obsolete("v3.1 道具不再改變暴露值上升速度。A13 刪除。")]
    public float RiseMultiplier
    {
        get => _riseMultiplier;
        set => _riseMultiplier = Mathf.Max(0f, value);
    }

    /// <summary>
    /// 【即將移除】是否抑制畫面煙霧效果。v3.1 的戒菸噴霧改成攻擊 BOSS，不再使用。
    /// </summary>
    [Obsolete("v3.1 戒菸噴霧改成攻擊 BOSS，不再抑制畫面效果。A13 刪除。")]
    public bool IsVisualSuppressed
    {
        get => _isVisualSuppressed;
        set => _isVisualSuppressed = value;
    }

    /// <summary>暴露值改變時觸發。參數：目前暴露值。</summary>
    public event Action<float> OnExposureChanged;

    /// <summary>進入／離開警示（≥ 80）時觸發。true 顯示警示、false 隱藏。</summary>
    public event Action<bool> OnWarningChanged;

    /// <summary>進入／離開 100 時觸發（咳嗽音效、減速）。</summary>
    public event Action<bool> OnMaxedChanged;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogWarning("[ExposureSystem] 同一個物件上找不到 PlayerController，暴露值 100 時無法減速。", this);
        }
    }

    private void Update()
    {
        if (!ShouldCalculate())
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        bool inSmoke;
        float riseSpeed = GetHighestRiseSpeed(out inSmoke);

        // 吸入二手菸量只算倒數中，開場在死巷裡的時間不算
        if (inSmoke && IsRoundPlaying())
        {
            _smokeSeconds += deltaTime;
        }

        float change = inSmoke ? riseSpeed * _riseMultiplier : -_decayRate;
        SetExposure(_exposure + change * deltaTime);
    }

    private void OnEnable()
    {
        // 重新啟用時，若暴露值仍是 100，把減速加回去
        if (_isMaxed)
        {
            ApplyMaxedSpeed();
        }
    }

    private void OnDisable()
    {
        // 元件被停用時，不要讓減速一直留在玩家身上
        if (_playerController != null)
        {
            _playerController.RemoveSpeedModifier(MaxExposureSpeedKey);
        }
    }

    /// <summary>
    /// 【只給 GameManager、TutorialManager 使用】把暴露值歸零（教學結束、教學中 HP 歸零、重新挑戰）。
    /// </summary>
    public void ResetExposure()
    {
        SetExposure(0f);
    }

    /// <summary>只在 Opening 或 Playing、而且沒有「暫停計時」時計算；沒有 GameManager 時照樣計算。</summary>
    private bool ShouldCalculate()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return true;
        }

        if (gameManager.IsTimerPaused)
        {
            return false;
        }

        return gameManager.State == GameState.Opening || gameManager.State == GameState.Playing;
    }

    /// <summary>是否在倒數中（Playing）；沒有 GameManager 時視為是（方便單獨測試）。</summary>
    private bool IsRoundPlaying()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager == null || gameManager.State == GameState.Playing;
    }

    /// <summary>
    /// 找出所有「正在冒煙、且玩家在半徑內」的來源中，上升速度最大的那一個。
    /// inSmoke：玩家是否在任何一個正在冒煙的來源範圍內。
    /// </summary>
    private float GetHighestRiseSpeed(out bool inSmoke)
    {
        inSmoke = false;
        float highest = 0f;
        Vector3 playerPosition = transform.position;

        var sources = SmokeSource.All;
        for (int i = 0; i < sources.Count; i++)
        {
            SmokeSource source = sources[i];
            if (source == null || !source.IsEmitting || source.Radius <= 0f)
            {
                continue;
            }

            // 只算水平距離
            Vector3 offset = source.transform.position - playerPosition;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance >= source.Radius)
            {
                continue;
            }

            inSmoke = true;
            float speed = _riseRate * (1f - distance / source.Radius);
            if (speed > highest)
            {
                highest = speed;
            }
        }

        return highest;
    }

    private void SetExposure(float value)
    {
        float newValue = Mathf.Clamp(value, 0f, MaxExposure);
        if (newValue == _exposure)
        {
            // 停在 0 或 100 時不重複觸發事件
            return;
        }

        _exposure = newValue;
        OnExposureChanged?.Invoke(_exposure);

        bool isWarning = _exposure >= _warningThreshold;
        if (isWarning != _isWarning)
        {
            _isWarning = isWarning;
            OnWarningChanged?.Invoke(_isWarning);
        }

        bool isMaxed = _exposure >= MaxExposure;
        if (isMaxed != _isMaxed)
        {
            _isMaxed = isMaxed;
            ApplyMaxedSpeed();
            OnMaxedChanged?.Invoke(_isMaxed);
        }
    }

    private void ApplyMaxedSpeed()
    {
        if (_playerController == null)
        {
            return;
        }

        if (_isMaxed)
        {
            _playerController.SetSpeedModifier(MaxExposureSpeedKey, _maxedSpeedMultiplier);
        }
        else
        {
            _playerController.RemoveSpeedModifier(MaxExposureSpeedKey);
        }
    }
}

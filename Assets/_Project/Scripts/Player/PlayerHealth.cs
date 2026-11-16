using System;
using UnityEngine;

/// <summary>
/// 玩家的血量。掛在 Player 身上。規格見 docs/INTERFACES.md 第 6 節、docs/DECISIONS.md。
/// - 開局滿血；TakeDamage、Heal 都限制在 0–MaxHp，變動時觸發 OnHpChanged。
/// - HP 歸零時觸發 OnDied（只觸發一次）→ GameManager 判定失敗（沒有倒地、沒有復起）。
/// - 暴露值 100（ExposureSystem.IsMaxed）時，每秒扣血（預設 5）。
/// - 場景裡有 GameManager 時，只在 Playing 狀態下會受傷或回血（遊戲結束、暫停、開場時都不會）；
///   沒有 GameManager 時照常運作（方便單獨測試）。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量")]
    [Tooltip("最大 HP（預設 100）")]
    [SerializeField] private float _maxHp = 100f;

    [Header("暴露值 100 時扣血")]
    [Tooltip("暴露值達到 100 時，每秒扣多少 HP（預設 5）")]
    [SerializeField] private float _maxedDamagePerSecond = 5f;

    [Tooltip("暴露值系統。留空會自動抓同一個物件上的 ExposureSystem")]
    [SerializeField] private ExposureSystem _exposureSystem;

    private float _currentHp;
    private bool _isDead;

    /// <summary>目前 HP。</summary>
    public float CurrentHp => _currentHp;

    /// <summary>最大 HP。</summary>
    public float MaxHp => _maxHp;

    /// <summary>HP 改變時觸發。參數：目前 HP、最大 HP。</summary>
    public event Action<float, float> OnHpChanged;

    /// <summary>HP 歸零時觸發一次 → GameManager 判定失敗。</summary>
    public event Action OnDied;

    private void Awake()
    {
        _currentHp = _maxHp;

        if (_exposureSystem == null)
        {
            _exposureSystem = GetComponent<ExposureSystem>();
        }
        if (_exposureSystem == null)
        {
            Debug.LogWarning("[PlayerHealth] 找不到 ExposureSystem：暴露值 100 時不會扣血。", this);
        }
    }

    private void Update()
    {
        if (_exposureSystem == null || !_exposureSystem.IsMaxed || !CanChangeHp())
        {
            return;
        }

        // 每幀扣一點，一秒剛好扣 _maxedDamagePerSecond。暫停時 deltaTime 為 0，也不會扣
        TakeDamage(_maxedDamagePerSecond * Time.deltaTime);
    }

    /// <summary>
    /// 回復 HP（B 的 EscortNPC 護送成功時呼叫 Heal(10)）。不會超過最大 HP。
    /// 已死亡、或 GameManager 不是 Playing 時無效。
    /// </summary>
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f || !CanChangeHp())
        {
            return;
        }

        float newHp = Mathf.Min(_currentHp + amount, _maxHp);
        if (newHp == _currentHp)
        {
            // 已經滿血，不重複觸發事件
            return;
        }

        _currentHp = newHp;
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    /// <summary>
    /// 扣除 HP。不會低於 0；歸零時觸發 OnDied（只觸發一次）。
    /// 已死亡、或 GameManager 不是 Playing 時無效。
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_isDead || amount <= 0f || !CanChangeHp())
        {
            return;
        }

        _currentHp = Mathf.Max(_currentHp - amount, 0f);
        OnHpChanged?.Invoke(_currentHp, _maxHp);

        if (_currentHp <= 0f)
        {
            _isDead = true;
            OnDied?.Invoke();
        }
    }

    /// <summary>
    /// 只有 Playing 狀態可以改變 HP；場景裡沒有 GameManager 時一律可以。
    /// （避免在開場或暫停時 HP 歸零，GameManager 卻不判定失敗）
    /// </summary>
    private bool CanChangeHp()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager == null || gameManager.State == GameState.Playing;
    }
}

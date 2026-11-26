using System;
using UnityEngine;

/// <summary>
/// BOSS 的血量、受傷與回血。實作 IHittable（玩家近戰）與 IBossDamageable（噴霧、開發者工具）。掛在 BOSS 根物件。
/// - 近戰每下 _meleeDamage（預設 3），BOSS 在天上時打不到；噴霧每下 7.5（數值在 PlayerEffects）。
/// - BOSS 戰還沒開始（Inactive、Intro）、已被打倒、遊戲暫停或「暫停計時」中 → 不受傷。
/// - 血量歸零 → OnBossDefeated（只觸發一次）。停止攻擊、清菸圈、通知 GameManager 由 BossController 處理。
/// - 淨化進度不跟血量連動（A11）：第三階段固定 60%，打倒才 100%。
/// - 回血：地面 = 抽菸人數 × 1／秒；天上 = 1 + 抽菸人數 × 1／秒；Final 不回血。【A16 實作，A14 只提供 Heal】
/// 規格見 docs/INTERFACES.md 第 14 節。
/// 【A14 完成】受傷、事件、Heal；【A16】自動回血。
/// </summary>
public class BossHealth : MonoBehaviour, IHittable, IBossDamageable
{
    /// <summary>場景中唯一的 BOSS 血量。</summary>
    public static BossHealth Instance { get; private set; }

    [Header("血量")]
    [Tooltip("BOSS 最大血量")]
    [SerializeField] private float _maxHp = 150f;

    [Tooltip("玩家近戰每下造成的傷害（BOSS 在天上時打不到）")]
    [SerializeField] private float _meleeDamage = 3f;

    [Header("回血（每秒，A16 使用）")]
    [Tooltip("BOSS 在地面時，每位還在抽菸的 NPC 每秒回多少")]
    [SerializeField] private float _groundRegenPerSmoker = 1f;

    [Tooltip("BOSS 在天上時，每秒基本回多少")]
    [SerializeField] private float _skyRegenBase = 1f;

    [Tooltip("BOSS 在天上時，每位還在抽菸的 NPC 每秒再回多少")]
    [SerializeField] private float _skyRegenPerSmoker = 1f;

    private float _currentHp;
    private bool _isDefeated;
    private BossController _controller;

    /// <summary>目前血量。</summary>
    public float CurrentHp => _currentHp;

    /// <summary>最大血量。</summary>
    public float MaxHp => _maxHp;

    /// <summary>是否已被打倒。</summary>
    public bool IsDefeated => _isDefeated;

    /// <summary>血量改變時觸發（受傷、回血）。參數：目前血量、最大血量。C 的 BOSS 血條監聽。</summary>
    public event Action<float, float> OnBossHpChanged;

    /// <summary>受到傷害時觸發。參數：實際扣掉的血量、來源（PlayerMelee 近戰／Spray 噴霧／Dev）。B 播不同的受擊特效。</summary>
    public event Action<float, DamageSource> OnBossDamaged;

    /// <summary>血量歸零、被打倒時觸發一次。</summary>
    public event Action OnBossDefeated;

    /// <summary>
    /// 每次進入 Play 時先清掉 static 的 Instance。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        _controller = GetComponent<BossController>();
        if (_controller == null)
        {
            Debug.LogWarning("[BossHealth] 同一個物件上找不到 BossController，BOSS 永遠不會受傷。", this);
        }

        _currentHp = _maxHp;

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[BossHealth] 場景中有超過一個 BossHealth，請只保留一個。", this);
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
    /// 扣 BOSS 血量（噴霧 (7.5, Spray)、開發者工具 (…, Dev)）。BOSS 戰開始前、被打倒後、暫停中沒有作用。
    /// </summary>
    public void TakeDamage(float amount, DamageSource source)
    {
        ApplyDamage(amount, source);
    }

    /// <summary>玩家近戰命中（PlayerAttack 呼叫）：每下 _meleeDamage；BOSS 在天上時無效。</summary>
    public void TakeHit(int hitCount)
    {
        if (_controller != null && _controller.State == BossState.Sky)
        {
            Debug.Log("[BossHealth] BOSS 在天上，近戰打不到。", this);
            return;
        }

        ApplyDamage(_meleeDamage * Mathf.Max(1, hitCount), DamageSource.PlayerMelee);
    }

    /// <summary>
    /// 回血（A16 的自動回血使用）。不會超過最大血量；BOSS 戰開始前、被打倒後、暫停中沒有作用。
    /// 淨化進度不會因為回血而改變。
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0f || !CanChangeHp())
        {
            return;
        }

        float newHp = Mathf.Min(_currentHp + amount, _maxHp);
        if (newHp == _currentHp)
        {
            return;
        }

        _currentHp = newHp;
        OnBossHpChanged?.Invoke(_currentHp, _maxHp);
    }

    private void ApplyDamage(float amount, DamageSource source)
    {
        if (amount <= 0f)
        {
            return;
        }
        if (!CanChangeHp())
        {
            Debug.Log("[BossHealth] BOSS 戰還沒開始（或已結束、暫停中），不受傷（" + source + "，" + amount + "）。", this);
            return;
        }

        float before = _currentHp;
        _currentHp = Mathf.Max(0f, _currentHp - amount);
        float dealt = before - _currentHp;

        Debug.Log("[BossHealth] 受到 " + dealt + " 傷害（" + source + "），剩餘 " + _currentHp + " / " + _maxHp, this);
        OnBossDamaged?.Invoke(dealt, source);
        OnBossHpChanged?.Invoke(_currentHp, _maxHp);

        if (_currentHp <= 0f)
        {
            _isDefeated = true;
            Debug.Log("[BossHealth] BOSS 被打倒！", this);
            OnBossDefeated?.Invoke();
        }
    }

    /// <summary>
    /// BOSS 戰進行中（地面、天上、最終局面）、還沒被打倒、遊戲 Playing 而且沒有「暫停計時」時才能改變血量。
    /// 場景裡沒有 GameManager 時只看 BOSS 狀態（方便單獨測試）。
    /// </summary>
    private bool CanChangeHp()
    {
        if (_isDefeated || _controller == null)
        {
            return false;
        }

        BossState state = _controller.State;
        if (state != BossState.Ground && state != BossState.Sky && state != BossState.Final)
        {
            return false;
        }

        GameManager gameManager = GameManager.Instance;
        return gameManager == null || (gameManager.State == GameState.Playing && !gameManager.IsTimerPaused);
    }

    private void OnValidate()
    {
        _maxHp = Mathf.Max(1f, _maxHp);
        _meleeDamage = Mathf.Max(0f, _meleeDamage);
        _groundRegenPerSmoker = Mathf.Max(0f, _groundRegenPerSmoker);
        _skyRegenBase = Mathf.Max(0f, _skyRegenBase);
        _skyRegenPerSmoker = Mathf.Max(0f, _skyRegenPerSmoker);
    }
}

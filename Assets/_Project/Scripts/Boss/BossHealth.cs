using System;
using UnityEngine;

/// <summary>
/// BOSS 的血量、受傷與回血。實作 IHittable（玩家近戰）。掛在 BOSS 根物件。
/// - 近戰每下 3（BOSS 在天上時打不到）；噴霧每下 7.5（由 PlayerEffects 呼叫 TakeDamage）。
/// - 回血：地面 = 抽菸人數 × 1／秒；天上 = 1 + 抽菸人數 × 1／秒；Final 不回血。
/// - GameManager 監聽 OnBossHpChanged 換算淨化進度：60% + 40% ×（1 − 目前 ÷ 最大）。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// 【A10 空殼】名稱、參數、Inspector 數值已定案；A14 完成。
/// </summary>
public class BossHealth : MonoBehaviour, IHittable
{
    /// <summary>場景中唯一的 BOSS 血量。</summary>
    public static BossHealth Instance { get; private set; }

    [Header("血量")]
    [Tooltip("BOSS 最大血量")]
    [SerializeField] private float _maxHp = 150f;

    [Tooltip("玩家近戰每下造成的傷害")]
    [SerializeField] private float _meleeDamage = 3f;

    [Header("回血（每秒）")]
    [Tooltip("BOSS 在地面時，每位還在抽菸的 NPC 每秒回多少")]
    [SerializeField] private float _groundRegenPerSmoker = 1f;

    [Tooltip("BOSS 在天上時，每秒基本回多少")]
    [SerializeField] private float _skyRegenBase = 1f;

    [Tooltip("BOSS 在天上時，每位還在抽菸的 NPC 每秒再回多少")]
    [SerializeField] private float _skyRegenPerSmoker = 1f;

    private float _currentHp;

    /// <summary>目前血量。</summary>
    public float CurrentHp => _currentHp;

    /// <summary>最大血量。</summary>
    public float MaxHp => _maxHp;

    /// <summary>是否已被打倒。</summary>
    public bool IsDefeated => _currentHp <= 0f;

#pragma warning disable 67 // A14 才會觸發
    /// <summary>血量改變時觸發。參數：目前血量、最大血量。C 的 BOSS 血條、GameManager 淨化換算監聽。</summary>
    public event Action<float, float> OnBossHpChanged;

    /// <summary>受到傷害時觸發。參數：傷害、來源。B 播受擊特效。</summary>
    public event Action<float, DamageSource> OnBossDamaged;

    /// <summary>被打倒時觸發一次。</summary>
    public event Action OnBossDefeated;
#pragma warning restore 67

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
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[BossHealth] 場景中有超過一個 BossHealth，請只保留一個。", this);
            return;
        }
        Instance = this;
        _currentHp = _maxHp;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>扣 BOSS 血量（噴霧、開發者工具用）。【A14 完成】</summary>
    public void TakeDamage(float amount, DamageSource source)
    {
        Debug.Log("[BossHealth] （A10 空殼）TakeDamage(" + amount + ", " + source + ")，A14 完成。", this);
    }

    /// <summary>玩家近戰命中（PlayerAttack 呼叫）：每下 _meleeDamage；BOSS 在天上時無效。【A14 完成】</summary>
    public void TakeHit(int hitCount)
    {
        Debug.Log("[BossHealth] （A10 空殼）TakeHit，近戰每下 " + _meleeDamage + "，A14 完成。", this);
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

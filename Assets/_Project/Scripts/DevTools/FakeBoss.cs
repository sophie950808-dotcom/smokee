using UnityEngine;

/// <summary>
/// 測試用假 BOSS（只在 TestScene_A 用）：實作 IBossDamageable，讓噴霧可以打。
/// 被打中時在 Console 印出受到的傷害與剩餘血量，並閃一下紅色；血量歸零後印出訊息並回滿，方便一直測。
/// 物件上需要有 Collider（可以是 Trigger），噴霧的射線才打得到。
/// 只模擬「受傷」，不會移動、不會攻擊；真正的 BOSS 是 A14 的 BossHealth。
/// </summary>
public class FakeBoss : MonoBehaviour, IBossDamageable
{
    [Header("血量")]
    [Tooltip("最大血量（和真正 BOSS 一樣預設 150）")]
    [SerializeField] private float _maxHp = 150f;

    [Header("顏色")]
    [Tooltip("平常的顏色")]
    [SerializeField] private Color _normalColor = new Color(0.35f, 0.35f, 0.35f);

    [Tooltip("被打中時閃的顏色")]
    [SerializeField] private Color _hitColor = Color.red;

    [Tooltip("閃色持續秒數")]
    [SerializeField] private float _flashSeconds = 0.15f;

    private float _currentHp;
    private float _flashUntil;
    private Renderer _renderer;

    /// <summary>目前血量（DevOverlay 顯示用）。</summary>
    public float CurrentHp => _currentHp;

    /// <summary>最大血量。</summary>
    public float MaxHp => _maxHp;

    private void Awake()
    {
        _currentHp = _maxHp;
        _renderer = GetComponentInChildren<Renderer>();
        SetColor(_normalColor);
    }

    private void Update()
    {
        if (_flashUntil > 0f && Time.time >= _flashUntil)
        {
            _flashUntil = 0f;
            SetColor(_normalColor);
        }
    }

    /// <summary>受到傷害（噴霧呼叫）：印出傷害與剩餘血量。</summary>
    public void TakeDamage(float amount, DamageSource source)
    {
        _currentHp = Mathf.Max(0f, _currentHp - amount);
        Debug.Log($"[FakeBoss] {name} 受到 {amount} 傷害（來源 {source}），剩餘血量 {_currentHp} / {_maxHp}", this);

        _flashUntil = Time.time + _flashSeconds;
        SetColor(_hitColor);

        if (_currentHp <= 0f)
        {
            Debug.Log($"[FakeBoss] {name} 血量歸零！（測試用，血量回滿）", this);
            _currentHp = _maxHp;
        }
    }

    private void SetColor(Color color)
    {
        if (_renderer == null)
        {
            return;
        }

        Material material = _renderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }
    }
}

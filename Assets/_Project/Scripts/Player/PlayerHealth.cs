using System;
using UnityEngine;

/// <summary>
/// 玩家的血量。HP 歸零時觸發 OnDied（GameManager 判定失敗，沒有倒地）。
/// （A2 空殼：暴露值 100 扣血在 A7 實作）
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量")]
    [Tooltip("最大 HP（預設 100）")]
    [SerializeField] private float _maxHp = 100f;

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
    }

    /// <summary>
    /// 回復 HP（B 的 EscortNPC 護送成功時呼叫 Heal(10)）。不會超過最大 HP。
    /// </summary>
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f)
        {
            return;
        }
        _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    /// <summary>
    /// 扣除 HP。不會低於 0；歸零時觸發 OnDied。
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_isDead || amount <= 0f)
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
}

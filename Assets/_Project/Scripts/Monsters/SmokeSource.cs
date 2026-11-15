using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 煙霧來源。掛在開場吸菸 NPC 與每一隻菸怪身上，ExposureSystem 依距離計算暴露值。
/// 啟用時自動加入 All，停用時自動移除。
/// </summary>
public class SmokeSource : MonoBehaviour
{
    private static readonly List<SmokeSource> _all = new List<SmokeSource>();

    [Header("煙霧設定")]
    [Tooltip("煙霧影響半徑（公尺）。小型菸怪預設 50，大型菸怪暫定 75")]
    [SerializeField] private float _radius = 50f;

    [Tooltip("是否正在冒煙。false 時不影響暴露值（菸怪被擊敗時設為 false）")]
    [SerializeField] private bool _isEmitting = true;

    /// <summary>目前所有啟用中的煙霧來源。</summary>
    public static IReadOnlyList<SmokeSource> All => _all;

    /// <summary>煙霧影響半徑（公尺）。</summary>
    public float Radius
    {
        get => _radius;
        set => _radius = Mathf.Max(0f, value);
    }

    /// <summary>是否正在冒煙。false 時不影響暴露值。</summary>
    public bool IsEmitting
    {
        get => _isEmitting;
        set => _isEmitting = value;
    }

    private void OnEnable()
    {
        if (!_all.Contains(this))
        {
            _all.Add(this);
        }
    }

    private void OnDisable()
    {
        _all.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        // 在 Scene 視窗選取物件時，畫出煙霧範圍方便調整
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}

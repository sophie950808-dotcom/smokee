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

    /// <summary>目前所有啟用中的煙霧來源（供 A 與小地圖讀取）。</summary>
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

    private void OnDestroy()
    {
        _all.Remove(this);
    }

    private void OnValidate()
    {
        // Inspector 輸入負數時自動修正為 0
        _radius = Mathf.Max(0f, _radius);
    }

    private void OnDrawGizmos()
    {
        // 平常就在 Scene 視窗畫出淡淡的煙霧範圍；停止冒煙時顏色更淡
        Gizmos.color = _isEmitting
            ? new Color(0.6f, 0.6f, 0.6f, 0.35f)
            : new Color(0.6f, 0.6f, 0.6f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }

    private void OnDrawGizmosSelected()
    {
        // 選取物件時畫得更明顯，方便調整半徑
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
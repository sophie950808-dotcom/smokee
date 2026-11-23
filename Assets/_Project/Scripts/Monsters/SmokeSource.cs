using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 煙霧來源。掛在開場朋友、每一隻菸怪、第三階段吸菸 NPC、BOSS、菸炮落地的煙霧區身上，
/// ExposureSystem 依距離計算暴露值。啟用時自動加入 All，停用時自動移除。
/// 規格見 docs/INTERFACES.md 第 11 節。
/// 實際半徑 = 基本半徑 × 自己的倍率 ×（受拒絕影響時的拒絕倍率）。
/// </summary>
public class SmokeSource : MonoBehaviour
{
    private static readonly List<SmokeSource> _all = new List<SmokeSource>();
    private static bool _refusalActive;
    private static float _refusalMultiplier = 1f;

    [Header("煙霧設定")]
    [Tooltip("基本煙霧半徑（公尺）。小菸怪、吸菸 NPC 4；BOSS 7")]
    [SerializeField] private float _radius = 4f;

    [Tooltip("這個來源自己的半徑倍率，預設 1")]
    [SerializeField] private float _radiusMultiplier = 1f;

    [Tooltip("是否正在冒煙。false 時不影響暴露值（菸怪被擊敗時設為 false）")]
    [SerializeField] private bool _isEmitting = true;

    [Tooltip("勾選 = 玩家拒絕幫忙時會變大、而且無法消除（只有菸怪要勾）")]
    [SerializeField] private bool _affectedByRefusal = false;

    /// <summary>目前所有啟用中的煙霧來源（供 A 與小地圖讀取）。</summary>
    public static IReadOnlyList<SmokeSource> All => _all;

    /// <summary>目前是否在「拒絕幫忙」懲罰中。</summary>
    public static bool RefusalActive => _refusalActive;

    /// <summary>拒絕幫忙時菸怪的半徑倍率（1 → 最多 2）。</summary>
    public static float RefusalMultiplier => _refusalMultiplier;

    /// <summary>拒絕幫忙懲罰開始／結束時觸發。參數：是否開始。B 用來換「打不掉」的外觀。</summary>
    public static event Action<bool> OnRefusalStateChanged;

    /// <summary>
    /// 實際煙霧半徑（公尺）＝ 基本半徑 × 倍率 ×（受拒絕影響時的拒絕倍率）。
    /// 設定時改的是基本半徑（相容舊程式）。
    /// </summary>
    public float Radius
    {
        get
        {
            float radius = _radius * _radiusMultiplier;
            if (_affectedByRefusal && _refusalActive)
            {
                radius *= _refusalMultiplier;
            }
            return radius;
        }
        set => _radius = Mathf.Max(0f, value);
    }

    /// <summary>基本煙霧半徑（公尺），Inspector 設定的值。</summary>
    public float BaseRadius
    {
        get => _radius;
        set => _radius = Mathf.Max(0f, value);
    }

    /// <summary>這個來源自己的半徑倍率，預設 1。不會小於 0。</summary>
    public float RadiusMultiplier
    {
        get => _radiusMultiplier;
        set => _radiusMultiplier = Mathf.Max(0f, value);
    }

    /// <summary>是否正在冒煙。false 時不影響暴露值。</summary>
    public bool IsEmitting
    {
        get => _isEmitting;
        set => _isEmitting = value;
    }

    /// <summary>是否受拒絕幫忙懲罰影響。菸怪設為 true（B 的 SmokeMonster 在 Awake 設定），其他來源 false。</summary>
    public bool AffectedByRefusal
    {
        get => _affectedByRefusal;
        set => _affectedByRefusal = value;
    }

    /// <summary>現在是否無法消除（拒絕幫忙中且受影響）。B 的 SmokeMonster.TakeHit 看到 true 就不算這一下。</summary>
    public bool IsIndestructible => _affectedByRefusal && _refusalActive;

    /// <summary>
    /// 【只給 PhaseManager 使用】設定拒絕幫忙懲罰。active：是否懲罰中；multiplier：菸怪半徑倍率（最小 1）。
    /// </summary>
    public static void SetRefusalState(bool active, float multiplier)
    {
        bool changed = active != _refusalActive;
        _refusalActive = active;
        _refusalMultiplier = active ? Mathf.Max(1f, multiplier) : 1f;

        if (changed)
        {
            OnRefusalStateChanged?.Invoke(active);
        }
    }

    /// <summary>
    /// 每次進入 Play 時清掉 static 的狀態（就算關閉 Domain Reload 也不會留下上一次的值）。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _all.Clear();
        _refusalActive = false;
        _refusalMultiplier = 1f;
        OnRefusalStateChanged = null;
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
        _radiusMultiplier = Mathf.Max(0f, _radiusMultiplier);
    }

    private void OnDrawGizmos()
    {
        // 平常就在 Scene 視窗畫出淡淡的煙霧範圍；停止冒煙時顏色更淡
        Gizmos.color = _isEmitting
            ? new Color(0.6f, 0.6f, 0.6f, 0.35f)
            : new Color(0.6f, 0.6f, 0.6f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, Radius);
    }

    private void OnDrawGizmosSelected()
    {
        // 選取物件時畫得更明顯，方便調整半徑
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}

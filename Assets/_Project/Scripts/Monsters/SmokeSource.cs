using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 煙霧來源。掛在開場朋友、每一隻菸怪、第三階段吸菸 NPC、BOSS、天上菸圈落地的煙霧區身上，
/// ExposureSystem 依距離計算暴露值。啟用時自動加入 All，停用時自動移除。
/// 規格見 docs/INTERFACES.md 第 11 節。
/// 實際半徑 = 基本半徑 × Inspector 倍率 × 所有 key 倍率相乘 ×（受拒絕影響時的拒絕倍率）。
/// 【A15 完成】key 倍率、OnRadiusChanged、拒絕幫忙懲罰（倍率與無法消除）、菸怪自動分類、換場景重設。
/// </summary>
public class SmokeSource : MonoBehaviour
{
    private static readonly List<SmokeSource> _all = new List<SmokeSource>();
    private static readonly List<SmokeSource> _notifyBuffer = new List<SmokeSource>();
    private static bool _refusalActive;
    private static float _refusalMultiplier = 1f;

    [Header("煙霧設定")]
    [Tooltip("基本煙霧半徑（公尺）。小菸怪、吸菸 NPC 4；BOSS 7")]
    [SerializeField] private float _radius = 4f;

    [Tooltip("這個來源自己的半徑倍率，預設 1（和程式用 key 設定的倍率相乘）")]
    [SerializeField] private float _radiusMultiplier = 1f;

    [Tooltip("是否正在冒煙。false 時不影響暴露值（菸怪被擊敗時設為 false）")]
    [SerializeField] private bool _isEmitting = true;

    [Tooltip("勾選 = 玩家拒絕幫忙時會變大、而且無法消除（只有菸怪要勾）。同一個物件上有 SmokeMonster 時會自動勾選")]
    [SerializeField] private bool _affectedByRefusal = false;

    private readonly Dictionary<string, float> _radiusModifiers = new Dictionary<string, float>();
    private float _lastNotifiedRadius = -1f;

    /// <summary>目前所有啟用中的煙霧來源（供 A 與小地圖讀取）。</summary>
    public static IReadOnlyList<SmokeSource> All => _all;

    /// <summary>目前是否在「拒絕幫忙」懲罰中。</summary>
    public static bool RefusalActive => _refusalActive;

    /// <summary>拒絕幫忙時菸怪的半徑倍率（1 → 最多 2）。沒有拒絕時是 1。</summary>
    public static float RefusalMultiplier => _refusalMultiplier;

    /// <summary>拒絕幫忙懲罰開始／結束時觸發。參數：是否開始。B 用來換「打不掉」的外觀。（static 事件，記得在 OnDisable 取消監聽）</summary>
    public static event Action<bool> OnRefusalStateChanged;

    /// <summary>
    /// 這個來源的實際半徑改變時觸發。參數：新的實際半徑（公尺）。
    /// B 用「實際半徑 ÷ BaseRadius」縮放煙霧粒子。剛出生時不會觸發，請在 Start 讀一次 Radius。
    /// </summary>
    public event Action<float> OnRadiusChanged;

    /// <summary>
    /// 實際煙霧半徑（公尺）＝ 基本半徑 × Inspector 倍率 × 所有 key 倍率 ×（受拒絕影響時的拒絕倍率）。
    /// ExposureSystem 用這個值。設定時改的是基本半徑（相容舊程式）。
    /// </summary>
    public float Radius
    {
        get
        {
            float radius = _radius * _radiusMultiplier;
            foreach (float modifier in _radiusModifiers.Values)
            {
                radius *= modifier;
            }
            if (_affectedByRefusal && _refusalActive)
            {
                radius *= _refusalMultiplier;
            }
            return radius;
        }
        set => BaseRadius = value;
    }

    /// <summary>基本煙霧半徑（公尺），Inspector 設定的值。</summary>
    public float BaseRadius
    {
        get => _radius;
        set
        {
            _radius = Mathf.Max(0f, value);
            NotifyIfRadiusChanged();
        }
    }

    /// <summary>這個來源自己的半徑倍率（Inspector 的值），預設 1。不會小於 0。其他機制請用 SetRadiusModifier，避免互相覆蓋。</summary>
    public float RadiusMultiplier
    {
        get => _radiusMultiplier;
        set
        {
            _radiusMultiplier = Mathf.Max(0f, value);
            NotifyIfRadiusChanged();
        }
    }

    /// <summary>是否正在冒煙。false 時不影響暴露值。</summary>
    public bool IsEmitting
    {
        get => _isEmitting;
        set => _isEmitting = value;
    }

    /// <summary>
    /// 是否受拒絕幫忙懲罰影響。菸怪是 true（同物件有 SmokeMonster 時 Awake 自動設定；B 的 SmokeMonster 也可以自己設），
    /// 開場朋友、吸菸 NPC、BOSS、煙霧區是 false。
    /// </summary>
    public bool AffectedByRefusal
    {
        get => _affectedByRefusal;
        set
        {
            _affectedByRefusal = value;
            NotifyIfRadiusChanged();
        }
    }

    /// <summary>現在是否無法消除（拒絕幫忙中且受影響）。B 的 SmokeMonster.TakeHit 看到 true 就不算這一下。</summary>
    public bool IsIndestructible => _affectedByRefusal && _refusalActive;

    /// <summary>
    /// 設定一個半徑倍率（用自己的 key，例如 "Wind"）。多個 key 的倍率相乘，不會互相覆蓋。
    /// multiplier 不會小於 0。key 是空字串時不做任何事。
    /// </summary>
    public void SetRadiusModifier(string key, float multiplier)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        _radiusModifiers[key] = Mathf.Max(0f, multiplier);
        NotifyIfRadiusChanged();
    }

    /// <summary>移除一個半徑倍率。key 不存在時不做任何事。</summary>
    public void RemoveRadiusModifier(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (_radiusModifiers.Remove(key))
        {
            NotifyIfRadiusChanged();
        }
    }

    /// <summary>
    /// 【只給 PhaseManager 使用】設定拒絕幫忙懲罰。active：是否懲罰中；multiplier：菸怪半徑倍率（最小 1）。
    /// 受影響的煙霧來源會各自觸發 OnRadiusChanged；開始／結束時觸發 OnRefusalStateChanged。
    /// </summary>
    public static void SetRefusalState(bool active, float multiplier)
    {
        bool changed = active != _refusalActive;
        _refusalActive = active;
        _refusalMultiplier = active ? Mathf.Max(1f, multiplier) : 1f;

        // 先通知半徑，再通知狀態（B 換外觀時讀到的半徑已經是新的）。
        // 複製一份清單，避免監聽的人在事件中停用物件、改到 _all。
        _notifyBuffer.Clear();
        _notifyBuffer.AddRange(_all);
        for (int i = 0; i < _notifyBuffer.Count; i++)
        {
            SmokeSource source = _notifyBuffer[i];
            if (source != null && source._affectedByRefusal)
            {
                source.NotifyIfRadiusChanged();
            }
        }
        _notifyBuffer.Clear();

        if (changed)
        {
            OnRefusalStateChanged?.Invoke(active);
        }
    }

    /// <summary>
    /// 【只給 PhaseManager 使用】場景載入、卸載時把拒絕狀態直接清回初始值（不觸發任何事件）。
    /// 因為 static 變數在重新載入場景後不會自己清掉。
    /// </summary>
    public static void ResetRefusalState()
    {
        _refusalActive = false;
        _refusalMultiplier = 1f;
    }

    /// <summary>
    /// 每次進入 Play 時清掉 static 的狀態（就算關閉 Domain Reload 也不會留下上一次的值）。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _all.Clear();
        _notifyBuffer.Clear();
        _refusalActive = false;
        _refusalMultiplier = 1f;
        OnRefusalStateChanged = null;
    }

    private void Awake()
    {
        // 菸怪自動分類：同一個物件上有 B 的 SmokeMonster 就受拒絕影響（B 漏設也不會壞）
        if (GetComponent<SmokeMonster>() != null)
        {
            _affectedByRefusal = true;
        }
    }

    private void OnEnable()
    {
        if (!_all.Contains(this))
        {
            _all.Add(this);
        }

        // 第一次啟用只記下目前半徑；停用期間半徑變了的話，重新啟用時補發一次事件
        if (_lastNotifiedRadius < 0f)
        {
            _lastNotifiedRadius = Radius;
        }
        else
        {
            NotifyIfRadiusChanged();
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

        // Play 中在 Inspector 改半徑時，也通知 B 縮放外觀
        if (Application.isPlaying && isActiveAndEnabled && _lastNotifiedRadius >= 0f)
        {
            NotifyIfRadiusChanged();
        }
    }

    /// <summary>實際半徑和上次通知的不一樣時，觸發 OnRadiusChanged。停用中不觸發（重新啟用時補發）。</summary>
    private void NotifyIfRadiusChanged()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        float radius = Radius;
        if (Mathf.Approximately(radius, _lastNotifiedRadius))
        {
            return;
        }

        _lastNotifiedRadius = radius;
        OnRadiusChanged?.Invoke(radius);
    }

    private void OnDrawGizmos()
    {
        // 平常就在 Scene 視窗畫出淡淡的煙霧範圍（實際半徑）；停止冒煙時顏色更淡；無法消除時偏紅
        Color color = IsIndestructible
            ? new Color(0.9f, 0.3f, 0.3f, 0.5f)
            : new Color(0.6f, 0.6f, 0.6f, 0.35f);
        if (!_isEmitting)
        {
            color.a = 0.12f;
        }
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }

    private void OnDrawGizmosSelected()
    {
        // 選取物件時畫得更明顯（橘色 = 實際半徑）；半徑被放大縮小時，另外用淡藍色畫出基本半徑
        float radius = Radius;
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radius);

        if (!Mathf.Approximately(radius, _radius))
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// 玩家的二手菸暴露值（0–100），依與最近的 SmokeSource 距離計算。
/// （A2 空殼：數值固定為 0，計算在 A6 實作）
/// </summary>
public class ExposureSystem : MonoBehaviour
{
    private float _exposure;
    private float _smokeSeconds;

    /// <summary>實際暴露值 0–100。</summary>
    public float Exposure => _exposure;

    /// <summary>畫面效果用的暴露值（B 讀這個）；戒菸噴霧生效時為 0，其餘等於 Exposure。</summary>
    public float VisualExposure => _exposure;

    /// <summary>暴露值 ≥ 80 時為 true。</summary>
    public bool IsWarning => _exposure >= 80f;

    /// <summary>暴露值 ≥ 100 時為 true。</summary>
    public bool IsMaxed => _exposure >= 100f;

    /// <summary>累計待在煙霧範圍內的秒數。</summary>
    public float SmokeSeconds => _smokeSeconds;

    /// <summary>暴露值改變時觸發。參數：目前暴露值。</summary>
    public event Action<float> OnExposureChanged;

    /// <summary>進入／離開警示（≥ 80）時觸發。true 顯示警示、false 隱藏。</summary>
    public event Action<bool> OnWarningChanged;

    /// <summary>進入／離開 100 時觸發（咳嗽音效、減速）。</summary>
    public event Action<bool> OnMaxedChanged;
}

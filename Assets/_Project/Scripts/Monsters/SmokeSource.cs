using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 煙霧來源：自動註冊到全域清單，供 A 組員的 ExposureSystem 計算與玩家的距離
/// </summary>
public class SmokeSource : MonoBehaviour
{
    // 全域清單，方便 ExposureSystem 隨時讀取
    public static readonly List<SmokeSource> All = new List<SmokeSource>();

    [Header("煙霧範圍")]
    [Tooltip("煙霧影響半徑 (單位: 公尺，企劃預設 50)")]
    public float radius = 50f;

    [Tooltip("是否正在散發煙霧")]
    public bool isActive = true;

    private void OnEnable()
    {
        if (!All.Contains(this))
        {
            All.Add(this);
        }
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void OnDestroy()
    {
        All.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
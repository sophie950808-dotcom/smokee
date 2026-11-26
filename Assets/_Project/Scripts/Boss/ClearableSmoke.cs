using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 場上所有「可以被口嚼錠清除」的 BOSS 煙霧登記表（static class）。
/// 煙霧區（A16）出生時 Register、消失時 Unregister；PlayerEffects 的口嚼錠呼叫 ClearInRadius。
/// （A14 決定）菸圈「不」登記，口嚼錠不能清除菸圈。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// </summary>
public static class ClearableSmoke
{
    private static readonly List<IClearableSmoke> _all = new List<IClearableSmoke>();
    private static readonly List<IClearableSmoke> _toClear = new List<IClearableSmoke>();

    /// <summary>目前場上登記的煙霧。</summary>
    public static IReadOnlyList<IClearableSmoke> All => _all;

    /// <summary>口嚼錠清除時觸發（就算範圍內一個都沒有也會觸發）。參數：中心、半徑。B 播清除特效。</summary>
    public static event Action<Vector3, float> OnCleared;

    /// <summary>登記一個可清除的煙霧（重複登記會被忽略）。</summary>
    public static void Register(IClearableSmoke smoke)
    {
        if (smoke != null && !_all.Contains(smoke))
        {
            _all.Add(smoke);
        }
    }

    /// <summary>取消登記（煙霧自然消失、被清除、被刪除時呼叫）。</summary>
    public static void Unregister(IClearableSmoke smoke)
    {
        _all.Remove(smoke);
    }

    /// <summary>
    /// 【只給 PlayerEffects（口嚼錠）與 BossAttack】清除 center 周圍 radius 公尺內（3D 距離）的所有煙霧，回傳清掉幾個。
    /// </summary>
    public static int ClearInRadius(Vector3 center, float radius)
    {
        _toClear.Clear();
        float sqrRadius = radius * radius;
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            IClearableSmoke smoke = _all[i];

            // 物件已經被刪掉但沒有取消登記 → 直接移除
            if (smoke == null || (smoke is UnityEngine.Object obj && obj == null))
            {
                _all.RemoveAt(i);
                continue;
            }

            if ((smoke.SmokePosition - center).sqrMagnitude <= sqrRadius)
            {
                _toClear.Add(smoke);
            }
        }

        // 先收集再清除：ClearSmoke 裡會 Unregister，不能邊走訪邊改清單
        foreach (IClearableSmoke smoke in _toClear)
        {
            _all.Remove(smoke);
            smoke.ClearSmoke();
        }

        int count = _toClear.Count;
        _toClear.Clear();
        OnCleared?.Invoke(center, radius);
        return count;
    }

    /// <summary>每次進入 Play 時清空（就算關閉 Domain Reload 也不會留下舊的）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _all.Clear();
        _toClear.Clear();
        OnCleared = null;
    }
}

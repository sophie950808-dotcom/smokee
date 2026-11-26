using UnityEngine;

/// <summary>
/// 可以被尼古丁口嚼錠清除的 BOSS 煙霧（A16 的煙霧區、DevTools 的假菸圈）。A14 決定菸圈「不」實作。
/// 出生時呼叫 ClearableSmoke.Register(this)，消失時呼叫 ClearableSmoke.Unregister(this)。
/// 飛行中的菸圈不要實作（DECISIONS：口嚼錠只清煙霧區）。
/// </summary>
public interface IClearableSmoke
{
    /// <summary>目前位置（用來判斷在不在清除範圍內）。</summary>
    Vector3 SmokePosition { get; }

    /// <summary>被口嚼錠清除：自己消失（Destroy 或停用），並呼叫 ClearableSmoke.Unregister。</summary>
    void ClearSmoke();
}

using UnityEngine;

/// <summary>
/// 測試用假菸圈（只在 TestScene_A 用）：實作 IClearableSmoke，啟用時登記到 ClearableSmoke。
/// 被口嚼錠清除時在 Console 印出訊息並消失。不會移動、不會傷害玩家；真正的菸圈是 A14 的工作。
/// 用來測試口嚼錠的清除半徑：一個放在玩家 15 公尺內、一個放在外面。
/// </summary>
public class FakeSmokeRing : MonoBehaviour, IClearableSmoke
{
    /// <summary>目前位置（判斷在不在清除範圍內）。</summary>
    public Vector3 SmokePosition => transform.position;

    private void OnEnable()
    {
        ClearableSmoke.Register(this);
    }

    private void OnDisable()
    {
        ClearableSmoke.Unregister(this);
    }

    /// <summary>被口嚼錠清除：印出訊息並刪除自己。</summary>
    public void ClearSmoke()
    {
        Debug.Log($"[FakeSmokeRing] {name} 被口嚼錠清除了（位置 {transform.position}）", this);
        Destroy(gameObject);
    }
}

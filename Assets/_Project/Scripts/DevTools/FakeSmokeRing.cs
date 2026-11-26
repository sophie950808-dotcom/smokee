using UnityEngine;

/// <summary>
/// 測試用假煙霧（只在 TestScene_A 用；名稱沿用 A13 的 FakeSmokeRing）：實作 IClearableSmoke，啟用時登記到 ClearableSmoke。
/// 模擬 A16 的煙霧區（A14 起口嚼錠只清煙霧區、不清菸圈）。被口嚼錠清除時在 Console 印出訊息並消失。不會移動、不會傷害玩家。
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

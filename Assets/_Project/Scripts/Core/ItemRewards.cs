using System;
using UnityEngine;

/// <summary>
/// 給玩家道具的統一入口，讓 A、B 的程式不用直接依賴 C 的背包。
/// B 的故事事件（貼片、口嚼錠）、A 的寶箱（噴霧）呼叫 Grant；C 的 Inventory 監聽 OnItemGranted 加進背包。
/// 規格見 docs/INTERFACES.md 第 12 節。
/// </summary>
public static class ItemRewards
{
    /// <summary>給玩家一個道具時觸發。參數：道具種類。C 的 Inventory 監聽。</summary>
    public static event Action<ItemType> OnItemGranted;

    /// <summary>給玩家一個道具（B 的故事事件、A 的寶箱呼叫）。</summary>
    public static void Grant(ItemType type)
    {
        if (OnItemGranted == null)
        {
            Debug.LogWarning("[ItemRewards] 沒有背包在監聽 OnItemGranted，道具 " + type + " 沒有加進背包。");
            return;
        }

        OnItemGranted.Invoke(type);
    }

    /// <summary>每次進入 Play 時清掉監聽者（就算關閉 Domain Reload 也不會留下舊的）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        OnItemGranted = null;
    }
}

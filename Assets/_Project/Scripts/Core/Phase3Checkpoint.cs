using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包裡一種道具的數量（第三階段快照用）。
/// </summary>
[Serializable]
public struct ItemStack
{
    /// <summary>道具種類。</summary>
    public ItemType Type;
    /// <summary>數量。</summary>
    public int Count;

    /// <summary>建立一筆道具數量。</summary>
    public ItemStack(ItemType type, int count)
    {
        Type = type;
        Count = count;
    }
}

/// <summary>
/// 第三階段快照：「第一次」進入第三階段時玩家的狀態。之後每次重新挑戰都還原成這份。
/// </summary>
[Serializable]
public class Phase3Snapshot
{
    /// <summary>第一次進入第三階段時的 HP。</summary>
    public float Hp = 100f;

    /// <summary>背包內容（道具種類與數量）。</summary>
    public List<ItemStack> Items = new List<ItemStack>();

    /// <summary>
    /// 道具有沒有真的被記下來。false = C 的 Inventory 還沒提供讀取方法，Items 是空的（還原時不要動背包）。
    /// </summary>
    public bool ItemsCaptured;

    /// <summary>第一次進入第三階段時的用時（秒，參考用；重新挑戰時用時是接續 CarriedElapsedTime）。</summary>
    public float ElapsedAtEntry;

    /// <summary>是不是開發者用的預設快照（沒有真正玩過就跳到第三階段）。</summary>
    public bool IsDefault;

    /// <summary>複製一份（避免外面改到存著的快照）。</summary>
    public Phase3Snapshot Clone()
    {
        return new Phase3Snapshot
        {
            Hp = Hp,
            Items = new List<ItemStack>(Items),
            ItemsCaptured = ItemsCaptured,
            ElapsedAtEntry = ElapsedAtEntry,
            IsDefault = IsDefault,
        };
    }

    /// <summary>一行文字說明（DevOverlay、Log 用）。</summary>
    public override string ToString()
    {
        string items;
        if (!ItemsCaptured)
        {
            items = "（道具未記錄：等 C 的 Inventory 提供方法）";
        }
        else if (Items.Count == 0)
        {
            items = "（沒有道具）";
        }
        else
        {
            List<string> parts = new List<string>();
            foreach (ItemStack stack in Items)
            {
                parts.Add(stack.Type + "×" + stack.Count);
            }
            items = string.Join("、", parts);
        }

        return "HP " + Hp.ToString("F0") + "｜道具 " + items + "｜進入時用時 " + ElapsedAtEntry.ToString("F1") + " 秒"
               + (IsDefault ? "｜預設快照" : "");
    }
}

/// <summary>
/// 第三階段起點：保存快照，讓「從第三階段重新挑戰」（A19）和「開發者跳到第三階段」（F4）共用同一個起點流程。
/// 這是 static 類別，重新載入場景後資料還在；回到主選單、重新開始一整局、每次按 Play 時清除。
/// 規則（GDD 10.3、DECISIONS）：
/// - HP、道具：還原成「第一次」進入第三階段時的快照（之後再重試也一樣用第一次的）。
/// - 用時：持續累計，從失敗那一刻接著算。
/// - 淨化進度從 60% 開始；新手教學與三個事件都視為已完成。
/// - 沒有真正玩過就跳到第三階段：使用預設快照（HP 100，貼片、口嚼錠、噴霧各 1）。
/// 規格見 docs/INTERFACES.md「第三階段起點」。
/// 【A11】快照的存取已完成；還原與重新載入在 A19 完成。
/// </summary>
public static class Phase3Checkpoint
{
    private static Phase3Snapshot _snapshot;
    private static bool _isRetryPending;
    private static float _carriedElapsedTime;
    private static int _retryCount;

    /// <summary>是否已經有快照（第一次進入第三階段後就有）。</summary>
    public static bool HasSnapshot => _snapshot != null;

    /// <summary>第一次進入第三階段時的快照（回傳複製品）；沒有時 null。</summary>
    public static Phase3Snapshot Snapshot => _snapshot != null ? _snapshot.Clone() : null;

    /// <summary>
    /// 下一次載入 GameScene 時，是否要直接從第三階段起點開始（跳過開場、教學、第一、二階段）。
    /// 【A19】B 的開場演出、C 的開場對話框看到 true 就不要播放／顯示。
    /// </summary>
    public static bool IsRetryPending => _isRetryPending;

    /// <summary>重新挑戰時要接續的用時（秒）。</summary>
    public static float CarriedElapsedTime => _carriedElapsedTime;

    /// <summary>已經從第三階段重新挑戰了幾次。</summary>
    public static int RetryCount => _retryCount;

    /// <summary>
    /// 【只給 PhaseManager】第一次進入第三階段時存下快照。已經有快照時不覆蓋，回傳 false。
    /// </summary>
    public static bool SaveFirstEntry(Phase3Snapshot snapshot)
    {
        if (snapshot == null)
        {
            return false;
        }
        if (_snapshot != null)
        {
            Debug.Log("[Phase3Checkpoint] 已經有第一次進入第三階段的快照，不覆蓋。目前快照：" + _snapshot);
            return false;
        }

        _snapshot = snapshot.Clone();
        Debug.Log("[Phase3Checkpoint] 存下第三階段快照：" + _snapshot);
        return true;
    }

    /// <summary>預設快照：HP 100，尼古丁貼片 1、尼古丁口嚼錠 1、尼古丁戒菸噴霧 1（開發者沒有真正玩過就跳到第三階段時用）。</summary>
    public static Phase3Snapshot CreateDefault()
    {
        return new Phase3Snapshot
        {
            Hp = 100f,
            Items = new List<ItemStack>
            {
                new ItemStack(ItemType.NicotinePatch, 1),
                new ItemStack(ItemType.NicotineGum, 1),
                new ItemStack(ItemType.NicotineSpray, 1),
            },
            ItemsCaptured = true,
            ElapsedAtEntry = 0f,
            IsDefault = true,
        };
    }

    /// <summary>
    /// 【只給 GameManager（A19）】要求下次載入場景時從第三階段起點開始，並記下要接續的用時。
    /// 【A19 完成】目前只印 Log。
    /// </summary>
    public static void RequestRetry(float elapsedAtFailure)
    {
        Debug.Log("[Phase3Checkpoint] RequestRetry(" + elapsedAtFailure.ToString("F1") + ") 尚未實作（A19）。");
    }

    /// <summary>
    /// 【只給 PhaseManager（A19）】場景載入後套用起點時呼叫：取消「待重新挑戰」標記。
    /// 【A19 完成】目前只印 Log。
    /// </summary>
    public static void ConsumePendingRetry()
    {
        Debug.Log("[Phase3Checkpoint] ConsumePendingRetry 尚未實作（A19）。");
    }

    /// <summary>清除快照與重新挑戰的資料（回到主選單、重新開始一整局時由 GameManager 呼叫）。</summary>
    public static void Clear()
    {
        _snapshot = null;
        _isRetryPending = false;
        _carriedElapsedTime = 0f;
        _retryCount = 0;
    }

    /// <summary>每次按 Play 時清空（就算專案關閉 Domain Reload 也不會留下上一次的快照）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Clear();
    }
}

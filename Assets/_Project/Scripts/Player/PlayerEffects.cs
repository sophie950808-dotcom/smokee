using System;
using UnityEngine;

/// <summary>
/// 3 種道具的效果（尼古丁貼片、尼古丁口嚼錠、尼古丁戒菸噴霧）。掛在 Player 身上。
/// C 的背包按右鍵時呼叫 TryUseItem。規格見 docs/INTERFACES.md 第 12 節。
/// 【A10 空殼】名稱與參數已定案；實際效果在 A13 完成（現在只會判斷能不能用並印出 Log）。
/// </summary>
public class PlayerEffects : MonoBehaviour
{
    [Header("尼古丁貼片")]
    [Tooltip("回復多少 HP（不超過上限）")]
    [SerializeField] private float _patchHealAmount = 30f;

    [Header("尼古丁口嚼錠")]
    [Tooltip("清除玩家周圍多少公尺內的 BOSS 菸圈與菸炮煙霧區")]
    [SerializeField] private float _gumClearRadius = 15f;

    [Header("尼古丁戒菸噴霧")]
    [Tooltip("每次對 BOSS 造成的傷害")]
    [SerializeField] private float _sprayDamage = 7.5f;

    [Tooltip("兩次噴霧之間的冷卻秒數")]
    [SerializeField] private float _sprayCooldown = 1.5f;

    [Tooltip("噴霧最遠距離（公尺）【暫定】")]
    [SerializeField] private float _sprayRange = 30f;

    [Header("提示文字")]
    [Tooltip("第三階段以前使用貼片／口嚼錠時顯示")]
    [SerializeField] private string _notYetMessage = "第三階段才能使用";

    private float _nextSprayTime;

    /// <summary>噴霧冷卻總秒數（C 的背包顯示冷卻用）。</summary>
    public float SprayCooldown => _sprayCooldown;

    /// <summary>噴霧冷卻還剩幾秒；0 = 可以使用。</summary>
    public float SprayCooldownRemaining => Mathf.Max(0f, _nextSprayTime - Time.time);

    /// <summary>道具成功使用時觸發。參數：道具種類（B 特效、音效）。</summary>
    public event Action<ItemType> OnItemUsed;

#pragma warning disable 67 // 事件還沒有被觸發（A13 才會用到／刻意保留舊名稱）
    /// <summary>噴霧射出時觸發。參數：起點、終點、有沒有打到 BOSS → B 播噴霧特效。</summary>
    public event Action<Vector3, Vector3, bool> OnSprayFired;

    /// <summary>【即將移除】舊道具的效果開始事件。v3.1 道具沒有持續時間，不會觸發。</summary>
    [Obsolete("v3.1 道具沒有持續時間，不會觸發。A13 刪除。")]
    public event Action<ItemType, float> OnEffectStarted;

    /// <summary>【即將移除】舊道具的效果結束事件。v3.1 道具沒有持續時間，不會觸發。</summary>
    [Obsolete("v3.1 道具沒有持續時間，不會觸發。A13 刪除。")]
    public event Action<ItemType> OnEffectEnded;
#pragma warning restore 67

    /// <summary>
    /// 使用道具（C 的背包按右鍵時呼叫）。只有回傳 Consumed 時背包要減 1，其他結果都不要扣。
    /// 第三階段以前使用貼片、口嚼錠會自動顯示「第三階段才能使用」並回傳 NotYet。
    /// </summary>
    public ItemUseResult TryUseItem(ItemType type)
    {
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null && !controller.IsActionAllowed(PlayerAction.UseItem))
        {
            return ItemUseResult.Locked;
        }

        bool isPhase3 = PhaseManager.Instance != null && PhaseManager.Instance.CurrentPhase == GamePhase.Phase3;

        switch (type)
        {
            case ItemType.NicotinePatch:
            case ItemType.NicotineGum:
                if (!isPhase3)
                {
                    TaskHint.ShowToast(_notYetMessage);
                    return ItemUseResult.NotYet;
                }
                // TODO A13：貼片 Heal(_patchHealAmount)；口嚼錠 BossAttack.ClearSmokeInRadius(位置, _gumClearRadius)
                Debug.Log("[PlayerEffects] （A10 空殼）使用 " + type + "，效果在 A13 完成。", this);
                OnItemUsed?.Invoke(type);
                return ItemUseResult.Consumed;

            case ItemType.NicotineSpray:
                if (SprayCooldownRemaining > 0f)
                {
                    return ItemUseResult.OnCooldown;
                }
                _nextSprayTime = Time.time + _sprayCooldown;
                // TODO A13：從鏡頭中央射出 _sprayRange 公尺，打到 BossHealth 才造成 _sprayDamage，並觸發 OnSprayFired
                Debug.Log("[PlayerEffects] （A10 空殼）使用噴霧，效果在 A13 完成。", this);
                OnItemUsed?.Invoke(type);
                return ItemUseResult.UsedNotConsumed;

            default:
                Debug.LogWarning("[PlayerEffects] " + type + " 是 v3.1 已刪除的道具，不能使用。", this);
                return ItemUseResult.Failed;
        }
    }

    /// <summary>【即將移除】舊的使用道具方法，現在等於呼叫 TryUseItem（不回傳結果）。</summary>
    [Obsolete("請改用 TryUseItem(ItemType)，並依回傳值決定背包要不要減 1。A13 刪除。")]
    public void ApplyItem(ItemType type)
    {
        TryUseItem(type);
    }

    // 下面的欄位 A13 才會用到；先讀一次，避免「欄位沒有使用」的警告
    private void OnValidate()
    {
        _patchHealAmount = Mathf.Max(0f, _patchHealAmount);
        _gumClearRadius = Mathf.Max(0f, _gumClearRadius);
        _sprayDamage = Mathf.Max(0f, _sprayDamage);
        _sprayCooldown = Mathf.Max(0f, _sprayCooldown);
        _sprayRange = Mathf.Max(0f, _sprayRange);
    }
}

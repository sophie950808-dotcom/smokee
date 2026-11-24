/// <summary>
/// 【可選】會開始 QTE 連打的互動目標（A16 的 SmokerNPC、DevTools 的假吸菸 NPC）。
/// 流程：玩家按 E → PlayerInteractor 呼叫 Interact()（目標自己開始 QTE，第一下自己算）→
/// 如果 IsQteActive 變成 true，PlayerInteractor 進入 QTE 模式：鎖定這個目標、鎖住移動，
/// 之後每按一次 E 就呼叫 RegisterQtePress()；IsQteActive 變回 false（成功、失敗、離開範圍）
/// 或物件被停用、刪除時，恢復正常找目標。
/// QTE 的計時與成功／失敗判定由目標自己負責，PlayerInteractor 只轉交 E 鍵。
/// </summary>
public interface IQteInteractable : IInteractable
{
    /// <summary>QTE 是否正在進行中。</summary>
    bool IsQteActive { get; }

    /// <summary>【只給 PlayerInteractor】QTE 期間每按一次 E 呼叫（不包含開始 QTE 的第一下）。</summary>
    void RegisterQtePress();
}

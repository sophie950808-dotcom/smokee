using UnityEngine;

/// <summary>
/// 可以按 E 互動的物件（A 的 TreasureChest、A 的 SmokerNPC、B 的故事 NPC）。
/// 只有 A 的 PlayerInteractor 會讀 E 鍵並呼叫 Interact()。
/// </summary>
public interface IInteractable
{
    /// <summary>HUD 顯示的提示，例如「按 E 開啟寶箱」。</summary>
    string InteractPrompt { get; }

    /// <summary>目前是否可以互動；已開過的寶箱回傳 false。</summary>
    bool CanInteract { get; }

    /// <summary>用來計算與玩家距離的位置。</summary>
    Transform InteractPoint { get; }

    /// <summary>玩家按 E 時呼叫。</summary>
    void Interact();
}

using UnityEngine;

/// <summary>
/// 【可選】互動目標自己的額外條件。IInteractable 同時實作這個介面時，
/// PlayerInteractor 除了「範圍內、CanInteract、沒被牆擋住」之外，還會再問一次 CanInteractFrom。
/// 例如 A 的 SmokerNPC：玩家要面向它（前方 90 度）、2 公尺內才算。
/// 沒實作的物件（B 的故事 NPC、寶箱）照原本的規則，不用改。
/// </summary>
public interface IInteractCondition
{
    /// <summary>
    /// 玩家現在的位置與面向能不能和這個物件互動。
    /// 參數：玩家根物件（position = 腳底，forward = 角色面向的水平方向）。
    /// </summary>
    bool CanInteractFrom(Transform player);
}

/// <summary>
/// 可以被玩家近戰攻擊命中的物件（B 的 SmokeMonster、A 的 BossHealth）。
/// A 的 PlayerAttack 只認這個介面。
/// </summary>
public interface IHittable
{
    /// <summary>
    /// 被近戰命中。hitCount 固定是 1（吸入劑已刪除）。
    /// </summary>
    void TakeHit(int hitCount);
}

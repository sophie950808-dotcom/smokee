/// <summary>
/// 可以被玩家近戰攻擊命中的物件（例如 B 的 SmokeMonster）。
/// A 的 PlayerAttack 只認這個介面。
/// </summary>
public interface IHittable
{
    /// <summary>
    /// 被攻擊命中。hitCount 通常是 1；吸入劑效果時是 3。
    /// </summary>
    void TakeHit(int hitCount);
}

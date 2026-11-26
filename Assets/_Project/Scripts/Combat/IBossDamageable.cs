/// <summary>
/// 可以被噴霧（以及開發者工具）造成傷害的 BOSS。
/// 實作者：A14 的 BossHealth（A14 起 DevTools 的 FakeBoss 已移除）。小菸怪「不」實作，所以噴霧對小菸怪無效。
/// PlayerEffects 的噴霧只認這個介面，不直接依賴 BossHealth。
/// 實作的物件（或子物件）一定要有 Collider（可以是 Trigger）。
/// </summary>
public interface IBossDamageable
{
    /// <summary>受到傷害。參數：傷害量、來源（噴霧是 DamageSource.Spray）。</summary>
    void TakeDamage(float amount, DamageSource source);
}

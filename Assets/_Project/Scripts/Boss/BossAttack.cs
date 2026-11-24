using System;
using UnityEngine;

/// <summary>
/// BOSS 的攻擊：地面吐小菸圈、天上吐菸炮（落地留下煙霧區）、Final 物理攻擊。
/// 口嚼錠清除菸圈與煙霧區改由 ClearableSmoke（A13）負責。
/// 掛在 BOSS 根物件。菸圈、菸炮、煙霧區的小腳本與 Prefab 在 A14／A16 建立；B 可以換 Prefab 的外觀。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// 【A10 空殼】名稱、參數、Inspector 數值已定案；菸圈在 A14、菸炮與物理攻擊在 A16 完成。
/// </summary>
public class BossAttack : MonoBehaviour
{
    [Header("小菸圈（BOSS 在地面時）")]
    [Tooltip("每隔幾秒吐一個")]
    [SerializeField] private float _smokeRingInterval = 2f;
    [Tooltip("命中玩家扣多少 HP")]
    [SerializeField] private float _smokeRingDamage = 8f;
    [Tooltip("飛行速度（公尺／秒）【暫定】")]
    [SerializeField] private float _smokeRingSpeed = 6f;
    [Tooltip("存在幾秒後消失【暫定】")]
    [SerializeField] private float _smokeRingLifetime = 4f;
    [Tooltip("命中判定半徑（公尺）【暫定】")]
    [SerializeField] private float _smokeRingHitRadius = 0.8f;

    [Header("菸炮（BOSS 在天上時）")]
    [Tooltip("每隔幾秒發射一發")]
    [SerializeField] private float _cannonInterval = 4f;
    [Tooltip("直接命中扣多少 HP")]
    [SerializeField] private float _cannonDamage = 15f;
    [Tooltip("發射後幾秒落地（落點會先顯示預警）【暫定】")]
    [SerializeField] private float _cannonFlightTime = 1.5f;
    [Tooltip("落地時的命中判定半徑（公尺）【暫定】")]
    [SerializeField] private float _cannonHitRadius = 1.5f;
    [Tooltip("落地後的煙霧區存在幾秒")]
    [SerializeField] private float _smokeZoneDuration = 3f;
    [Tooltip("煙霧區半徑（公尺）【暫定】")]
    [SerializeField] private float _smokeZoneRadius = 3f;

    [Header("物理攻擊（菸全部捏熄後）")]
    [Tooltip("玩家在幾公尺內會被攻擊")]
    [SerializeField] private float _meleeRange = 3f;
    [Tooltip("每隔幾秒揮擊一次")]
    [SerializeField] private float _meleeInterval = 3f;
    [Tooltip("命中扣多少 HP")]
    [SerializeField] private float _meleeDamage = 10f;
    [Tooltip("揮下前的預備時間（秒），讓玩家有機會閃開【暫定】")]
    [SerializeField] private float _meleeWindup = 0.6f;

#pragma warning disable 67 // A14、A16 才會觸發
    /// <summary>吐出小菸圈時觸發。參數：起點、方向。B 播特效。</summary>
    public event Action<Vector3, Vector3> OnSmokeRingFired;

    /// <summary>發射菸炮時觸發。參數：落點、幾秒後落地。B 在落點顯示預警。</summary>
    public event Action<Vector3, float> OnCannonFired;

    /// <summary>菸炮落地時觸發。參數：落點（之後留下煙霧區）。</summary>
    public event Action<Vector3> OnCannonLanded;

    /// <summary>物理攻擊的預備動作開始時觸發。</summary>
    public event Action OnMeleeWindup;

    /// <summary>物理攻擊揮下時觸發。參數：有沒有打中玩家。</summary>
    public event Action<bool> OnMeleeSwing;
#pragma warning restore 67

    /// <summary>
    /// 清掉範圍內的菸圈與菸炮煙霧區，回傳清掉幾個。A13 起等於呼叫 ClearableSmoke.ClearInRadius
    /// （PlayerEffects 的口嚼錠直接呼叫 ClearableSmoke；清除特效請聽 ClearableSmoke.OnCleared）。
    /// A14、A16 的菸圈與煙霧區要實作 IClearableSmoke 並登記到 ClearableSmoke。
    /// </summary>
    public int ClearSmokeInRadius(Vector3 center, float radius)
    {
        return ClearableSmoke.ClearInRadius(center, radius);
    }

    private void OnValidate()
    {
        _smokeRingInterval = Mathf.Max(0.1f, _smokeRingInterval);
        _smokeRingDamage = Mathf.Max(0f, _smokeRingDamage);
        _smokeRingSpeed = Mathf.Max(0f, _smokeRingSpeed);
        _smokeRingLifetime = Mathf.Max(0.1f, _smokeRingLifetime);
        _smokeRingHitRadius = Mathf.Max(0f, _smokeRingHitRadius);
        _cannonInterval = Mathf.Max(0.1f, _cannonInterval);
        _cannonDamage = Mathf.Max(0f, _cannonDamage);
        _cannonFlightTime = Mathf.Max(0.1f, _cannonFlightTime);
        _cannonHitRadius = Mathf.Max(0f, _cannonHitRadius);
        _smokeZoneDuration = Mathf.Max(0f, _smokeZoneDuration);
        _smokeZoneRadius = Mathf.Max(0f, _smokeZoneRadius);
        _meleeRange = Mathf.Max(0f, _meleeRange);
        _meleeInterval = Mathf.Max(0.1f, _meleeInterval);
        _meleeDamage = Mathf.Max(0f, _meleeDamage);
        _meleeWindup = Mathf.Max(0f, _meleeWindup);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BOSS 的攻擊：吐小菸圈（地面；A16 起天上也吐同一種菸圈）、Final 物理攻擊（A16）。
/// （A14 決定）菸炮取消，改成和菸圈同一種東西：天上發射的菸圈落到地面時留下 3 秒煙霧區（A16）。
/// 口嚼錠清除菸圈與煙霧區由 ClearableSmoke（A13）負責。
/// 掛在 BOSS 根物件，讀同一個物件上 BossController 的狀態決定要不要攻擊；遊戲暫停、「暫停計時」中不攻擊。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// 【A14 完成】地面菸圈；【A16】天上菸圈與煙霧區、物理攻擊。
/// </summary>
public class BossAttack : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("菸圈 Prefab（Prefabs/Boss/SmokeRing.prefab）")]
    [SerializeField] private BossSmokeRing _smokeRingPrefab;

    [Tooltip("菸圈的發射點。留空用 BOSS 根物件往上 2 公尺")]
    [SerializeField] private Transform _ringMuzzle;

    [Tooltip("瞄準玩家腳底往上幾公尺（胸口）")]
    [SerializeField] private float _aimHeight = 1f;

    [Tooltip("哪些 Layer 會擋住菸圈（牆、地面）。預設全部；Trigger 碰撞體不會擋")]
    [SerializeField] private LayerMask _ringObstacleMask = ~0;

    [Header("小菸圈（地面；A16 起天上也用同一組）")]
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

    [Header("菸炮（A14 決定取消、改成菸圈；A16 整理這些欄位，留下煙霧區時間與半徑）")]
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

    private const float DefaultMuzzleHeight = 2f;

    private BossController _controller;
    private PlayerHealth _player;
    private BossState _lastState = BossState.Inactive;
    private float _ringCooldown;
    private bool _warnedMissingPrefab;
    private readonly List<BossSmokeRing> _activeRings = new List<BossSmokeRing>();

    /// <summary>場上還在飛的菸圈數量（開發者工具用）。</summary>
    public int ActiveRingCount => _activeRings.Count;

    /// <summary>距離下一個菸圈還有幾秒（開發者工具用；不在吐菸圈的狀態時是下次開始吐時的完整間隔）。</summary>
    public float SmokeRingCooldownRemaining => Mathf.Max(0f, _ringCooldown);

    /// <summary>吐出小菸圈時觸發。參數：起點、方向（已正規化）。B 播特效、音效。</summary>
    public event Action<Vector3, Vector3> OnSmokeRingFired;

#pragma warning disable 67 // A16 才會觸發（菸炮兩個事件 A16 會改成煙霧區事件）

    /// <summary>發射菸炮時觸發。參數：落點、幾秒後落地。B 在落點顯示預警。</summary>
    public event Action<Vector3, float> OnCannonFired;

    /// <summary>菸炮落地時觸發。參數：落點（之後留下煙霧區）。</summary>
    public event Action<Vector3> OnCannonLanded;

    /// <summary>物理攻擊的預備動作開始時觸發。</summary>
    public event Action OnMeleeWindup;

    /// <summary>物理攻擊揮下時觸發。參數：有沒有打中玩家。</summary>
    public event Action<bool> OnMeleeSwing;
#pragma warning restore 67

    private void Awake()
    {
        _controller = GetComponent<BossController>();
        if (_controller == null)
        {
            Debug.LogWarning("[BossAttack] 同一個物件上找不到 BossController，BOSS 不會攻擊。", this);
        }
        _ringCooldown = _smokeRingInterval;
    }

    private void Update()
    {
        if (_controller == null)
        {
            return;
        }

        BossState state = _controller.State;
        if (state != _lastState)
        {
            // 剛進入會吐菸圈的狀態：重新計時（第一個菸圈在一個間隔之後）
            _lastState = state;
            _ringCooldown = _smokeRingInterval;
        }

        if (!CanAct())
        {
            return;
        }

        // A14 只有地面吐菸圈。TODO【A16】天上也吐（leaveZoneOnGround = true）；Final 改物理攻擊
        if (state == BossState.Ground)
        {
            _ringCooldown -= Time.deltaTime;
            if (_ringCooldown <= 0f)
            {
                _ringCooldown += _smokeRingInterval;
                FireSmokeRing(false);
            }
        }
    }

    /// <summary>
    /// 【只給 BossController】BOSS 被打倒時：移除場上所有菸圈（不觸發口嚼錠的清除特效）。之後狀態不是 Ground 就不會再吐。
    /// </summary>
    public void StopAndClearAll()
    {
        // 先複製一份：Despawn 會透過 OnDestroy 把自己從清單移除
        BossSmokeRing[] rings = _activeRings.ToArray();
        _activeRings.Clear();
        foreach (BossSmokeRing ring in rings)
        {
            if (ring != null)
            {
                ring.Despawn();
            }
        }
        _ringCooldown = _smokeRingInterval;
    }

    /// <summary>
    /// 清掉範圍內的菸圈與菸炮煙霧區，回傳清掉幾個。A13 起等於呼叫 ClearableSmoke.ClearInRadius
    /// （PlayerEffects 的口嚼錠直接呼叫 ClearableSmoke；清除特效請聽 ClearableSmoke.OnCleared）。
    /// A16 的煙霧區要實作 IClearableSmoke 並登記到 ClearableSmoke（A14 決定菸圈不登記，口嚼錠不清菸圈）。
    /// </summary>
    public int ClearSmokeInRadius(Vector3 center, float radius)
    {
        return ClearableSmoke.ClearInRadius(center, radius);
    }

    private void FireSmokeRing(bool leaveZoneOnGround)
    {
        if (_smokeRingPrefab == null)
        {
            if (!_warnedMissingPrefab)
            {
                _warnedMissingPrefab = true;
                Debug.LogWarning("[BossAttack] 沒有指定菸圈 Prefab，請執行選單 Tools/Smokee/A - A14 建立 BOSS Prefab。", this);
            }
            return;
        }

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerHealth>();
            if (_player == null)
            {
                return;
            }
        }

        Vector3 origin = _ringMuzzle != null ? _ringMuzzle.position : transform.position + Vector3.up * DefaultMuzzleHeight;
        Vector3 target = _player.transform.position + Vector3.up * _aimHeight;
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }
        direction.Normalize();

        BossSmokeRing ring = Instantiate(_smokeRingPrefab, origin, Quaternion.LookRotation(direction));
        _activeRings.Add(ring);
        ring.Launch(direction, _smokeRingSpeed, _smokeRingDamage, _smokeRingLifetime, _smokeRingHitRadius,
                    transform, _ringObstacleMask, leaveZoneOnGround, HandleRingEnded);

        OnSmokeRingFired?.Invoke(origin, direction);
    }

    private void HandleRingEnded(BossSmokeRing ring)
    {
        _activeRings.Remove(ring);
    }

    /// <summary>遊戲 Playing、而且沒有「暫停計時」時才攻擊；沒有 GameManager 時一律可以（方便單獨測試）。</summary>
    private static bool CanAct()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager == null || (gameManager.State == GameState.Playing && !gameManager.IsTimerPaused);
    }

    private void OnValidate()
    {
        _aimHeight = Mathf.Max(0f, _aimHeight);
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

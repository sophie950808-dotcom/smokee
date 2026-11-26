using System;
using UnityEngine;

/// <summary>
/// BOSS 吐出的小菸圈（地面、天上共用同一種，A14 決定合併菸炮）。由 BossAttack 生成並呼叫 Launch。
/// - 朝「發射當下」玩家胸口的方向直線飛行，不追蹤，玩家可以閃避。
/// - 碰到玩家：PlayerHealth.TakeDamage(傷害, DamageSource.SmokeRing) 後消失。
/// - 撞到牆、地面等實心物件（Trigger 不算、BOSS 自己不算）或超過存在時間：消失。
///   【A16】天上發射的菸圈落到地面時，留下 3 秒煙霧區（煙霧區是煙霧來源，可被口嚼錠清除）。
/// - （A14 決定）口嚼錠「不能」清除菸圈（只清天上菸圈落地留下的煙霧區）。菸圈本身「不是」煙霧來源（不增加暴露值）。
/// - 遊戲暫停、「暫停計時」中會停在空中。
/// Prefab：SmokeRing.prefab（A，放這支腳本，沒有碰撞體）裡面巢狀 SmokeRingVisual.prefab（B，外觀）。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// </summary>
public class BossSmokeRing : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private float _lifeRemaining;
    private float _hitRadius;
    private Transform _owner;
    private LayerMask _obstacleMask;
    private bool _leaveZoneOnGround;
    private bool _launched;
    private bool _ended;
    private Action<BossSmokeRing> _onEnded;

    private readonly Collider[] _overlapResults = new Collider[16];
    private readonly RaycastHit[] _rayHits = new RaycastHit[16];

    /// <summary>
    /// 【只給 BossAttack】發射。direction 會自動正規化；owner = BOSS 根物件（菸圈不會撞到 BOSS 自己）；
    /// onEnded = 菸圈消失時通知 BossAttack（用來算場上數量）。
    /// leaveZoneOnGround：落地時留下煙霧區（天上發射用，A16 實作；A14 一律 false）。
    /// </summary>
    public void Launch(Vector3 direction, float speed, float damage, float lifetime, float hitRadius,
                       Transform owner, LayerMask obstacleMask, bool leaveZoneOnGround, Action<BossSmokeRing> onEnded)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _speed = Mathf.Max(0f, speed);
        _damage = Mathf.Max(0f, damage);
        _lifeRemaining = Mathf.Max(0.01f, lifetime);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _owner = owner;
        _obstacleMask = obstacleMask;
        _leaveZoneOnGround = leaveZoneOnGround;
        _onEnded = onEnded;
        _launched = true;
        transform.rotation = Quaternion.LookRotation(_direction);
    }

    /// <summary>【只給 BossAttack】BOSS 被打倒等情況，直接移除（不觸發清除特效）。</summary>
    public void Despawn()
    {
        End(null);
    }

    private void OnDestroy()
    {
        // 不管是怎麼消失的（包含場景卸載），都通知 BossAttack 一次
        Action<BossSmokeRing> callback = _onEnded;
        _onEnded = null;
        callback?.Invoke(this);
    }

    private void Update()
    {
        if (!_launched || _ended || !CanMove())
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        _lifeRemaining -= deltaTime;
        if (_lifeRemaining <= 0f)
        {
            End(null);
            return;
        }

        Vector3 from = transform.position;
        float stepLength = _speed * deltaTime;

        // 先看這一步會不會撞到牆、地面
        if (stepLength > 0f && TryFindObstacle(from, stepLength, out RaycastHit obstacle))
        {
            transform.position = obstacle.point;
            if (_leaveZoneOnGround && obstacle.normal.y > 0.5f)
            {
                // TODO【A16】天上發射的菸圈落到地面：在 obstacle.point 生成 3 秒煙霧區（BossSmokeZone）
            }
            End(null);
            return;
        }

        transform.position = from + _direction * stepLength;

        PlayerHealth player = FindPlayerInRange(transform.position);
        if (player != null)
        {
            player.TakeDamage(_damage, DamageSource.SmokeRing);
            End("打中玩家（" + _damage + "）");
        }
    }

    /// <summary>沿著飛行方向找最先撞到的實心物件（玩家、BOSS 自己、Trigger 不算）。</summary>
    private bool TryFindObstacle(Vector3 from, float distance, out RaycastHit nearest)
    {
        nearest = default;
        int count = Physics.RaycastNonAlloc(from, _direction, _rayHits, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _rayHits[i];
            if (IsOwnerCollider(hit.collider) || hit.collider.GetComponentInParent<PlayerHealth>() != null)
            {
                continue;
            }
            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearest = hit;
                found = true;
            }
        }
        return found;
    }

    /// <summary>命中半徑內有沒有玩家（玩家的 CharacterController 是實心碰撞體）。</summary>
    private PlayerHealth FindPlayerInRange(Vector3 center)
    {
        int count = Physics.OverlapSphereNonAlloc(center, _hitRadius, _overlapResults, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider candidate = _overlapResults[i];
            if (IsOwnerCollider(candidate))
            {
                continue;
            }
            PlayerHealth player = candidate.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                return player;
            }
        }
        return null;
    }

    private bool IsOwnerCollider(Collider candidate)
    {
        return _owner != null && candidate.transform.IsChildOf(_owner);
    }

    /// <summary>遊戲 Playing、而且沒有「暫停計時」時才會動；沒有 GameManager 時一律會動。</summary>
    private static bool CanMove()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager == null || (gameManager.State == GameState.Playing && !gameManager.IsTimerPaused);
    }

    private void End(string reason)
    {
        if (_ended)
        {
            return;
        }
        _ended = true;
        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log("[BossSmokeRing] 菸圈" + reason + "。", this);
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _hitRadius);
    }
}

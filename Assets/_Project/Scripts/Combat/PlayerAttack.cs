using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家近戰攻擊：按滑鼠左鍵揮擊，打中角色前方扇形範圍內「最近、而且沒被牆擋住」的一個 IHittable。
/// 掛在 Player（和 PlayerController 同一個物件）上。
/// 判定只看水平角度，所以第一、第三人稱的結果一樣；抬頭低頭不影響。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [Header("攻擊範圍")]
    [Tooltip("攻擊距離（公尺），從胸口位置算起")]
    [SerializeField] private float _range = 2f;

    [Tooltip("攻擊角度（度），角色正前方左右各一半。例如 90 = 左右各 45 度")]
    [Range(1f, 360f)]
    [SerializeField] private float _angle = 90f;

    [Tooltip("攻擊判定的起點高度（公尺，從角色腳底算起），大約胸口位置")]
    [SerializeField] private float _originHeight = 1.2f;

    [Header("冷卻")]
    [Tooltip("兩次揮擊之間至少要隔幾秒")]
    [SerializeField] private float _cooldown = 0.5f;

    [Header("傷害")]
    [Tooltip("每次命中呼叫 TakeHit 時傳入的次數。預設 1；之後吸入劑道具會用程式改成 3")]
    [Min(1)]
    [SerializeField] private int _hitCount = 1;

    [Header("Layer")]
    [Tooltip("哪些 Layer 的物件可以被攻擊。預設全部")]
    [SerializeField] private LayerMask _targetMask = ~0;

    [Tooltip("哪些 Layer 會擋住攻擊（牆）。預設全部；Trigger 碰撞體不會擋")]
    [SerializeField] private LayerMask _obstacleMask = ~0;

    /// <summary>每次命中時傳給 TakeHit 的次數（最小 1）。吸入劑道具效果期間改成 3，結束改回 1。</summary>
    public int HitCount
    {
        get => _hitCount;
        set => _hitCount = Mathf.Max(1, value);
    }

    /// <summary>現在按左鍵會打到的目標；沒有時為 null。每幀更新，和實際攻擊用同一套判定。</summary>
    public IHittable CurrentTarget => _currentTarget;

    /// <summary>「現在會打到的目標」改變時觸發。參數：新目標，null 代表範圍內沒有可攻擊的目標。TargetMarker 用來顯示頭上標記。</summary>
    public event Action<IHittable> OnTargetChanged;

    /// <summary>每次揮擊時觸發（不論有沒有打中）。B 可以接揮擊音效、動畫。</summary>
    public event Action OnAttack;

    /// <summary>命中目標時觸發。參數：被打中的目標、命中位置（世界座標）。B 可以接命中音效、特效。</summary>
    public event Action<IHittable, Vector3> OnHit;

    private const float GizmoFlashSeconds = 0.2f;

    private PlayerController _playerController;
    private float _nextAttackTime;
    private IHittable _currentTarget;
    private Vector3 _currentHitPoint;
    private readonly Collider[] _overlapResults = new Collider[64];
    private readonly RaycastHit[] _lineHits = new RaycastHit[16];

    // 只給 Gizmos 用：最近一次揮擊的時間與結果
    private float _lastSwingTime = -100f;
    private bool _lastSwingHit;
    private Vector3 _lastHitPoint;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // 每幀先算出「現在會打到誰」：給頭上標記用，揮擊時也直接打這個目標
        bool canControl = _playerController.IsControlEnabled;
        IHittable newTarget = null;
        Vector3 newHitPoint = Vector3.zero;
        if (canControl)
        {
            TryFindTarget(out newTarget, out newHitPoint);
        }

        _currentHitPoint = newHitPoint;
        SetTarget(newTarget);

        if (!canControl)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        _nextAttackTime = Time.time + _cooldown;
        Swing();
    }

    private void Swing()
    {
        _lastSwingTime = Time.time;
        _lastSwingHit = false;

        OnAttack?.Invoke();

        IHittable target = _currentTarget;
        Vector3 hitPoint = _currentHitPoint;
        if (target == null)
        {
            return;
        }

        _lastSwingHit = true;
        _lastHitPoint = hitPoint;

        target.TakeHit(_hitCount);
        OnHit?.Invoke(target, hitPoint);
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

    private void SetTarget(IHittable target)
    {
        if (ReferenceEquals(target, _currentTarget))
        {
            return;
        }

        _currentTarget = target;
        OnTargetChanged?.Invoke(target);
    }

    /// <summary>找出前方扇形範圍內、沒被牆擋住、最近的一個 IHittable。</summary>
    private bool TryFindTarget(out IHittable bestTarget, out Vector3 bestPoint)
    {
        bestTarget = null;
        bestPoint = Vector3.zero;
        float bestDistance = float.MaxValue;

        Vector3 origin = AttackOrigin;
        int count = Physics.OverlapSphereNonAlloc(
            origin, _range, _overlapResults, _targetMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider candidate = _overlapResults[i];

            // 忽略玩家自己
            if (candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            IHittable hittable = candidate.GetComponentInParent<IHittable>();
            if (hittable == null)
            {
                continue;
            }

            Bounds bounds = candidate.bounds;
            Vector3 closestPoint = bounds.ClosestPoint(origin);
            float distance = Vector3.Distance(origin, closestPoint);
            if (distance > _range || distance >= bestDistance)
            {
                continue;
            }

            if (!IsInsideAngle(origin, bounds.center))
            {
                continue;
            }

            Transform targetRoot = ((Component)hittable).transform;
            if (IsBlocked(origin, bounds.center, targetRoot))
            {
                continue;
            }

            bestTarget = hittable;
            bestPoint = closestPoint;
            bestDistance = distance;
        }

        return bestTarget != null;
    }

    /// <summary>目標是否在角色面向的水平角度範圍內。</summary>
    private bool IsInsideAngle(Vector3 origin, Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - origin;
        toTarget.y = 0f;

        // 目標幾乎就在正上方／正下方（貼在身上），直接算在範圍內
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return Vector3.Angle(forward, toTarget) <= _angle * 0.5f;
    }

    /// <summary>
    /// 從起點拉一條線到目標，最先撞到的東西（玩家自己除外）如果不是目標本身，就算被牆擋住。
    /// </summary>
    private bool IsBlocked(Vector3 origin, Vector3 targetPoint, Transform targetRoot)
    {
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance < 0.0001f)
        {
            return false;
        }

        int count = Physics.RaycastNonAlloc(
            origin, direction / distance, _lineHits, distance, _obstacleMask, QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        Transform nearestTransform = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _lineHits[i];
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestTransform = hit.collider.transform;
            }
        }

        // 什麼都沒撞到，或最先撞到的就是目標 → 沒被擋住
        return nearestTransform != null && !nearestTransform.IsChildOf(targetRoot);
    }

    private Vector3 AttackOrigin => transform.position + Vector3.up * _originHeight;

    private void OnDrawGizmosSelected()
    {
        DrawRangeFan(new Color(1f, 0.85f, 0.1f, 0.9f));
    }

    private void OnDrawGizmos()
    {
        // 按 Play 時，揮擊後短暫閃一下：命中紅色、沒命中白色
        if (!Application.isPlaying || Time.time - _lastSwingTime > GizmoFlashSeconds)
        {
            return;
        }

        DrawRangeFan(_lastSwingHit ? Color.red : Color.white);
        if (_lastSwingHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastHitPoint, 0.15f);
        }
    }

    /// <summary>在角色胸口高度畫出水平的扇形攻擊範圍。</summary>
    private void DrawRangeFan(Color color)
    {
        Gizmos.color = color;
        Vector3 origin = AttackOrigin;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        float halfAngle = _angle * 0.5f;
        const int segments = 24;

        Vector3 previous = origin + Quaternion.Euler(0f, -halfAngle, 0f) * forward * _range;
        Gizmos.DrawLine(origin, previous);
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -halfAngle + _angle * i / segments;
            Vector3 point = origin + Quaternion.Euler(0f, currentAngle, 0f) * forward * _range;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawLine(origin, previous);
    }
}

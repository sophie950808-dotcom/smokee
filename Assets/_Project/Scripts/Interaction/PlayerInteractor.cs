using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家互動：每幀找出範圍內最近、可以互動、而且沒被牆擋住的 IInteractable，按 E 時呼叫它的 Interact()。
/// 掛在 Player（和 PlayerController 同一個物件）上。
/// 整個專案只有這支腳本會讀 E 鍵。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("互動範圍")]
    [Tooltip("互動距離（公尺），從胸口位置量到物件的 InteractPoint")]
    [SerializeField] private float _range = 2.5f;

    [Tooltip("距離與擋牆判定的起點高度（公尺，從角色腳底算起），大約胸口位置")]
    [SerializeField] private float _originHeight = 1.2f;

    [Header("Layer")]
    [Tooltip("哪些 Layer 的物件可以被互動。預設全部")]
    [SerializeField] private LayerMask _interactableMask = ~0;

    [Tooltip("哪些 Layer 會擋住互動（牆）。預設全部；Trigger 碰撞體不會擋")]
    [SerializeField] private LayerMask _obstacleMask = ~0;

    /// <summary>目前對準的互動目標；沒有時為 null。</summary>
    public IInteractable CurrentTarget => _currentTarget;

    /// <summary>互動目標改變時觸發。參數：新的目標，null 代表附近沒有可互動物件。C 的 HUD 用來顯示／隱藏提示。</summary>
    public event Action<IInteractable> OnFocusChanged;

    // 物件的碰撞體可能比 InteractPoint 遠一點，搜尋範圍多抓一些，最後再用 InteractPoint 的距離篩選
    private const float SearchPadding = 1f;

    private PlayerController _playerController;
    private IInteractable _currentTarget;
    private readonly Collider[] _overlapResults = new Collider[64];
    private readonly RaycastHit[] _lineHits = new RaycastHit[16];

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // 控制被停用（例如問答 UI 開著）或暫停時，沒有目標，提示也會消失
        IInteractable newTarget = _playerController.IsControlEnabled ? FindBestTarget() : null;
        SetTarget(newTarget);

        if (_currentTarget == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            _currentTarget.Interact();

            // 互動後物件可能變成不能互動或被刪除，馬上重新找，提示不會多停一幀
            SetTarget(FindBestTarget());
        }
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

    private void SetTarget(IInteractable target)
    {
        if (ReferenceEquals(target, _currentTarget))
        {
            return;
        }

        _currentTarget = target;
        OnFocusChanged?.Invoke(target);
    }

    /// <summary>找出範圍內最近、CanInteract 為 true、沒被牆擋住的 IInteractable。</summary>
    private IInteractable FindBestTarget()
    {
        Vector3 origin = InteractOrigin;
        int count = Physics.OverlapSphereNonAlloc(
            origin, _range + SearchPadding, _overlapResults, _interactableMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider candidate = _overlapResults[i];
            if (candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            IInteractable interactable = candidate.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract)
            {
                continue;
            }

            Transform root = ((Component)interactable).transform;
            Transform point = interactable.InteractPoint != null ? interactable.InteractPoint : root;
            float distance = Vector3.Distance(origin, point.position);
            if (distance > _range || distance >= bestDistance)
            {
                continue;
            }

            if (IsBlocked(origin, point.position, root))
            {
                continue;
            }

            best = interactable;
            bestDistance = distance;
        }

        return best;
    }

    /// <summary>
    /// 從起點拉一條線到 InteractPoint，最先撞到的東西（玩家自己除外）如果不是這個物件本身，就算被牆擋住。
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

        return nearestTransform != null && !nearestTransform.IsChildOf(targetRoot);
    }

    private Vector3 InteractOrigin => transform.position + Vector3.up * _originHeight;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(InteractOrigin, _range);
    }
}

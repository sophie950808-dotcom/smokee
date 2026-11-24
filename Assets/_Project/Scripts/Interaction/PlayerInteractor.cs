using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家互動：每幀找出範圍內最近、可以互動、而且沒被牆擋住的 IInteractable，按 E 時呼叫它的 Interact()。
/// 掛在 Player（和 PlayerController 同一個物件）上。
/// 整個專案只有這支腳本會讀 E 鍵。
/// - 目標另外實作 IInteractCondition 時，還要通過它自己的條件（例如吸菸 NPC 的前方 90 度、2 公尺內）。
/// - 目標實作 IQteInteractable、按 E 後開始 QTE 時，進入 QTE 模式：鎖定這個目標、鎖住移動與跳躍，
///   之後每次按 E 都轉給它的 RegisterQtePress()；QTE 結束後恢復正常。
/// - 不能互動的時機：PlayerController 不允許 Interact（控制鎖、新手教學、暫停）、「暫停計時」中（對話、過場）、遊戲結束後。
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

    /// <summary>玩家按 E 成功互動時觸發。參數：互動的對象（B 可接音效）。</summary>
    public event Action<IInteractable> OnInteracted;

    /// <summary>
    /// 是否正在 QTE 連打（捏熄吸菸 NPC 的菸）。這段期間 E 鍵全部交給 QTE 目標，不會找其他目標。
    /// </summary>
    public bool IsInQte => _qteTarget != null;

    /// <summary>正在 QTE 的目標；沒有時為 null。</summary>
    public IQteInteractable CurrentQteTarget => _qteTarget;

    /// <summary>進入／離開 QTE 模式時觸發。參數：true = 開始、false = 結束。UI 可以用來切換提示。</summary>
    public event Action<bool> OnQteModeChanged;

    // QTE 期間鎖住移動用的 key
    private const string QteLockKey = "Qte";

    // 物件的碰撞體可能比 InteractPoint 遠一點，搜尋範圍多抓一些，最後再用 InteractPoint 的距離篩選
    private const float SearchPadding = 1f;

    private PlayerController _playerController;
    private IInteractable _currentTarget;
    private IQteInteractable _qteTarget;
    private readonly Collider[] _overlapResults = new Collider[64];
    private readonly RaycastHit[] _lineHits = new RaycastHit[16];

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        bool allowed = _playerController.IsActionAllowed(PlayerAction.Interact) && !IsBlockedByGame();
        Keyboard keyboard = Keyboard.current;
        bool ePressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;

        // QTE 模式：E 鍵全部轉給 QTE 目標，不找其他目標
        if (_qteTarget != null)
        {
            if (IsQteTargetGone(_qteTarget) || !_qteTarget.IsQteActive)
            {
                ExitQte();
                // 往下走一般流程，這一幀就恢復找目標（這一下 E 不會被拿去互動別人）
                ePressed = false;
            }
            else
            {
                SetTarget(_qteTarget);
                if (allowed && ePressed)
                {
                    _qteTarget.RegisterQtePress();
                }
                return;
            }
        }

        // 控制被停用、教學鎖住互動、對話、過場、暫停、遊戲結束時，沒有目標，提示也會消失
        IInteractable newTarget = allowed ? FindBestTarget() : null;
        SetTarget(newTarget);

        if (_currentTarget == null || !ePressed)
        {
            return;
        }

        IInteractable interacted = _currentTarget;
        interacted.Interact();
        OnInteracted?.Invoke(interacted);

        // 按 E 後開始 QTE → 進入 QTE 模式（第一下由目標自己在 Interact 裡算）
        if (interacted is IQteInteractable qte && !IsQteTargetGone(qte) && qte.IsQteActive)
        {
            EnterQte(qte);
            return;
        }

        // 互動後物件可能變成不能互動或被刪除，馬上重新找，提示不會多停一幀
        SetTarget(FindBestTarget());
    }

    private void OnDisable()
    {
        ExitQte();
        SetTarget(null);
    }

    /// <summary>「暫停計時」中（對話、過場、視窗）或遊戲結束後不能互動。測試場景沒有 GameManager 時不擋。</summary>
    private static bool IsBlockedByGame()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager != null && (gameManager.IsTimerPaused || gameManager.State == GameState.Ended);
    }

    /// <summary>QTE 目標被刪除或停用了沒有。</summary>
    private static bool IsQteTargetGone(IQteInteractable target)
    {
        Component component = target as Component;
        if (component == null)
        {
            // 被 Destroy 的 Unity 物件也會走到這裡（Unity 的 == null 判斷）
            return true;
        }
        if (component is Behaviour behaviour)
        {
            return !behaviour.isActiveAndEnabled;
        }
        return !component.gameObject.activeInHierarchy;
    }

    private void EnterQte(IQteInteractable target)
    {
        _qteTarget = target;
        _playerController.SetActionLocked(PlayerAction.Move, QteLockKey, true);
        _playerController.SetActionLocked(PlayerAction.Jump, QteLockKey, true);
        SetTarget(target);
        OnQteModeChanged?.Invoke(true);
    }

    private void ExitQte()
    {
        if (_qteTarget == null)
        {
            return;
        }

        _qteTarget = null;
        if (_playerController != null)
        {
            _playerController.SetActionLocked(PlayerAction.Move, QteLockKey, false);
            _playerController.SetActionLocked(PlayerAction.Jump, QteLockKey, false);
        }
        OnQteModeChanged?.Invoke(false);
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

    /// <summary>找出範圍內最近、CanInteract 為 true、通過自己的條件（IInteractCondition）、沒被牆擋住的 IInteractable。</summary>
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

            // 目標自己的額外條件（例如吸菸 NPC：面向它、前方 90 度、2 公尺內）
            if (interactable is IInteractCondition condition && !condition.CanInteractFrom(transform))
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

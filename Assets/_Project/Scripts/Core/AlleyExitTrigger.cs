using UnityEngine;

/// <summary>
/// 死巷出口觸發器。B 把它掛在死巷出口的 Collider（勾選 Is Trigger）上即可，不用自己寫程式。
/// 玩家穿過時呼叫 RouteManager.Instance.NotifyPlayerLeftAlley()。
/// 「只在路線 B、開場結束後才有效、只觸發一次」由 RouteManager 判斷，玩家來回穿過也沒關係。
/// 規格見 docs/INTERFACES.md 第 4 節。
/// </summary>
public class AlleyExitTrigger : MonoBehaviour
{
    private void Reset()
    {
        // 加上元件時自動把 Collider 設成 Trigger
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogWarning("[AlleyExitTrigger] 這個物件沒有 Collider，請加一個 Box Collider 並勾選 Is Trigger。", this);
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("[AlleyExitTrigger] Collider 沒有勾選 Is Trigger，玩家會被擋住而且不會觸發。", this);
        }
        if (RouteManager.Instance == null)
        {
            Debug.LogWarning("[AlleyExitTrigger] 場景裡找不到 RouteManager，穿過出口不會開始倒數。", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        RouteManager routeManager = RouteManager.Instance;
        if (routeManager != null)
        {
            routeManager.NotifyPlayerLeftAlley();
        }
    }
}

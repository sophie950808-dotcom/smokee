using System;
using UnityEngine;

/// <summary>
/// 3 種道具的效果（尼古丁貼片、尼古丁口嚼錠、尼古丁戒菸噴霧）。掛在 Player 身上（和 PlayerController、PlayerHealth 同一個物件）。
/// C 的背包按右鍵時呼叫 TryUseItem，依回傳值決定要不要扣數量（只有 Consumed 要減 1）。規格見 docs/INTERFACES.md 第 12 節。
/// - 貼片：只能在第三階段；HP +30（不超過上限）；滿血時不能用（不消耗）；一次性。
/// - 口嚼錠：只能在第三階段；清除玩家周圍 15 公尺內的 BOSS 菸圈與煙霧區（ClearableSmoke）；一次性。
/// - 噴霧：無次數限制、冷卻 1.5 秒；從鏡頭中央往前瞬間判定，只對 IBossDamageable（BOSS）造成 7.5 傷害。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerEffects : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("噴霧瞄準用的攝影機。留空會自動抓 Main Camera")]
    [SerializeField] private Camera _aimCamera;

    [Tooltip("噴霧特效的起點（例如手的位置）。留空用玩家胸口（腳底往上 _sprayOriginHeight 公尺）")]
    [SerializeField] private Transform _sprayOrigin;

    [Header("尼古丁貼片")]
    [Tooltip("回復多少 HP（不超過上限）")]
    [SerializeField] private float _patchHealAmount = 30f;

    [Header("尼古丁口嚼錠")]
    [Tooltip("清除玩家周圍多少公尺內的 BOSS 菸圈與菸炮煙霧區")]
    [SerializeField] private float _gumClearRadius = 15f;

    [Header("尼古丁戒菸噴霧")]
    [Tooltip("每次對 BOSS 造成的傷害")]
    [SerializeField] private float _sprayDamage = 7.5f;

    [Tooltip("兩次噴霧之間的冷卻秒數")]
    [SerializeField] private float _sprayCooldown = 1.5f;

    [Tooltip("噴霧最遠距離（公尺），從鏡頭算起【暫定】")]
    [SerializeField] private float _sprayRange = 30f;

    [Tooltip("沒有指定 _sprayOrigin 時，特效起點離腳底多高（公尺）")]
    [SerializeField] private float _sprayOriginHeight = 1.2f;

    [Tooltip("哪些 Layer 會被噴霧判定（BOSS、牆）。預設全部；Trigger 碰撞體只有 BOSS 會被打到")]
    [SerializeField] private LayerMask _sprayMask = ~0;

    [Header("提示文字")]
    [Tooltip("第三階段以前使用貼片／口嚼錠時顯示（Toast），也是 OnItemUseRejected 的原因文字")]
    [SerializeField] private string _notYetMessage = "第三階段才能使用";

    [Tooltip("HP 已經滿了還用貼片時顯示（Toast），道具不消耗")]
    [SerializeField] private string _fullHpMessage = "HP 已經是滿的";

    [Tooltip("噴霧冷卻中的原因文字（只放進 OnItemUseRejected，不跳 Toast）")]
    [SerializeField] private string _cooldownMessage = "噴霧冷卻中";

    [Tooltip("操作被鎖住時的原因文字（只放進 OnItemUseRejected，不跳 Toast）")]
    [SerializeField] private string _lockedMessage = "現在不能使用道具";

    [Tooltip("其他原因失敗時的原因文字（只放進 OnItemUseRejected，不跳 Toast）")]
    [SerializeField] private string _failedMessage = "這個道具不能使用";

    private PlayerController _playerController;
    private PlayerHealth _playerHealth;
    private float _nextSprayTime;
    private readonly RaycastHit[] _sprayHits = new RaycastHit[32];

    // 給 DevOverlay 顯示用：最近一次使用的結果
    private string _lastUseText = "（還沒使用）";

    /// <summary>噴霧冷卻總秒數（C 的背包顯示冷卻用）。</summary>
    public float SprayCooldown => _sprayCooldown;

    /// <summary>噴霧冷卻還剩幾秒；0 = 可以使用。暫停選單期間不會減少。</summary>
    public float SprayCooldownRemaining => Mathf.Max(0f, _nextSprayTime - Time.time);

    /// <summary>口嚼錠清除半徑（公尺）。DevOverlay、B 的特效可以讀。</summary>
    public float GumClearRadius => _gumClearRadius;

    /// <summary>【開發者工具用】最近一次使用道具的結果文字。</summary>
    public string LastUseText => _lastUseText;

    /// <summary>
    /// 貼片、口嚼錠現在能不能用（= 第三階段）。DevOverlay、C 的背包可以用來把圖示變灰。
    /// </summary>
    public bool IsPhase3ItemAvailable =>
        PhaseManager.Instance != null && PhaseManager.Instance.CurrentPhase == GamePhase.Phase3;

    /// <summary>道具成功使用時觸發。參數：道具種類（B 特效、音效）。</summary>
    public event Action<ItemType> OnItemUsed;

    /// <summary>
    /// 道具沒有用成功時觸發（道具不消耗）。參數：道具、結果（NotYet／OnCooldown／Locked／Failed）、原因文字。
    /// 「第三階段才能使用」PlayerEffects 已經自動用 TaskHint.ShowToast 顯示，C 不用再顯示一次；這個事件給音效、背包抖動用。
    /// </summary>
    public event Action<ItemType, ItemUseResult, string> OnItemUseRejected;

    /// <summary>噴霧射出時觸發。參數：起點、終點（打中的位置或最遠距離）、有沒有打到 BOSS → B 播噴霧特效。</summary>
    public event Action<Vector3, Vector3, bool> OnSprayFired;

    /// <summary>口嚼錠使用時觸發。參數：中心、半徑、清掉幾個。B 可以播特效（也可以聽 ClearableSmoke.OnCleared）。</summary>
    public event Action<Vector3, float, int> OnGumUsed;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _playerHealth = GetComponent<PlayerHealth>();
        if (_playerHealth == null)
        {
            Debug.LogWarning("[PlayerEffects] 同一個物件上找不到 PlayerHealth，尼古丁貼片沒有效果。", this);
        }
    }

    /// <summary>
    /// 使用道具（C 的背包按右鍵時呼叫）。只有回傳 Consumed 時背包要減 1，其他結果都不要扣。
    /// 第三階段以前使用貼片、口嚼錠會自動顯示「第三階段才能使用」並回傳 NotYet。
    /// </summary>
    public ItemUseResult TryUseItem(ItemType type)
    {
        if (!CanUseNow())
        {
            return Reject(type, ItemUseResult.Locked, _lockedMessage, false);
        }

        switch (type)
        {
            case ItemType.NicotinePatch:
                if (!IsPhase3ItemAvailable)
                {
                    return Reject(type, ItemUseResult.NotYet, _notYetMessage, true);
                }
                return UsePatch();

            case ItemType.NicotineGum:
                if (!IsPhase3ItemAvailable)
                {
                    return Reject(type, ItemUseResult.NotYet, _notYetMessage, true);
                }
                return UseGum();

            case ItemType.NicotineSpray:
                if (SprayCooldownRemaining > 0f)
                {
                    return Reject(type, ItemUseResult.OnCooldown, _cooldownMessage, false);
                }
                return UseSpray();

            default:
                return Reject(type, ItemUseResult.Failed, _failedMessage, false);
        }
    }

    /// <summary>操作沒被鎖（教學、對話、暫停）、沒在「暫停計時」中、遊戲沒結束。</summary>
    private bool CanUseNow()
    {
        if (_playerController != null && !_playerController.IsActionAllowed(PlayerAction.UseItem))
        {
            return false;
        }

        GameManager gameManager = GameManager.Instance;
        return gameManager == null || (!gameManager.IsTimerPaused && gameManager.State != GameState.Ended);
    }

    private ItemUseResult UsePatch()
    {
        if (_playerHealth == null)
        {
            return Reject(ItemType.NicotinePatch, ItemUseResult.Failed, _failedMessage, false);
        }

        // 滿血時不能用：不消耗，跳提示
        if (_playerHealth.CurrentHp >= _playerHealth.MaxHp)
        {
            return Reject(ItemType.NicotinePatch, ItemUseResult.Failed, _fullHpMessage, true);
        }

        float before = _playerHealth.CurrentHp;
        _playerHealth.Heal(_patchHealAmount);
        float after = _playerHealth.CurrentHp;

        _lastUseText = "貼片：HP " + before.ToString("F0") + " → " + after.ToString("F0");
        Debug.Log("[PlayerEffects] 使用尼古丁貼片：HP " + before + " → " + after + "（上限 " + _playerHealth.MaxHp + "）", this);
        OnItemUsed?.Invoke(ItemType.NicotinePatch);
        return ItemUseResult.Consumed;
    }

    private ItemUseResult UseGum()
    {
        Vector3 center = transform.position;
        int cleared = ClearableSmoke.ClearInRadius(center, _gumClearRadius);

        _lastUseText = "口嚼錠：清除 " + cleared + " 個（半徑 " + _gumClearRadius + "）";
        Debug.Log("[PlayerEffects] 使用尼古丁口嚼錠：清除半徑 " + _gumClearRadius + " 公尺內的煙霧 " + cleared + " 個。", this);
        OnGumUsed?.Invoke(center, _gumClearRadius, cleared);
        OnItemUsed?.Invoke(ItemType.NicotineGum);
        return ItemUseResult.Consumed;
    }

    private ItemUseResult UseSpray()
    {
        Camera aimCamera = GetAimCamera();
        if (aimCamera == null)
        {
            Debug.LogWarning("[PlayerEffects] 找不到瞄準用的攝影機，噴霧無法使用。", this);
            return Reject(ItemType.NicotineSpray, ItemUseResult.Failed, _failedMessage, false);
        }

        _nextSprayTime = Time.time + _sprayCooldown;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 start = _sprayOrigin != null ? _sprayOrigin.position : transform.position + Vector3.up * _sprayOriginHeight;
        Vector3 end = ray.origin + ray.direction * _sprayRange;

        IBossDamageable boss = FindSprayTarget(ray, out Vector3 hitPoint, out bool hitSomething);
        if (hitSomething)
        {
            end = hitPoint;
        }

        bool hitBoss = boss != null;
        if (hitBoss)
        {
            boss.TakeDamage(_sprayDamage, DamageSource.Spray);
        }

        _lastUseText = "噴霧：" + (hitBoss ? "打中 BOSS（" + _sprayDamage + "）" : hitSomething ? "打到牆或其他物件" : "沒打到東西");
        Debug.Log("[PlayerEffects] 使用尼古丁戒菸噴霧：" + _lastUseText, this);
        OnSprayFired?.Invoke(start, end, hitBoss);
        OnItemUsed?.Invoke(ItemType.NicotineSpray);
        return ItemUseResult.UsedNotConsumed;
    }

    /// <summary>
    /// 沿著鏡頭中央的射線，找最先碰到的東西：
    /// 玩家自己、Trigger（不是 BOSS 的）、小菸怪都會被穿過；碰到 BOSS 就打中，碰到其他實心物件（牆）就停下。
    /// </summary>
    private IBossDamageable FindSprayTarget(Ray ray, out Vector3 hitPoint, out bool hitSomething)
    {
        hitPoint = Vector3.zero;
        hitSomething = false;

        int count = Physics.RaycastNonAlloc(ray, _sprayHits, _sprayRange, _sprayMask, QueryTriggerInteraction.Collide);
        float nearestDistance = float.MaxValue;
        IBossDamageable nearestBoss = null;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _sprayHits[i];
            Collider hitCollider = hit.collider;
            if (hitCollider.transform.IsChildOf(transform) || hit.distance >= nearestDistance)
            {
                continue;
            }

            IBossDamageable boss = hitCollider.GetComponentInParent<IBossDamageable>();
            if (boss == null)
            {
                // 小菸怪不擋噴霧（噴霧對小菸怪無效，也不該被它擋住）；不是 BOSS 的 Trigger 也不擋
                if (hitCollider.isTrigger || hitCollider.GetComponentInParent<IHittable>() != null)
                {
                    continue;
                }
            }

            nearestDistance = hit.distance;
            nearestBoss = boss;
            hitPoint = hit.point;
            hitSomething = true;
        }

        return nearestBoss;
    }

    private Camera GetAimCamera()
    {
        if (_aimCamera == null)
        {
            _aimCamera = Camera.main;
        }
        return _aimCamera;
    }

    private ItemUseResult Reject(ItemType type, ItemUseResult result, string reason, bool showToast)
    {
        if (showToast)
        {
            TaskHint.ShowToast(reason);
        }

        _lastUseText = type + "：沒有使用（" + result + "，" + reason + "）";
        OnItemUseRejected?.Invoke(type, result, reason);
        return result;
    }

    private void OnValidate()
    {
        _patchHealAmount = Mathf.Max(0f, _patchHealAmount);
        _gumClearRadius = Mathf.Max(0f, _gumClearRadius);
        _sprayDamage = Mathf.Max(0f, _sprayDamage);
        _sprayCooldown = Mathf.Max(0f, _sprayCooldown);
        _sprayRange = Mathf.Max(0.1f, _sprayRange);
        _sprayOriginHeight = Mathf.Max(0f, _sprayOriginHeight);
    }

    private void OnDrawGizmosSelected()
    {
        // 口嚼錠清除範圍
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _gumClearRadius);
    }
}

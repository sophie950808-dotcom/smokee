using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 第三階段街上的吸菸者（爸爸、北友會兩位朋友、阿公，共 4 位）。實作 IInteractable，身上有 SmokeSource（半徑 4）。
/// 和第二階段故事裡的 NPC 是不同物件。
/// 捏熄：面向 NPC（前方 90 度）、2 公尺內按 E 開始 QTE，2 秒內連打 E 5 次（第一下算 1 下）。
/// QTE 期間不能移動、可以轉視角；被打不中斷；超時失敗、進度歸零、可以馬上重試。
/// E 鍵由 PlayerInteractor 讀，再交給 ActiveQte；C 的 QteBarUI 只負責顯示。
/// 規格見 docs/INTERFACES.md 第 15 節。
/// 【A10 空殼】名稱、參數、Inspector 數值已定案；A16 完成。
/// </summary>
public class SmokerNPC : MonoBehaviour, IInteractable
{
    private static readonly List<SmokerNPC> _all = new List<SmokerNPC>();
    private static SmokerNPC _activeQte;

    [Header("顯示")]
    [Tooltip("名字，例如「阿公」")]
    [SerializeField] private string _displayName = "吸菸者";

    [Tooltip("HUD 互動提示")]
    [SerializeField] private string _prompt = "按 E 捏熄菸蒂";

    [Tooltip("計算互動距離的位置。留空用 NPC 本身")]
    [SerializeField] private Transform _interactPoint;

    [Header("捏熄判定")]
    [Tooltip("玩家要在幾公尺內")]
    [SerializeField] private float _snuffDistance = 2f;

    [Tooltip("玩家正前方幾度內（90 = 左右各 45 度）")]
    [SerializeField] private float _snuffAngle = 90f;

    [Header("QTE 連打")]
    [Tooltip("要連打 E 幾下")]
    [SerializeField] private int _qteRequiredPresses = 5;

    [Tooltip("幾秒內要完成")]
    [SerializeField] private float _qteDuration = 2f;

    private bool _isSmoking = false;
    private int _qtePresses = 0;
    private float _qteTimeRemaining = 0f;

    /// <summary>所有吸菸 NPC（C 的小地圖用）。</summary>
    public static IReadOnlyList<SmokerNPC> All => _all;

    /// <summary>還在抽菸的人數（BOSS 回血用）。</summary>
    public static int SmokingCount
    {
        get
        {
            int count = 0;
            foreach (SmokerNPC npc in _all)
            {
                if (npc != null && npc.IsSmoking)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>正在 QTE 的那位；沒有時為 null。C 的 QteBarUI 每幀讀它的進度。</summary>
    public static SmokerNPC ActiveQte => _activeQte;

#pragma warning disable 67, 414 // A16 才會觸發
    /// <summary>還在抽菸的人數改變時觸發。參數：目前人數。</summary>
    public static event Action<int> OnSmokingCountChanged;

    /// <summary>任何一位被捏熄時觸發（B 熄菸特效、C 小地圖）。</summary>
    public static event Action<SmokerNPC> OnAnySnuffed;

    /// <summary>QTE 開始時觸發。C 的 QteBarUI 顯示。</summary>
    public static event Action<SmokerNPC> OnQteStarted;

    /// <summary>QTE 結束時觸發。參數：NPC、是否成功。C 的 QteBarUI 隱藏。</summary>
    public static event Action<SmokerNPC, bool> OnQteEnded;

    /// <summary>這位被捏熄時觸發。</summary>
    public event Action<SmokerNPC> OnSnuffed;
#pragma warning restore 67, 414

    /// <summary>是否還在抽菸（小地圖圖示：false = 熄滅）。</summary>
    public bool IsSmoking => _isSmoking;

    /// <summary>名字，例如「阿公」。</summary>
    public string DisplayName => _displayName;

    /// <summary>QTE 已按幾下。</summary>
    public int QtePresses => _qtePresses;

    /// <summary>QTE 需要按幾下。</summary>
    public int QteRequired => _qteRequiredPresses;

    /// <summary>QTE 還剩幾秒。</summary>
    public float QteTimeRemaining => _qteTimeRemaining;

    /// <summary>QTE 總秒數。</summary>
    public float QteDuration => _qteDuration;

    /// <summary>HUD 顯示的提示。</summary>
    public string InteractPrompt => _prompt;

    /// <summary>正在抽菸、BOSS 戰中、玩家面向它且距離夠近時才能互動。【A16 完成，目前只看是否在抽菸】</summary>
    public bool CanInteract => _isSmoking;

    /// <summary>計算互動距離的位置。</summary>
    public Transform InteractPoint => _interactPoint != null ? _interactPoint : transform;

    /// <summary>每次進入 Play 時清掉 static 的狀態。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _all.Clear();
        _activeQte = null;
        OnSmokingCountChanged = null;
        OnAnySnuffed = null;
        OnQteStarted = null;
        OnQteEnded = null;
    }

    private void OnEnable()
    {
        if (!_all.Contains(this))
        {
            _all.Add(this);
        }
    }

    private void OnDisable()
    {
        _all.Remove(this);
        if (_activeQte == this)
        {
            _activeQte = null;
        }
    }

    /// <summary>玩家按 E 時呼叫（只有 PlayerInteractor 會呼叫）→ 開始 QTE。【A16 完成】</summary>
    public void Interact()
    {
        Debug.Log("[SmokerNPC] （A10 空殼）" + _displayName + " Interact，QTE 在 A16 完成。", this);
    }

    /// <summary>【只給 PhaseManager／BossController】開始抽菸（BOSS 戰開始、重新挑戰時）。【A16 完成】</summary>
    public void BeginSmoking()
    {
        Debug.Log("[SmokerNPC] （A10 空殼）" + _displayName + " BeginSmoking，A16 完成。", this);
    }

    /// <summary>【只給 PlayerInteractor】QTE 期間每按一次 E 呼叫。【A16 完成】</summary>
    public void RegisterQtePress()
    {
        Debug.Log("[SmokerNPC] （A10 空殼）RegisterQtePress，A16 完成。", this);
    }

    private void OnValidate()
    {
        _snuffDistance = Mathf.Max(0.1f, _snuffDistance);
        _snuffAngle = Mathf.Clamp(_snuffAngle, 1f, 360f);
        _qteRequiredPresses = Mathf.Max(1, _qteRequiredPresses);
        _qteDuration = Mathf.Max(0.1f, _qteDuration);
    }
}

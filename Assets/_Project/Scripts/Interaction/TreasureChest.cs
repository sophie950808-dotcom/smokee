using System;
using UnityEngine;

/// <summary>
/// 吸菸室旁的藏寶圖寶箱（新版，不可沿用 C 的舊問答 ChestController）。實作 IInteractable。
/// - 拿到藏寶圖之前寶箱不存在：_visualRoot（模型與碰撞體）隱藏，看不到、碰不到、不能按 E。
/// - 按 E：淨化 &lt; 600 → 提示「路上好像還有還沒消除的煙怪」；≥ 600 → 打開、給噴霧、進入第三階段。
/// 腳本掛在根物件、一直啟用（Instance 一直找得到）。規格見 docs/INTERFACES.md 第 13 節。
/// 【A10 空殼】名稱、參數已定案；流程在 A17 完成。
/// </summary>
public class TreasureChest : MonoBehaviour, IInteractable
{
    /// <summary>場景中唯一的寶箱。</summary>
    public static TreasureChest Instance { get; private set; }

    [Header("引用")]
    [Tooltip("寶箱的模型與碰撞體（放在子物件）。拿到藏寶圖之前隱藏")]
    [SerializeField] private GameObject _visualRoot;

    [Tooltip("計算互動距離的位置。留空用寶箱本身")]
    [SerializeField] private Transform _interactPoint;

    [Header("規則")]
    [Tooltip("打開寶箱需要的淨化值（600 = 60%）")]
    [SerializeField] private int _requiredUnits = 600;

    [Header("文字")]
    [Tooltip("靠近時的互動提示")]
    [SerializeField] private string _prompt = "按 E 開啟寶箱";
    [Tooltip("淨化不足時的短暫提示")]
    [SerializeField] private string _lockedMessage = "路上好像還有還沒消除的煙怪";

    private bool _isRevealed = false;
    private bool _isOpened = false;

    /// <summary>寶箱是否已出現（= 拿到藏寶圖）。C 的小地圖用來顯示寶箱。</summary>
    public bool IsRevealed => _isRevealed;

    /// <summary>寶箱是否已經打開。</summary>
    public bool IsOpened => _isOpened;

#pragma warning disable 67 // A17 才會觸發
    /// <summary>寶箱出現時觸發（B 可以播出現特效）。</summary>
    public event Action OnChestRevealed;

    /// <summary>寶箱打開時觸發（B 開箱動畫、音效）。</summary>
    public event Action OnChestOpened;

    /// <summary>淨化不足、按 E 被擋時觸發。</summary>
    public event Action OnChestLocked;
#pragma warning restore 67

    /// <summary>HUD 顯示的提示。</summary>
    public string InteractPrompt => _prompt;

    /// <summary>拿到藏寶圖、而且還沒打開時才能互動。</summary>
    public bool CanInteract => _isRevealed && !_isOpened;

    /// <summary>計算互動距離的位置。</summary>
    public Transform InteractPoint => _interactPoint != null ? _interactPoint : transform;

    /// <summary>
    /// 每次進入 Play 時先清掉 static 的 Instance。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TreasureChest] 場景中有超過一個 TreasureChest，請只保留一個。", this);
            return;
        }
        Instance = this;

        // 拿到藏寶圖之前寶箱不存在
        if (_visualRoot != null)
        {
            _visualRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>玩家按 E 時呼叫（只有 PlayerInteractor 會呼叫）。【A17 完成】</summary>
    public void Interact()
    {
        Debug.Log("[TreasureChest] （A10 空殼）Interact，A17 完成。需要淨化 " + _requiredUnits
                  + "，不足時提示「" + _lockedMessage + "」。", this);
    }
}

using System;
using UnityEngine;

/// <summary>
/// 整局流程的導演：新手教學 → 第一階段 → 三個故事事件（40 秒空檔、拒絕幫忙）→ 寶箱 → BOSS → 結局。
/// 和 GameManager 放在同一個物件上，用 PhaseManager.Instance 取得。規格見 docs/INTERFACES.md 第 4 節。
/// A 用事件通知 B、C（OnXxx）；B、C 用 ReportXxx／NotifyXxx 方法回報。
/// 【A10 空殼】名稱、參數、Inspector 數值已定案；流程在 A11（階段、空檔、拒絕）、A17（藏寶圖）完成。
/// </summary>
public class PhaseManager : MonoBehaviour
{
    /// <summary>場景中唯一的 PhaseManager。</summary>
    public static PhaseManager Instance { get; private set; }

    [Header("淨化值（整數，1000 = 100%）")]
    [Tooltip("教學菸怪每隻的淨化值（不計入，0）")]
    [SerializeField] private int _tutorialMonsterUnits = 0;

    [Tooltip("第一階段 5 隻菸怪，每隻的淨化值（4% = 40）")]
    [SerializeField] private int _phase1MonsterUnits = 40;

    [Tooltip("40 秒空檔的菸怪，每隻的淨化值（2.5% = 25；兩段各 2 隻）")]
    [SerializeField] private int _eventGapMonsterUnits = 25;

    [Tooltip("故事地點的 3 隻菸怪，依序的淨化值（一個事件共 100 = 10%）")]
    [SerializeField] private int[] _storyEventMonsterUnits = { 33, 33, 34 };

    [Tooltip("第一階段淨化目標（20% = 200），達到後 NPC 跑來求救")]
    [SerializeField] private int _phase1TargetUnits = 200;

    [Header("故事事件")]
    [Tooltip("一個事件結束後，幾秒後下一位 NPC 跑來求救")]
    [SerializeField] private float _eventGapSeconds = 40f;

    [Header("拒絕幫忙")]
    [Tooltip("每隔幾秒菸怪煙霧擴大一次")]
    [SerializeField] private float _refusalGrowInterval = 10f;

    [Tooltip("每次擴大增加多少倍率（0.1 = 原本的 10%，線性增加）")]
    [SerializeField] private float _refusalGrowStep = 0.1f;

    [Tooltip("最多擴大到幾倍")]
    [SerializeField] private float _refusalMaxMultiplier = 2f;

    [Header("任務提示文字")]
    [Tooltip("第一階段的任務提示")]
    [SerializeField] private string _phase1Hint = "消滅所有的煙怪吧！";

    [Tooltip("進入第二階段時的任務提示")]
    [SerializeField] private string _phase2Hint = "城市中還有很多需要幫助的人，快去看看吧！";

    [Tooltip("拒絕幫忙期間的任務提示（不直接告訴玩家要回去）")]
    [SerializeField] private string _refusalHint = "煙霧好像越來越濃了……";

    [Tooltip("拿到藏寶圖後的任務提示")]
    [SerializeField] private string _treasureMapHint = "到吸菸室旁的寶箱看看";

    private GamePhase _currentPhase = GamePhase.Opening;
    private int _currentEventIndex = -1;
    private bool _isInEventGap = false;
    private float _eventGapRemaining = 0f;
    private bool _isHelpRefused = false;
    private float _refusalSmokeMultiplier = 1f;
    private bool _hasTreasureMap = false;

    /// <summary>目前階段。</summary>
    public GamePhase CurrentPhase => _currentPhase;

    /// <summary>目前（或下一個）故事事件編號 0、1、2；還沒到第二階段時 -1。</summary>
    public int CurrentEventIndex => _currentEventIndex;

    /// <summary>是否在兩個事件之間的 40 秒空檔。</summary>
    public bool IsInEventGap => _isInEventGap;

    /// <summary>40 秒空檔還剩幾秒。</summary>
    public float EventGapRemaining => _eventGapRemaining;

    /// <summary>玩家目前是否拒絕幫忙中。</summary>
    public bool IsHelpRefused => _isHelpRefused;

    /// <summary>拒絕期間菸怪的煙霧倍率（1 → 最多 2）。</summary>
    public float RefusalSmokeMultiplier => _refusalSmokeMultiplier;

    /// <summary>是否已經拿到藏寶圖（事件三完成）。</summary>
    public bool HasTreasureMap => _hasTreasureMap;

#pragma warning disable 67 // A11、A17 才會觸發
    /// <summary>階段改變時觸發。參數：新階段。HUD、小地圖、B 的菸怪生成、BOSS 監聽。</summary>
    public event Action<GamePhase> OnPhaseChanged;

    /// <summary>要第 n 個故事事件的 NPC 跑來求救時觸發。參數：事件編號 0/1/2。B 的 StoryEventDirector 監聽。</summary>
    public event Action<int> OnStoryEventRequested;

    /// <summary>40 秒空檔開始時觸發。參數：空檔編號 0（事件一後）/1（事件二後）。B 在街上生成 2 隻空檔菸怪。</summary>
    public event Action<int> OnEventGapStarted;

    /// <summary>40 秒空檔結束時觸發。參數：空檔編號。緊接著會觸發 OnStoryEventRequested(下一個)。</summary>
    public event Action<int> OnEventGapEnded;

    /// <summary>拒絕幫忙狀態改變時觸發。true = 開始拒絕（NPC 跟著玩家）；false = 接下任務、恢復。</summary>
    public event Action<bool> OnHelpRefusedChanged;

    /// <summary>拿到藏寶圖時觸發（寶箱出現、小地圖顯示寶箱）。</summary>
    public event Action OnTreasureMapObtained;

    /// <summary>要播 BOSS 登場動畫時觸發（重新挑戰時也會再觸發）。B 監聽。</summary>
    public event Action OnBossIntroRequested;

    /// <summary>要顯示第三階段規則介紹視窗時觸發。C 的 RulesIntroWindow 監聽。</summary>
    public event Action OnRulesIntroRequested;

    /// <summary>規則視窗關掉、BOSS 戰正式開始時觸發。C 的 BOSS 血條顯示。</summary>
    public event Action OnBossFightStarted;

    /// <summary>BOSS 被打倒、要播結局動畫時觸發。B 監聽。</summary>
    public event Action OnEndingRequested;
#pragma warning restore 67

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
            Debug.LogWarning("[PhaseManager] 場景中有超過一個 PhaseManager，請只保留一個。", this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        CheckUnitsTotal();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 這隻菸怪要給多少淨化值（B 的生成器呼叫）。indexInGroup 只對 StoryEvent 有用（0、1、2 → 33、33、34）。
    /// </summary>
    public int GetMonsterUnits(MonsterGroup group, int indexInGroup)
    {
        switch (group)
        {
            case MonsterGroup.Tutorial:
                return _tutorialMonsterUnits;
            case MonsterGroup.Phase1:
                return _phase1MonsterUnits;
            case MonsterGroup.EventGap:
                return _eventGapMonsterUnits;
            case MonsterGroup.StoryEvent:
                if (_storyEventMonsterUnits == null || _storyEventMonsterUnits.Length == 0)
                {
                    return 0;
                }
                int index = Mathf.Clamp(indexInGroup, 0, _storyEventMonsterUnits.Length - 1);
                return _storyEventMonsterUnits[index];
            default:
                return 0;
        }
    }

    /// <summary>【B 的 StoryEventDirector 呼叫】事件一對話結束（強制）、或事件二、三按「幫忙」時。【A11 完成】</summary>
    public void ReportEventAccepted(int index)
    {
        Debug.Log("[PhaseManager] （A10 空殼）ReportEventAccepted(" + index + ")，A11 完成。", this);
    }

    /// <summary>【B 的 StoryEventDirector 呼叫】事件二、三按「拒絕」時（可以重複拒絕）。【A11 完成】</summary>
    public void ReportHelpRefused(int index)
    {
        Debug.Log("[PhaseManager] （A10 空殼）ReportHelpRefused(" + index + ")，A11 完成。", this);
    }

    /// <summary>【B 的 StoryEventDirector 呼叫】給完道具、傳送回街道之後。【A11、A17 完成】</summary>
    public void ReportEventCompleted(int index)
    {
        Debug.Log("[PhaseManager] （A10 空殼）ReportEventCompleted(" + index + ")，A11 完成。", this);
    }

    /// <summary>【B 的 BOSS 登場演出呼叫】登場動畫播完時。【A14 完成】</summary>
    public void NotifyBossIntroFinished()
    {
        Debug.Log("[PhaseManager] （A10 空殼）NotifyBossIntroFinished，A14 完成。", this);
    }

    /// <summary>【C 的 RulesIntroWindow 呼叫】玩家點擊關掉規則介紹視窗時。【A14 完成】</summary>
    public void NotifyRulesIntroClosed()
    {
        Debug.Log("[PhaseManager] （A10 空殼）NotifyRulesIntroClosed，A14 完成。", this);
    }

    /// <summary>【B 的結局演出呼叫】結局動畫播完 → 勝利結算。【A14 完成】</summary>
    public void NotifyEndingFinished()
    {
        Debug.Log("[PhaseManager] （A10 空殼）NotifyEndingFinished，A14 完成。", this);
    }

    /// <summary>【只給 A 的 TreasureChest】寶箱打開 → 進入第三階段。【A17 完成】</summary>
    public void EnterPhase3()
    {
        Debug.Log("[PhaseManager] （A10 空殼）EnterPhase3，A17 完成。", this);
    }

    /// <summary>【只給 A 的開發者工具】直接跳到第三階段。【F4 完成】</summary>
    public void DevJumpToPhase3()
    {
        Debug.Log("[PhaseManager] （A10 空殼）DevJumpToPhase3，F4 完成。", this);
    }

    /// <summary>檢查淨化值總和：第一階段 5 隻 + 空檔 4 隻 + 事件 3 × 3 隻，必須剛好 600（60%）。</summary>
    private void CheckUnitsTotal()
    {
        int storyTotal = 0;
        if (_storyEventMonsterUnits != null)
        {
            foreach (int units in _storyEventMonsterUnits)
            {
                storyTotal += units;
            }
        }

        int total = _phase1MonsterUnits * 5 + _eventGapMonsterUnits * 4 + storyTotal * 3;
        if (total != 600)
        {
            Debug.LogWarning("[PhaseManager] 第一、二階段的淨化值總和是 " + total
                             + "，不是 600（60%）。寶箱可能永遠打不開，請檢查 Inspector 的淨化值。", this);
        }
    }

    // 下面的欄位 A11 才會用到；先在這裡讀一次，避免「欄位沒有使用」的警告
    private void OnValidate()
    {
        _phase1TargetUnits = Mathf.Max(0, _phase1TargetUnits);
        _eventGapSeconds = Mathf.Max(0f, _eventGapSeconds);
        _refusalGrowInterval = Mathf.Max(0.1f, _refusalGrowInterval);
        _refusalGrowStep = Mathf.Max(0f, _refusalGrowStep);
        _refusalMaxMultiplier = Mathf.Max(1f, _refusalMaxMultiplier);
        if (string.IsNullOrEmpty(_phase1Hint) || string.IsNullOrEmpty(_phase2Hint)
            || string.IsNullOrEmpty(_refusalHint) || string.IsNullOrEmpty(_treasureMapHint))
        {
            Debug.LogWarning("[PhaseManager] 有任務提示文字是空的。", this);
        }
    }
}

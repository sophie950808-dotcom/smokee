using System;
using UnityEngine;

/// <summary>
/// 整局流程的導演：新手教學 → 第一階段 → 三個故事事件（40 秒空檔、拒絕幫忙）→ 寶箱 → BOSS → 結局。
/// 和 GameManager 放在同一個物件上，用 PhaseManager.Instance 取得。規格見 docs/INTERFACES.md 第 4 節。
/// A 用事件通知 B、C（OnXxx）；B、C 用 ReportXxx／NotifyXxx 方法回報。
/// 【A11】階段切換、事件順序、40 秒空檔、拒絕狀態、藏寶圖、進入第三階段（含快照）、里程碑、結局 → 勝利已完成。
/// 【A14】BOSS 登場 → 規則視窗 → BOSS 戰的串接；沒有結局動畫（沒人監聽 OnEndingRequested）時直接勝利結算。
/// 【A15】拒絕時的煙霧倍率（SmokeSource.SetRefusalState），之後完成。
/// </summary>
public class PhaseManager : MonoBehaviour
{
    /// <summary>場景中唯一的 PhaseManager。</summary>
    public static PhaseManager Instance { get; private set; }

    /// <summary>故事事件的數量（事件一、二、三）。</summary>
    public const int StoryEventCount = 3;

    private const string PhaseHintSource = "Phase";
    private const string RefusalHintSource = "Refusal";
    private static readonly int[] MilestonePercents = { 20, 60, 100 };

    /// <summary>目前這個故事事件進行到哪裡。</summary>
    private enum EventStage { None, Requested, Refused, InProgress, Completed }

    /// <summary>第三階段 BOSS 戰的開場進行到哪裡（登場演出 → 規則視窗 → 戰鬥）。</summary>
    private enum BossFlowStage { None, Intro, Rules, Fight }

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
    [Tooltip("一個事件結束後，幾秒後下一位 NPC 跑來求救（暫停計時期間不倒數）")]
    [SerializeField] private float _eventGapSeconds = 40f;

    [Header("拒絕幫忙（A15 使用）")]
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
    private EventStage _eventStage = EventStage.None;
    private bool _isInEventGap = false;
    private int _eventGapIndex = -1;
    private float _eventGapRemaining = 0f;
    private bool _isHelpRefused = false;
    private float _refusalSmokeMultiplier = 1f;
    private bool _hasTreasureMap = false;
    private BossFlowStage _bossFlowStage = BossFlowStage.None;
    private readonly bool[] _milestoneReached = new bool[MilestonePercents.Length];
    private GameManager _subscribedGameManager;
    private RouteManager _subscribedRouteManager;

    /// <summary>目前階段。</summary>
    public GamePhase CurrentPhase => _currentPhase;

    /// <summary>目前（或下一個）故事事件編號 0、1、2；還沒到第二階段時 -1。</summary>
    public int CurrentEventIndex => _currentEventIndex;

    /// <summary>目前的故事事件是否已經接下、正在進行（傳送、打菸怪、故事）。</summary>
    public bool IsEventInProgress => _eventStage == EventStage.InProgress;

    /// <summary>是否在兩個事件之間的 40 秒空檔。</summary>
    public bool IsInEventGap => _isInEventGap;

    /// <summary>40 秒空檔還剩幾秒。</summary>
    public float EventGapRemaining => _eventGapRemaining;

    /// <summary>玩家目前是否拒絕幫忙中。</summary>
    public bool IsHelpRefused => _isHelpRefused;

    /// <summary>拒絕期間菸怪的煙霧倍率（1 → 最多 2）。【A15】目前固定 1。</summary>
    public float RefusalSmokeMultiplier => _refusalSmokeMultiplier;

    /// <summary>是否已經拿到藏寶圖（事件三完成）。</summary>
    public bool HasTreasureMap => _hasTreasureMap;

    /// <summary>階段改變時觸發。參數：新階段。HUD、小地圖、B 的菸怪生成、BOSS 監聽。</summary>
    public event Action<GamePhase> OnPhaseChanged;

    /// <summary>淨化進度第一次達到里程碑時觸發（每個只觸發一次）。參數：20、60、100（%）。</summary>
    public event Action<int> OnPurificationMilestone;

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

    /// <summary>要播 BOSS 登場動畫時觸發（進入第三階段時；重新挑戰時也會再觸發）。B 監聽。</summary>
    public event Action OnBossIntroRequested;

    /// <summary>BOSS 登場演出播完、要顯示第三階段規則介紹視窗時觸發。C 的 RulesIntroWindow 監聽。</summary>
    public event Action OnRulesIntroRequested;

    /// <summary>規則視窗關掉、BOSS 戰正式開始時觸發（接著 BossController.StartFight）。C 的 BOSS 血條顯示。</summary>
    public event Action OnBossFightStarted;

    /// <summary>BOSS 被打倒、要播結局動畫時觸發。B 監聽，播完呼叫 NotifyEndingFinished()。</summary>
    public event Action OnEndingRequested;

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

        // GameManager、RouteManager 的 Instance 在 Awake 就設定好了
        _subscribedGameManager = GameManager.Instance;
        if (_subscribedGameManager != null)
        {
            _subscribedGameManager.OnProgressChanged += HandleProgressChanged;
        }
        else
        {
            Debug.LogWarning("[PhaseManager] 場景裡找不到 GameManager，階段流程無法運作。", this);
        }

        _subscribedRouteManager = RouteManager.Instance;
        if (_subscribedRouteManager != null)
        {
            _subscribedRouteManager.OnOpeningFinished += HandleOpeningFinished;
        }
    }

    private void OnDestroy()
    {
        if (_subscribedGameManager != null)
        {
            _subscribedGameManager.OnProgressChanged -= HandleProgressChanged;
        }
        if (_subscribedRouteManager != null)
        {
            _subscribedRouteManager.OnOpeningFinished -= HandleOpeningFinished;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Instance != this || !_isInEventGap)
        {
            return;
        }

        GameManager gameManager = GameManager.Instance;
        // 暫停選單（Paused）、暫停計時（對話、傳送…）期間空檔不倒數
        if (gameManager == null || gameManager.State != GameState.Playing || gameManager.IsTimerPaused)
        {
            return;
        }

        _eventGapRemaining = Mathf.Max(0f, _eventGapRemaining - Time.deltaTime);
        if (_eventGapRemaining <= 0f)
        {
            EndEventGap();
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

    /// <summary>淨化進度是否已經達到過某個里程碑（20、60、100）。</summary>
    public bool HasReachedMilestone(int percent)
    {
        for (int i = 0; i < MilestonePercents.Length; i++)
        {
            if (MilestonePercents[i] == percent)
            {
                return _milestoneReached[i];
            }
        }
        return false;
    }

    /// <summary>
    /// 開始新手教學：GameManager 進入 Playing、階段切到 Tutorial、呼叫 TutorialManager.BeginTutorial()。
    /// 平常由 RouteManager 的「開場結束（拒絕路線）」自動觸發；GameManager 的開發者自動開始也會呼叫。只在 Opening 階段有效。
    /// </summary>
    public void BeginTutorialPhase()
    {
        GameManager gameManager = GameManager.Instance;
        if (_currentPhase != GamePhase.Opening || gameManager == null || gameManager.State != GameState.Opening)
        {
            return;
        }

        SetPhase(GamePhase.Tutorial);
        gameManager.BeginRun();

        TutorialManager tutorialManager = TutorialManager.Instance;
        if (tutorialManager != null)
        {
            tutorialManager.BeginTutorial();
        }
        else
        {
            Debug.LogWarning("[PhaseManager] 場景裡沒有 TutorialManager：教學不會進行，請用開發者按鍵 F1 完成教學。", this);
        }
    }

    /// <summary>
    /// 【給 A12 的 TutorialManager】新手教學完成 → 第一階段開始：用時、HP、暴露值、淨化重設（OnRunReset），
    /// 任務提示「消滅所有的煙怪吧！」。只在 Tutorial 階段有效。
    /// </summary>
    public void NotifyTutorialFinished()
    {
        if (_currentPhase != GamePhase.Tutorial)
        {
            Debug.Log("[PhaseManager] 不在新手教學階段（目前 " + _currentPhase + "），NotifyTutorialFinished 已忽略。", this);
            return;
        }

        SetPhase(GamePhase.Phase1);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRunForPhase1();
        }
        TaskHint.SetTask(PhaseHintSource, _phase1Hint);
    }

    /// <summary>【B 的 StoryEventDirector 呼叫】事件一對話結束（強制）、或事件二、三按「幫忙」時（拒絕後重新接受也是這個）。</summary>
    public void ReportEventAccepted(int index)
    {
        if (!IsCurrentEvent(index, "ReportEventAccepted"))
        {
            return;
        }
        if (_eventStage != EventStage.Requested && _eventStage != EventStage.Refused)
        {
            Debug.LogWarning("[PhaseManager] 事件 " + index + " 目前是 " + _eventStage + "，不能接受，已忽略。", this);
            return;
        }

        if (_isHelpRefused)
        {
            SetHelpRefused(false);
        }
        _eventStage = EventStage.InProgress;
        Debug.Log("[PhaseManager] 接下事件 " + index + "。", this);
    }

    /// <summary>【B 的 StoryEventDirector 呼叫】事件二、三按「拒絕」時（可以重複拒絕）。事件一不能拒絕。</summary>
    public void ReportHelpRefused(int index)
    {
        if (!IsCurrentEvent(index, "ReportHelpRefused"))
        {
            return;
        }
        if (index == 0)
        {
            Debug.LogWarning("[PhaseManager] 事件一是強制進行，不能拒絕，已忽略。", this);
            return;
        }
        if (_eventStage == EventStage.Refused)
        {
            Debug.Log("[PhaseManager] 事件 " + index + " 再次拒絕（維持拒絕中）。", this);
            return;
        }
        if (_eventStage != EventStage.Requested)
        {
            Debug.LogWarning("[PhaseManager] 事件 " + index + " 目前是 " + _eventStage + "，不能拒絕，已忽略。", this);
            return;
        }

        _eventStage = EventStage.Refused;
        SetHelpRefused(true);
        Debug.Log("[PhaseManager] 拒絕事件 " + index + "。", this);
    }

    /// <summary>
    /// 【B 的 StoryEventDirector 呼叫】給完道具、傳送回街道之後。
    /// 事件一、二 → 開始 40 秒空檔；事件三 → 拿到藏寶圖。
    /// </summary>
    public void ReportEventCompleted(int index)
    {
        if (!IsCurrentEvent(index, "ReportEventCompleted"))
        {
            return;
        }
        if (_eventStage != EventStage.InProgress)
        {
            Debug.LogWarning("[PhaseManager] 事件 " + index + " 目前是 " + _eventStage + "（還沒接下），不能完成，已忽略。", this);
            return;
        }

        _eventStage = EventStage.Completed;
        Debug.Log("[PhaseManager] 完成事件 " + index + "。", this);

        if (index < StoryEventCount - 1)
        {
            StartEventGap(index);
        }
        else
        {
            GrantTreasureMap();
        }
    }

    /// <summary>【B 的 BOSS 登場演出呼叫】登場動畫播完時 → 觸發 OnRulesIntroRequested（C 顯示規則視窗）。只在第三階段登場中有效。</summary>
    public void NotifyBossIntroFinished()
    {
        if (_currentPhase != GamePhase.Phase3 || _bossFlowStage != BossFlowStage.Intro)
        {
            Debug.LogWarning("[PhaseManager] 不在 BOSS 登場中（階段 " + _currentPhase + "，" + _bossFlowStage + "），NotifyBossIntroFinished 已忽略。", this);
            return;
        }

        _bossFlowStage = BossFlowStage.Rules;
        if (OnRulesIntroRequested == null)
        {
            Debug.LogWarning("[PhaseManager] 沒有規則介紹視窗（C 的 RulesIntroWindow 還沒接）。開發者模式可按 9 直接開始 BOSS 戰。", this);
        }
        OnRulesIntroRequested?.Invoke();
    }

    /// <summary>【C 的 RulesIntroWindow 呼叫】玩家點擊關掉規則介紹視窗時 → BOSS 戰開始。只在規則視窗顯示中有效。</summary>
    public void NotifyRulesIntroClosed()
    {
        if (_currentPhase != GamePhase.Phase3 || _bossFlowStage != BossFlowStage.Rules)
        {
            Debug.LogWarning("[PhaseManager] 規則視窗不在顯示中（階段 " + _currentPhase + "，" + _bossFlowStage + "），NotifyRulesIntroClosed 已忽略。", this);
            return;
        }

        StartBossFight();
    }

    /// <summary>
    /// 【只給 A 的 DevTools】跳過登場演出與規則視窗，直接開始 BOSS 戰。只在第三階段、BOSS 戰還沒開始時有效。
    /// </summary>
    public void DevStartBossFight()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.DevMode)
        {
            return;
        }
        if (_currentPhase != GamePhase.Phase3
            || (_bossFlowStage != BossFlowStage.Intro && _bossFlowStage != BossFlowStage.Rules))
        {
            Debug.Log("[PhaseManager] 開發者開始 BOSS 戰：要先進入第三階段（按 2），而且 BOSS 戰還沒開始（目前 "
                      + _currentPhase + "，" + _bossFlowStage + "）。", this);
            return;
        }

        Debug.Log("[PhaseManager] 開發者開始 BOSS 戰（跳過登場演出與規則視窗）。", this);
        StartBossFight();
    }

    /// <summary>【B 的結局演出呼叫】結局動畫播完 → 勝利結算。只在 Ending 階段有效。</summary>
    public void NotifyEndingFinished()
    {
        if (_currentPhase != GamePhase.Ending)
        {
            Debug.Log("[PhaseManager] 不在結局階段（目前 " + _currentPhase + "），NotifyEndingFinished 已忽略。", this);
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.FinishWithVictory();
        }
    }

    /// <summary>
    /// 【只給 A 的 TreasureChest（A17）】寶箱打開 → 進入第三階段。只在第二階段、拿到藏寶圖後有效，只有第一次有效。
    /// 淨化設為 60% 並鎖住；第一次進入時存下快照；觸發 OnPhaseChanged(Phase3)、OnBossIntroRequested。
    /// </summary>
    public void EnterPhase3()
    {
        if (_currentPhase != GamePhase.Phase2 || !_hasTreasureMap)
        {
            Debug.LogWarning("[PhaseManager] 只有第二階段、拿到藏寶圖後才能進入第三階段（目前 " + _currentPhase
                             + "，藏寶圖：" + _hasTreasureMap + "），已忽略。", this);
            return;
        }

        StartPhase3();
    }

    /// <summary>
    /// 【只給 A 的 DevTools】直接跳到第三階段。
    /// 【A11】遊戲進行中（第一、二階段）：在場景內直接切到第三階段，快照用當下真實的 HP。
    /// 【F4／A19】一開場就跳（預設快照、重新載入場景）之後完成。
    /// </summary>
    public void DevJumpToPhase3()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.DevMode)
        {
            return;
        }
        if (gameManager.State != GameState.Playing
            || (_currentPhase != GamePhase.Phase1 && _currentPhase != GamePhase.Phase2))
        {
            Debug.Log("[PhaseManager] 開發者跳到第三階段：目前只支援第一、二階段（目前 " + _currentPhase
                      + "）；一開場就跳過去在 F4／A19 完成。", this);
            return;
        }

        Debug.Log("[PhaseManager] 開發者跳到第三階段（教學與三個事件視為完成）。", this);
        StartPhase3();
    }

    /// <summary>【只給 A 的 DevTools】把 40 秒空檔的剩餘秒數直接設成指定值（不用每次等 40 秒）。</summary>
    public void DevSetEventGapRemaining(float seconds)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.DevMode || !_isInEventGap)
        {
            return;
        }
        _eventGapRemaining = Mathf.Max(0f, seconds);
    }

    private void HandleOpeningFinished(OpeningRoute route)
    {
        // 路線 A（接受香菸）由 RouteManager 重新載入場景；路線 B 開場結束 → 新手教學
        if (route == OpeningRoute.RefuseSmoke)
        {
            BeginTutorialPhase();
        }
    }

    private void HandleProgressChanged(float progress)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        // 一律用整數判斷，避免小數誤差
        int units = gameManager.PurificationUnits;
        CheckMilestones(units);

        if (_currentPhase == GamePhase.Phase1 && units >= _phase1TargetUnits)
        {
            EnterPhase2();
        }
        else if (_currentPhase == GamePhase.Phase3 && gameManager.IsBossDefeated && units >= GameManager.PurificationMax)
        {
            EnterEnding();
        }
    }

    private void CheckMilestones(int units)
    {
        for (int i = 0; i < MilestonePercents.Length; i++)
        {
            if (_milestoneReached[i] || units < MilestonePercents[i] * 10)
            {
                continue;
            }

            _milestoneReached[i] = true;
            Debug.Log("[PhaseManager] 淨化里程碑：" + MilestonePercents[i] + "%", this);
            OnPurificationMilestone?.Invoke(MilestonePercents[i]);
        }
    }

    private void EnterPhase2()
    {
        SetPhase(GamePhase.Phase2);
        TaskHint.SetTask(PhaseHintSource, _phase2Hint);
        RequestStoryEvent(0);
    }

    private void RequestStoryEvent(int index)
    {
        _currentEventIndex = index;
        _eventStage = EventStage.Requested;
        Debug.Log("[PhaseManager] 請求故事事件 " + index + "（NPC 跑來求救）。", this);
        OnStoryEventRequested?.Invoke(index);
    }

    private void StartEventGap(int gapIndex)
    {
        _isInEventGap = true;
        _eventGapIndex = gapIndex;
        _eventGapRemaining = _eventGapSeconds;
        Debug.Log("[PhaseManager] 空檔 " + gapIndex + " 開始（" + _eventGapSeconds + " 秒）。", this);
        OnEventGapStarted?.Invoke(gapIndex);
    }

    private void EndEventGap()
    {
        int gapIndex = _eventGapIndex;
        _isInEventGap = false;
        _eventGapIndex = -1;
        _eventGapRemaining = 0f;
        Debug.Log("[PhaseManager] 空檔 " + gapIndex + " 結束。", this);
        OnEventGapEnded?.Invoke(gapIndex);
        RequestStoryEvent(gapIndex + 1);
    }

    private void SetHelpRefused(bool refused)
    {
        if (_isHelpRefused == refused)
        {
            return;
        }

        _isHelpRefused = refused;
        if (refused)
        {
            TaskHint.SetTask(RefusalHintSource, _refusalHint);
        }
        else
        {
            TaskHint.ClearTask(RefusalHintSource);
        }

        // TODO【A15】拒絕期間每 _refusalGrowInterval 秒倍率 +_refusalGrowStep（最多 _refusalMaxMultiplier），
        //            並呼叫 SmokeSource.SetRefusalState(...)；接下任務後恢復 1。A11 只處理狀態與事件。
        _refusalSmokeMultiplier = 1f;

        OnHelpRefusedChanged?.Invoke(refused);
    }

    private void GrantTreasureMap()
    {
        if (_hasTreasureMap)
        {
            return;
        }

        _hasTreasureMap = true;
        TaskHint.SetTask(PhaseHintSource, _treasureMapHint);
        Debug.Log("[PhaseManager] 拿到藏寶圖。", this);
        OnTreasureMapObtained?.Invoke();
    }

    /// <summary>進入第三階段的共用流程（寶箱、開發者跳關）。</summary>
    private void StartPhase3()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        // 教學與三個事件都視為已完成
        _isInEventGap = false;
        _eventGapIndex = -1;
        _eventGapRemaining = 0f;
        if (_isHelpRefused)
        {
            SetHelpRefused(false);
        }
        _currentEventIndex = StoryEventCount - 1;
        _eventStage = EventStage.Completed;
        _hasTreasureMap = true;

        // 淨化 60% 並鎖住（會觸發 60% 里程碑）
        gameManager.LockPurificationForPhase3();

        // 第一次進入第三階段時存下快照（已經有就不覆蓋）
        Phase3Checkpoint.SaveFirstEntry(CaptureSnapshot(gameManager));

        TaskHint.ClearTask(PhaseHintSource);
        TaskHint.ClearTask(RefusalHintSource);

        SetPhase(GamePhase.Phase3);

        _bossFlowStage = BossFlowStage.Intro;
        BossController boss = BossController.Instance;
        if (boss != null)
        {
            boss.BeginIntro();
        }
        else
        {
            Debug.LogWarning("[PhaseManager] 場景裡沒有 BossController（Boss.prefab），BOSS 戰無法進行。", this);
        }

        if (OnBossIntroRequested == null)
        {
            Debug.LogWarning("[PhaseManager] 沒有 BOSS 登場演出（B 的演出還沒接）。開發者模式可按 9 直接開始 BOSS 戰。", this);
        }
        OnBossIntroRequested?.Invoke();
    }

    /// <summary>BOSS 戰正式開始（規則視窗關掉、開發者按鍵）：OnBossFightStarted → BossController.StartFight()。</summary>
    private void StartBossFight()
    {
        _bossFlowStage = BossFlowStage.Fight;
        Debug.Log("[PhaseManager] BOSS 戰開始。", this);
        OnBossFightStarted?.Invoke();

        if (BossController.Instance != null)
        {
            BossController.Instance.StartFight();
        }
    }

    private Phase3Snapshot CaptureSnapshot(GameManager gameManager)
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        Phase3Snapshot snapshot = new Phase3Snapshot
        {
            Hp = playerHealth != null ? playerHealth.CurrentHp : 100f,
            ElapsedAtEntry = gameManager.ElapsedTime,
            IsDefault = false,
            // TODO【A19／C】C 的 Inventory 提供 GetCount(ItemType) 後，在這裡記下貼片、口嚼錠、噴霧的數量並設 ItemsCaptured = true
            ItemsCaptured = false,
        };
        return snapshot;
    }

    private void EnterEnding()
    {
        if (_currentPhase == GamePhase.Ending)
        {
            return;
        }

        SetPhase(GamePhase.Ending);

        // 沒有人播結局動畫（B17 還沒接）→ 直接勝利結算。B17 監聽後就會改成等 NotifyEndingFinished()
        if (OnEndingRequested == null)
        {
            Debug.Log("[PhaseManager] 沒有結局動畫（沒有人監聽 OnEndingRequested），直接勝利結算。", this);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FinishWithVictory();
            }
            return;
        }
        OnEndingRequested.Invoke();
    }

    private bool IsCurrentEvent(int index, string caller)
    {
        if (_currentPhase != GamePhase.Phase2)
        {
            Debug.LogWarning("[PhaseManager] 不在第二階段（目前 " + _currentPhase + "），" + caller + "(" + index + ") 已忽略。", this);
            return false;
        }
        if (_isInEventGap)
        {
            Debug.LogWarning("[PhaseManager] 空檔中，" + caller + "(" + index + ") 已忽略。", this);
            return false;
        }
        if (index != _currentEventIndex)
        {
            Debug.LogWarning("[PhaseManager] 目前是事件 " + _currentEventIndex + "，" + caller + "(" + index + ") 已忽略。", this);
            return false;
        }
        return true;
    }

    private void SetPhase(GamePhase phase)
    {
        if (_currentPhase == phase)
        {
            return;
        }

        _currentPhase = phase;
        Debug.Log("[PhaseManager] 階段：" + phase, this);
        OnPhaseChanged?.Invoke(phase);
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

        int total = _phase1MonsterUnits * 5 + _eventGapMonsterUnits * 4 + storyTotal * StoryEventCount;
        if (total != GameManager.Phase3PurificationUnits)
        {
            Debug.LogWarning("[PhaseManager] 第一、二階段的淨化值總和是 " + total
                             + "，不是 600（60%）。寶箱可能永遠打不開，請檢查 Inspector 的淨化值。", this);
        }
    }

    private void OnValidate()
    {
        _phase1TargetUnits = Mathf.Max(0, _phase1TargetUnits);
        _eventGapSeconds = Mathf.Max(0f, _eventGapSeconds);
        // 拒絕的三個數值 A15 才會用到；先在這裡讀一次，避免「欄位沒有使用」的警告
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

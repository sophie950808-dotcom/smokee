using System;
using UnityEngine;

/// <summary>
/// BOSS 大煙怪的狀態機：登場 → 天上 12 秒 ⇄ 地面 8 秒 → 菸全部捏熄後永久在地面（Final）→ 被打倒。
/// 掛在 BOSS 根物件，同一個物件還有 BossHealth、BossAttack、SmokeSource（半徑 7）。
/// BOSS 的移動（高度、降落）與縮放由這支腳本控制根物件；B 的模型、特效放在子物件，依事件播放。
/// BOSS 戰開始前（Inactive、Intro）不攻擊、不受傷、不冒煙；遊戲暫停、「暫停計時」中所有計時與攻擊都停。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// 【A14 完成】Inactive → Intro → Ground（A14 一直停在地面）→ Defeated。
/// 【A16】開打先在天上、地面 8 秒 ⇄ 天上 12 秒輪流、體型等級、Final、重新挑戰重設。
/// </summary>
public class BossController : MonoBehaviour
{
    /// <summary>場景中唯一的 BOSS。</summary>
    public static BossController Instance { get; private set; }

    [Header("狀態時間（秒）")]
    [Tooltip("地面停留時間")]
    [SerializeField] private float _groundDuration = 8f;

    [Tooltip("天上停留時間")]
    [SerializeField] private float _skyDuration = 12f;

    [Header("天上高度與體型（血量每降 25% 一級，第 0 級是滿血）")]
    [Tooltip("各級的飛行高度（公尺）【暫定】")]
    [SerializeField] private float[] _skyHeights = { 12f, 10f, 8f, 6f };

    [Tooltip("各級的體型倍率【暫定】")]
    [SerializeField] private float[] _sizeScales = { 1f, 0.85f, 0.7f, 0.55f };

    [Header("位置（B 在 GameScene 拖進來）")]
    [Tooltip("BOSS 在地面時的位置")]
    [SerializeField] private Transform _groundPoint;

    [Header("任務提示文字")]
    [Tooltip("BOSS 戰開始時的任務提示")]
    [SerializeField] private string _fightHint = "捏熄吸菸者的菸，打倒大煙怪！";
    [Tooltip("菸全部捏熄後的任務提示")]
    [SerializeField] private string _finalHint = "打倒大煙怪！";

    [Header("轉向")]
    [Tooltip("BOSS 戰中在地面時，每秒最多轉幾度面向玩家（只轉水平方向）")]
    [SerializeField] private float _turnSpeed = 360f;

    private const string BossHintSource = "Boss";

    private BossState _state = BossState.Inactive;
    private BossHealth _health;
    private BossAttack _attack;
    private SmokeSource _smokeSource;
    private Transform _player;
    private int _sizeLevel = 0;
    private float _stateTimeRemaining = 0f;

    /// <summary>目前 BOSS 狀態。</summary>
    public BossState State => _state;

    /// <summary>體型等級 0–3（血量每降 25% 加一級：飛得更低、體型更小）。</summary>
    public int SizeLevel => _sizeLevel;

    /// <summary>目前狀態還剩幾秒切換（B 可以做預告特效）。</summary>
    public float StateTimeRemaining => _stateTimeRemaining;

    /// <summary>BOSS 狀態改變時觸發。參數：新狀態。B 播起飛、降落、最終局面、倒下的動畫。</summary>
    public event Action<BossState> OnStateChanged;

#pragma warning disable 67 // A16 才會觸發

    /// <summary>體型等級改變時觸發。參數：新等級 0–3。B 換縮小特效。</summary>
    public event Action<int> OnSizeLevelChanged;
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
            Debug.LogWarning("[BossController] 場景中有超過一個 BOSS，請只保留一個。", this);
            return;
        }
        Instance = this;

        _health = GetComponent<BossHealth>();
        _attack = GetComponent<BossAttack>();
        _smokeSource = GetComponent<SmokeSource>();
        if (_health == null || _attack == null || _smokeSource == null)
        {
            Debug.LogWarning("[BossController] BOSS 根物件上缺少 BossHealth／BossAttack／SmokeSource，請用選單 Tools/Smokee/A - A14 建立 BOSS Prefab。", this);
        }

        // BOSS 戰開始前不冒煙（不影響第一、二階段的暴露值）
        if (_smokeSource != null)
        {
            _smokeSource.IsEmitting = false;
        }
        if (_health != null)
        {
            _health.OnBossDefeated += HandleBossDefeated;
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnBossDefeated -= HandleBossDefeated;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        // 暫停選單、「暫停計時」期間狀態計時也要停（A16 的地面／天上輪替用）
        GameManager gameManager = GameManager.Instance;
        bool canAct = gameManager == null || (gameManager.State == GameState.Playing && !gameManager.IsTimerPaused);
        if (!canAct)
        {
            return;
        }

        switch (_state)
        {
            case BossState.Ground:
                UpdateGround();
                break;
            case BossState.Sky:
                UpdateSky();
                break;
            case BossState.Final:
                UpdateFinal();
                break;
        }
    }

    /// <summary>【只給 PhaseManager】開始登場（狀態變成 Intro）。這時 BOSS 還不攻擊、不受傷。</summary>
    public void BeginIntro()
    {
        if (_state != BossState.Inactive)
        {
            Debug.LogWarning("[BossController] 目前是 " + _state + "，只有 Inactive 時能開始登場，已忽略。", this);
            return;
        }
        SetState(BossState.Intro);
    }

    /// <summary>
    /// 【只給 PhaseManager】規則視窗關掉後開始戰鬥：開始冒煙、任務提示「捏熄吸菸者的菸，打倒大煙怪！」。
    /// A14 直接進入地面並一直停在地面；【A16】改成先在天上。只在 Intro 狀態有效。
    /// </summary>
    public void StartFight()
    {
        if (_state != BossState.Intro)
        {
            Debug.LogWarning("[BossController] 目前是 " + _state + "，只有 Intro 時能開始戰鬥，已忽略。", this);
            return;
        }

        if (_groundPoint != null)
        {
            transform.position = _groundPoint.position;
        }
        if (_smokeSource != null)
        {
            _smokeSource.IsEmitting = true;
        }
        TaskHint.SetTask(BossHintSource, _fightHint);

        // TODO【A16】改成 EnterSky()，地面與天上輪流
        SetState(BossState.Ground);
    }

    /// <summary>地面：A14 不切換（一直停在地面），吐菸圈由 BossAttack 依狀態處理。</summary>
    private void UpdateGround()
    {
        FacePlayer(Time.deltaTime);
        // TODO【A16】_stateTimeRemaining 從 _groundDuration 倒數，到 0 起飛到天上
    }

    /// <summary>水平轉向玩家（最多每秒 _turnSpeed 度）。A14 只在地面；【A16】Final 也要轉。</summary>
    private void FacePlayer(float deltaTime)
    {
        if (_player == null)
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }
            _player = playerHealth.transform;
        }

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(toPlayer);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * deltaTime);
    }

    /// <summary>天上。【A16 實作】高度、體型、_skyDuration 倒數後降落。</summary>
    private void UpdateSky()
    {
    }

    /// <summary>最終局面（菸全部捏熄）。【A16 實作】永久在地面、不回血、改用物理攻擊。</summary>
    private void UpdateFinal()
    {
    }

    /// <summary>
    /// 血量歸零：停止所有攻擊、清掉場上菸圈、停止冒煙、狀態 Defeated，最後通知 GameManager（淨化 100%、停止用時 → 結局）。
    /// </summary>
    private void HandleBossDefeated()
    {
        if (_state == BossState.Defeated)
        {
            return;
        }

        if (_attack != null)
        {
            _attack.StopAndClearAll();
        }
        if (_smokeSource != null)
        {
            _smokeSource.IsEmitting = false;
        }
        TaskHint.ClearTask(BossHintSource);
        _stateTimeRemaining = 0f;
        SetState(BossState.Defeated);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyBossDefeated();
        }
        else
        {
            Debug.LogWarning("[BossController] 場景裡沒有 GameManager，打倒 BOSS 後不會結算。", this);
        }
    }

    private void SetState(BossState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        Debug.Log("[BossController] BOSS 狀態：" + state, this);
        OnStateChanged?.Invoke(state);
    }

    private void OnValidate()
    {
        _groundDuration = Mathf.Max(0.1f, _groundDuration);
        _skyDuration = Mathf.Max(0.1f, _skyDuration);
        _turnSpeed = Mathf.Max(0f, _turnSpeed);
        if (_skyHeights == null || _skyHeights.Length != 4 || _sizeScales == null || _sizeScales.Length != 4)
        {
            Debug.LogWarning("[BossController] 天上高度與體型都要剛好 4 級。", this);
        }
        if (string.IsNullOrEmpty(_fightHint) || string.IsNullOrEmpty(_finalHint))
        {
            Debug.LogWarning("[BossController] 任務提示文字是空的。", this);
        }
    }
}

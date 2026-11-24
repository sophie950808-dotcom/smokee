using System;
using UnityEngine;

/// <summary>
/// BOSS 大煙怪的狀態機：登場 → 天上 12 秒 ⇄ 地面 8 秒 → 菸全部捏熄後永久在地面（Final）→ 被打倒。
/// 掛在 BOSS 根物件，同一個物件還有 BossHealth、BossAttack、SmokeSource（半徑 7）。
/// BOSS 的移動（高度、降落）與縮放由這支腳本控制根物件；B 的模型、特效放在子物件，依事件播放。
/// 規格見 docs/INTERFACES.md 第 14 節。
/// 【A10 空殼】名稱、參數、Inspector 數值已定案；地面版在 A14、天上版與 Final 在 A16 完成。
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

    private BossState _state = BossState.Inactive;
    private int _sizeLevel = 0;
    private float _stateTimeRemaining = 0f;

    /// <summary>目前 BOSS 狀態。</summary>
    public BossState State => _state;

    /// <summary>體型等級 0–3（血量每降 25% 加一級：飛得更低、體型更小）。</summary>
    public int SizeLevel => _sizeLevel;

    /// <summary>目前狀態還剩幾秒切換（B 可以做預告特效）。</summary>
    public float StateTimeRemaining => _stateTimeRemaining;

#pragma warning disable 67 // A14、A16 才會觸發
    /// <summary>BOSS 狀態改變時觸發。參數：新狀態。B 播起飛、降落、最終局面、倒下的動畫。</summary>
    public event Action<BossState> OnStateChanged;

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
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>【只給 PhaseManager】開始登場（狀態變成 Intro）。【A14 完成】</summary>
    public void BeginIntro()
    {
        Debug.Log("[BossController] （A10 空殼）BeginIntro，A14 完成。地面位置："
                  + (_groundPoint != null ? _groundPoint.name : "未設定"), this);
    }

    /// <summary>【只給 PhaseManager】規則視窗關掉後開始戰鬥（先在天上）。【A14 完成】</summary>
    public void StartFight()
    {
        Debug.Log("[BossController] （A10 空殼）StartFight，A14 完成。", this);
    }

    private void OnValidate()
    {
        _groundDuration = Mathf.Max(0.1f, _groundDuration);
        _skyDuration = Mathf.Max(0.1f, _skyDuration);
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

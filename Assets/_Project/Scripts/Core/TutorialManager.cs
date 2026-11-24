using System;
using UnityEngine;

/// <summary>
/// 新手教學 4 步驟：① 按 V 切換視角 ② WASD 走到車庫外的發光位置 ③ 走到教學菸怪旁 ④ 左鍵擊敗它。
/// 每一步只能做指定的動作（滑鼠視角、跳躍、奔跑全程可用；E、道具教學中都鎖住）。
/// 教學期間 HP 歸零不進結算：HP 補滿、暴露值歸零，從當下那一步繼續。
/// 由 PhaseManager 呼叫 BeginTutorial()。規格見 docs/INTERFACES.md 第 5 節。
/// 【A10 空殼】名稱、參數、Inspector 欄位已定案；流程在 A12 完成。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    /// <summary>場景中唯一的 TutorialManager。</summary>
    public static TutorialManager Instance { get; private set; }

    [Header("場景引用（B 在 GameScene 拖進來）")]
    [Tooltip("第 2 步：車庫外的發光位置")]
    [SerializeField] private Transform _moveTarget;

    [Tooltip("第 3、4 步：教學菸怪（淨化值 0）")]
    [SerializeField] private SmokeMonster _tutorialMonster;

    [Header("判定範圍（公尺）")]
    [Tooltip("第 2 步：走到發光位置多近算到達【暫定】")]
    [SerializeField] private float _moveTargetRadius = 1.5f;

    [Tooltip("第 3 步：走到教學菸怪多近算完成【暫定】")]
    [SerializeField] private float _approachRadius = 3f;

    [Header("任務提示文字")]
    [Tooltip("第 1 步的任務提示")]
    [SerializeField] private string _switchViewHint = "按 V 鍵切換視角";
    [Tooltip("第 2 步的任務提示")]
    [SerializeField] private string _moveHint = "試著按 WASD 走走看看吧！";
    [Tooltip("第 3 步的任務提示")]
    [SerializeField] private string _approachHint = "試著走到小煙怪旁邊！";
    [Tooltip("第 4 步的任務提示")]
    [SerializeField] private string _attackHint = "按下左鍵消滅菸怪！";

    private TutorialStep _currentStep = TutorialStep.None;

    /// <summary>目前的教學步驟。</summary>
    public TutorialStep CurrentStep => _currentStep;

    /// <summary>教學是否進行中。</summary>
    public bool IsActive => _currentStep != TutorialStep.None && _currentStep != TutorialStep.Done;

    /// <summary>第 2 步的目標位置（B 的發光效果可以讀）。</summary>
    public Transform MoveTarget => _moveTarget;

    /// <summary>教學菸怪（B 的發光效果可以讀）。</summary>
    public SmokeMonster TutorialMonster => _tutorialMonster;

#pragma warning disable 67 // A12 才會觸發
    /// <summary>教學步驟改變時觸發。參數：新步驟。B 依步驟切換發光提示。</summary>
    public event Action<TutorialStep> OnStepChanged;

    /// <summary>教學完成時觸發。</summary>
    public event Action OnTutorialFinished;

    /// <summary>教學期間 HP 歸零、補滿後從當下那一步繼續時觸發（B 可以重新播發光提示）。</summary>
    public event Action OnTutorialRestarted;
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
            Debug.LogWarning("[TutorialManager] 場景中有超過一個 TutorialManager，請只保留一個。", this);
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

    /// <summary>【只給 PhaseManager】開始新手教學（從第 1 步）。【A12 完成】</summary>
    public void BeginTutorial()
    {
        Debug.Log("[TutorialManager] （A10 空殼）BeginTutorial，A12 完成。", this);
    }

    private void OnValidate()
    {
        _moveTargetRadius = Mathf.Max(0.1f, _moveTargetRadius);
        _approachRadius = Mathf.Max(0.1f, _approachRadius);
        if (string.IsNullOrEmpty(_switchViewHint) || string.IsNullOrEmpty(_moveHint)
            || string.IsNullOrEmpty(_approachHint) || string.IsNullOrEmpty(_attackHint))
        {
            Debug.LogWarning("[TutorialManager] 有任務提示文字是空的。", this);
        }
    }
}

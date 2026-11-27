using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>通用幫忙選擇視窗；只回傳選擇，不執行故事或拒絕懲罰。</summary>
public sealed class HelpPromptUI : MonoBehaviour
{
    [Header("可替換的介面引用")]
    [Tooltip("開關的子面板；根物件需保持啟用。")]
    [SerializeField] private GameObject _panel;
    [Tooltip("外部傳入的問題文字。")]
    [SerializeField] private TMP_Text _questionText;
    [Tooltip("幫忙按鈕；由程式註冊，不要再手動重複綁定 OnClick。")]
    [SerializeField] private Button _acceptButton;
    [Tooltip("拒絕按鈕；由程式註冊，不要再手動重複綁定 OnClick。")]
    [SerializeField] private Button _refuseButton;

    private const string LockKey = "HelpPrompt";
    private Action _onAccept;
    private Action _onRefuse;
    private PlayerController _lockedPlayer;
    private GameManager _lockedManager;
    private bool _releasePending;
    private int _shownFrame;

    /// <summary>場景中唯一的幫忙選擇視窗。</summary>
    public static HelpPromptUI Instance { get; private set; }
    /// <summary>是否正在等待玩家選擇。</summary>
    public bool IsShowing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Debug.LogError("HelpPromptUI: only one instance is allowed in a scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
        if (_acceptButton) _acceptButton.onClick.AddListener(Accept);
        if (_refuseButton) _refuseButton.onClick.AddListener(Refuse);
        if (_panel) _panel.SetActive(false);
    }

    /// <summary>顯示任意問題；選擇後只呼叫對應回呼一次。請在對話完成後呼叫。</summary>
    public void Show(string question, Action onAccept, Action onRefuse)
    {
        if (!isActiveAndEnabled || Instance != this)
            throw new InvalidOperationException("HelpPromptUI must be active and unique.");
        if (IsShowing)
            throw new InvalidOperationException("Finish the current help prompt before calling Show again.");
        if (DialogueUI.Instance && DialogueUI.Instance.IsShowing)
            throw new InvalidOperationException("Show the help prompt from the dialogue completion callback.");
        if (!_panel || _panel == gameObject || !_questionText || !_acceptButton || !_refuseButton)
            throw new InvalidOperationException("HelpPromptUI references must be assigned to child UI objects.");

        _onAccept = onAccept;
        _onRefuse = onRefuse;
        _shownFrame = Time.frameCount;
        _releasePending = false;
        _lockedPlayer = _lockedPlayer ? _lockedPlayer : FindFirstObjectByType<PlayerController>();
        _lockedManager = _lockedManager ? _lockedManager : GameManager.Instance;
        if (_lockedPlayer) _lockedPlayer.SetControlLock(LockKey, true);
        if (_lockedManager) _lockedManager.SetTimerPaused(LockKey, true);
        _questionText.text = question ?? string.Empty;
        IsShowing = true;
        _acceptButton.interactable = _refuseButton.interactable = false;
        _panel.SetActive(true);
    }

    private bool CanChoose => IsShowing && Time.frameCount > _shownFrame && Time.timeScale > 0
        && (!GameManager.Instance || GameManager.Instance.State != GameState.Paused);

    private void Update()
    {
        if (!IsShowing) return;
        bool allowed = CanChoose;
        _acceptButton.interactable = allowed;
        _refuseButton.interactable = allowed;
    }

    private void Accept() => Choose(true);
    private void Refuse() => Choose(false);

    private void Choose(bool accepted)
    {
        if (!CanChoose) return;
        Action callback = accepted ? _onAccept : _onRefuse;
        _onAccept = _onRefuse = null;
        IsShowing = false;
        _acceptButton.interactable = _refuseButton.interactable = false;
        _panel.SetActive(false);
        _releasePending = true;
        // 先清理再通知，回呼可安全開啟下一段對話或下一個選擇視窗。
        callback?.Invoke();
    }

    private void LateUpdate()
    {
        // 留到本幀 Update 全部結束，避免這次選擇同時觸發玩家攻擊。
        if (_releasePending && !IsShowing) ReleaseLocks();
    }

    private void ReleaseLocks()
    {
        if (_lockedPlayer) _lockedPlayer.SetControlLock(LockKey, false);
        if (_lockedManager) _lockedManager.SetTimerPaused(LockKey, false);
        _lockedPlayer = null;
        _lockedManager = null;
        _releasePending = false;
    }

    private void OnDisable()
    {
        // 停用表示中止，不擅自替玩家選擇，也不執行舊回呼。
        IsShowing = false;
        _onAccept = _onRefuse = null;
        if (_panel) _panel.SetActive(false);
        ReleaseLocks();
    }

    private void OnDestroy()
    {
        if (_acceptButton) _acceptButton.onClick.RemoveListener(Accept);
        if (_refuseButton) _refuseButton.onClick.RemoveListener(Refuse);
        if (Instance == this) Instance = null;
    }
}

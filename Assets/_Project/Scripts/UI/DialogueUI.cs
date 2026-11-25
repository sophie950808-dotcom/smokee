using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>通用對話播放器；不保存故事流程，也不直接修改游標。</summary>
[DefaultExecutionOrder(-100)]
public sealed class DialogueUI : MonoBehaviour
{
    [Header("可替換的 UI 元件")]
    [Tooltip("只有這個子物件會在對話結束時隱藏；根物件須保持啟用。")]
    [SerializeField] private GameObject _dialoguePanel;
    [Tooltip("角色名稱文字；旁白時隱藏。")]
    [SerializeField] private TMP_Text _speakerText;
    [Tooltip("當前一句的台詞文字。")]
    [SerializeField] private TMP_Text _dialogueText;

    private const string LockKey = "Dialogue";
    private DialogueLine[] _lines;
    private int _index;
    private int _shownFrame;
    private int _advancedFrame = -1;
    private Action _onFinished;
    private PlayerController _lockedPlayer;
    private GameManager _lockedManager;
    private bool _releasePending;

    /// <summary>場景中唯一的對話播放器。</summary>
    public static DialogueUI Instance { get; private set; }
    /// <summary>目前是否正在顯示對話。</summary>
    public bool IsShowing { get; private set; }
    /// <summary>開始播放非空對話後通知。</summary>
    public event Action OnDialogueStarted;
    /// <summary>全部播放完、隱藏面板後通知。</summary>
    public event Action OnDialogueFinished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Debug.LogError("DialogueUI：場景只允許一個對話播放器。", this);
            enabled = false;
            return;
        }
        Instance = this;
        if (_dialoguePanel) _dialoguePanel.SetActive(false);
    }

    /// <summary>播放一組角色台詞；完成後回呼一次。空資料立即完成。</summary>
    public void Show(IReadOnlyList<DialogueLine> lines, Action onFinished)
    {
        if (!isActiveAndEnabled || Instance != this)
            throw new InvalidOperationException("DialogueUI 必須啟用且為場景中的唯一實例。");
        if (IsShowing)
            throw new InvalidOperationException("請等目前對話完成後再 Show，避免覆蓋尚未完成的回呼。");
        if (!_dialoguePanel || _dialoguePanel == gameObject || !_speakerText || !_dialogueText)
            throw new InvalidOperationException("DialogueUI 的子面板、角色名稱或台詞引用尚未正確設定。");

        if (lines == null || lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }
        _lines = new DialogueLine[lines.Count];
        for (int i = 0; i < lines.Count; i++) _lines[i] = lines[i];
        _onFinished = onFinished;
        _index = 0;
        _shownFrame = Time.frameCount;
        _releasePending = false;
        _lockedPlayer = _lockedPlayer ? _lockedPlayer : FindFirstObjectByType<PlayerController>();
        _lockedManager = _lockedManager ? _lockedManager : GameManager.Instance;
        if (_lockedPlayer) _lockedPlayer.SetControlLock(LockKey, true);
        if (_lockedManager) _lockedManager.SetTimerPaused(LockKey, true);
        IsShowing = true;
        DisplayLine();
        _dialoguePanel.SetActive(true);
        OnDialogueStarted?.Invoke();
    }

    /// <summary>使用資料檔播放對話；正式資料可於 C11 補入。</summary>
    public void Show(DialogueData data, Action onFinished)
    {
        Show(data ? data.Lines : null, onFinished);
    }

    private void Update()
    {
        if (!IsShowing || Time.timeScale == 0 || Time.frameCount == _shownFrame
            || Time.frameCount == _advancedFrame) return;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        // 唯一的輸入入口：不再同時訂閱 Button.onClick，兩鍵同幀也只前進一句。
        if (click || space) Advance();
    }

    private void Advance()
    {
        _advancedFrame = Time.frameCount;
        if (++_index < _lines.Length)
        {
            DisplayLine();
            return;
        }
        Action callback = _onFinished;
        _onFinished = null;
        _lines = null;
        IsShowing = false;
        _dialoguePanel.SetActive(false);
        _releasePending = true;
        // 先清理再通知，讓回呼可安全地 Show 下一組。
        try { OnDialogueFinished?.Invoke(); }
        finally { callback?.Invoke(); }
    }

    private void DisplayLine()
    {
        DialogueLine line = _lines[_index];
        _speakerText.text = line.Speaker ?? string.Empty;
        _speakerText.gameObject.SetActive(!string.IsNullOrEmpty(line.Speaker));
        _dialogueText.text = line.Text ?? string.Empty;
    }

    private void LateUpdate()
    {
        // 保留控制鎖至本幀所有 Update 完成，避免最後一次翻頁也變成攻擊或跳躍。
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
        // 被移除或停用代表中止，不把未播放完畢誤報為完成。
        IsShowing = false;
        _onFinished = null;
        _lines = null;
        if (_dialoguePanel) _dialoguePanel.SetActive(false);
        ReleaseLocks();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

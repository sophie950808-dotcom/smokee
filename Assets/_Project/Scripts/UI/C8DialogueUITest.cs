using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>只用於 TestScene_C 的虛構對話與輸入驗證。</summary>
public sealed class C8DialogueUITest : MonoBehaviour
{
    [Header("C8 測試引用")]
    [Tooltip("場景中的通用對話框。")]
    [SerializeField] private DialogueUI _dialogueUI;
    [Tooltip("顯示測試進度與回呼次數。")]
    [SerializeField] private TMP_Text _status;
    [Tooltip("對話框的子面板。")]
    [SerializeField] private GameObject _panel;
    [Tooltip("角色名稱。")]
    [SerializeField] private TMP_Text _speaker;
    [Tooltip("當前台詞。")]
    [SerializeField] private TMP_Text _text;
    private bool _testing;
    private int _callbacks;

    private static DialogueLine[] Sample() => new[]
    {
        new DialogueLine { Speaker = "測試角色甲", Text = "測試第一句：同一個框可以播放任意對話。" },
        new DialogueLine { Speaker = "測試角色乙", Text = "測試第二句：現在換另一位角色說話。" },
        new DialogueLine { Speaker = "", Text = "測試第三句：這是旁白，角色名稱應隱藏。" }
    };

    /// <summary>開始人工點擊驗收，可在結束後重複操作。</summary>
    public void PlaySample()
    {
        if (_testing || _dialogueUI.IsShowing) return;
        _status.text = "左鍵或空白鍵逐句播放；回呼次數：" + _callbacks;
        _dialogueUI.Show(Sample(), () =>
        {
            _callbacks++;
            _status.text = "播放完成；累計回呼：" + _callbacks + "。可重新播放。";
        });
    }

    /// <summary>在 Play Mode 執行真實 Input System 輸入與回呼測試。</summary>
    public void RunChecks()
    {
        if (_testing || _dialogueUI.IsShowing) return;
        StartCoroutine(CheckSequence());
    }

    private IEnumerator CheckSequence()
    {
        _testing = true;
        int starts = 0, finishes = 0, callbacks = 0;
        Action started = () => starts++;
        Action finished = () => finishes++;
        Mouse previousMouse = Mouse.current;
        Keyboard previousKeyboard = Keyboard.current;
        Mouse mouse = InputSystem.AddDevice<Mouse>("C8TestMouse");
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>("C8TestKeyboard");
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        _dialogueUI.OnDialogueStarted += started;
        _dialogueUI.OnDialogueFinished += finished;
        float originalScale = Time.timeScale;
        BaseInputModule uiInput = EventSystem.current ? EventSystem.current.currentInputModule : null;
        bool uiInputEnabled = uiInput && uiInput.enabled;
        try
        {
            // 測試的是 DialogueUI 的原始輸入；避免合成點擊誤觸場景內既有 C2 按鈕。
            if (uiInput) uiInput.enabled = false;
            _status.text = "自動驗證中，請暫時不要操作滑鼠與鍵盤。";
            yield return null;
            _dialogueUI.Show(Sample(), () => callbacks++);
            Require(_dialogueUI.IsShowing && _panel.activeSelf && _speaker.text == "測試角色甲", "首句與角色");
            yield return null;
            yield return Click(mouse);
            Require(_text.text.StartsWith("測試第二句") && _speaker.text == "測試角色乙" && callbacks == 0, "一次點擊只前進一句");
            // 按住不應重複翻頁。
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            Require(_text.text.StartsWith("測試第三句") && !_speaker.gameObject.activeSelf, "旁白隱藏角色名");
            yield return null;
            yield return null;
            Require(_dialogueUI.IsShowing && callbacks == 0, "按住不連翻");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;

            Time.timeScale = 0;
            yield return Click(mouse);
            Require(_dialogueUI.IsShowing && callbacks == 0, "暫停不翻頁");
            Time.timeScale = originalScale;
            yield return Space(keyboard);
            Require(!_dialogueUI.IsShowing && !_panel.activeSelf && callbacks == 1 && finishes == 1, "結束隱藏且只回呼一次");
            yield return Click(mouse);
            Require(callbacks == 1, "結束後多點不再回呼");

            data.Lines.Add(new DialogueLine { Speaker = "測試角色丙", Text = "資料容器的單句測試。" });
            _dialogueUI.Show(data, () => callbacks++);
            yield return null;
            // 同幀左鍵與空白鍵仍只能完成一次。
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            Require(callbacks == 2 && finishes == 2 && !_dialogueUI.IsShowing, "資料容器與雙輸入");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;

            _dialogueUI.Show(data, () =>
            {
                callbacks++;
                _dialogueUI.Show(data, () => callbacks++);
            });
            yield return null;
            yield return Click(mouse);
            Require(_dialogueUI.IsShowing && callbacks == 3, "回呼內接續 Show");
            yield return Click(mouse);
            Require(!_dialogueUI.IsShowing && callbacks == 4, "接續對話完成");
            _dialogueUI.Show(Array.Empty<DialogueLine>(), () => callbacks++);
            _dialogueUI.Show((DialogueData)null, () => callbacks++);
            Require(callbacks == 6 && starts == 4 && finishes == 4 && !_panel.activeSelf, "空資料立即完成且不遺留面板");
            _status.text = "C8 自動驗證通過。可點選播放測試進行人工驗收。";
            Debug.Log("C8_AUTOTEST_PASS: speakers, narration, click, space, hold, pause, simultaneous input, repeat, chained Show, empty data and single callback.", this);
        }
        finally
        {
            Time.timeScale = originalScale;
            _dialogueUI.OnDialogueStarted -= started;
            _dialogueUI.OnDialogueFinished -= finished;
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (uiInput) uiInput.enabled = uiInputEnabled;
            Destroy(data);
            _testing = false;
        }
    }

    private static IEnumerator Click(Mouse mouse)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return null;
    }

    private static IEnumerator Space(Keyboard keyboard)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("C8_AUTOTEST_FAIL：" + label);
    }
}

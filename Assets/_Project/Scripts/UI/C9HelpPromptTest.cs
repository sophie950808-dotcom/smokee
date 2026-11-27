using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>TestScene_C 專用測試：真實 UI 事件、E 互動、既有 HUD 與操作鎖。</summary>
public sealed class C9HelpPromptTest : MonoBehaviour
{
    [Header("C9 測試引用")]
    [Tooltip("選擇視窗。")]
    [SerializeField] private HelpPromptUI _ui;
    [Tooltip("視窗子面板。")]
    [SerializeField] private GameObject _panel;
    [Tooltip("問題文字。")]
    [SerializeField] private TMP_Text _question;
    [Tooltip("幫忙按鈕。")]
    [SerializeField] private Button _accept;
    [Tooltip("拒絕按鈕。")]
    [SerializeField] private Button _refuse;
    [Tooltip("沿用 C7 的測試玩家；不建立第二個玩家。")]
    [SerializeField] private PlayerController _player;
    [Tooltip("A 的既有 E 互動元件。")]
    [SerializeField] private PlayerInteractor _interactor;
    [Tooltip("既有 HUD 的唯一互動提示文字。")]
    [SerializeField] private TMP_Text _prompt;
    [Tooltip("第一個測試互動目標。")]
    [SerializeField] private C9TestInteractable _targetA;
    [Tooltip("第二個測試互動目標。")]
    [SerializeField] private C9TestInteractable _targetB;
    [Tooltip("顯示測試結果。")]
    [SerializeField] private TMP_Text _status;
    private bool _testing;

    /// <summary>人工驗收選擇視窗；問題是測試內容。</summary>
    public void ShowDemo()
    {
        if (_testing || _ui.IsShowing || (DialogueUI.Instance && DialogueUI.Instance.IsShowing)) return;
        _ui.Show("這是 C9 測試問題，願意幫忙嗎？",
            () => _status.text = "選擇：幫忙。可再次開啟測試視窗。",
            () => _status.text = "選擇：拒絕。可再次開啟測試視窗。");
    }

    /// <summary>把測試目標放在既有玩家旁，供真實 E 鍵測試。</summary>
    public void PrepareInteraction()
    {
        if (_testing || _ui.IsShowing) return;
        PreparePlayer();
        PlaceTargets(true);
        _status.text = "點一下 Game 畫面後按 E；拒絕後可再次按 E 詢問。";
    }

    /// <summary>從新一輪 Play 執行自動驗證。</summary>
    public void RunChecks()
    {
        if (_testing || _ui.IsShowing || (DialogueUI.Instance && DialogueUI.Instance.IsShowing)) return;
        StartCoroutine(CheckSequence());
    }

    private void PreparePlayer()
    {
        if (GameManager.Instance.State == GameState.Opening) GameManager.Instance.BeginRun();
        _player.SetControlEnabled(true);
    }

    private void PlaceTargets(bool first)
    {
        Vector3 origin = _player.transform.position + Vector3.up * 1.2f;
        _targetA.transform.position = origin + Vector3.forward * (first ? 1 : 20);
        _targetB.transform.position = origin + Vector3.forward * (first ? 20 : 1);
        _targetA.SetAvailable(true);
        _targetB.SetAvailable(true);
        Physics.SyncTransforms();
    }

    private IEnumerator CheckSequence()
    {
        _testing = true;
        bool originallyControlled = _player.IsControlEnabled;
        CursorLockMode originalLock = Cursor.lockState;
        bool originalVisible = Cursor.visible;
        Vector3 aPosition = _targetA.transform.position, bPosition = _targetB.transform.position;
        Mouse previousMouse = Mouse.current;
        Keyboard previousKeyboard = Keyboard.current;
        Mouse mouse = InputSystem.AddDevice<Mouse>("C9TestMouse");
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>("C9TestKeyboard");
        BaseInputModule module = EventSystem.current ? EventSystem.current.currentInputModule : null;
        bool moduleEnabled = module && module.enabled;
        int accepts = 0, refuses = 0;
        try
        {
            if (module) module.enabled = false; // 指標事件由測試精確送給實際 Button，避免誤觸舊 UI。
            PreparePlayer();
            _status.text = "C9 自動驗證中，請暫時不要操作滑鼠與鍵盤。";
            _ui.Show("測試問題甲", () => accepts++, () => refuses++);
            _accept.onClick.Invoke();
            Require(_ui.IsShowing && accepts == 0, "opening frame cannot choose");
            yield return null;
            Require(_panel.activeSelf && _question.text == "測試問題甲" && !_player.IsControlEnabled
                && Cursor.visible && Cursor.lockState == CursorLockMode.None
                && GameManager.Instance.TimerPauseKeys.Contains("HelpPrompt"), "display and locks");
            float time = GameManager.Instance.ElapsedTime;
            yield return new WaitForSecondsRealtime(0.1f);
            Require(GameManager.Instance.ElapsedTime == time, "timer stopped");
            PointerClick(_accept);
            _accept.onClick.Invoke();
            _refuse.onClick.Invoke();
            Require(accepts == 1 && refuses == 0 && !_ui.IsShowing && !_panel.activeSelf
                && !_player.IsActionAllowed(PlayerAction.Attack), "accept once, hide and retain same-frame lock");
            yield return null;
            Require(_player.IsControlEnabled && !GameManager.Instance.TimerPauseKeys.Contains("HelpPrompt"), "release own locks");

            _player.SetControlLock("C9External", true);
            GameManager.Instance.SetTimerPaused("C9External", true);
            _ui.Show("測試問題乙", () => accepts++, () => refuses++);
            yield return null;
            PointerClick(_refuse);
            _accept.onClick.Invoke();
            yield return null;
            Require(refuses == 1 && accepts == 1 && !_player.IsControlEnabled
                && GameManager.Instance.TimerPauseKeys.Contains("C9External")
                && !GameManager.Instance.TimerPauseKeys.Contains("HelpPrompt"), "refuse once and preserve other owner locks");
            _player.SetControlLock("C9External", false);
            GameManager.Instance.SetTimerPaused("C9External", false);

            _ui.Show("暫停測試", () => accepts++, () => refuses++);
            yield return null;
            GameManager.Instance.SetPaused(true);
            yield return null;
            _accept.onClick.Invoke();
            Require(_ui.IsShowing && accepts == 1, "pause blocks selection");
            GameManager.Instance.SetPaused(false);
            yield return null;
            PointerClick(_accept);
            yield return null;

            _ui.Show("接續測試", () =>
            {
                accepts++;
                _ui.Show("回呼內的新問題", () => accepts++, () => refuses++);
            }, () => refuses++);
            yield return null;
            PointerClick(_accept);
            _refuse.onClick.Invoke();
            Require(_ui.IsShowing && _question.text == "回呼內的新問題" && refuses == 1, "reentrant Show protected from repeated click");
            yield return null;
            PointerClick(_refuse);
            yield return null;
            Require(accepts == 3 && refuses == 2, "new callback replaces old callback");

            _ui.Show("中止測試", () => accepts++, () => refuses++);
            yield return null;
            _ui.enabled = false;
            Require(!_ui.IsShowing && !_panel.activeSelf && !GameManager.Instance.TimerPauseKeys.Contains("HelpPrompt"), "disable cancels and releases");
            _ui.enabled = true;
            _ui.Show("重新啟用測試", () => accepts++, () => refuses++);
            yield return null;
            PointerClick(_accept);
            yield return null;
            Require(accepts == 4 && refuses == 2, "no duplicated listener after enable");

            // 由 A 的 PlayerInteractor 真的尋找目標，再由既有 HUD 顯示字串。
            PlaceTargets(true);
            yield return null;
            Require(ReferenceEquals(_interactor.CurrentTarget, _targetA) && _prompt.enabled
                && _prompt.text == _targetA.InteractPrompt, "existing HUD shows target A");
            PlaceTargets(false);
            yield return null;
            Require(ReferenceEquals(_interactor.CurrentTarget, _targetB) && _prompt.text == _targetB.InteractPrompt, "target switch updates prompt");
            _targetB.SetAvailable(false);
            yield return null;
            Require(_interactor.CurrentTarget == null && !_prompt.enabled, "invalid target hides prompt");
            PlaceTargets(true);
            yield return null;
            int oldInteractions = _targetA.InteractionCount;
            yield return PressE(keyboard);
            Require(_ui.IsShowing && !_prompt.enabled && _targetA.InteractionCount == oldInteractions + 1, "E opens prompt and hides HUD hint");

            // 視窗中滾輪、右鍵不能改變背包，也不能攻擊或互動。
            Inventory inventory = Inventory.Instance;
            ItemRewards.Grant(ItemType.NicotinePatch);
            ItemRewards.Grant(ItemType.NicotineGum);
            ItemType? selection = inventory.SelectedItem;
            int patchCount = inventory.GetCount(ItemType.NicotinePatch);
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) }.WithButton(MouseButton.Right));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
            Require(inventory.SelectedItem == selection && inventory.GetCount(ItemType.NicotinePatch) == patchCount
                && !_player.IsActionAllowed(PlayerAction.Attack) && !_player.IsActionAllowed(PlayerAction.Interact), "modal input blocks inventory, attack and E");
            int oldRefuses = _targetA.RefuseCount;
            PointerClick(_refuse);
            yield return null;
            yield return PressE(keyboard);
            Require(_ui.IsShowing && _targetA.InteractionCount == oldInteractions + 2 && _targetA.RefuseCount == oldRefuses + 1, "refusal allows E to ask again");
            PointerClick(_accept);
            yield return null;

            _targetA.transform.position += Vector3.forward * 20;
            Physics.SyncTransforms();
            yield return null;
            Require(_interactor.CurrentTarget == null && !_prompt.enabled, "leaving range hides prompt");
            DialogueUI.Instance.Show(new[] { new DialogueLine { Speaker = "測試", Text = "測試第一句：同一個框可以播放任意對話。" } },
                () => _ui.Show("對話後的選擇", () => accepts++, () => refuses++));
            yield return null;
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Require(!DialogueUI.Instance.IsShowing && _ui.IsShowing && _question.text == "對話後的選擇", "C8 completion opens C9");
            PointerClick(_accept);
            yield return null;
            Require(accepts == 5 && refuses == 2 && !_ui.IsShowing, "final callbacks");
            _status.text = "C9 自動驗證通過：選擇、回呼、互動提示與操作鎖定。";
            Debug.Log("C9_AUTOTEST_PASS: button raycasts, accept/refuse once, repeat/reentrant Show, pause, lifecycle cleanup, own locks, timer, HUD targets, E/retry, inventory/attack locks and C8-to-C9 handoff.", this);
        }
        finally
        {
            if (_ui.IsShowing) { _ui.enabled = false; _ui.enabled = true; }
            if (GameManager.Instance.State == GameState.Paused) GameManager.Instance.SetPaused(false);
            _player.SetControlLock("C9External", false);
            GameManager.Instance.SetTimerPaused("C9External", false);
            _targetA.transform.position = aPosition;
            _targetB.transform.position = bPosition;
            _targetA.SetAvailable(true);
            _targetB.SetAvailable(true);
            Physics.SyncTransforms();
            _player.SetControlEnabled(originallyControlled);
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (module) module.enabled = moduleEnabled;
            Cursor.lockState = originalLock;
            Cursor.visible = originalVisible;
            _testing = false;
        }
    }

    private static IEnumerator PressE(Keyboard keyboard)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private static void PointerClick(Button button)
    {
        Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransform rect = (RectTransform)button.transform;
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center))
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "button is reachable above other UI");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("C9_AUTOTEST_FAIL: " + message);
    }
}

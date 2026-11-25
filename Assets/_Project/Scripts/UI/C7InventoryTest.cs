using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>只供 TestScene_C 驗證背包與 A 的道具介面；不保存正式遊戲流程。</summary>
public sealed class C7InventoryTest : MonoBehaviour
{
    [Header("C7 測試引用")]
    [Tooltip("測試背包。")]
    [SerializeField] private Inventory _inventory;
    [Tooltip("測試場景的玩家實例，不是共用 Prefab 資產。")]
    [SerializeField] private PlayerController _player;
    [Tooltip("同一個測試玩家的 HP。")]
    [SerializeField] private PlayerHealth _health;
    [Tooltip("同一個測試玩家的道具效果。")]
    [SerializeField] private PlayerEffects _effects;
    [Tooltip("用來設定受控測試階段；不執行故事流程。")]
    [SerializeField] private PhaseManager _phase;
    [Tooltip("測試結果文字。")]
    [SerializeField] private TMP_Text _status;
    [Tooltip("背包噴霧冷卻顯示。")]
    [SerializeField] private Image _cooldownFill;
    private bool _running;

    /// <summary>取得三種測試道具，供滾輪與右鍵人工驗收。</summary>
    public void GiveDemoItems()
    {
        if (_running) return;
        PreparePlayer();
        SetTestPhase(GamePhase.Phase3);
        foreach (ItemType type in new[] { ItemType.NicotinePatch, ItemType.NicotineGum, ItemType.NicotineSpray })
            ItemRewards.Grant(type);
        _health.TakeDamage(30);
        _status.text = "C7 測試：滾輪切換、右鍵使用；貼片回血，口嚼錠用後減少，噴霧可重複使用。";
    }

    /// <summary>以合成滑鼠輸入驗證取得、選取、消耗、冷卻與鎖定。</summary>
    public void RunChecks()
    {
        if (_running) return;
        if (_inventory.SelectedItem != null)
        {
            _status.text = "請停止後重新按 Play，再執行自動驗證。";
            return;
        }
        StartCoroutine(CheckSequence());
    }

    private void PreparePlayer()
    {
        if (GameManager.Instance.State == GameState.Opening) GameManager.Instance.BeginRun();
        _player.SetControlEnabled(true);
    }

    private void SetTestPhase(GamePhase value)
    {
        // 測試夾具只切換記憶體內的階段，避免觸發正式故事、BOSS 或快照流程。
        typeof(PhaseManager).GetField("_currentPhase", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(_phase, value);
    }

    private IEnumerator CheckSequence()
    {
        _running = true;
        GamePhase originalPhase = _phase.CurrentPhase;
        Mouse originalMouse = Mouse.current;
        CursorLockMode originalLock = Cursor.lockState;
        bool originalVisible = Cursor.visible;
        Mouse mouse = InputSystem.AddDevice<Mouse>("C7TestMouse");
        BaseInputModule module = EventSystem.current ? EventSystem.current.currentInputModule : null;
        bool wasEnabled = module && module.enabled;
        int used = 0, changes = 0;
        Action<ItemType> onUsed = _ => used++;
        Action changed = () => changes++;
        _effects.OnItemUsed += onUsed;
        _inventory.OnInventoryChanged += changed;
        try
        {
            if (module) module.enabled = false;
            PreparePlayer();
            SetTestPhase(GamePhase.Phase1);
            _status.text = "C7 自動驗證中，請暫時不要操作滑鼠。";
            yield return null;
            Require(_inventory.TryUseSelected() == ItemUseResult.Failed, "empty inventory");
            ItemRewards.Grant(ItemType.NicotinePatch);
            ItemRewards.Grant(ItemType.NicotineGum);
            ItemRewards.Grant(ItemType.NicotineSpray);
            ItemRewards.Grant((ItemType)2);
            Require(_inventory.GetCount(ItemType.NicotinePatch) == 1 && _inventory.GetCount(ItemType.NicotineGum) == 1
                && _inventory.GetCount(ItemType.NicotineSpray) == 1 && _inventory.GetCount((ItemType)2) == 0 && changes == 3, "grant three items and ignore removed type");
            yield return Scroll(mouse, 120);
            Require(_inventory.SelectedItem == ItemType.NicotineGum, "scroll forward");
            Require(_inventory.TryUseSelected() == ItemUseResult.NotYet && _inventory.GetCount(ItemType.NicotineGum) == 1, "pre phase3 denial");
            yield return Scroll(mouse, 120);
            Require(_inventory.SelectedItem == ItemType.NicotineSpray, "sparse enum selection");
            yield return Scroll(mouse, -120);
            Require(_inventory.SelectedItem == ItemType.NicotineGum, "scroll backward");

            _player.SetActionLocked(PlayerAction.UseItem, "C7Test", true);
            yield return Scroll(mouse, 120);
            yield return Right(mouse);
            Require(_inventory.SelectedItem == ItemType.NicotineGum && used == 0, "tutorial action lock");
            _player.SetActionLocked(PlayerAction.UseItem, "C7Test", false);
            GameManager.Instance.SetPaused(true);
            yield return Scroll(mouse, 120);
            yield return Right(mouse);
            Require(_inventory.SelectedItem == ItemType.NicotineGum && used == 0, "pause lock");
            GameManager.Instance.SetPaused(false);
            bool finished = false;
            DialogueUI.Instance.Show(new[] { new DialogueLine { Speaker = "測試", Text = "測試第一句：同一個框可以播放任意對話。" } }, () => finished = true);
            yield return null;
            yield return Scroll(mouse, 120);
            yield return Right(mouse);
            Require(_inventory.SelectedItem == ItemType.NicotineGum && used == 0, "dialogue lock");
            yield return Button(mouse, MouseButton.Left);
            Require(finished && !DialogueUI.Instance.IsShowing, "dialogue finished");
            yield return null;

            SetTestPhase(GamePhase.Phase3);
            yield return Scroll(mouse, -120);
            Require(_inventory.SelectedItem == ItemType.NicotinePatch, "select patch");
            Require(_inventory.TryUseSelected() == ItemUseResult.Failed && _inventory.GetCount(ItemType.NicotinePatch) == 1, "full HP does not consume");
            _health.TakeDamage(30);
            float before = _health.CurrentHp;
            yield return Right(mouse);
            Require(_health.CurrentHp == Mathf.Min(_health.MaxHp, before + 30) && _inventory.GetCount(ItemType.NicotinePatch) == 0, "patch actual healing and consumption");
            Require(_inventory.SelectedItem == ItemType.NicotineGum, "skip empty slot");
            yield return Right(mouse);
            Require(_inventory.GetCount(ItemType.NicotineGum) == 0 && _inventory.SelectedItem == ItemType.NicotineSpray, "gum consumption");
            yield return Right(mouse);
            Require(_inventory.GetCount(ItemType.NicotineSpray) == 1 && _effects.SprayCooldownRemaining > 0 && _cooldownFill.fillAmount > 0, "spray infinite and cooldown UI");
            int usesBefore = used;
            yield return Right(mouse);
            Require(used == usesBefore && _inventory.GetCount(ItemType.NicotineSpray) == 1, "cooldown rejection");
            yield return new WaitForSeconds(_effects.SprayCooldown + 0.1f);
            Require(_cooldownFill.fillAmount == 0, "cooldown completes");
            yield return Right(mouse);
            Require(used == usesBefore + 1 && _inventory.GetCount(ItemType.NicotineSpray) == 1, "spray repeat");
            _inventory.enabled = false;
            _inventory.enabled = true;
            ItemRewards.Grant(ItemType.NicotinePatch);
            Require(_inventory.GetCount(ItemType.NicotinePatch) == 1, "no duplicate reward subscription");
            _status.text = "C7 自動驗證通過：領取、切換、消耗、冷卻與操作鎖定。";
            Debug.Log("C7_AUTOTEST_PASS: rewards, sparse enum, wheel, right-click, empty inventory, phase restrictions, full HP, healing, consumption, cooldown, repeat, pause, dialogue and action locks.", this);
        }
        finally
        {
            if (GameManager.Instance.State == GameState.Paused) GameManager.Instance.SetPaused(false);
            _player.SetActionLocked(PlayerAction.UseItem, "C7Test", false);
            SetTestPhase(originalPhase);
            _effects.OnItemUsed -= onUsed;
            _inventory.OnInventoryChanged -= changed;
            InputSystem.RemoveDevice(mouse);
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            if (module) module.enabled = wasEnabled;
            Cursor.lockState = originalLock;
            Cursor.visible = originalVisible;
            _running = false;
        }
    }

    private static IEnumerator Right(Mouse mouse) => Button(mouse, MouseButton.Right);
    private static IEnumerator Button(Mouse mouse, MouseButton button)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(button));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return null;
    }
    private static IEnumerator Scroll(Mouse mouse, float y)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, y) });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return null;
    }
    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("C7_AUTOTEST_FAIL: " + label);
    }
}

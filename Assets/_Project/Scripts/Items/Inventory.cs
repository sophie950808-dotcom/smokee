using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>三種道具的背包；只管理持有數量，不重複實作道具效果。</summary>
[DefaultExecutionOrder(-50)]
public sealed class Inventory : MonoBehaviour
{
    [Header("玩家引用")]
    [Tooltip("留空時於 Start 尋找場景中的 PlayerController；沒有玩家時禁止輸入。")]
    [SerializeField] private PlayerController _player;
    [Tooltip("與指定玩家同一物件上的 PlayerEffects。")]
    [SerializeField] private PlayerEffects _effects;
    private static readonly ItemType[] Order = { ItemType.NicotinePatch, ItemType.NicotineGum, ItemType.NicotineSpray };
    private readonly int[] _counts = new int[3];
    private int _selected = -1;
    private bool _usingItem;

    /// <summary>場景唯一背包。</summary>
    public static Inventory Instance { get; private set; }
    /// <summary>目前選取的持有道具；沒有道具時為 null。</summary>
    public ItemType? SelectedItem => _selected < 0 ? (ItemType?)null : Order[_selected];
    /// <summary>UI 讀取玩家的道具冷卻與階段限制。</summary>
    public PlayerEffects Effects => _effects;
    /// <summary>持有數量或選取項目改變時通知。</summary>
    public event Action OnInventoryChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Debug.LogError("Inventory: only one inventory is allowed in a scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance == this) ItemRewards.OnItemGranted += ReceiveItem;
    }

    private void Start()
    {
        if (!_player) _player = FindFirstObjectByType<PlayerController>();
        if (_player && !_effects) _effects = _player.GetComponent<PlayerEffects>();
    }

    private void Update()
    {
        if (!CanReadInput() || Mouse.current == null) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0) SelectNext(scroll > 0 ? 1 : -1);
        if (Mouse.current.rightButton.wasPressedThisFrame) TryUseSelected();
    }

    /// <summary>取得指定道具數量；已取消或未知的類型回傳 0。</summary>
    public int GetCount(ItemType type)
    {
        int index = Array.IndexOf(Order, type);
        return index < 0 ? 0 : _counts[index];
    }

    private void ReceiveItem(ItemType type)
    {
        int index = Array.IndexOf(Order, type);
        if (index < 0 || _counts[index] == int.MaxValue) return;
        _counts[index]++;
        if (_selected < 0) _selected = index;
        OnInventoryChanged?.Invoke();
    }

    private bool CanReadInput()
    {
        GameManager manager = GameManager.Instance;
        return isActiveAndEnabled && Instance == this && _player && _effects
            && Time.timeScale > 0 && _player.IsActionAllowed(PlayerAction.UseItem)
            && (!manager || (!manager.IsTimerPaused && manager.State == GameState.Playing));
    }

    /// <summary>依方向切換至下一個持有道具；操作被鎖時不切換。</summary>
    public void SelectNext(int direction)
    {
        if (!CanReadInput() || direction == 0) return;
        int next = FindOwned(_selected, direction > 0 ? 1 : -1);
        if (next == _selected) return;
        _selected = next;
        OnInventoryChanged?.Invoke();
    }

    private int FindOwned(int start, int direction)
    {
        for (int step = 1; step <= Order.Length; step++)
        {
            int index = ((start + direction * step) % Order.Length + Order.Length) % Order.Length;
            if (_counts[index] > 0) return index;
        }
        return -1;
    }

    /// <summary>要求 A 的 PlayerEffects 使用選取道具；只有 Consumed 才扣 1。</summary>
    public ItemUseResult TryUseSelected()
    {
        if (!CanReadInput()) return ItemUseResult.Locked;
        if (_usingItem || _selected < 0 || _counts[_selected] <= 0) return ItemUseResult.Failed;
        int usedIndex = _selected;
        _usingItem = true;
        try
        {
            ItemUseResult result = _effects.TryUseItem(Order[usedIndex]);
            if (result == ItemUseResult.Consumed)
            {
                _counts[usedIndex]--;
                if (_counts[usedIndex] == 0) _selected = FindOwned(usedIndex, 1);
                OnInventoryChanged?.Invoke();
            }
            return result;
        }
        finally { _usingItem = false; }
    }

    private void OnDisable() => ItemRewards.OnItemGranted -= ReceiveItem;
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

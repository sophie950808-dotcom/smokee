using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>三格背包原型；圖示、字型與背景均可於 Inspector 替換。</summary>
public sealed class InventoryUI : MonoBehaviour
{
    [Serializable]
    private sealed class Slot
    {
        [Tooltip("這一格的道具資料。")]
        public HealthItemData Data;
        [Tooltip("名稱文字。")]
        public TMP_Text Name;
        [Tooltip("數量文字。")]
        public TMP_Text Count;
        [Tooltip("道具圖示；資料沒有 Sprite 時顯示純色佔位圖形。")]
        public Image Icon;
        [Tooltip("選取框背景。")]
        public Image Background;
    }
    [Header("背包與 UI")]
    [Tooltip("本 UI 顯示的唯一背包。")]
    [SerializeField] private Inventory _inventory;
    [Tooltip("固定三格；不要依 ItemType 數值作為陣列位置。")]
    [SerializeField] private Slot[] _slots;
    [Tooltip("目前選取道具的說明。")]
    [SerializeField] private TMP_Text _description;
    [Tooltip("噴霧冷卻剩餘時間。")]
    [SerializeField] private TMP_Text _cooldownText;
    [Tooltip("噴霧冷卻覆蓋圖，使用 Filled 類型。")]
    [SerializeField] private Image _cooldownFill;

    private void OnEnable()
    {
        if (_inventory) _inventory.OnInventoryChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (_inventory) _inventory.OnInventoryChanged -= Refresh;
    }
    private void Update() => Refresh();

    private void Refresh()
    {
        if (!_inventory || _slots == null) return;
        PlayerEffects effects = _inventory.Effects;
        HealthItemData selected = null;
        foreach (Slot slot in _slots)
        {
            if (slot == null || !slot.Data) continue;
            int count = _inventory.GetCount(slot.Data.Type);
            bool chosen = _inventory.SelectedItem == slot.Data.Type;
            bool available = count > 0 && effects && (slot.Data.Type == ItemType.NicotineSpray || effects.IsPhase3ItemAvailable);
            slot.Name.text = slot.Data.DisplayName;
            slot.Count.text = slot.Data.Type == ItemType.NicotineSpray && count > 0 ? "無限次" : "× " + count;
            slot.Icon.sprite = slot.Data.Icon;
            slot.Icon.color = available ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            slot.Background.color = chosen ? new Color(0.22f, 0.45f, 0.6f) : new Color(0.12f, 0.16f, 0.22f);
            if (chosen) selected = slot.Data;
        }
        _description.text = selected ? selected.InfoText : "背包目前沒有道具";
        bool hasSpray = _inventory.GetCount(ItemType.NicotineSpray) > 0;
        float remaining = effects ? effects.SprayCooldownRemaining : 0;
        _cooldownFill.fillAmount = hasSpray && effects && effects.SprayCooldown > 0 ? Mathf.Clamp01(remaining / effects.SprayCooldown) : 0;
        _cooldownText.text = !hasSpray ? "尚未取得噴霧" : remaining > 0 ? "冷卻 " + remaining.ToString("F1") + " 秒" : "噴霧已就緒";
    }
}

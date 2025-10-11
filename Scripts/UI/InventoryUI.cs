using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Begin.Items;

namespace Begin.UI {
    public class InventoryUI : MonoBehaviour {
        [Header("Layout")]
        public RectTransform itemsContainer;
        public Button itemButtonPrefab;
        public Text equippedText;
        public Text statsText;
        public Text capacityText;

        [Header("Preview")]
        public Image previewIcon;
        public Text previewName;
        public Text previewDescription;
        public Text previewMeta;

        readonly List<SlotEntry> _slots = new();
        int _selectedIndex = -1;
        System.Action<int> _capacityHandler;

        struct SlotEntry {
            public Button button;
            public Text label;
            public int index;
        }

        void OnEnable() {
            ItemDB.Warmup();
            InventoryService.OnChanged += RefreshStats;
            InventoryService.OnSlotChanged += HandleSlotChanged;
            _capacityHandler ??= _ => RebuildAll();
            InventoryService.OnCapacityChanged += _capacityHandler;
            RebuildAll();
        }

        void OnDisable() {
            InventoryService.OnChanged -= RefreshStats;
            InventoryService.OnSlotChanged -= HandleSlotChanged;
            if (_capacityHandler != null) InventoryService.OnCapacityChanged -= _capacityHandler;
        }

        void HandleSlotChanged(int index, InventoryService.InventorySlot slot) {
            UpdateSlot(index, slot);
            if (index == _selectedIndex) ShowPreview(slot);
            RefreshStats();
            UpdateCapacityLabel();
        }

        void RebuildAll() {
            foreach (Transform c in itemsContainer) Destroy(c.gameObject);
            _slots.Clear();

            int capacity = InventoryService.Capacity;
            for (int i = 0; i < capacity; i++) {
                var btn = Instantiate(itemButtonPrefab, itemsContainer);
                btn.gameObject.SetActive(true);
                int captured = i;
                btn.onClick.AddListener(() => SelectSlot(captured));
                var entry = new SlotEntry {
                    button = btn,
                    label = btn.GetComponentInChildren<Text>(),
                    index = i
                };
                _slots.Add(entry);
                UpdateSlot(i, InventoryService.GetSlot(i));
            }

            UpdateEquipped();
            RefreshStats();
            UpdateCapacityLabel();
            ClearPreview();
        }

        void UpdateSlot(int index, InventoryService.InventorySlot slot) {
            if (index < 0 || index >= _slots.Count) return;
            var entry = _slots[index];
            if (entry.label != null) {
                if (slot.IsEmpty) {
                    entry.label.text = $"[{index + 1}] —";
                } else {
                    var name = slot.Definition ? slot.Definition.displayName : slot.ItemId;
                    entry.label.text = $"[{index + 1}] {name} x{slot.Quantity}";
                }
            }
            if (entry.button != null)
                entry.button.interactable = !slot.IsEmpty;
            _slots[index] = entry;
        }

        void SelectSlot(int index) {
            _selectedIndex = index;
            var slot = InventoryService.GetSlot(index);
            if (slot.IsEmpty) ClearPreview();
            else ShowPreview(slot);
        }

        void ShowPreview(InventoryService.InventorySlot slot) {
            if (previewIcon) {
                previewIcon.enabled = slot.Definition && slot.Definition.icon;
                previewIcon.sprite = slot.Definition ? slot.Definition.icon : null;
            }
            if (previewName) previewName.text = slot.Definition ? slot.Definition.displayName : slot.ItemId;
            if (previewDescription) previewDescription.text = slot.Definition ? slot.Definition.description : string.Empty;

            if (previewMeta) {
                var lines = new List<string>();
                if (slot.Definition) {
                    if (slot.Definition.hpBonus != 0) lines.Add($"HP +{slot.Definition.hpBonus}");
                    if (slot.Definition.damageBonus != 0) lines.Add($"DMG +{slot.Definition.damageBonus}");
                    if (slot.Definition.strengthBonus != 0) lines.Add($"STR +{slot.Definition.strengthBonus}");
                    if (slot.Definition.dexterityBonus != 0) lines.Add($"DEX +{slot.Definition.dexterityBonus}");
                    if (slot.Definition.intelligenceBonus != 0) lines.Add($"INT +{slot.Definition.intelligenceBonus}");
                    if (slot.Definition.stackable) lines.Add($"Стак: {slot.Quantity}/{slot.Definition.maxStack}");
                }
                if (!string.IsNullOrEmpty(slot.Metadata)) lines.Add($"Meta: {slot.Metadata}");
                previewMeta.text = lines.Count > 0 ? string.Join("\n", lines) : "";
            }
        }

        void ClearPreview() {
            if (previewIcon) { previewIcon.enabled = false; previewIcon.sprite = null; }
            if (previewName) previewName.text = "Выберите предмет";
            if (previewDescription) previewDescription.text = string.Empty;
            if (previewMeta) previewMeta.text = string.Empty;
        }

        void UpdateEquipped() {
            if (equippedText == null) return;
            var slots = System.Enum.GetValues(typeof(EquipmentSlot));
            var lines = new List<string>();
            foreach (EquipmentSlot s in slots) {
                if (s == EquipmentSlot.None) continue;
                string key = s.ToString();
                InventoryService.Equipped.TryGetValue(key, out var id);
                var def = !string.IsNullOrEmpty(id) ? ItemDB.Get(id) : null;
                lines.Add($"{s}: {(def ? def.displayName : "—")}");
            }
            equippedText.text = string.Join("\n", lines);
        }

        void RefreshStats() {
            UpdateEquipped();
            if (statsText == null) return;
            int hpBonus = InventoryService.TotalHpBonus(ItemDB.Get);
            int dmgBonus = InventoryService.TotalDamageBonus(ItemDB.Get);
            int strBonus = InventoryService.TotalStrengthBonus(ItemDB.Get);
            int dexBonus = InventoryService.TotalDexterityBonus(ItemDB.Get);
            int intBonus = InventoryService.TotalIntelligenceBonus(ItemDB.Get);
            statsText.text = $"HP +{hpBonus}   DMG +{dmgBonus}\nSTR +{strBonus}   DEX +{dexBonus}   INT +{intBonus}";
        }

        void UpdateCapacityLabel() {
            if (!capacityText) return;
            int filled = 0;
            foreach (var slot in InventoryService.Slots) if (!slot.IsEmpty) filled++;
            capacityText.text = $"Слоты: {filled}/{InventoryService.Capacity}";
        }
    }
}

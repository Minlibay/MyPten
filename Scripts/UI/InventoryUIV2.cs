using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Begin.Items;

namespace Begin.UI {
    public class InventoryUIV2 : MonoBehaviour {
        [Header("Left: Items")]
        public RectTransform itemsContainer;
        public Button itemButtonPrefab;

        [Header("Right: Equipped")]
        public RectTransform slotsContainer;
        public Button slotRowPrefab;

        [Header("Stats")]
        public Text statsText;

        EquipmentSlot[] slots = new[] { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Weapon };

        void OnEnable() {
            ItemDB.Warmup();
            InventoryService.OnChanged += Rebuild;
            Rebuild();
        }
        void OnDisable() { InventoryService.OnChanged -= Rebuild; }

        void Rebuild() {
            // очистка
            foreach (Transform t in itemsContainer) Destroy(t.gameObject);
            foreach (Transform t in slotsContainer) Destroy(t.gameObject);

            // ЛЕВАЯ КОЛОНКА — предметы
            foreach (var slot in InventoryService.Slots) {
                if (slot.IsEmpty) continue;
                var def = slot.Definition;
                if (def == null) continue;

                var btn = Instantiate(itemButtonPrefab, itemsContainer);
                btn.gameObject.SetActive(true);

                SetText(btn.transform, "Name", $"{def.displayName} [{def.slot}] x{slot.Quantity}");
                bool equippedHere = IsEquipped(def.slot, def.id);
                SetText(btn.transform, "Action", equippedHere ? "Снят" : "Экип.");

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    if (equippedHere) {
                        InventoryService.Unequip(def.slot);
                    } else {
                        if (def.slot != EquipmentSlot.None)
                            InventoryService.Equip(def.slot, def.id);
                    }
                    Rebuild();
                });
            }

            // ПРАВАЯ КОЛОНКА — слоты
            foreach (var s in slots) {
                var row = Instantiate(slotRowPrefab, slotsContainer);
                row.gameObject.SetActive(true);

                SetText(row.transform, "Slot", s.ToString());

                string equippedId = GetEquippedId(s);
                var def = equippedId != null ? ItemDB.Get(equippedId) : null;
                SetText(row.transform, "Name", def ? def.displayName : "—");

                var has = def != null;
                SetText(row.transform, "Action", has ? "Снять" : "—");

                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => {
                    if (has) InventoryService.Unequip(s);
                    Rebuild();
                });
            }

            // Статы
            int hpBonus = InventoryService.TotalHpBonus(ItemDB.Get);
            int dmgBonus = InventoryService.TotalDamageBonus(ItemDB.Get);
            int strBonus = InventoryService.TotalStrengthBonus(ItemDB.Get);
            int dexBonus = InventoryService.TotalDexterityBonus(ItemDB.Get);
            int intBonus = InventoryService.TotalIntelligenceBonus(ItemDB.Get);
            if (statsText)
                statsText.text = $"HP +{hpBonus}   DMG +{dmgBonus}\nSTR +{strBonus}   DEX +{dexBonus}   INT +{intBonus}";
        }

        static void SetText(Transform root, string child, string value) {
            var t = root.Find(child);
            var ui = t ? t.GetComponent<Text>() : null;
            if (ui) ui.text = value;
        }

        bool IsEquipped(EquipmentSlot slot, string itemId) {
            var key = slot.ToString();
            return InventoryService.Equipped.ContainsKey(key) && InventoryService.Equipped[key] == itemId;
        }
        string GetEquippedId(EquipmentSlot slot) {
            var key = slot.ToString();
            return InventoryService.Equipped.ContainsKey(key) ? InventoryService.Equipped[key] : null;
        }
    }
}

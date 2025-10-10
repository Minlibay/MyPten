using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Begin.Items;

namespace Begin.UI {
    public class InventoryUI : MonoBehaviour {
        public RectTransform itemsContainer;
        public Button itemButtonPrefab;
        public Text equippedText;
        public Text statsText;

        void OnEnable() {
            ItemDB.Warmup();
            InventoryService.OnChanged += Rebuild;
            Rebuild();
        }
        void OnDisable() { InventoryService.OnChanged -= Rebuild; }

        void Rebuild() {
            foreach (Transform c in itemsContainer) Destroy(c.gameObject);

            // список предметов
            foreach (var id in InventoryService.Items.ToArray()) {
                var def = ItemDB.Get(id);
                if (def == null) continue;

                var b = Instantiate(itemButtonPrefab, itemsContainer);
                b.gameObject.SetActive(true);
                b.GetComponentInChildren<Text>().text = $"{def.displayName} [{def.slot}] (+HP {def.hpBonus})";
                b.onClick.AddListener(()=> {
                    if (def.slot == EquipmentSlot.None) return;
                    InventoryService.Equip(def.slot, def.id);
                });
            }

            // экип
            var slots = System.Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Where(s=>s!=EquipmentSlot.None);
            var lines = slots.Select(s => {
                var key = s.ToString();
                string id = InventoryService.Equipped.ContainsKey(key) ? InventoryService.Equipped[key] : null;
                var def = id!=null ? ItemDB.Get(id) : null;
                return $"{s}: {(def!=null ? def.displayName : "—")}";
            });
            if (equippedText) equippedText.text = string.Join("\n", lines);

            // статы
            int hpBonus = InventoryService.TotalHpBonus(ItemDB.Get);
            int dmgBonus = InventoryService.TotalDamageBonus(ItemDB.Get);
            if (statsText) statsText.text = $"HP +{hpBonus}   DMG +{dmgBonus}";
        }
    }
}

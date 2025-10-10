using System.Linq;
using UnityEngine;
using Begin.Items;
using Begin.Talents;

namespace Begin.Economy {
    public class Vendor : MonoBehaviour {
        public string[] stockIds; // текущие товары

        public void RefreshStock(int count = 6) {
            ItemDB.Warmup();
            var all = ItemDB.All().OrderBy(_ => Random.value).Take(count).Select(i=> i.id).ToArray();
            stockIds = all;
        }

        public int Price(ItemDefinition def) {
        float r = def.rarity switch {
            ItemRarity.Common => 1f,
            ItemRarity.Uncommon => 1.5f,
            ItemRarity.Rare => 2.5f,
            ItemRarity.Epic => 4f,
            ItemRarity.Legendary => 8f,
            _ => 1f
        };
        float basePrice = def.basePrice * r;
        float discountPct = TalentService.Total(TalentType.VendorDiscount); // 0..100
        float mul = Mathf.Clamp01(1f - discountPct/100f);
        return Mathf.Max(1, Mathf.RoundToInt(basePrice * mul));
    }

        public bool Buy(string id) {
            var def = ItemDB.Get(id); if (def == null) return false;
            int price = Price(def);
            if (Currency.TrySpend(price)) { InventoryService.Give(id); return true; }
            return false;
        }

        public int SellPrice(string id) {
            var def = ItemDB.Get(id); if (def == null) return 0;
            return Mathf.RoundToInt(Price(def) * 0.4f);
        }

        public bool Sell(string id) {
            int p = SellPrice(id);
            InventoryService.Remove(id);
            Currency.Give(p);
            return true;
        }
    }
}

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Begin.Items;
using Begin.Economy;

namespace Begin.UI {
    public class VendorUI : MonoBehaviour {
        public Vendor vendor;
        public RectTransform stockContainer;
        public Button stockButtonPrefab;
        public RectTransform sellContainer;
        public Button sellButtonPrefab;
        public Text goldText;

        void OnEnable() {
            ItemDB.Warmup();

            // если невалидный/пустой список — обновим
            if (vendor != null) {
                bool needRefresh = vendor.stockIds == null
                                   || vendor.stockIds.Length == 0
                                   || !vendor.stockIds.Any(id => !string.IsNullOrWhiteSpace(id) && ItemDB.Get(id) != null);
                if (needRefresh) vendor.RefreshStock();
            }

            Rebuild();
        }

        void Update() { if (goldText) goldText.text = "Gold: " + Currency.Gold; }

        public void Rebuild() {
            foreach (Transform t in stockContainer) Destroy(t.gameObject);
            foreach (Transform t in sellContainer) Destroy(t.gameObject);

            int createdStock = 0;

            // Витрина
            if (vendor?.stockIds != null) {
                foreach (var id in vendor.stockIds) {
                    var def = ItemDB.Get(id); if (def == null) continue;
                    var b = Instantiate(stockButtonPrefab, stockContainer);
                    b.gameObject.SetActive(true);
                    SetText(b.transform, "Name", $"{def.displayName} [{def.slot}]");
                    SetText(b.transform, "Price", vendor.Price(def) + "g");
                    b.onClick.AddListener(()=> { if (vendor.Buy(def.id)) Rebuild(); });
                    createdStock++;
                }
            }

            if (createdStock == 0) {
                MakePlaceholder(stockContainer, "Нет товаров");
            }

            // Продажа
            int createdSell = 0;
            foreach (var id in InventoryService.Items) {
                var def = ItemDB.Get(id); if (def == null) continue;
                var b = Instantiate(sellButtonPrefab, sellContainer);
                b.gameObject.SetActive(true);
                SetText(b.transform, "Name", $"Продать: {def.displayName}");
                SetText(b.transform, "Price", "+" + vendor.SellPrice(def.id) + "g");
                b.onClick.AddListener(()=> { if (vendor.Sell(def.id)) Rebuild(); });
                createdSell++;
            }
            if (createdSell == 0) {
                MakePlaceholder(sellContainer, "Пока пусто");
            }
        }

        static void SetText(Transform root, string child, string value) {
            var t = root.Find(child);
            var ui = t ? t.GetComponent<Text>() : null;
            if (ui) ui.text = value;
        }

        static void MakePlaceholder(RectTransform parent, string text) {
            var go = new GameObject("Placeholder", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 18;
            txt.color = new Color(1,1,1,0.6f);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);
        }
    }
}

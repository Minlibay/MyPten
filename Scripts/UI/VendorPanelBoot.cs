using UnityEngine;
using Begin.Economy;
using Begin.Items;
using System.Linq;

namespace Begin.UI {
    [DefaultExecutionOrder(-50)]
    public class VendorPanelBoot : MonoBehaviour {
        public Vendor vendor;
        public bool alwaysRefreshOnOpen = true;

        void Start() {
            if (vendor == null) return;
            ItemDB.Warmup();

            // Освежаем, если запрошено всегда или текущий список невалидный
            if (alwaysRefreshOnOpen || !IsValidStock()) {
                vendor.RefreshStock();
            }

            // Перерисовать UI
            var ui = GetComponent<VendorUI>();
            if (ui != null) ui.Rebuild();
        }

        bool IsValidStock() {
            if (vendor.stockIds == null || vendor.stockIds.Length == 0) return false;
            // хотя бы один элемент должен существовать в базе
            return vendor.stockIds.Any(id => !string.IsNullOrWhiteSpace(id) && ItemDB.Get(id) != null);
        }
    }
}

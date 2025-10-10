#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Begin.Items;

public static class BeginVendorDiag {
    [MenuItem("Tools/Begin/Vendor ▸ Diagnose Selected Panel")]
    public static void Diagnose() {
        var go = Selection.activeGameObject;
        if (!go) { EditorUtility.DisplayDialog("Vendor Diag", "Выдели корневой объект панели (Card или VendorPanel).", "OK"); return; }

        var vendor = go.GetComponentInChildren<Begin.Economy.Vendor>(true);
        var ui     = go.GetComponentInChildren<Begin.UI.VendorUI>(true);

        var msg = "";

        // 1) База предметов
        ItemDB.Warmup();
        var all = ItemDB.All()?.Count() ?? 0;
        msg += $"ItemDB: {all} предмет(ов)\n";

        // 2) Vendor / stockIds
        if (!vendor) msg += "Vendor: НЕ НАЙДЕН\n";
        else {
            var size = vendor.stockIds == null ? 0 : vendor.stockIds.Length;
            var valid = vendor.stockIds != null ? vendor.stockIds.Count(id => !string.IsNullOrWhiteSpace(id) && ItemDB.Get(id) != null) : 0;
            msg += $"Vendor.stockIds: size={size}, valid={valid}\n";
        }

        // 3) Проверка путей контейнеров
        if (!ui) msg += "VendorUI: НЕ НАЙДЕН\n";
        else {
            msg += $"stockContainer: {(ui.stockContainer ? ui.stockContainer.name : "NULL")}\n";
            msg += $"sellContainer : {(ui.sellContainer ? ui.sellContainer.name : "NULL")}\n";
            msg += $"stockButtonPrefab: {(ui.stockButtonPrefab ? ui.stockButtonPrefab.name : "NULL")}\n";
            msg += $"sellButtonPrefab : {(ui.sellButtonPrefab ? ui.sellButtonPrefab.name : "NULL")}\n";
        }

        EditorUtility.DisplayDialog("Vendor Diag", msg, "OK");
    }

    [MenuItem("Tools/Begin/Vendor ▸ Force Refresh Stock & Rebuild")]
    public static void ForceRefresh() {
        var go = Selection.activeGameObject;
        if (!go) { EditorUtility.DisplayDialog("Vendor", "Выдели корневой объект панели (Card).", "OK"); return; }

        var vendor = go.GetComponentInChildren<Begin.Economy.Vendor>(true);
        var ui     = go.GetComponentInChildren<Begin.UI.VendorUI>(true);

        ItemDB.Warmup();
        if (vendor) vendor.RefreshStock();
        if (ui) ui.Rebuild();

        EditorUtility.DisplayDialog("Vendor", "Ассортимент обновлён и UI перерисован.", "OK");
    }

    [MenuItem("Tools/Begin/Vendor ▸ Fix Layout (Anchors & Sizes)")]
    public static void FixLayout() {
        var card = Selection.activeGameObject;
        if (!card) { EditorUtility.DisplayDialog("Vendor Fix", "Выдели объект Card.", "OK"); return; }

        // Card должен занимать 70% экрана, центр
        var crt = card.GetComponent<RectTransform>();
        if (crt) {
            crt.anchorMin = new Vector2(0.15f, 0.15f);
            crt.anchorMax = new Vector2(0.85f, 0.85f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
        }

        // Columns растянуть внутри Card
        var columns = card.transform.Find("Columns") as RectTransform;
        if (columns) {
            columns.anchorMin = new Vector2(0.05f, 0.08f);
            columns.anchorMax = new Vector2(0.95f, 0.86f);
            columns.offsetMin = columns.offsetMax = Vector2.zero;
            var hlg = columns.GetComponent<HorizontalLayoutGroup>();
            if (hlg) { hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true; hlg.spacing = 24; }
        }

        // Заголовки «Ассортимент» и «Ваши предметы»
        FixTitle(card.transform, "Stock/Text");
        FixTitle(card.transform, "Sell/Text");

        // ScrollView → Viewport → Content якоря
        FixScroll(card.transform, "Stock/ScrollView");
        FixScroll(card.transform, "Sell/ScrollView");

        EditorUtility.DisplayDialog("Vendor Fix", "Анкоры и размеры исправлены.", "OK");
    }

    static void FixTitle(Transform card, string path) {
        var t = card.Find("Columns/" + path) as RectTransform;
        if (!t) return;
        t.anchorMin = new Vector2(0,1); t.anchorMax = new Vector2(1,1);
        t.anchoredPosition = new Vector2(0,-4); t.sizeDelta = new Vector2(0,28);
    }

    static void FixScroll(Transform card, string path) {
        var sv = card.Find("Columns/" + path) as RectTransform;
        if (!sv) return;
        sv.anchorMin = new Vector2(0,0); sv.anchorMax = new Vector2(1,1);
        sv.offsetMin = new Vector2(0,0); sv.offsetMax = new Vector2(0,-36);

        var vp = sv.Find("Viewport") as RectTransform;
        if (vp) { vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one; vp.offsetMin = vp.offsetMax = Vector2.zero; }

        var content = vp ? vp.Find("Content") as RectTransform : null;
        if (content) { content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1); content.pivot = new Vector2(0.5f,1); }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Begin.EditorTools {
    public static class BeginVendorPanelBuilder {
        [MenuItem("Tools/Begin/Create ▸ Vendor Panel (Auto, v2)")]
        public static void CreateVendorPanel() {
            var canvas = EnsureCanvas();
            EnsureEventSystem();

            // Root panel (полупрозрачный тёмный фон)
            var panel = CreateUIObject("VendorPanel", canvas.transform);
            var bg = panel.AddComponent<Image>(); bg.color = new Color(0,0,0,0.55f);

            // Card контейнер (светлая карточка по центру)
            var card = CreateUIObject("Card", panel.transform);
            var cardImg = card.AddComponent<Image>(); cardImg.color = new Color(0.15f,0.15f,0.15f,0.95f);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.15f, 0.15f);
            cardRT.anchorMax = new Vector2(0.85f, 0.85f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

            // Заголовок
            var header = CreateText(card.transform, "Торговец", 28, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            var headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.5f, 1f);
            headerRT.anchorMax = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0, -32);
            headerRT.sizeDelta = new Vector2(800, 44);

            // Gold
            var goldText = CreateText(card.transform, "Gold: 0", 18, TextAnchor.UpperLeft, FontStyle.Normal, new Color(1,1,1,0.9f));
            var goldRT = goldText.GetComponent<RectTransform>();
            goldRT.anchorMin = new Vector2(0.05f, 0.89f);
            goldRT.anchorMax = new Vector2(0.35f, 0.94f);
            goldRT.offsetMin = goldRT.offsetMax = Vector2.zero;

            // Колонки
            var columns = CreateUIObject("Columns", card.transform);
            var columnsRT = columns.GetComponent<RectTransform>();
            columnsRT.anchorMin = new Vector2(0.05f, 0.08f);
            columnsRT.anchorMax = new Vector2(0.95f, 0.86f);
            columnsRT.offsetMin = columnsRT.offsetMax = Vector2.zero;

            var hlg = columns.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.UpperCenter;
            hlg.spacing = 24; hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = true;

            // Левая колонка (Stock) — ScrollView
            var stock = CreateScrollColumn(columns.transform, "Stock", "Ассортимент");
            var stockContent = stock.content;

            // Правая колонка (Sell) — ScrollView
            var sell = CreateScrollColumn(columns.transform, "Sell", "Ваши предметы");
            var sellContent = sell.content;

            // Шаблоны кнопок (карточек)
            var templates = CreateUIObject("Templates", card.transform);
            var stockBtn = CreateCardButton(templates.transform, "StockButton");
            var sellBtn  = CreateCardButton(templates.transform, "SellButton");
            stockBtn.gameObject.SetActive(false);
            sellBtn.gameObject.SetActive(false);

            // Логика
            var vendor = card.AddComponent<Begin.Economy.Vendor>();
            var vendorUI = card.AddComponent<Begin.UI.VendorUI>();
            var boot = card.AddComponent<Begin.UI.VendorPanelBoot>();

            vendorUI.vendor = vendor;
            vendorUI.stockContainer = stockContent;
            vendorUI.sellContainer = sellContent;
            vendorUI.stockButtonPrefab = stockBtn;
            vendorUI.sellButtonPrefab  = sellBtn;
            vendorUI.goldText = goldText;

            boot.vendor = vendor;
            if (vendor.stockIds == null || vendor.stockIds.Length == 0) vendor.RefreshStock();

            Selection.activeGameObject = card;
            EditorUtility.DisplayDialog("Begin", "VendorPanel v2 создан.", "OK");
        }

        // ---------- helpers ----------
        static GameObject EnsureCanvas() {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) return canvas.gameObject;
            var go = new GameObject("Canvas", typeof(RectTransform));
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>(); go.AddComponent<GraphicRaycaster>();
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static void EnsureEventSystem() {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputSystemUiType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiType != null) es.AddComponent(inputSystemUiType);
            else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        static GameObject CreateUIObject(string name, Transform parent) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Text CreateText(Transform parent, string content, int size, TextAnchor anchor, FontStyle style, Color color) {
            var go = CreateUIObject("Text", parent);
            var txt = go.AddComponent<Text>();
            txt.text = content; txt.fontSize = size; txt.alignment = anchor; txt.fontStyle = style; txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(200, 40);
            return txt;
        }

        // Колонка-скролл с заголовком и контентом
        static (ScrollRect scroll, RectTransform content) CreateScrollColumn(Transform parent, string name, string title) {
            var wrap = CreateUIObject(name, parent);
            var wrapRT = wrap.GetComponent<RectTransform>(); wrapRT.sizeDelta = Vector2.zero;

            var titleText = CreateText(wrap.transform, title, 20, TextAnchor.UpperLeft, FontStyle.Bold, Color.white);
            var tRT = titleText.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0f, 1f); tRT.anchorMax = new Vector2(1f, 1f); tRT.anchoredPosition = new Vector2(0, -4); tRT.sizeDelta = new Vector2(0, 28);

            // Scroll View
            var scrollGO = CreateUIObject("ScrollView", wrap.transform);
            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0f, 0f); sRT.anchorMax = new Vector2(1f, 1f); sRT.offsetMin = new Vector2(0, 0); sRT.offsetMax = new Vector2(0, -36);

            var img = scrollGO.AddComponent<Image>(); img.color = new Color(1,1,1,0.06f);
            var mask = scrollGO.AddComponent<Mask>(); mask.showMaskGraphic = false;

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = CreateUIObject("Viewport", scrollGO.transform);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one; vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            var vpMaskImg = viewport.AddComponent<Image>(); vpMaskImg.color = new Color(1,1,1,0.02f);
            var vpMask = viewport.AddComponent<Mask>(); vpMask.showMaskGraphic = false;

            var content = CreateUIObject("Content", viewport.transform).GetComponent<RectTransform>();
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft; vlg.spacing = 8; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT; scroll.content = content;

            return (scroll, content);
        }

        // Карточка-кнопка с двумя текстами: Name (слева), Price (справа)
        static Button CreateCardButton(Transform parent, string name) {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.92f, 0.92f, 0.92f, 1f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor   = new Color(0.92f,0.92f,0.92f,1f);
            colors.highlightedColor = new Color(1f,1f,1f,1f);
            colors.pressedColor  = new Color(0.85f,0.85f,0.85f,1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.6f,0.6f,0.6f,1f);
            btn.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48); // ширина потянется, высота фикс
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 48;

            // Name (слева)
            var nameGO = CreateUIObject("Name", go.transform);
            var nameT = nameGO.AddComponent<Text>();
            nameT.text = "Название";
            nameT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameT.fontSize = 18; nameT.color = Color.black; nameT.alignment = TextAnchor.MiddleLeft;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0,0); nameRT.anchorMax = new Vector2(1,1);
            nameRT.offsetMin = new Vector2(16, 0); nameRT.offsetMax = new Vector2(-70, 0); // место под цену справа

            // Price (справа)
            var priceGO = CreateUIObject("Price", go.transform);
            var priceT = priceGO.AddComponent<Text>();
            priceT.text = "0g";
            priceT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceT.fontSize = 18; priceT.color = Color.black; priceT.alignment = TextAnchor.MiddleRight;
            var priceRT = priceGO.GetComponent<RectTransform>();
            priceRT.anchorMin = new Vector2(1,0); priceRT.anchorMax = new Vector2(1,1);
            priceRT.pivot = new Vector2(1,0.5f);
            priceRT.sizeDelta = new Vector2(60, 0);

            // Лёгкая тень
            var shadow = go.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,0.15f); shadow.effectDistance = new Vector2(0, -1);

            return btn;
        }
    }
}
#endif

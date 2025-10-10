#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Begin.EditorTools {
    public static class BeginInventoryPanelBuilder {
        [MenuItem("Tools/Begin/Create ▸ Inventory Panel (Auto, v2)")]
        public static void CreateInventoryPanel() {
            var canvas = EnsureCanvas();
            EnsureEventSystem();

            // Root panel (полноэкранный, затемнение)
            var panel = CreateUI("InventoryPanel", canvas.transform);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>(); bg.color = new Color(0,0,0,0.55f);

            // Card контейнер
            var card = CreateUI("Card", panel.transform);
            var cardImg = card.AddComponent<Image>(); cardImg.color = new Color(0.15f,0.15f,0.15f,0.95f);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.15f, 0.15f);
            cardRT.anchorMax = new Vector2(0.85f, 0.85f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

            // Заголовок
            var header = CreateText(card.transform, "Инвентарь", 28, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            var headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.5f,1f); headerRT.anchorMax = new Vector2(0.5f,1f);
            headerRT.anchoredPosition = new Vector2(0,-32); headerRT.sizeDelta = new Vector2(800,44);

            // Нижняя строка со статами
            var statsText = CreateText(card.transform, "HP +0   DMG +0", 18, TextAnchor.LowerLeft, FontStyle.Normal, new Color(1,1,1,0.9f));
            var statsRT = statsText.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.05f,0.05f); statsRT.anchorMax = new Vector2(0.95f,0.1f);
            statsRT.offsetMin = statsRT.offsetMax = Vector2.zero;

            // Колонки
            var columns = CreateUI("Columns", card.transform);
            var hlg = columns.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.UpperCenter; hlg.spacing = 24;
            hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = true;
            var columnsRT = columns.GetComponent<RectTransform>();
            columnsRT.anchorMin = new Vector2(0.05f,0.12f); columnsRT.anchorMax = new Vector2(0.95f,0.86f);
            columnsRT.offsetMin = columnsRT.offsetMax = Vector2.zero;

            // Левая колонка (список предметов)
            var items = CreateScrollColumn(columns.transform, "Items", "Ваши предметы");
            var itemsContent = items.content;

            // Правая колонка (слоты «Экипировано»)
            var equippedWrap = CreateUI("Equipped", columns.transform);
            var eqTitle = CreateText(equippedWrap.transform, "Экипировано", 20, TextAnchor.UpperLeft, FontStyle.Bold, Color.white);
            var eqTitleRT = eqTitle.GetComponent<RectTransform>();
            eqTitleRT.anchorMin = new Vector2(0,1); eqTitleRT.anchorMax = new Vector2(1,1);
            eqTitleRT.anchoredPosition = new Vector2(0,-4); eqTitleRT.sizeDelta = new Vector2(0,28);

            var eqScrollGO = CreateUI("ScrollView", equippedWrap.transform);
            var eqRT = eqScrollGO.GetComponent<RectTransform>();
            eqRT.anchorMin = Vector2.zero; eqRT.anchorMax = Vector2.one; eqRT.offsetMin = new Vector2(0,0); eqRT.offsetMax = new Vector2(0,-36);
            var eqImg = eqScrollGO.AddComponent<Image>(); eqImg.color = new Color(1,1,1,0.06f);
            var eqMask = eqScrollGO.AddComponent<Mask>(); eqMask.showMaskGraphic = false;

            var eqScroll = eqScrollGO.AddComponent<ScrollRect>(); eqScroll.horizontal = false;
            var eqViewport = CreateUI("Viewport", eqScrollGO.transform);
            var vpRT = eqViewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one; vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            var vpMaskImg = eqViewport.AddComponent<Image>(); vpMaskImg.color = new Color(1,1,1,0.02f);
            var vpMask = eqViewport.AddComponent<Mask>(); vpMask.showMaskGraphic = false;
            var eqContent = CreateUI("Content", eqViewport.transform).GetComponent<RectTransform>();
            var vlg = eqContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft; vlg.spacing = 8; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var fitter = eqContent.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            eqScroll.viewport = vpRT; eqScroll.content = eqContent;

            // Шаблон карточки предмета слева
            var templates = CreateUI("Templates", card.transform);
            var itemCard = CreateCardButton(templates.transform, "ItemCard", "Название", "Экип.");
            itemCard.gameObject.SetActive(false);

            // Шаблон строки слота справа
            var slotRow = CreateSlotRow(templates.transform, "SlotRow", "Head", "—", "Снять");
            slotRow.gameObject.SetActive(false);

            // Логика
            var ui = card.AddComponent<Begin.UI.InventoryUIV2>();
            ui.itemsContainer = itemsContent;
            ui.itemButtonPrefab = itemCard;
            ui.slotsContainer = eqContent;
            ui.slotRowPrefab = slotRow;
            ui.statsText = statsText;

            Selection.activeGameObject = card;
            EditorUtility.DisplayDialog("Begin", "InventoryPanel v2 создан.", "OK");
        }

        // ---------- helpers ----------
        static GameObject EnsureCanvas() {
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) return canvas.gameObject;
            var go = new GameObject("Canvas", typeof(RectTransform));
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>(); go.AddComponent<GraphicRaycaster>();
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static void EnsureEventSystem() {
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>()) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputSystemUiType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiType != null) es.AddComponent(inputSystemUiType);
            else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        static GameObject CreateUI(string name, Transform parent) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Text CreateText(Transform parent, string content, int size, TextAnchor anchor, FontStyle style, Color color) {
            var go = CreateUI("Text", parent);
            var txt = go.AddComponent<Text>();
            txt.text = content; txt.fontSize = size; txt.alignment = anchor; txt.fontStyle = style; txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(200, 40);
            return txt;
        }

        static (ScrollRect scroll, RectTransform content) CreateScrollColumn(Transform parent, string name, string title) {
            var wrap = CreateUI(name, parent);
            var titleText = CreateText(wrap.transform, title, 20, TextAnchor.UpperLeft, FontStyle.Bold, Color.white);
            var tRT = titleText.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0,1); tRT.anchorMax = new Vector2(1,1); tRT.anchoredPosition = new Vector2(0,-4); tRT.sizeDelta = new Vector2(0,28);

            var scrollGO = CreateUI("ScrollView", wrap.transform);
            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0,0); sRT.anchorMax = new Vector2(1,1); sRT.offsetMin = new Vector2(0,0); sRT.offsetMax = new Vector2(0,-36);

            var img = scrollGO.AddComponent<Image>(); img.color = new Color(1,1,1,0.06f);
            var mask = scrollGO.AddComponent<Mask>(); mask.showMaskGraphic = false;

            var scroll = scrollGO.AddComponent<ScrollRect>(); scroll.horizontal = false;

            var viewport = CreateUI("Viewport", scrollGO.transform);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one; vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            var vpMaskImg = viewport.AddComponent<Image>(); vpMaskImg.color = new Color(1,1,1,0.02f);
            var vpMask = viewport.AddComponent<Mask>(); vpMask.showMaskGraphic = false;

            var content = CreateUI("Content", viewport.transform).GetComponent<RectTransform>();
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft; vlg.spacing = 8; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT; scroll.content = content;

            return (scroll, content);
        }

        // Карточка предмета: слева Name, справа Action ("Экип.")
        static Button CreateCardButton(Transform parent, string name, string labelLeft, string labelRight) {
            var go = CreateUI(name, parent);
            var img = go.AddComponent<Image>(); img.color = new Color(0.92f,0.92f,0.92f,1f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.92f,0.92f,0.92f,1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f,0.85f,0.85f,1f);
            colors.disabledColor = new Color(0.6f,0.6f,0.6f,1f);
            btn.colors = colors;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0,48);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 48;

            var nameGO = CreateUI("Name", go.transform);
            var nameT = nameGO.AddComponent<Text>();
            nameT.text = labelLeft; nameT.fontSize = 18; nameT.color = Color.black; nameT.alignment = TextAnchor.MiddleLeft;
            nameT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0,0); nameRT.anchorMax = new Vector2(1,1);
            nameRT.offsetMin = new Vector2(16,0); nameRT.offsetMax = new Vector2(-90,0);

            var actGO = CreateUI("Action", go.transform);
            var actT = actGO.AddComponent<Text>();
            actT.text = labelRight; actT.fontSize = 18; actT.color = Color.black; actT.alignment = TextAnchor.MiddleRight;
            actT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var actRT = actGO.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(1,0); actRT.anchorMax = new Vector2(1,1);
            actRT.pivot = new Vector2(1,0.5f); actRT.sizeDelta = new Vector2(80,0);

            go.AddComponent<Shadow>().effectColor = new Color(0,0,0,0.15f);

            return btn;
        }

        // Строка слота: SlotName (слева), ItemName (центр), Action (справа)
        static Button CreateSlotRow(Transform parent, string name, string slot, string item, string action) {
            var go = CreateUI(name, parent);
            var img = go.AddComponent<Image>(); img.color = new Color(0.2f,0.2f,0.2f,0.9f);
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0,48);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 48;

            var slotGO = CreateUI("Slot", go.transform);
            var slotT = slotGO.AddComponent<Text>();
            slotT.text = slot; slotT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotT.fontSize = 18; slotT.color = Color.white; slotT.alignment = TextAnchor.MiddleLeft;
            var slotRT = slotGO.GetComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0,0); slotRT.anchorMax = new Vector2(0.25f,1); slotRT.offsetMin = new Vector2(12,0); slotRT.offsetMax = new Vector2(-4,0);

            var nameGO = CreateUI("Name", go.transform);
            var nameT = nameGO.AddComponent<Text>();
            nameT.text = item; nameT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameT.fontSize = 18; nameT.color = new Color(1,1,1,0.9f); nameT.alignment = TextAnchor.MiddleLeft;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.25f,0); nameRT.anchorMax = new Vector2(1,1); nameRT.offsetMin = new Vector2(8,0); nameRT.offsetMax = new Vector2(-90,0);

            var actGO = CreateUI("Action", go.transform);
            var actT = actGO.AddComponent<Text>();
            actT.text = action; actT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            actT.fontSize = 18; actT.color = Color.white; actT.alignment = TextAnchor.MiddleRight;
            var actRT = actGO.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(1,0); actRT.anchorMax = new Vector2(1,1);
            actRT.pivot = new Vector2(1,0.5f); actRT.sizeDelta = new Vector2(80,0);

            go.AddComponent<Shadow>().effectColor = new Color(0,0,0,0.15f);

            return btn;
        }
    }
}
#endif

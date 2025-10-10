#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Begin.EditorTools {
    public static class BeginProgressHUDBuilder {
        [MenuItem("Tools/Begin/Create ▸ Progress HUD (Level + XP)")]
        public static void CreateHUD() {
            // Canvas
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (!canvas) {
                var go = new GameObject("Canvas", typeof(RectTransform));
                var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>(); go.AddComponent<GraphicRaycaster>();
                canvas = go.GetComponent<Canvas>();
            }

            // Root
            var root = new GameObject("ProgressHUD", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.90f);
            rt.anchorMax = new Vector2(0.45f, 0.98f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Level text
            var lvlGO = new GameObject("LevelText", typeof(RectTransform));
            lvlGO.transform.SetParent(root.transform, false);
            var lvlT = lvlGO.AddComponent<Text>();
            lvlT.font = font; lvlT.alignment = TextAnchor.MiddleLeft; lvlT.fontSize = 20; lvlT.color = Color.white;
            lvlT.text = "Ур. 1";
            var lvlRT = lvlGO.GetComponent<RectTransform>();
            lvlRT.anchorMin = new Vector2(0f, 0.5f); lvlRT.anchorMax = new Vector2(0.25f, 1f);
            lvlRT.offsetMin = new Vector2(0, 0); lvlRT.offsetMax = new Vector2(-6, 0);

            // XP Slider
            var xpGO = new GameObject("XPBar", typeof(RectTransform));
            xpGO.transform.SetParent(root.transform, false);
            var xpRT = xpGO.GetComponent<RectTransform>();
            xpRT.anchorMin = new Vector2(0.27f, 0.25f); xpRT.anchorMax = new Vector2(1f, 0.85f);
            xpRT.offsetMin = xpRT.offsetMax = Vector2.zero;

            var slider = xpGO.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 100; slider.value = 0;
            slider.transition = Selectable.Transition.None;

            // BG
            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(xpGO.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0,0,0,0.35f);

            // Fill area + Fill
            var faGO = new GameObject("Fill Area", typeof(RectTransform));
            faGO.transform.SetParent(xpGO.transform, false);
            var faRT = faGO.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0.02f, 0.2f); faRT.anchorMax = new Vector2(0.98f, 0.8f); faRT.offsetMin = faRT.offsetMax = Vector2.zero;

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(faGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>(); fillImg.color = new Color(0.15f, 0.65f, 1f, 1f);

            slider.fillRect = fillRT;
            slider.targetGraphic = fillImg;

            // XP text (правее бара)
            var xpTxtGO = new GameObject("XPText", typeof(RectTransform));
            xpTxtGO.transform.SetParent(root.transform, false);
            var xpTxt = xpTxtGO.AddComponent<Text>();
            xpTxt.font = font; xpTxt.alignment = TextAnchor.MiddleRight; xpTxt.fontSize = 16; xpTxt.color = Color.white;
            xpTxt.text = "0/100";
            var xpTxtRT = xpTxtGO.GetComponent<RectTransform>();
            xpTxtRT.anchorMin = new Vector2(0.78f, -0.05f); xpTxtRT.anchorMax = new Vector2(1f, 0.25f);
            xpTxtRT.offsetMin = xpTxtRT.offsetMax = Vector2.zero;

            // Logic
            var ui = root.AddComponent<Begin.UI.ProgressUI>();
            ui.levelText = lvlT;
            ui.xpBar = slider;
            ui.xpText = xpTxt;

            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Begin", "Progress HUD создан. Можно дублировать его в Hub/Battle сценах.", "OK");
        }
    }
}
#endif

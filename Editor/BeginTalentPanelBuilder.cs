#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Begin.EditorTools {
    public static class BeginTalentPanelBuilder {
        [MenuItem("Tools/Begin/Create ▸ Talents Panel (Auto)")]
        public static void CreatePanel() {
            var canvas = Object.FindFirstObjectByType<Canvas>()?.transform;
            if (!canvas) {
                var go = new GameObject("Canvas", typeof(RectTransform));
                var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>(); go.AddComponent<GraphicRaycaster>();
                canvas = go.transform;
            }

            var panel = new GameObject("TalentsPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            var bg = panel.AddComponent<Image>(); bg.color = new Color(0,0,0,0.55f);
            var prt = panel.GetComponent<RectTransform>(); prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;

            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(panel.transform, false);
            var cimg = card.AddComponent<Image>(); cimg.color = new Color(0.15f,0.15f,0.15f,0.95f);
            var crt = card.GetComponent<RectTransform>(); crt.anchorMin = new Vector2(0.15f,0.15f); crt.anchorMax = new Vector2(0.85f,0.85f); crt.offsetMin = crt.offsetMax = Vector2.zero;

            var title = NewText(card.transform, "Таланты", 28, TextAnchor.MiddleCenter, FontStyle.Bold);
            var trt = title.GetComponent<RectTransform>(); trt.anchorMin = trt.anchorMax = new Vector2(0.5f,1); trt.anchoredPosition = new Vector2(0,-32); trt.sizeDelta = new Vector2(800,44);

            // верхняя строка: очки + Сброс
            var top = new GameObject("TopBar", typeof(RectTransform));
            top.transform.SetParent(card.transform,false);
            var topRT = top.GetComponent<RectTransform>(); topRT.anchorMin = new Vector2(0.05f,0.86f); topRT.anchorMax = new Vector2(0.95f,0.92f); topRT.offsetMin = topRT.offsetMax = Vector2.zero;

            var points = NewText(top.transform, "Очки талантов: 0", 18, TextAnchor.MiddleLeft, FontStyle.Normal);
            var pointsRT = points.GetComponent<RectTransform>(); pointsRT.anchorMin=new Vector2(0,0); pointsRT.anchorMax=new Vector2(0.5f,1); pointsRT.offsetMin=pointsRT.offsetMax=Vector2.zero;

            var respecBtn = NewButton(top.transform, "Сбросить");
            var rrt = respecBtn.GetComponent<RectTransform>(); rrt.anchorMin=new Vector2(1,0); rrt.anchorMax=new Vector2(1,1); rrt.pivot=new Vector2(1,0.5f); rrt.sizeDelta=new Vector2(140,40);

            // список
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform));
            scrollGO.transform.SetParent(card.transform,false);
            var srt = scrollGO.GetComponent<RectTransform>(); srt.anchorMin=new Vector2(0.05f,0.08f); srt.anchorMax=new Vector2(0.95f,0.84f); srt.offsetMin=srt.offsetMax=Vector2.zero;
            var simg = scrollGO.AddComponent<Image>(); simg.color = new Color(1,1,1,0.06f);
            scrollGO.AddComponent<RectMask2D>();
            var scroll = scrollGO.AddComponent<ScrollRect>(); scroll.horizontal=false;

            var vp = new GameObject("Viewport", typeof(RectTransform));
            vp.transform.SetParent(scrollGO.transform,false);
            var vpRT = vp.GetComponent<RectTransform>(); vpRT.anchorMin=Vector2.zero; vpRT.anchorMax=Vector2.one; vpRT.offsetMin=vpRT.offsetMax=Vector2.zero;
            vp.AddComponent<RectMask2D>();
            scroll.viewport = vpRT;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vp.transform,false);
            var cntRT = content.GetComponent<RectTransform>(); cntRT.anchorMin=new Vector2(0,1); cntRT.anchorMax=new Vector2(1,1); cntRT.pivot=new Vector2(0.5f,1);
            var vlg = content.AddComponent<VerticalLayoutGroup>(); vlg.spacing=8; vlg.childAlignment=TextAnchor.UpperLeft; vlg.childForceExpandWidth=true; vlg.childForceExpandHeight=false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cntRT;

            // шаблон строки
            var templates = new GameObject("Templates", typeof(RectTransform));
            templates.transform.SetParent(card.transform,false);
            var row = NewRow(templates.transform);
            row.gameObject.SetActive(false);

            // компонент логики
            var ui = card.AddComponent<Begin.UI.TalentsUI>();
            ui.listContainer = cntRT;
            ui.rowPrefab = row;
            ui.pointsText = points;
            ui.respecButton = respecBtn;

            // подключим дерево (создай SampleTree)
            var tree = Resources.Load<Begin.Talents.TalentTree>("Talents/SampleTree");
            if (!tree) Debug.LogWarning("SampleTree не найден. Создай через Tools/Begin/Create ▸ Sample Talent Tree");
            ui.tree = tree;

            Selection.activeGameObject = card;
            EditorUtility.DisplayDialog("Begin","TalentsPanel создан. Привяжи его в HubUI как talentsPanel.","OK");
        }

        static Text NewText(Transform p, string val, int size, TextAnchor a, FontStyle s) {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(p,false);
            var t = go.AddComponent<Text>(); t.text = val; t.fontSize = size; t.alignment = a; t.fontStyle = s; t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(200,40);
            return t;
        }
        static Button NewButton(Transform p, string label) {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(p,false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.9f,0.9f,0.9f,1);
            var b = go.AddComponent<Button>();
            var txt = NewText(go.transform, label, 18, TextAnchor.MiddleCenter, FontStyle.Normal);
            txt.color = Color.black;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(120,40);
            return b;
        }
        static Button NewRow(Transform p) {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(p,false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.92f,0.92f,0.92f,1);
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0,56);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 56;

            var name = NewText(go.transform, "Название [0/3]", 18, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.gameObject.name = "Name";
            var nrt = name.GetComponent<RectTransform>(); nrt.anchorMin=new Vector2(0,0); nrt.anchorMax=new Vector2(1,1); nrt.offsetMin=new Vector2(16,16); nrt.offsetMax=new Vector2(-80,-28); name.color = Color.black;

            var desc = NewText(go.transform, "описание", 14, TextAnchor.LowerLeft, FontStyle.Normal);
            desc.gameObject.name = "Desc";
            var drt = desc.GetComponent<RectTransform>(); drt.anchorMin=new Vector2(0,0); drt.anchorMax=new Vector2(1,1); drt.offsetMin=new Vector2(16,6); drt.offsetMax=new Vector2(-80,-34); desc.color = new Color(0,0,0,0.75f);

            var act = NewText(go.transform, "+", 22, TextAnchor.MiddleRight, FontStyle.Bold);
            act.gameObject.name = "Action";
            var art = act.GetComponent<RectTransform>(); art.anchorMin=new Vector2(1,0); art.anchorMax=new Vector2(1,1); art.pivot=new Vector2(1,0.5f); art.sizeDelta=new Vector2(60,0);
            act.color = Color.black;

            go.AddComponent<Shadow>().effectColor = new Color(0,0,0,0.15f);
            return btn;
        }
    }
}
#endif

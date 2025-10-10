using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Begin.UI {
    public class DamageNumbers : MonoBehaviour {
        static DamageNumbers _inst;
        Canvas canvas;
        Camera cam;
        Transform canvasT;

        // базовый масштаб world-space Canvas (очень маленький)
        const float BASE_CANVAS_SCALE = 0.01f;

        void Awake() {
            _inst = this;
            cam = Camera.main;

            // World-Space Canvas (крошечный)
            var go = new GameObject("DamageCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            canvasT = go.transform;
            canvasT.localScale = Vector3.one * BASE_CANVAS_SCALE;

            // размер канваса в «мире» маленький — этого достаточно
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1f, 1f);
        }

        void LateUpdate() {
            if (!cam) cam = Camera.main;
            if (canvas && cam) {
                // биллборд к камере
                canvasT.rotation = Quaternion.LookRotation(canvasT.position - cam.transform.position);
            }
        }

        /// <summary>
        /// Показать число урона над worldPos. scale ~ 1 = маленькое.
        /// </summary>
        public static void Show(Vector3 worldPos, int amount, Color? color = null, float scale = 1f) {
            if (_inst == null) {
                var host = new GameObject("~DamageNumbers");
                _inst = host.AddComponent<DamageNumbers>();
            }
            _inst.StartCoroutine(_inst.Spawn(worldPos + Vector3.up * 1.2f, amount, color ?? Color.yellow, scale));
        }

        IEnumerator Spawn(Vector3 worldPos, int amount, Color color, float scale) {
            // создаём текст
            var go = new GameObject("Num", typeof(RectTransform));
            go.transform.SetParent(canvasT, false);
            var rt = go.GetComponent<RectTransform>();
            rt.position = worldPos;

            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 40;                        // маленький размер
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.text = amount.ToString();

            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0,0,0,0.45f);
            sh.effectDistance = new Vector2(0, -0.5f);

            // финальный масштаб с учётом дистанции (чуть-чуть компенсируем, без «плакатов»)
            float dist = (cam ? Vector3.Distance(worldPos, cam.transform.position) : 10f);
            // чем дальше — чуть крупнее, но в разумных пределах
            float distMul = Mathf.Clamp(dist * 0.04f, 0.7f, 1.6f);
            rt.localScale = Vector3.one * (1.1f * scale * distMul);  // базово маленькое

            // анимация: небольшой подскок и затухание
            float t = 0f, life = 0.8f;
            Vector3 start = worldPos;
            Vector3 end = worldPos + new Vector3(Random.Range(-0.15f,0.15f), 0.8f, 0f);  // ниже и компактнее
            while (t < life) {
                t += Time.deltaTime;
                float k = t / life;
                rt.position = Vector3.Lerp(start, end, k);
                var c = txt.color; c.a = 1f - k; txt.color = c;
                yield return null;
            }
            Destroy(go);
        }
    }
}

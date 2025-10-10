using UnityEngine;
using Begin.AI;
using Begin.World;
using Begin.Core;
using Begin.UI;
using Begin.Combat;
using Begin.Talents;
using Begin.Progress;
using Begin.Player;

namespace Begin.Battleflow {
    public class BattleManager : MonoBehaviour {
        [Header("Player Avatar")]
        [Tooltip("Префаб с визуалом игрока (например, Low Poly Fantasy Warrior). Если не задан — появится капсула.")]
        [SerializeField] GameObject playerAvatarPrefab;

        [Tooltip("Смещение префаба игрока относительно корня с CharacterController.")]
        [SerializeField] Vector3 playerAvatarOffset = Vector3.zero;

        [Tooltip("Автоматически опустить модель к земле по габаритам меша и капсулы персонажа.")]
        [SerializeField] bool autoAlignPlayerAvatar = true;

        BattleHUD hud;
        WaveSpawner spawner;
        PlayerHealth player;

        void Start() {
            EnsureEventSystem();
            // карта
            var mapGO = new GameObject("Map");
            var gen = mapGO.AddComponent<MapGeneratorSimple>();
            gen.obstacles = new GameObject[] { MakeCube("RockA"), MakeCube("RockB") };
            gen.Generate();

            // игрок
            var playerGO = PlayerAvatarBuilder.EnsurePlayerRoot(playerAvatarPrefab, playerAvatarOffset, autoAlignPlayerAvatar, Camera.main);
            player = playerGO.GetComponent<PlayerHealth>();

            // камера
            var cam = Camera.main;
            if (cam) {
                var follow = cam.GetComponent<Begin.Control.CameraFollow>() ?? cam.gameObject.AddComponent<Begin.Control.CameraFollow>();
                follow.target = playerGO.transform;
            }

            // HUD
            hud = CreateHUD(player);

            // спавнер волн
            // спавнер волн (новый API)
        var spGO = new GameObject("WaveSpawner");
        spawner = spGO.AddComponent<WaveSpawner>();
        spawner.player = playerGO.transform;                              // цель для врагов
        spawner.table  = Resources.Load<Begin.AI.WaveTable>("Waves/SampleWaves"); // или перетащи в инспекторе
        spawner.delayBetweenWaves = 2f;

        // обновление HUD при смене волны (см. патч WaveSpawner ниже)
        spawner.onWaveChanged += (cur, total) => hud.SetWave(cur, total);

        // финал боя: бонус XP и возврат в Hub
        spawner.onAllCleared += () => {
            int waves = spawner ? spawner.totalWaves : 1;
            int bonus = 50 + 25 * Mathf.Max(0, waves - 1);
            ProgressService.AddXP(bonus);
            Begin.Core.SceneLoader.Load("Hub");
        };
        }

        Transform[] MakeSpawnPoints() {
            var arr = new Transform[4];
            for (int i = 0; i < 4; i++) {
                var g = new GameObject("Spawn_" + i);
                g.transform.position = new Vector3((i < 2 ? -1 : 1) * 18, 0, (i % 2 == 0 ? -1 : 1) * 18);
                arr[i] = g.transform;
            }
            return arr;
        }

        GameObject MakeCube(string name) {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.localScale = new Vector3(2, 1, 2);
            return g;
        }

        BattleHUD CreateHUD(PlayerHealth ph) {
            // Canvas
            var canvasGO = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGO.AddComponent<UnityEngine.Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // шрифт Unity 6
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // === HP Slider (c фоном и заполнением) ===
            var hpGO = new GameObject("HP", typeof(RectTransform));
            hpGO.transform.SetParent(canvasGO.transform, false);
            var hpRT = hpGO.GetComponent<RectTransform>();
            hpRT.anchorMin = new Vector2(0.05f, 0.90f);
            hpRT.anchorMax = new Vector2(0.45f, 0.95f);
            hpRT.offsetMin = hpRT.offsetMax = Vector2.zero;

            var hp = hpGO.AddComponent<UnityEngine.UI.Slider>();
            hp.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            hp.minValue = 0f; hp.maxValue = ph.max; hp.value = ph.current;
            hp.transition = UnityEngine.UI.Selectable.Transition.None;

            // Background
            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(hpGO.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.35f);

            // Fill Area
            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(hpGO.transform, false);
            var faRT = fillAreaGO.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0.02f, 0.2f);   // чуть врезать края
            faRT.anchorMax = new Vector2(0.98f, 0.8f);
            faRT.offsetMin = faRT.offsetMax = Vector2.zero;

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(0.86f, 0.18f, 0.18f, 1f); // красный

            hp.fillRect = fillRT;
            hp.targetGraphic = fillImg;

            // === HP Text (поверх бара) ===
            var hpTextGO = new GameObject("HPText", typeof(RectTransform));
            hpTextGO.transform.SetParent(hpGO.transform, false);
            var hpTextRT = hpTextGO.GetComponent<RectTransform>();
            hpTextRT.anchorMin = Vector2.zero;
            hpTextRT.anchorMax = Vector2.one;
            hpTextRT.offsetMin = hpTextRT.offsetMax = Vector2.zero;

            var hpText = hpTextGO.AddComponent<UnityEngine.UI.Text>();
            hpText.font = font;                 // ← используем существующую переменную font
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.fontSize = 18;
            hpText.color = Color.white;


            // === Wave Text ===
            var waveGO = new GameObject("WaveText", typeof(RectTransform));
            waveGO.transform.SetParent(canvasGO.transform, false);
            var waveText = waveGO.AddComponent<UnityEngine.UI.Text>();
            waveText.font = font; waveText.alignment = TextAnchor.MiddleCenter; waveText.fontSize = 24; waveText.color = Color.white;
            var waveRT = waveGO.GetComponent<RectTransform>();
            waveRT.anchorMin = new Vector2(0.5f, 0.92f); waveRT.anchorMax = new Vector2(0.5f, 0.97f); waveRT.sizeDelta = new Vector2(260, 36);

            // === Gold Text ===
            var goldGO = new GameObject("GoldText", typeof(RectTransform));
            goldGO.transform.SetParent(canvasGO.transform, false);
            var goldText = goldGO.AddComponent<UnityEngine.UI.Text>();
            goldText.font = font; goldText.alignment = TextAnchor.MiddleLeft; goldText.fontSize = 20; goldText.color = Color.white;
            var goldRT = goldGO.GetComponent<RectTransform>();
            goldRT.anchorMin = new Vector2(0.05f, 0.84f); goldRT.anchorMax = new Vector2(0.25f, 0.89f); goldRT.offsetMin = goldRT.offsetMax = Vector2.zero;

            // === Exit Button ===
            var exitBtn = CreateButton(canvasGO.transform, new Vector2(0.95f, 0.95f), "Выйти", font);

            // Wire HUD
            var hud = canvasGO.AddComponent<BattleHUD>();
            hud.hp = hp;
            hud.waveText = waveText;
            hud.goldText = goldText;
            hud.exitButton = exitBtn;
            hud.player = ph;
            hud.hpText = hpText;

            return hud;
}

        UnityEngine.UI.Button CreateButton(Transform parent, Vector2 anchor, string label, Font font) {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.9f,0.9f,0.9f,1f);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = new Vector2(160, 44);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var txt = textGO.AddComponent<UnityEngine.UI.Text>();
            txt.font = font; txt.text = label; txt.alignment = TextAnchor.MiddleCenter; txt.fontSize = 20;
            var trt = textGO.GetComponent<RectTransform>(); trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;

            return btn;
        }

        void EnsureEventSystem() {
            if (!UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()) {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // если есть новый Input System — добавим его модуль, иначе старый
                var inputSystemUiType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemUiType != null) es.AddComponent(inputSystemUiType);
                else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}

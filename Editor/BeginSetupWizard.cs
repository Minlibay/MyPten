#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Begin.Core;
using Begin.PlayerData;
using Begin.UI;

public static class BeginSetupWizard {
    [MenuItem("Tools/TopDown Begin/Create Bootstrap & Character Creation")]
    public static void CreateAll() {
        // 1) Гарантируем папки
        EnsureFolders(
            "Assets/Scenes",
            "Assets/ScriptableObjects",
            "Assets/ScriptableObjects/Classes"
        );

        // 2) Создаём классы
        var w = ScriptableObject.CreateInstance<CharacterClass>();
        w.id="warrior"; w.displayName="Воин"; w.baseHP=120; w.baseSTR=8; w.description="Живучий боец ближнего боя.";
        AssetDatabase.CreateAsset(w, "Assets/ScriptableObjects/Classes/Warrior.asset");

        var r = ScriptableObject.CreateInstance<CharacterClass>();
        r.id="rogue"; r.displayName="Плут"; r.baseHP=90; r.baseDEX=9; r.description="Быстрый и смертоносный.";
        AssetDatabase.CreateAsset(r, "Assets/ScriptableObjects/Classes/Rogue.asset");

        var m = ScriptableObject.CreateInstance<CharacterClass>();
        m.id="mage"; m.displayName="Маг"; m.baseHP=80; m.baseINT=10; m.description="Контроль и урон по площади.";
        AssetDatabase.CreateAsset(m, "Assets/ScriptableObjects/Classes/Mage.asset");

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        // 3) Boot
        var boot = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureGameManager();
        new GameObject("BootLoader").AddComponent<BootLoader>();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/Boot.unity");

        // 4) CharacterCreation
        var cc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureGameManager();
        CreateCharacterCreationUI(new CharacterClass[]{ w, r, m });
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/CharacterCreation.unity");

        // 5) Hub (заглушка)
        var hub = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureGameManager();
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGO.AddComponent<UnityEngine.Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var t = new GameObject("Info", typeof(RectTransform));
        t.transform.SetParent(canvasGO.transform, false);
        var text = t.AddComponent<UnityEngine.UI.Text>();
        text.text = "HUB (заглушка). Создание персонажа завершено.";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var rt = t.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f); rt.sizeDelta=new Vector2(600,60);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/Hub.unity");

        // 6) Вернуться в Boot
        EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
        EditorUtility.DisplayDialog("Begin Setup", "Созданы Boot, CharacterCreation, Hub и классы персонажей.", "OK");
    }

    // --- helpers ---
    static void EnsureFolders(params string[] paths) {
        foreach (var path in paths) CreateFolderRecursive(path);
    }

    static void CreateFolderRecursive(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string acc = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++) {
            string next = acc + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) {
                AssetDatabase.CreateFolder(acc, parts[i]);
            }
            acc = next;
        }
    }

    static void EnsureGameManager() {
        if (Object.FindObjectOfType<GameManager>() == null) {
            new GameObject("GameManager").AddComponent<GameManager>();
        }
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null) {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    static void CreateCharacterCreationUI(CharacterClass[] classes) {
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGO.AddComponent<UnityEngine.Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Name input
        var nameGO = new GameObject("NameInput", typeof(RectTransform));
        nameGO.transform.SetParent(canvasGO.transform, false);
        var nameRT = nameGO.GetComponent<RectTransform>(); nameRT.anchorMin = new Vector2(0.5f,0.8f); nameRT.anchorMax=nameRT.anchorMin; nameRT.sizeDelta=new Vector2(300,40);
        var nameInput = nameGO.AddComponent<UnityEngine.UI.InputField>();
        var nameText = new GameObject("Text", typeof(RectTransform)).AddComponent<UnityEngine.UI.Text>();
        nameText.transform.SetParent(nameGO.transform,false);
        nameText.text = ""; nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var textRT = nameText.GetComponent<RectTransform>(); textRT.anchorMin=Vector2.zero; textRT.anchorMax=Vector2.one; textRT.offsetMin=textRT.offsetMax=Vector2.zero;
        nameInput.textComponent = nameText;
        var ph = new GameObject("Placeholder", typeof(RectTransform)).AddComponent<UnityEngine.UI.Text>();
        ph.transform.SetParent(nameGO.transform,false); ph.text="Имя персонажа"; ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); ph.color = new Color(1,1,1,0.5f);
        var phRT = ph.GetComponent<RectTransform>(); phRT.anchorMin=Vector2.zero; phRT.anchorMax=Vector2.one; phRT.offsetMin=phRT.offsetMax=Vector2.zero;
        nameInput.placeholder = ph;

        // Selected class label
        var selLabel = new GameObject("SelectedClass", typeof(RectTransform));
        selLabel.transform.SetParent(canvasGO.transform,false);
        var selTxt = selLabel.AddComponent<UnityEngine.UI.Text>();
        selTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        selTxt.text = "Класс: —";
        var selRT = selLabel.GetComponent<RectTransform>(); selRT.anchorMin=selRT.anchorMax=new Vector2(0.5f,0.75f); selRT.sizeDelta=new Vector2(300,30);

        // Error label
        var errGO = new GameObject("ErrorText", typeof(RectTransform));
        errGO.transform.SetParent(canvasGO.transform,false);
        var errTxt = errGO.AddComponent<UnityEngine.UI.Text>();
        errTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        errTxt.color = Color.red; errTxt.text = "";
        var errRT = errGO.GetComponent<RectTransform>(); errRT.anchorMin=errRT.anchorMax=new Vector2(0.5f,0.2f); errRT.sizeDelta=new Vector2(380,30);

        // Classes container
        var container = new GameObject("Classes", typeof(RectTransform));
        container.transform.SetParent(canvasGO.transform,false);
        var cRT = container.GetComponent<RectTransform>(); cRT.anchorMin=new Vector2(0.5f,0.5f); cRT.anchorMax=cRT.anchorMin; cRT.sizeDelta=new Vector2(640,200);

        // Button prefab
        var btnPrefab = CreateButton(canvasGO.transform, new Vector2(0,0), "ClassButton");
        btnPrefab.gameObject.SetActive(false);

        // Confirm button
        var confirm = CreateButton(canvasGO.transform, new Vector2(0.5f,0.3f), "Создать");
        // Wire UI script
        var ui = canvasGO.AddComponent<CharacterCreationUI>();
        ui.nameInput = nameInput;
        ui.classesContainer = cRT;
        ui.classButtonPrefab = btnPrefab;
        ui.confirmButton = confirm;
        ui.selectedClassText = selTxt;
        ui.errorText = errTxt;
        ui.availableClasses = classes;
        nameInput.onValueChanged.AddListener(ui.OnNameChanged);
    }

    static UnityEngine.UI.Button CreateButton(Transform parent, Vector2 anchor, string label) {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = new Vector2(200, 48);
        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var txt = textGO.AddComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = label; txt.alignment = TextAnchor.MiddleCenter;
        var trt = textGO.GetComponent<RectTransform>(); trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }
}
#endif

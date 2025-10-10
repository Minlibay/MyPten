using UnityEngine;
using UnityEngine.UI;
using Begin.PlayerData;
using Begin.Core;

namespace Begin.UI {
    public class CharacterCreationUI : MonoBehaviour {
        [Header("Inputs")]
        public InputField nameInput;
        public Transform classesContainer;   // parent where class buttons will be spawned
        public Button classButtonPrefab;
        public Button confirmButton;
        public Text selectedClassText;
        public Text errorText;

        [Header("Data")]
        public CharacterClass[] availableClasses;

        CharacterClass _selected;

        void Start() {
            BuildClassButtons();
            confirmButton.onClick.AddListener(OnConfirm);
            RefreshUI();
        }

void BuildClassButtons() {
    // очистить предыдущие
    foreach (Transform c in classesContainer) Destroy(c.gameObject);

    if (availableClasses == null || availableClasses.Length == 0) return;

    foreach (var cc in availableClasses) {
        var b = Instantiate(classButtonPrefab, classesContainer);
        b.gameObject.SetActive(true); // включаем, иначе он остаётся скрытым
        var ccLocal = cc;

        // подпись
        var txt = b.GetComponentInChildren<Text>();
        if (txt != null) {
            txt.text = ccLocal.displayName;
            txt.color = Color.black;   // текст чёрным
            txt.fontSize = 24;
        }

        // делаем кнопку чуть серой, чтобы было видно
        var img = b.GetComponent<Image>();
        if (img != null) {
            img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        }

        // обработчик клика
        b.onClick.AddListener(() => {
            _selected = ccLocal;
            RefreshUI();
        });
    }
}

        void RefreshUI() {
            selectedClassText.text = _selected == null ? "Класс: —" : $"Класс: {_selected.displayName}";
            errorText.text = "";
            confirmButton.interactable = _selected != null && !string.IsNullOrWhiteSpace(nameInput.text);
        }

        public void OnNameChanged(string _) => RefreshUI();

        void OnConfirm() {
            if (_selected == null) { errorText.text = "Выберите класс."; return; }
            if (string.IsNullOrWhiteSpace(nameInput.text)) { errorText.text = "Введите имя."; return; }
            var p = PlayerProfile.CreateNew(nameInput.text.Trim(), _selected.id);
            PlayerProfile.Save(p);
            GameManager.I.CurrentProfile = p;
            SceneLoader.Load("Hub");
        }

        public void OnClearProfile() {
            PlayerProfile.Clear();
            RefreshUI();
        }
    }
}

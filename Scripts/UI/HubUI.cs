using UnityEngine;
using UnityEngine.UI;

namespace Begin.UI {
    public class HubUI : MonoBehaviour {
        [Header("Buttons")]
        public Button battleButton;
        public Button inventoryButton;
        public Button talentsButton;
        public Button vendorButton;

        [Header("Panels")]
        public GameObject inventoryPanel;
        public GameObject talentsPanel;
        public GameObject vendorPanel;

        GameObject _activePanel;

        void Start() {
            if (battleButton)   battleButton.onClick.AddListener(OnBattle);
            if (inventoryButton) inventoryButton.onClick.AddListener(() => TogglePanel(inventoryPanel));
            if (talentsButton)   talentsButton.onClick.AddListener(() => TogglePanel(talentsPanel));
            if (vendorButton)    vendorButton.onClick.AddListener(() => TogglePanel(vendorPanel));

            // на всякий случай – всё закрыть при старте
            CloseAll();
        }

        void Update() {
            // ESC закрывает всё
            if (Input.GetKeyDown(KeyCode.Escape)) CloseAll();
        }

        void TogglePanel(GameObject panel) {
            if (panel == null) return;

            // повторный клик по той же панели — закрыть
            if (_activePanel == panel) {
                panel.SetActive(false);
                _activePanel = null;
                return;
            }

            // закрыть предыдущую и открыть новую
            CloseAll();
            panel.SetActive(true);
            _activePanel = panel;
        }

        public void CloseAll() {
            if (inventoryPanel) inventoryPanel.SetActive(false);
            if (talentsPanel)   talentsPanel.SetActive(false);
            if (vendorPanel)    vendorPanel.SetActive(false);
            _activePanel = null;
        }

        void OnBattle() {
    Begin.Core.SceneLoader.Load("Battle");
        }
    }
}

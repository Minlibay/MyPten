using UnityEngine;
using UnityEngine.UI;
using Begin.Combat;
using Begin.Economy;

namespace Begin.UI {
    public class BattleHUD : MonoBehaviour {
        public Slider hp;
        public Text hpText;        // ← НОВОЕ
        public Text waveText;
        public Text goldText;
        public Button exitButton;
        public PlayerHealth player;

        void Start() {
            if (exitButton) exitButton.onClick.AddListener(()=> Begin.Core.SceneLoader.Load("Hub"));
            InvokeRepeating(nameof(Refresh), 0.1f, 0.15f);
        }

        void Refresh() {
            if (player) {
                if (hp) { hp.maxValue = player.max; hp.value = player.current; }
                if (hpText) hpText.text = $"{Mathf.CeilToInt(player.current)}/{Mathf.CeilToInt(player.max)}";
            }
            if (goldText) goldText.text = $"Gold: {Currency.Gold}";
        }

        public void SetWave(int current, int total) {
            if (waveText) waveText.text = $"Wave {current}/{total}";
        }
    }
}

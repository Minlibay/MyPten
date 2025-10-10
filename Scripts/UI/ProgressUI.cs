using UnityEngine;
using UnityEngine.UI;
using Begin.Progress;

namespace Begin.UI {
    public class ProgressUI : MonoBehaviour {
        public Text levelText;
        public Slider xpBar;
        public Text xpText;

        void OnEnable() {
            ProgressService.OnChanged += Refresh;
            ProgressService.OnLevelUp += _ => Refresh();
            Refresh();
        }
        void OnDisable() {
            ProgressService.OnChanged -= Refresh;
            ProgressService.OnLevelUp -= _ => Refresh();
        }

        void Refresh() {
            if (levelText) levelText.text = $"Ур. {ProgressService.Level}";
            if (xpBar) {
                xpBar.minValue = 0;
                xpBar.maxValue = ProgressService.XPNeeded;
                xpBar.value = ProgressService.XP;
            }
            if (xpText) xpText.text = $"{ProgressService.XP}/{ProgressService.XPNeeded}";
        }
    }
}

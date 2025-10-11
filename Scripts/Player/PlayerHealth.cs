using UnityEngine;
using Begin.Core;
using Begin.PlayerData;

namespace Begin.Combat {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour {
        public int baseHP = 100;
        public float max;
        public float current;

        void OnEnable() {
            PlayerStatService.OnChanged += HandleStatsChanged;
            HandleStatsChanged(PlayerStatService.Current);
        }

        void OnDisable() {
            PlayerStatService.OnChanged -= HandleStatsChanged;
        }

        void HandleStatsChanged(PlayerDerivedStats stats) {
            max = Mathf.Max(baseHP, stats.MaxHealth);
            if (current <= 0 || current > max) current = max;
        }

        public void Take(float dmg) {
            current -= dmg;
            if (current <= 0f) SceneLoader.Load("Hub");
        }
    }
}

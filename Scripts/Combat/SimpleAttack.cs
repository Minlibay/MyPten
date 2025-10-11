using UnityEngine;
using Begin.PlayerData;
using Begin.UI;

namespace Begin.Combat {
    public class SimpleAttack : MonoBehaviour {
        public float distance = 2.5f;
        public float baseCooldown = 0.8f;

        float _cooldownTimer;
        PlayerDerivedStats _stats;

        void OnEnable() {
            PlayerStatService.OnChanged += HandleStatsChanged;
            HandleStatsChanged(PlayerStatService.Current);
        }

        void OnDisable() {
            PlayerStatService.OnChanged -= HandleStatsChanged;
        }

        void HandleStatsChanged(PlayerDerivedStats stats) {
            _stats = stats;
        }

        void Update() {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space) && _cooldownTimer <= 0f) {
                PerformAttack();
                float speedMul = Mathf.Max(0.2f, _stats.AttackSpeedMultiplier);
                float cooldownMul = Mathf.Max(0.1f, 1f - _stats.CooldownReduction);
                _cooldownTimer = Mathf.Max(0.1f, baseCooldown * cooldownMul / speedMul);
            }
        }

        void PerformAttack() {
            float damage = Mathf.Max(1f, _stats.AttackPower);
            bool crit = Random.value < _stats.CritChance;
            if (crit) damage *= 1.5f;

            var origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, transform.forward, out var hit, distance)) {
                var h = hit.collider.GetComponent<Health>();
                if (h != null) {
                    h.Take(damage);
                    DamageNumbers.Show(hit.point, Mathf.RoundToInt(damage), crit ? Color.red : Color.yellow, 1f);
                }
            }
        }
    }
}

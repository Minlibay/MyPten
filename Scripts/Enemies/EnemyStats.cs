using UnityEngine;
using Begin.Enemies;
using Begin.Combat;
using Begin.Core;

namespace Begin.AI {
    [RequireComponent(typeof(Health))]
    public class EnemyStats : MonoBehaviour, IPooled {
        public EnemyDefinition def;
        public float moveSpeed;
        public float touchDamage;
        public float touchInterval;

        Health _health;

        Health _health;

        void Awake() {
            _health = GetComponent<Health>();
            ApplyDefinition(true);
        }

        public void ApplyDefinition(bool refill) {
            if (!def) return;
            moveSpeed = def.moveSpeed;
            touchDamage = def.touchDamage;
            touchInterval = Mathf.Max(0.1f, def.touchInterval);
            if (_health) {
                _health.autoDestroy = false;
                _health.SetMax(def.maxHP, refill);
            }
        }

        public void OnSpawned() { ApplyDefinition(true); }

        public void OnDespawned() { if (_health) _health.ResetState(true); }
    }
}

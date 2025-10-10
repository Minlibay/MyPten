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

        [SerializeField, HideInInspector]
        Health _healthComponent;

        void Awake() {
            CacheComponents();
            ApplyDefinition(true);
        }

        void OnValidate() {
            CacheComponents();
        }

        void CacheComponents() {
            if (!_healthComponent)
                _healthComponent = GetComponent<Health>();
        }

        public void ApplyDefinition(bool refill) {
            if (!def) return;
            moveSpeed = def.moveSpeed;
            touchDamage = def.touchDamage;
            touchInterval = Mathf.Max(0.1f, def.touchInterval);
            if (_healthComponent) {
                _healthComponent.autoDestroy = false;
                _healthComponent.SetMax(def.maxHP, refill);
            }
        }

        public void OnSpawned() { ApplyDefinition(true); }

        public void OnDespawned() { if (_healthComponent) _healthComponent.ResetState(true); }
    }
}

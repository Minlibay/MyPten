using UnityEngine;
using Begin.Enemies;
using Begin.Combat;

namespace Begin.AI {
    [RequireComponent(typeof(Health))]
    public class EnemyStats : MonoBehaviour {
        public EnemyDefinition def;
        public float moveSpeed;
        public float touchDamage;

        void Awake() {
            if (!def) return;
            moveSpeed = def.moveSpeed;
            touchDamage = def.touchDamage;
            var h = GetComponent<Health>();
            h.max = def.maxHP; h.current = def.maxHP;
        }
    }
}

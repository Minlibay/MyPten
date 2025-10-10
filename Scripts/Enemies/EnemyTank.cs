using UnityEngine;

namespace Begin.AI {
    public class EnemyTank : EnemyBase {
        void Update() {
            // просто уверенный подход, медленно
            float dist = ChaseSimple(0.65f);
            ApplyTouchDamage(dist, 1.2f);
        }
    }
}

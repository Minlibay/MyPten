using UnityEngine;

namespace Begin.AI {
    public class EnemyTank : EnemyBase {
        void Update() {
            // просто уверенный подход, медленно
            ChaseSimple(0.65f);
        }
    }
}

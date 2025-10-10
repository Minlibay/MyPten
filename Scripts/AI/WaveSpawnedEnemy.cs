using Begin.Combat;
using UnityEngine;

namespace Begin.AI {
    [RequireComponent(typeof(Health))]
    public class WaveSpawnedEnemy : MonoBehaviour {
        WaveSpawner owner;
        Health health;

        void Awake() {
            health = GetComponent<Health>();
        }

        public void Attach(WaveSpawner spawner) {
            if (health) health.onDeath -= HandleDeath;
            owner = spawner;
            if (owner && health) health.onDeath += HandleDeath;
        }

        void HandleDeath() {
            owner?.OnSpawnedEnemyDeath(gameObject);
        }

        void OnDisable() {
            if (health) health.onDeath -= HandleDeath;
            owner = null;
        }
    }
}

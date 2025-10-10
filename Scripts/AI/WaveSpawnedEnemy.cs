using Begin.Combat;
using Begin.Enemies;
using UnityEngine;

namespace Begin.AI {
    [RequireComponent(typeof(Health))]
    public class WaveSpawnedEnemy : MonoBehaviour {
        WaveSpawner owner;
        Health health;
        EnemyDefinition definition;

        void Awake() {
            health = GetComponent<Health>();
        }

        public void Attach(WaveSpawner spawner, EnemyDefinition def) {
            if (health) health.onDeath -= HandleDeath;
            owner = spawner;
            definition = def;
            if (owner && health) health.onDeath += HandleDeath;
        }

        void HandleDeath() {
            owner?.OnSpawnedEnemyDeath(gameObject);
        }

        void OnDisable() {
            if (health) health.onDeath -= HandleDeath;
            owner = null;
            definition = null;
        }

        public EnemyDefinition Definition => definition;
    }
}

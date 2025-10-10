using UnityEngine;
using Begin.Combat;

namespace Begin.AI {
    public class EnemyBullet : MonoBehaviour, Begin.Core.IPooled {
        Vector3 velocity;
        float damage;
        float life;

        public void Init(Vector3 vel, float dmg) {
            velocity = vel; damage = dmg; life = 3f;
        }

        void Update() {
            transform.position += velocity * Time.deltaTime;
            life -= Time.deltaTime;
            if (life <= 0f) Begin.Core.Pool.Despawn(gameObject);
        }

        void OnTriggerEnter(Collider other) {
            if (other.TryGetComponent<PlayerHealth>(out var ph)) {
                ph.Take(damage);
                Begin.Core.Pool.Despawn(gameObject);
            } else if (!other.isTrigger) {
                Begin.Core.Pool.Despawn(gameObject);
            }
        }

        public void OnSpawned() { }
        public void OnDespawned() { }
    }
}

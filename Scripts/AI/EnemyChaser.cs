using UnityEngine;
using Begin.Combat;

namespace Begin.AI {
    [RequireComponent(typeof(Health))]
    public class EnemyChaser : MonoBehaviour {
        public float speed = 3.5f;
        public float touchDamage = 10f;
        public float touchCooldown = 1.0f;
        Transform target;
        float cd;
        Health hp;

        void Start() {
            hp = GetComponent<Health>();
            var pl = GameObject.FindWithTag("Player");
            if (pl) target = pl.transform;
        }

        void Update() {
            if (!target) return;
            cd -= Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            var look = target.position; look.y = transform.position.y;
            transform.LookAt(look);
        }

        void OnTriggerStay(Collider other) {
            if (cd > 0f) return;
            var ph = other.GetComponent<PlayerHealth>();
            if (ph) {
                ph.Take(touchDamage);
                cd = touchCooldown;
            }
        }
    }
}

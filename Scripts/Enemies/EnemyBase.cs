using UnityEngine;
using Begin.Combat;

namespace Begin.AI {
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyBase : MonoBehaviour {
        protected Transform target;
        protected PlayerHealth playerHealth;
        protected EnemyMotor motor;
        protected EnemyStats stats;

        [Header("Distances")]
        public float chaseDistance = 30f;   // дальше — стоим
        public float keepDistance  = 0.9f;  // множитель к stoppingDistance мотора

        protected virtual void Awake() {
            motor = GetComponent<EnemyMotor>();
            stats = GetComponent<EnemyStats>();
            if (stats && stats.def)
                motor.acceleration = Mathf.Max(12f, stats.def.moveSpeed * 4f);
        }

        public virtual void Init(Transform t) {
            target = t;
            playerHealth = target ? target.GetComponent<PlayerHealth>() : null;
            touchTimer = 0f;
        }

        protected virtual void UpdateTargetHealthCache() {
            if (playerHealth && target && playerHealth.transform == target) return;
            playerHealth = target ? target.GetComponent<PlayerHealth>() : null;
        }

        protected bool HasTarget() {
            if (target) return true;
            var found = GameObject.FindWithTag("Player");
            if (found) target = found.transform;
            return target != null;
        }

        protected Vector3 DirToTarget(out float dist) {
            var d = target.position - transform.position; d.y = 0f;
            dist = d.magnitude;
            return dist > 0.001f ? d / dist : Vector3.zero;
        }

        protected float ChaseSimple(float speedMul = 1f) {
            if (!HasTarget()) { motor.Stop(); return float.PositiveInfinity; }
            float dist; var dir = DirToTarget(out dist);
            float stop = motor.stoppingDistance * keepDistance;

            if (dist > stop && dist < chaseDistance)
                motor.SetDesiredVelocity(dir * (stats.moveSpeed * speedMul));
            else
                motor.Stop();

            return dist;
        }

        float touchTimer;

        protected void ApplyTouchDamage(float currentDistance, float rangeMultiplier = 1f) {
            if (!target && !HasTarget()) return;
            if (!stats || stats.touchDamage <= 0f) return;

            if (touchTimer > 0f) touchTimer -= Time.deltaTime;
            if (touchTimer > 0f) return;

            float reach = motor ? motor.stoppingDistance * keepDistance : 1f;
            reach = Mathf.Max(0.6f, reach * rangeMultiplier);

            if (currentDistance > reach) return;

            UpdateTargetHealthCache();
            if (!playerHealth) return;

            playerHealth.Take(stats.touchDamage);
            float cd = stats.touchInterval > 0f ? stats.touchInterval : 0.75f;
            touchTimer = Mathf.Max(0.1f, cd);
        }
    }
}

using UnityEngine;

namespace Begin.AI {
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyBase : MonoBehaviour {
        protected Transform target;
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

        public virtual void Init(Transform t) { target = t; }

        protected bool HasTarget() => target != null;

        protected Vector3 DirToTarget(out float dist) {
            var d = target.position - transform.position; d.y = 0f;
            dist = d.magnitude;
            return dist > 0.001f ? d / dist : Vector3.zero;
        }

        protected void ChaseSimple(float speedMul = 1f) {
            if (!HasTarget()) { motor.Stop(); return; }
            float dist; var dir = DirToTarget(out dist);
            float stop = motor.stoppingDistance * keepDistance;

            if (dist > stop && dist < chaseDistance)
                motor.SetDesiredVelocity(dir * (stats.moveSpeed * speedMul));
            else
                motor.Stop();
        }
    }
}

using UnityEngine;

namespace Begin.AI {
    public class EnemyRunner : EnemyBase {
        float zigTime;
        Vector3 side;

        void Update() {
            if (!HasTarget()) { motor.Stop(); return; }

            float dist; var dir = DirToTarget(out dist);

            // лёгкий зиг-заг при беге
            zigTime += Time.deltaTime;
            if (zigTime > 0.75f) { zigTime = 0f; side = Vector3.Cross(Vector3.up, dir).normalized * Random.Range(-1f, 1f); }

            float stop = motor.stoppingDistance * keepDistance;
            if (dist > stop && dist < chaseDistance) {
                var move = (dir + 0.35f * side).normalized * (stats.moveSpeed * 1.35f);
                motor.SetDesiredVelocity(move);
            } else {
                motor.Stop();
            }

            ApplyTouchDamage(dist, 1.1f);
        }
    }
}

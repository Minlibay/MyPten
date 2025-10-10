using UnityEngine;

namespace Begin.AI {
    public class EnemyShooter : EnemyBase {
        public float fireInterval = 1.6f;
        public float bulletSpeed = 10f;
        public float bulletDamage = 10f;
        public float preferred = 8f;   // комфортная дистанция
        float cooldown;

        void Update() {
            if (!HasTarget()) { motor.Stop(); return; }

            float dist; var dir = DirToTarget(out dist);

            // держим дистанцию: ближе — отходим, дальше — подходим, в коридоре — стоим
            float low = Mathf.Max(preferred - 1.0f, motor.stoppingDistance * keepDistance);
            float high = preferred + 1.5f;

            if (dist < low)       motor.SetDesiredVelocity(-dir * (stats.moveSpeed * 0.9f));
            else if (dist > high) motor.SetDesiredVelocity( dir * (stats.moveSpeed * 0.9f));
            else                  motor.Stop();

            // таймер выстрела
            cooldown -= Time.deltaTime;
            if (cooldown <= 0f && dist < 16f) {
                cooldown = fireInterval;
                Shoot(dir);
            }
        }

        void Shoot(Vector3 dirToPlayer) {
            var origin = transform.position + Vector3.up * 1.0f;
            // НЕ разворачиваем всего врага — только направление пули
            var b = Begin.Core.Pool.Spawn("bullet", origin + dirToPlayer * 0.6f, Quaternion.LookRotation(dirToPlayer));
            if (b && b.TryGetComponent<EnemyBullet>(out var eb)) {
                eb.Init(dirToPlayer * bulletSpeed, bulletDamage);
            }
        }
    }
}

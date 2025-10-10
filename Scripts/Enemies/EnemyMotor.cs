using UnityEngine;

namespace Begin.AI {
    /// Отвечает только за физику перемещения и визуальный поворот модели.
    [RequireComponent(typeof(CharacterController))]
    public class EnemyMotor : MonoBehaviour {
        public float acceleration = 20f;
        public float deceleration = 25f;
        public float turnSpeed = 720f;         // град/сек
        public float stoppingDistance = 1.1f;  // дистанция, на которой тормозим
        public Transform model;                // что вращать визуально (если null — корень)

        CharacterController cc;
        Vector3 vel;           // текущая горизонтальная скорость
        Vector3 desired;       // желаемая скорость (задаёт мозг)
        float lastMoveMag;

        void Awake() {
            cc = GetComponent<CharacterController>();
            if (!model) model = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        public void SetDesiredVelocity(Vector3 v) { desired = new Vector3(v.x, 0, v.z); }

        public void Stop() { desired = Vector3.zero; }

        void Update() {
            // сглажение разгона/торможения
            Vector3 dv = desired - vel;
            float accel = (desired.sqrMagnitude > vel.sqrMagnitude) ? acceleration : deceleration;
            vel += Vector3.ClampMagnitude(dv, accel * Time.deltaTime);

            // перемещение
            cc.SimpleMove(vel);

            // мягкий поворот ТОЛЬКО если реально двигаемся
            Vector3 flatVel = vel; flatVel.y = 0;
            float mag = flatVel.magnitude;
            if (mag > 0.05f) {
                Quaternion targetRot = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
                model.rotation = Quaternion.RotateTowards(model.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
            lastMoveMag = mag;
        }

        public bool IsMoving() => lastMoveMag > 0.05f;
    }
}

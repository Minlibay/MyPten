using UnityEngine;

namespace Begin.Control {
    public class CameraFollow : MonoBehaviour {
        public Transform target;
        public Vector3 offset = new Vector3(0, 20, -20);
        public Vector3 euler = new Vector3(45, 45, 0);
        public float lerp = 10f;

        void LateUpdate() {
            if (!target) return;
            transform.position = Vector3.Lerp(transform.position, target.position + offset, Time.deltaTime * lerp);
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}

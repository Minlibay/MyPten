using UnityEngine;

namespace Begin.Control {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour {
        public float moveSpeed = 6f;
        public Camera cam;
        private CharacterController cc;

        void Awake() { cc = GetComponent<CharacterController>(); }

        void Update() {
            // WASD
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;
            cc.SimpleMove(dir * moveSpeed);

            // Поворот к курсору (топ-даун)
            if (cam != null) {
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                var ground = new Plane(Vector3.up, transform.position);
                if (ground.Raycast(ray, out float enter)) {
                    var look = ray.GetPoint(enter);
                    look.y = transform.position.y;
                    transform.LookAt(look);
                }
            }
        }
    }
}

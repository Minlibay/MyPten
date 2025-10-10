using UnityEngine;

namespace Begin.Combat {
    public class Health : MonoBehaviour {
        public float max = 40f;
        public float current;

        public System.Action onDeath;

        void Awake() { current = max; }

        public void Take(float dmg) {
            current -= dmg;
            if (current <= 0f) {
                onDeath?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

namespace Begin.Combat {
    public class Health : MonoBehaviour {
        public float max = 40f;
        public float current;
        public bool autoDestroy = true;

        public System.Action onDeath;

        bool _dead;

        void Awake() {
            ResetState(true);
        }

        public void Take(float dmg) {
            if (_dead) return;
            current -= dmg;
            if (current <= 0f) {
                current = 0f;
                _dead = true;
                onDeath?.Invoke();
                if (autoDestroy) Destroy(gameObject);
            }
        }

        public void ResetState(bool refill = false) {
            _dead = false;
            if (refill || current <= 0f || current > max) current = max;
            else current = Mathf.Clamp(current, 0f, max);
        }

        public void SetMax(float value, bool refill = true) {
            max = value;
            if (refill) current = max;
            ResetState(!refill);
        }
    }
}

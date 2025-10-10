using UnityEngine;
using Begin.Talents;
using Begin.UI;            // ← добавили

namespace Begin.Combat {
    public class SimpleAttack : MonoBehaviour {
        public float damage = 25f;
        public float distance = 2.5f;

        void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                float dmg = damage + TalentService.Total(TalentType.Damage);
                var origin = transform.position + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, transform.forward, out var hit, distance)) {
                    var h = hit.collider.GetComponent<Health>();
                    if (h != null) {
                        h.Take(dmg);
                        // ВСПЛЫВАЮЩЕЕ ЧИСЛО
                        DamageNumbers.Show(hit.point, Mathf.RoundToInt(dmg), Color.yellow, 1f);
                    }
                }
            }
        }
    }
}

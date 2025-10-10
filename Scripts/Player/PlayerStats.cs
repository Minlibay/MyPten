using UnityEngine;

namespace Begin.PlayerStats {
    [CreateAssetMenu(menuName = "Begin/Player Stats")]
    public class PlayerStatsSO : ScriptableObject {
        public int baseHP = 100;
        public int baseDamage = 10;
    }
}

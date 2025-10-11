using UnityEngine;
using Begin.Economy;

namespace Begin.Enemies {
    [CreateAssetMenu(menuName="Begin/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject {
        [Header("Stats")]
        public string id = "runner";
        public string displayName = "Runner";
        public float maxHP = 50f;
        public float moveSpeed = 4f;
        public float touchDamage = 10f;
        public float touchInterval = 0.75f;

        [Header("Rewards / Modifiers")]
        public int xpPerKill = 6;
        public int goldMin = 1;
        public int goldMax = 3;
        [Range(0f,1f)] public float itemDropChance = 0.15f;
        public DropTable dropTable;

        [Header("Prefab")]
        public GameObject prefab; // визуальный префаб (без логики)
    }
}

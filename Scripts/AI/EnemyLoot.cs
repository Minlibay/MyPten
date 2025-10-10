using UnityEngine;
using Begin.Combat;
using Begin.Economy;
using Begin.Items;
using Begin.Talents;
using Begin.Enemies;
using Begin.Progress;
using System.Linq;

namespace Begin.AI {
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyLoot : MonoBehaviour {
        EnemyStats stats;

        void Awake() {
            stats = GetComponent<EnemyStats>();
            var hp = GetComponent<Health>();
            hp.onDeath += OnDeath;
        }

        void OnDeath() {
            var def = stats ? stats.def : null;
            if (!def) {
                var tracker = GetComponent<WaveSpawnedEnemy>();
                def = tracker ? tracker.Definition : null;
            }
            Grant(def);
        }

        public static void Grant(EnemyDefinition def) {
            if (def == null) {
                Debug.LogWarning("EnemyLoot: missing definition, rewards skipped", def);
                return;
            }

            // GOLD
            float goldPct = 1f + TalentService.Total(TalentType.GoldGain) / 100f;
            int minGold = Mathf.Min(def.goldMin, def.goldMax);
            int maxGold = Mathf.Max(def.goldMin, def.goldMax);
            int gold = Random.Range(minGold, maxGold + 1);
            int payout = Mathf.Max(0, Mathf.RoundToInt(gold * goldPct));
            Currency.Give(payout);

            // ITEM
            float chance = def.itemDropChance + TalentService.Total(TalentType.ItemDropChance) / 100f;
            if (Random.value <= chance) {
                ItemDB.Warmup();
                var pick = ItemDB.All().OrderBy(_ => Random.value).FirstOrDefault();
                if (pick != null) InventoryService.TryAdd(pick.id);
            }

            // XP
            ProgressService.AddXP(Mathf.Max(0, def.xpPerKill));
        }
    }
}

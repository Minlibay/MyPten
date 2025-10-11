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

            var economy = EconomyBalance.Active;

            // GOLD
            float goldPct = 1f + TalentService.Total(TalentType.GoldGain) / 100f;
            int minGold = Mathf.Min(def.goldMin, def.goldMax);
            int maxGold = Mathf.Max(def.goldMin, def.goldMax);
            int gold = Random.Range(minGold, maxGold + 1);
            float globalGoldMul = economy != null ? economy.globalGoldMultiplier : 1f;
            int payout = Mathf.Max(0, Mathf.RoundToInt(gold * goldPct * globalGoldMul));
            Currency.Give(payout);

            // ITEM
            float chance = def.itemDropChance + TalentService.Total(TalentType.ItemDropChance) / 100f;
            bool awarded = false;
            if (Random.value <= chance) {
                ItemDB.Warmup();
                var table = def.dropTable;
                if (table) {
                    var roll = table.Roll();
                    if (roll.HasValue) {
                        var result = roll.Value;
                        if (!string.IsNullOrEmpty(result.itemId)) {
                            awarded = InventoryService.TryAdd(result.itemId, Mathf.Max(1, result.quantity));
                        }
                    }
                }

                if (!awarded) {
                    var pick = ItemDB.All().OrderBy(_ => Random.value).FirstOrDefault();
                    if (pick != null) {
                        awarded = InventoryService.TryAdd(pick.id);
                    }
                }
            }

            // XP
            float xpMul = economy != null ? economy.globalXpMultiplier : 1f;
            int xp = Mathf.Max(0, Mathf.RoundToInt(def.xpPerKill * xpMul));
            ProgressService.AddXP(xp);
        }
    }
}

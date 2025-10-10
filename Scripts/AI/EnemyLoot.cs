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
            if (def != null) {
                // GOLD
                float goldPct = 1f + TalentService.Total(TalentType.GoldGain) / 100f;
                int gold = Random.Range(def.goldMin, def.goldMax + 1);
                Currency.Give(Mathf.RoundToInt(gold * goldPct));

                // ITEM
                float chance = def.itemDropChance + TalentService.Total(TalentType.ItemDropChance)/100f;
                if (Random.value <= chance) {
                    ItemDB.Warmup();
                    var pick = ItemDB.All().OrderBy(_=>Random.value).FirstOrDefault();
                    if (pick != null) InventoryService.Give(pick.id);
                }

                // XP
                ProgressService.AddXP(def.xpPerKill);
            }
        }
    }
}

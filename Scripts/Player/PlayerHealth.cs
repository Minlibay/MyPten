using UnityEngine;
using Begin.Core;
using Begin.Items;
using Begin.Talents;

namespace Begin.Combat {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour {
        public int baseHP = 100;
        public float max;
        public float current;

        void OnEnable() {
            Recalc();
            InventoryService.OnChanged += Recalc;
            TalentService.OnChanged += Recalc;
        }
        void OnDisable() {
            InventoryService.OnChanged -= Recalc;
            TalentService.OnChanged -= Recalc;
        }

        void Recalc() {
            var bonusEquip = InventoryService.TotalHpBonus(ItemDB.Get);
            var bonusTalent = TalentService.Total(TalentType.MaxHP);
            max = baseHP + bonusEquip + bonusTalent;
            if (current <= 0 || current > max) current = max;
        }

        public void Take(float dmg) {
            current -= dmg;
            if (current <= 0f) SceneLoader.Load("Hub");
        }
    }
}

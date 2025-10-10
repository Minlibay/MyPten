using UnityEngine;
using Begin.Core;
using Begin.Items;
using Begin.PlayerData;
using Begin.Talents;

namespace Begin.Combat {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour {
        public int baseHP = 100;
        public float max;
        public float current;

        System.Action<PlayerProfile> _profileChangedHandler;

        void OnEnable() {
            Recalc();
            InventoryService.OnChanged += Recalc;
            TalentService.OnChanged += Recalc;
            _profileChangedHandler ??= _ => Recalc();
            GameManager.OnProfileChanged += _profileChangedHandler;
        }
        void OnDisable() {
            InventoryService.OnChanged -= Recalc;
            TalentService.OnChanged -= Recalc;
            if (_profileChangedHandler != null) GameManager.OnProfileChanged -= _profileChangedHandler;
        }

        void Recalc() {
            int baseStat = baseHP;
            var cls = GameManager.I ? GameManager.I.CurrentClass : null;
            if (cls != null) baseStat = cls.baseHP;

            var bonusEquip = InventoryService.TotalHpBonus(ItemDB.Get);
            var bonusTalent = TalentService.Total(TalentType.MaxHP);
            max = baseStat + bonusEquip + bonusTalent;
            if (current <= 0 || current > max) current = max;
        }

        public void Take(float dmg) {
            current -= dmg;
            if (current <= 0f) SceneLoader.Load("Hub");
        }
    }
}

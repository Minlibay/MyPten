using System;
using UnityEngine;
using Begin.Core;
using Begin.Items;
using Begin.Talents;

namespace Begin.PlayerData {
    public struct PlayerDerivedStats {
        public int Strength;
        public int Dexterity;
        public int Intelligence;
        public float MaxHealth;
        public float AttackPower;
        public float AttackSpeedMultiplier;
        public float CooldownReduction;
        public float MoveSpeed;
        public float AbilityPower;
        public float CritChance;
    }

    public static class PlayerStatService {
        public const float BaseAttackDamage = 12f;
        public const float BaseMoveSpeed = 6f;

        public static event Action<PlayerDerivedStats> OnChanged;

        static PlayerDerivedStats _current;
        static bool _dirty = true;
        static readonly Action<int, InventoryService.InventorySlot> SlotChangedHandler = (_, __) => MarkDirty();

        static PlayerStatService() {
            GameManager.OnProfileChanged += _ => MarkDirty();
            InventoryService.OnChanged += MarkDirty;
            InventoryService.OnSlotChanged += SlotChangedHandler;
            TalentService.OnChanged += MarkDirty;
        }

        static void MarkDirty() {
            _dirty = true;
            Recalculate();
        }

        static void Recalculate() {
            if (!_dirty) return;
            _dirty = false;

            var profile = GameManager.GetOrLoadProfile();
            var cls = GameManager.I?.CurrentClass ?? CharacterClassRegistry.Get(profile?.classId);

            var stats = new PlayerDerivedStats {
                Strength = cls ? cls.baseSTR : 0,
                Dexterity = cls ? cls.baseDEX : 0,
                Intelligence = cls ? cls.baseINT : 0
            };

            stats.Strength += InventoryService.TotalStrengthBonus(ItemDB.Get);
            stats.Dexterity += InventoryService.TotalDexterityBonus(ItemDB.Get);
            stats.Intelligence += InventoryService.TotalIntelligenceBonus(ItemDB.Get);

            stats.Strength += Mathf.RoundToInt(TalentService.Total(TalentType.Strength));
            stats.Dexterity += Mathf.RoundToInt(TalentService.Total(TalentType.Dexterity));
            stats.Intelligence += Mathf.RoundToInt(TalentService.Total(TalentType.Intelligence));

            float hpBase = cls ? cls.baseHP : 100f;
            float hpBonusEquip = InventoryService.TotalHpBonus(ItemDB.Get);
            float hpBonusTalent = TalentService.Total(TalentType.MaxHP);
            stats.MaxHealth = hpBase + hpBonusEquip + hpBonusTalent + stats.Strength * 5f;

            float baseDamage = BaseAttackDamage + InventoryService.TotalDamageBonus(ItemDB.Get) + TalentService.Total(TalentType.Damage);
            stats.AttackPower = baseDamage + stats.Strength * 1.5f + stats.Intelligence * 0.75f;

            stats.AttackSpeedMultiplier = 1f + stats.Dexterity * 0.02f + TalentService.Total(TalentType.AttackSpeed) / 100f;
            stats.CooldownReduction = Mathf.Clamp01(stats.Intelligence * 0.01f + TalentService.Total(TalentType.CooldownReduction) / 100f);
            stats.MoveSpeed = BaseMoveSpeed + stats.Dexterity * 0.05f + TalentService.Total(TalentType.MoveSpeed);
            stats.CritChance = Mathf.Clamp01(0.05f + stats.Dexterity * 0.01f);
            stats.AbilityPower = stats.Intelligence * 1.2f;

            _current = stats;
            OnChanged?.Invoke(_current);
        }

        public static PlayerDerivedStats Current {
            get {
                if (_dirty) Recalculate();
                return _current;
            }
        }
    }
}

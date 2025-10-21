using UnityEngine;
using Begin.Combat;

namespace Begin.Items {
    public enum EquipmentSlot { None, Head, Chest, Weapon }

    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

    [CreateAssetMenu(menuName = "Begin/Item")]
    public class ItemDefinition : ScriptableObject {
        public string id;                 // уникальный (например, "helm_basic")
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public EquipmentSlot slot = EquipmentSlot.None;
        public ItemRarity rarity = ItemRarity.Common;

        [Header("Bonuses")]
        public int hpBonus;
        public int damageBonus;
        public int strengthBonus;
        public int dexterityBonus;
        public int intelligenceBonus;

        [Header("Weapon")]
        public WeaponArchetype weaponArchetype = WeaponArchetype.None;

        [Header("Stacking")]
        public bool stackable = false;
        [Min(1)] public int maxStack = 1;

        [Header("Economy")]
        public int basePrice = 10;
    }
}

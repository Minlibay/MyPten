using UnityEngine;

namespace Begin.Items {
    public enum EquipmentSlot { None, Head, Chest, Weapon }

    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

    [CreateAssetMenu(menuName = "Begin/Item")]
    public class ItemDefinition : ScriptableObject {
        public string id;                 // уникальный (например, "helm_basic")
        public string displayName;
        public Sprite icon;
        public EquipmentSlot slot = EquipmentSlot.None;
        public ItemRarity rarity = ItemRarity.Common;

        [Header("Bonuses")]
        public int hpBonus;
        public int damageBonus;

        [Header("Economy")]
        public int basePrice = 10;
    }
}

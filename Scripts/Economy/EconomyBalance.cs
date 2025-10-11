using System.Collections.Generic;
using UnityEngine;
using Begin.Items;

namespace Begin.Economy {
    [CreateAssetMenu(menuName = "Begin/Economy/Economy Balance", fileName = "EconomyBalance")]
    public class EconomyBalance : ScriptableObject {
        private static EconomyBalance instance;

        [System.Serializable]
        public class RarityPricing {
            public ItemRarity rarity = ItemRarity.Common;
            [Min(0f)] public float buyMultiplier = 1f;
            [Min(0f)] public float sellMultiplier = 0.4f;
        }

        [Header("Vendor")] public int defaultVendorStock = 6;
        public List<RarityPricing> rarityPricing = new List<RarityPricing> {
            new RarityPricing { rarity = ItemRarity.Common, buyMultiplier = 1f, sellMultiplier = 0.4f },
            new RarityPricing { rarity = ItemRarity.Uncommon, buyMultiplier = 1.5f, sellMultiplier = 0.45f },
            new RarityPricing { rarity = ItemRarity.Rare, buyMultiplier = 2.5f, sellMultiplier = 0.5f },
            new RarityPricing { rarity = ItemRarity.Epic, buyMultiplier = 4f, sellMultiplier = 0.6f },
            new RarityPricing { rarity = ItemRarity.Legendary, buyMultiplier = 8f, sellMultiplier = 0.7f }
        };

        [Header("Rewards")] public float globalGoldMultiplier = 1f;
        public float globalXpMultiplier = 1f;
        [Min(0f)] public float vendorSellFallbackMultiplier = 0.4f;

        public static EconomyBalance Active {
            get {
                if (instance == null) {
                    instance = Resources.Load<EconomyBalance>("Balance/EconomyBalance");
                    if (instance == null) {
                        Debug.LogWarning("EconomyBalance: No balance asset found in Resources/Balance. Using default inline values.");
                        instance = CreateInstance<EconomyBalance>();
                    }
                }

                return instance;
            }
        }

        public float GetBuyMultiplier(ItemRarity rarity) {
            foreach (var rp in rarityPricing) {
                if (rp.rarity == rarity) return Mathf.Max(0f, rp.buyMultiplier);
            }

            return 1f;
        }

        public float GetSellMultiplier(ItemRarity rarity) {
            foreach (var rp in rarityPricing) {
                if (rp.rarity == rarity) return Mathf.Max(0f, rp.sellMultiplier);
            }

            return Mathf.Max(0f, vendorSellFallbackMultiplier);
        }

        public void ForceReload() {
            instance = null;
        }
    }
}

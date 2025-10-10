using System;
using System.Collections.Generic;
using UnityEngine;

namespace Begin.Talents {
    public enum TalentType { MaxHP, Damage, GoldGain, ItemDropChance, VendorDiscount, MoveSpeed }

    [Serializable]
    public class TalentPrereq {
        public string nodeId;   // id узла, который должен иметь ранг >= requiredRank
        public int requiredRank = 1;
    }

    [CreateAssetMenu(menuName = "Begin/Talents/Node")]
    public class TalentNode : ScriptableObject {
        public string id;                       // уникальный ключ, например "hp_1"
        public string title = "Talent";
        [TextArea] public string description;
        public TalentType type;
        public int maxRank = 3;
        public float[] valuesPerRank = new float[] { 5, 10, 15 };  // значение эффекта на рангах
        public List<TalentPrereq> requires = new();                // зависимости

        public float GetValueAt(int rank) {
            rank = Mathf.Clamp(rank, 0, maxRank);
            if (rank == 0) return 0f;
            int idx = Mathf.Clamp(rank-1, 0, valuesPerRank.Length-1);
            return valuesPerRank[idx];
        }
    }

    [CreateAssetMenu(menuName = "Begin/Talents/Tree")]
    public class TalentTree : ScriptableObject {
        public List<TalentNode> nodes = new();
    }
}

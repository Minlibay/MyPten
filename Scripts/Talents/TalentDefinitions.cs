using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Begin.Talents {
    public enum TalentType { MaxHP, Damage, GoldGain, ItemDropChance, VendorDiscount, MoveSpeed, Strength, Dexterity, Intelligence, AttackSpeed, CooldownReduction }

    [Serializable]
    public class TalentRequirementData {
        public string nodeId;   // id узла, который должен иметь ранг >= requiredRank
        public int requiredRank = 1;
    }

    [CreateAssetMenu(menuName = "Begin/Talents/Node")]
    public class TalentNode : ScriptableObject, ISerializationCallbackReceiver {
        public string id;                       // уникальный ключ, например "hp_1"
        public string title = "Talent";
        [TextArea] public string description;
        public TalentType type;
        public int maxRank = 3;
        public float[] valuesPerRank = new float[] { 5, 10, 15 };  // значение эффекта на рангах
        [SerializeField]
        List<TalentRequirementData> _requirements = new();       // зависимости
        [FormerlySerializedAs("requirements"), SerializeField, HideInInspector]
        List<TalentRequirement> legacyRequirementAssets = new();
        [SerializeField, HideInInspector]
        List<TalentPrereq> legacyPrereqAssets = new();

        public List<TalentRequirementData> requirements => _requirements;

        public float GetValueAt(int rank) {
            rank = Mathf.Clamp(rank, 0, maxRank);
            if (rank == 0) return 0f;
            int idx = Mathf.Clamp(rank-1, 0, valuesPerRank.Length-1);
            return valuesPerRank[idx];
        }

        public IEnumerable<TalentRequirementData> EnumerateRequirements() {
            if (_requirements != null) {
                foreach (var req in _requirements) {
                    if (req == null || string.IsNullOrEmpty(req.nodeId)) continue;
                    yield return req;
                }
            }

            if (legacyRequirementAssets != null) {
                foreach (var legacy in legacyRequirementAssets) {
                    if (legacy == null || string.IsNullOrEmpty(legacy.nodeId)) continue;
                    yield return new TalentRequirementData { nodeId = legacy.nodeId, requiredRank = legacy.requiredRank };
                }
            }

            if (legacyPrereqAssets != null) {
                foreach (var legacy in legacyPrereqAssets) {
                    if (legacy == null || string.IsNullOrEmpty(legacy.nodeId)) continue;
                    yield return new TalentRequirementData { nodeId = legacy.nodeId, requiredRank = legacy.requiredRank };
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            bool changed = false;

            if (legacyRequirementAssets != null && legacyRequirementAssets.Count > 0) {
                foreach (var legacy in legacyRequirementAssets) {
                    if (legacy == null || string.IsNullOrEmpty(legacy.nodeId)) continue;
                    if (_requirements == null) _requirements = new List<TalentRequirementData>();
                    if (!_requirements.Any(r => r != null && r.nodeId == legacy.nodeId && r.requiredRank == legacy.requiredRank)) {
                        _requirements.Add(new TalentRequirementData { nodeId = legacy.nodeId, requiredRank = legacy.requiredRank });
                        changed = true;
                    }
                }
                legacyRequirementAssets.Clear();
                changed = true;
            }

            if (legacyPrereqAssets != null && legacyPrereqAssets.Count > 0) {
                foreach (var legacy in legacyPrereqAssets) {
                    if (legacy == null || string.IsNullOrEmpty(legacy.nodeId)) continue;
                    if (_requirements == null) _requirements = new List<TalentRequirementData>();
                    if (!_requirements.Any(r => r != null && r.nodeId == legacy.nodeId && r.requiredRank == legacy.requiredRank)) {
                        _requirements.Add(new TalentRequirementData { nodeId = legacy.nodeId, requiredRank = legacy.requiredRank });
                        changed = true;
                    }
                }
                legacyPrereqAssets.Clear();
                changed = true;
            }

#if UNITY_EDITOR
            if (changed) {
                UnityEditor.EditorApplication.delayCall += () => {
                    if (this != null) {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                };
            }
#endif
        }
    }

    [CreateAssetMenu(menuName = "Begin/Talents/Tree")]
    public class TalentTree : ScriptableObject {
        public List<TalentNode> nodes = new();
    }
}

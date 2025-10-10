using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Begin.Core;
using Begin.PlayerData;

namespace Begin.Talents {
    public static class TalentService {
        static TalentTree _tree;

        public static event Action OnChanged;

        public static void BindTree(TalentTree tree) { _tree = tree; }

        static TalentService() {
            GameManager.OnProfileChanged += _ => OnChanged?.Invoke();
        }

        static PlayerProfile Profile => GameManager.GetOrLoadProfile();

        static void Save() {
            var profile = Profile;
            if (profile == null) return;
            PlayerProfile.Save(profile);
            OnChanged?.Invoke();
        }

        public static int Points => Profile?.talentPoints ?? 0;
        public static void AddPoints(int n){
            if (n <= 0) return;
            var profile = Profile;
            if (profile == null) return;
            profile.talentPoints += n;
            Save();
        }

        public static int GetRank(string nodeId) {
            var profile = Profile;
            if (profile == null || string.IsNullOrEmpty(nodeId)) return 0;
            var record = profile.talentRanks.Find(t => t != null && t.talentId == nodeId);
            return record?.rank ?? 0;
        }

        static bool SpendPoint() {
            var profile = Profile;
            if (profile == null) return false;
            if (profile.talentPoints <= 0) return false;
            profile.talentPoints--;
            return true;
        }

        public static bool CanUpgrade(TalentNode node){
            if (node == null || _tree == null) return false;
            int r = GetRank(node.id);
            if (r >= node.maxRank) return false;
            if (Points <= 0) return false;
            // зависимости
            if (node.requirements != null){
                foreach (var req in node.requirements){
                    if (req == null || string.IsNullOrEmpty(req.nodeId)) continue;
                    int have = GetRank(req.nodeId);
                    if (have < req.requiredRank) return false;
                }
            }
            return true;
        }

        public static bool Upgrade(TalentNode node){
            if (!CanUpgrade(node)) return false;
            if (!SpendPoint()) return false;
            int r = GetRank(node.id) + 1;
            var profile = Profile;
            if (profile == null) return false;
            var record = profile.talentRanks.Find(t => t != null && t.talentId == node.id);
            if (record != null) record.rank = r;
            else profile.talentRanks.Add(new PlayerProfile.TalentRankRecord { talentId = node.id, rank = r });
            Save();
            return true;
        }

        public static void RespecAll(){
            // вернём все очки, сбросим ранги
            var profile = Profile;
            if (profile == null) return;
            int refunded = profile.talentRanks.Sum(kv => kv?.rank ?? 0);
            profile.talentPoints += refunded;
            profile.talentRanks.Clear();
            Save();
        }

        // ---- агрегированные значения по типам ----
        public static float Total(TalentType type){
            if (_tree == null) return 0f;
            float sum = 0f;
            foreach (var n in _tree.nodes){
                if (n.type != type) continue;
                sum += n.GetValueAt(GetRank(n.id));
            }
            return sum;
        }
    }
}

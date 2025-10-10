using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Begin.Talents {
    [Serializable] class SaveData {
        public int points = 0;
        public Dictionary<string,int> ranks = new();
    }

    public static class TalentService {
        const string KEY = "begin_talents";
        static SaveData _data;
        static TalentTree _tree;

        public static event Action OnChanged;

        static SaveData Data {
            get {
                if (_data != null) return _data;
                if (!PlayerPrefs.HasKey(KEY)) { _data = new SaveData(); return _data; }
                try { _data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY)); }
                catch { _data = new SaveData(); }
                return _data;
            }
        }

        public static void BindTree(TalentTree tree) { _tree = tree; }
        static void Save() { PlayerPrefs.SetString(KEY, JsonUtility.ToJson(Data)); OnChanged?.Invoke(); }

        public static int Points => Data.points;
        public static void AddPoints(int n){ Data.points += Mathf.Max(0,n); Save(); }
        public static bool TrySpendPoint(){ if (Data.points>0){ Data.points--; return true; } return false; }

        public static int GetRank(string nodeId) => Data.ranks.TryGetValue(nodeId, out var r) ? r : 0;

        public static bool CanUpgrade(TalentNode node){
            if (node == null || _tree == null) return false;
            int r = GetRank(node.id);
            if (r >= node.maxRank) return false;
            if (Points <= 0) return false;
            // зависимости
            foreach (var req in node.requires){
                int have = GetRank(req.nodeId);
                if (have < req.requiredRank) return false;
            }
            return true;
        }

        public static bool Upgrade(TalentNode node){
            if (!CanUpgrade(node)) return false;
            if (!TrySpendPoint()) return false;
            int r = GetRank(node.id) + 1;
            Data.ranks[node.id] = r;
            Save();
            return true;
        }

        public static void RespecAll(){
            // вернём все очки, сбросим ранги
            int refunded = Data.ranks.Sum(kv => kv.Value);
            Data.points += refunded;
            Data.ranks.Clear();
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

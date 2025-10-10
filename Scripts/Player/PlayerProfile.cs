using System;
using System.Collections.Generic;
using Begin.Util;
using UnityEngine;

namespace Begin.PlayerData {
    [System.Serializable]
    public class PlayerProfile {
        public string playerName;
        public string classId;

        [Serializable]
        public class EquippedItemRecord {
            public string slot;
            public string itemId;
        }

        [Serializable]
        public class TalentRankRecord {
            public string talentId;
            public int rank;
        }

        // Новые поля
        public int gold = 0;
        public int seed = 0;
        public int level = 1;
        public int xp = 0;
        public int talentPoints = 0;
        public List<string> inventoryItems = new();
        public List<EquippedItemRecord> equippedItems = new();
        public List<TalentRankRecord> talentRanks = new();

        // --- сохранение / загрузка ---
        public static PlayerProfile Load() {
            var profile = JsonUtil.Load<PlayerProfile>("begin_profile", null);
            profile?.EnsureIntegrity();
            return profile;
        }

        public static void Save(PlayerProfile p) {
            if (p == null) return;
            p.EnsureIntegrity();
            JsonUtil.Save("begin_profile", p);
        }

        public static void Clear() => UnityEngine.PlayerPrefs.DeleteKey("begin_profile");

        // --- служебные ---
        public static PlayerProfile CreateNew(string name, string classId) {
            return new PlayerProfile {
                playerName = name,
                classId = classId,
                gold = 0,
                seed = UnityEngine.Random.Range(100000, 999999),
                level = 1,
                xp = 0,
                talentPoints = 0,
                inventoryItems = new List<string>(),
                equippedItems = new List<EquippedItemRecord>(),
                talentRanks = new List<TalentRankRecord>()
            };
        }

        public static void ResetProfile() {
            Clear();
        }

        public void EnsureIntegrity() {
            playerName ??= string.Empty;
            classId ??= string.Empty;
            if (inventoryItems == null) inventoryItems = new List<string>();
            if (equippedItems == null) equippedItems = new List<EquippedItemRecord>();
            if (talentRanks == null) talentRanks = new List<TalentRankRecord>();
            gold = Math.Max(0, gold);
            level = Math.Max(1, level);
            xp = Math.Max(0, xp);
            talentPoints = Math.Max(0, talentPoints);

            // удалить пустые записи, чтобы не плодить мусор
            equippedItems.RemoveAll(e => e == null || string.IsNullOrEmpty(e.slot) || string.IsNullOrEmpty(e.itemId));
            talentRanks.RemoveAll(t => t == null || string.IsNullOrEmpty(t.talentId) || t.rank <= 0);
        }
    }
}

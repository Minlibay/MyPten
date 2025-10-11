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

        [Serializable]
        public class InventoryItemRecord {
            public string itemId;
            public int quantity = 0;
            public string metadataJson;

            public void Clear() {
                itemId = null;
                quantity = 0;
                metadataJson = null;
            }
        }

        // Новые поля
        public int gold = 0;
        public int seed = 0;
        public int level = 1;
        public int xp = 0;
        public int talentPoints = 0;
        public List<string> inventoryItems = new();
        public List<InventoryItemRecord> inventoryStacks = new();
        public int inventoryCapacity = 24;
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
                inventoryStacks = new List<InventoryItemRecord>(),
                inventoryCapacity = 24,
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
            if (inventoryStacks == null) inventoryStacks = new List<InventoryItemRecord>();
            if (equippedItems == null) equippedItems = new List<EquippedItemRecord>();
            if (talentRanks == null) talentRanks = new List<TalentRankRecord>();
            gold = Math.Max(0, gold);
            level = Math.Max(1, level);
            xp = Math.Max(0, xp);
            talentPoints = Math.Max(0, talentPoints);
            inventoryCapacity = Math.Max(1, inventoryCapacity);

            // миграция старого списка строковых ID
            if (inventoryStacks.Count == 0 && inventoryItems.Count > 0) {
                foreach (var id in inventoryItems) {
                    if (string.IsNullOrEmpty(id)) continue;
                    inventoryStacks.Add(new InventoryItemRecord { itemId = id, quantity = 1 });
                }
                inventoryItems.Clear();
            }

            // удалить пустые записи, чтобы не плодить мусор
            equippedItems.RemoveAll(e => e == null || string.IsNullOrEmpty(e.slot) || string.IsNullOrEmpty(e.itemId));
            talentRanks.RemoveAll(t => t == null || string.IsNullOrEmpty(t.talentId) || t.rank <= 0);

            // поддерживать размер инвентаря
            for (int i = inventoryStacks.Count; i < inventoryCapacity; i++)
                inventoryStacks.Add(new InventoryItemRecord());
            if (inventoryStacks.Count > inventoryCapacity)
                inventoryStacks.RemoveRange(inventoryCapacity, inventoryStacks.Count - inventoryCapacity);

            for (int i = 0; i < inventoryStacks.Count; i++) {
                var record = inventoryStacks[i];
                if (record == null) {
                    inventoryStacks[i] = new InventoryItemRecord();
                    continue;
                }

                if (string.IsNullOrEmpty(record.itemId) || record.quantity <= 0) {
                    record.Clear();
                } else {
                    record.quantity = Math.Max(1, record.quantity);
                }
            }
        }
    }
}

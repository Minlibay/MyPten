using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Begin.Core;
using Begin.PlayerData;

namespace Begin.Items {
    public static class InventoryService {
        public static event Action OnChanged;

        static readonly List<string> EmptyItems = new();
        static readonly Dictionary<string,string> EmptyEquipped = new();
        static readonly Dictionary<string,string> EquippedCache = new();

        static InventoryService() {
            GameManager.OnProfileChanged += _ => {
                RefreshCache();
                OnChanged?.Invoke();
            };
        }

        static PlayerProfile Profile => GameManager.I ? GameManager.I.CurrentProfile : null;

        static void RefreshCache() {
            EquippedCache.Clear();
            var profile = Profile;
            if (profile == null) return;
            foreach (var entry in profile.equippedItems) {
                if (entry == null || string.IsNullOrEmpty(entry.slot) || string.IsNullOrEmpty(entry.itemId)) continue;
                EquippedCache[entry.slot] = entry.itemId;
            }
        }

        static void SaveAndNotify() {
            var profile = Profile;
            if (profile == null) return;
            PlayerProfile.Save(profile);
            RefreshCache();
            OnChanged?.Invoke();
        }

        public static IReadOnlyList<string> Items {
            get {
                var profile = Profile;
                return profile?.inventoryItems ?? EmptyItems;
            }
        }

        public static IReadOnlyDictionary<string,string> Equipped {
            get {
                RefreshCache();
                return EquippedCache.Count > 0 ? EquippedCache : EmptyEquipped;
            }
        }

        public static void Give(string itemId) {
            if (string.IsNullOrEmpty(itemId)) return;
            var profile = Profile;
            if (profile == null) return;
            if (!profile.inventoryItems.Contains(itemId)) {
                profile.inventoryItems.Add(itemId);
                SaveAndNotify();
            }
        }
        public static void Remove(string itemId) {
            if (string.IsNullOrEmpty(itemId)) return;
            var profile = Profile;
            if (profile == null) return;
            profile.inventoryItems.Remove(itemId);
            profile.equippedItems.RemoveAll(e => e != null && e.itemId == itemId);
            SaveAndNotify();
        }
        public static void Equip(EquipmentSlot slot, string itemId) {
            var profile = Profile;
            if (profile == null) return;
            var s = slot.ToString();
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(itemId)) return;
            var existing = profile.equippedItems.FirstOrDefault(e => e.slot == s);
            if (existing != null) existing.itemId = itemId;
            else profile.equippedItems.Add(new PlayerProfile.EquippedItemRecord { slot = s, itemId = itemId });
            SaveAndNotify();
        }
        public static void Unequip(EquipmentSlot slot) {
            var profile = Profile;
            if (profile == null) return;
            var s = slot.ToString();
            if (string.IsNullOrEmpty(s)) return;
            profile.equippedItems.RemoveAll(e => e.slot == s);
            SaveAndNotify();
        }

        // bonuses
        public static int TotalHpBonus(Func<string,ItemDefinition> db) {
            if (db == null) return 0;
            int sum = 0;
            foreach (var kv in Equipped) sum += db(kv.Value)?.hpBonus ?? 0;
            return sum;
        }
        public static int TotalDamageBonus(Func<string,ItemDefinition> db) {
            if (db == null) return 0;
            int sum = 0;
            foreach (var kv in Equipped) sum += db(kv.Value)?.damageBonus ?? 0;
            return sum;
        }

        public static void ClearAll() {
            var profile = Profile;
            if (profile == null) return;
            profile.inventoryItems.Clear();
            profile.equippedItems.Clear();
            SaveAndNotify();
        }
    }
}

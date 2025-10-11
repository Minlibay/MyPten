using System;
using System.Collections.Generic;
using System.Linq;
using Begin.Core;
using Begin.PlayerData;

namespace Begin.Items {
    public static class InventoryService {
        public readonly struct InventorySlot {
            public readonly int Index;
            public readonly string ItemId;
            public readonly int Quantity;
            public readonly string Metadata;
            public readonly ItemDefinition Definition;

            public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;

            public InventorySlot(int index, PlayerProfile.InventoryItemRecord record, ItemDefinition definition) {
                Index = index;
                Definition = definition;
                if (record == null) {
                    ItemId = null;
                    Quantity = 0;
                    Metadata = null;
                } else {
                    ItemId = record.itemId;
                    Quantity = record.quantity;
                    Metadata = record.metadataJson;
                }
            }
        }

        public static event Action OnChanged;
        public static event Action<int, InventorySlot> OnSlotChanged;
        public static event Action<int> OnCapacityChanged;

        static readonly List<string> EmptyItems = new();
        static readonly Dictionary<string, string> EmptyEquipped = new();
        static readonly Dictionary<string, string> EquippedCache = new();
        static readonly List<InventorySlot> SlotCache = new();
        static readonly List<string> FlattenCache = new();
        static bool _slotCacheDirty = true;

        static InventoryService() {
            GameManager.OnProfileChanged += _ => {
                RefreshCache();
                MarkSlotsDirty();
                OnChanged?.Invoke();
                var profile = Profile;
                OnCapacityChanged?.Invoke(profile?.inventoryCapacity ?? 0);
            };
        }

        static PlayerProfile Profile {
            get {
                var profile = GameManager.GetOrLoadProfile();
                profile?.EnsureIntegrity();
                return profile;
            }
        }

        static void RefreshCache() {
            EquippedCache.Clear();
            var profile = Profile;
            if (profile == null) return;
            foreach (var entry in profile.equippedItems) {
                if (entry == null || string.IsNullOrEmpty(entry.slot) || string.IsNullOrEmpty(entry.itemId)) continue;
                EquippedCache[entry.slot] = entry.itemId;
            }
        }

        static void MarkSlotsDirty() {
            _slotCacheDirty = true;
        }

        static void SyncLegacyInventory(PlayerProfile profile) {
            if (profile == null) return;
            profile.inventoryItems.Clear();
            foreach (var stack in profile.inventoryStacks) {
                if (stack == null || string.IsNullOrEmpty(stack.itemId) || stack.quantity <= 0) continue;
                for (int i = 0; i < stack.quantity; i++) profile.inventoryItems.Add(stack.itemId);
            }
        }

        static void SaveAndNotify(IEnumerable<int> changedSlots = null, bool capacityChanged = false) {
            var profile = Profile;
            if (profile == null) return;
            SyncLegacyInventory(profile);
            PlayerProfile.Save(profile);
            RefreshCache();
            MarkSlotsDirty();
            OnChanged?.Invoke();
            if (capacityChanged) OnCapacityChanged?.Invoke(profile.inventoryCapacity);
            if (changedSlots != null) {
                foreach (var idx in changedSlots.Distinct()) {
                    var slot = GetSlot(idx);
                    OnSlotChanged?.Invoke(idx, slot);
                }
            }
        }

        public static int Capacity {
            get {
                var profile = Profile;
                return profile?.inventoryCapacity ?? 0;
            }
        }

        public static void SetCapacity(int newCapacity) {
            var profile = Profile;
            if (profile == null) return;
            newCapacity = Math.Max(1, newCapacity);
            if (profile.inventoryCapacity == newCapacity) return;
            profile.inventoryCapacity = newCapacity;
            InventoryAlgorithms.EnsureSlots(profile.inventoryStacks, profile.inventoryCapacity);
            SaveAndNotify(Enumerable.Range(0, profile.inventoryStacks.Count), capacityChanged: true);
        }

        public static IReadOnlyList<InventorySlot> Slots {
            get {
                if (_slotCacheDirty) RebuildSlotCache();
                return SlotCache;
            }
        }

        static void RebuildSlotCache() {
            SlotCache.Clear();
            var profile = Profile;
            if (profile != null) {
                for (int i = 0; i < profile.inventoryStacks.Count; i++) {
                    SlotCache.Add(GetSlot(i));
                }
            }
            _slotCacheDirty = false;
        }

        public static InventorySlot GetSlot(int index) {
            var profile = Profile;
            if (profile == null || index < 0 || index >= profile.inventoryStacks.Count)
                return new InventorySlot(index, null, null);

            var record = profile.inventoryStacks[index];
            ItemDefinition def = null;
            if (record != null && !string.IsNullOrEmpty(record.itemId)) {
                def = ItemDB.Get(record.itemId);
            }
            return new InventorySlot(index, record, def);
        }

        public static IReadOnlyList<string> Items {
            get {
                FlattenCache.Clear();
                foreach (var slot in Slots) {
                    if (slot.IsEmpty) continue;
                    for (int i = 0; i < Math.Max(1, slot.Quantity); i++) {
                        FlattenCache.Add(slot.ItemId);
                    }
                }
                return FlattenCache.Count > 0 ? FlattenCache : EmptyItems;
            }
        }

        public static IReadOnlyDictionary<string, string> Equipped {
            get {
                RefreshCache();
                return EquippedCache.Count > 0 ? EquippedCache : EmptyEquipped;
            }
        }

        public static bool TryAdd(string itemId, int quantity = 1, string metadata = null) {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            var profile = Profile;
            if (profile == null) return false;
            var def = ItemDB.Get(itemId);
            if (def == null) return false;

            List<int> affected;
            bool added = InventoryAlgorithms.TryAdd(profile.inventoryStacks, profile.inventoryCapacity, def, quantity, metadata, out affected);
            if (added) SaveAndNotify(affected);
            return added;
        }

        public static bool Remove(string itemId, int quantity = 1, string metadata = null) {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            var profile = Profile;
            if (profile == null) return false;

            List<int> affected;
            bool removed = InventoryAlgorithms.TryRemove(profile.inventoryStacks, profile.inventoryCapacity, itemId, quantity, metadata, out affected);
            if (removed) {
                profile.equippedItems.RemoveAll(e => e != null && e.itemId == itemId);
                SaveAndNotify(affected);
            }
            return removed;
        }

        public static bool TryConsumeSlot(int slotIndex, int quantity = 1) {
            var profile = Profile;
            if (profile == null) return false;

            if (!InventoryAlgorithms.TryRemoveAt(profile.inventoryStacks, profile.inventoryCapacity, slotIndex, quantity, out var _))
                return false;

            SaveAndNotify(new[] { slotIndex });
            return true;
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

        static int TotalBonus(Func<string, ItemDefinition> db, Func<ItemDefinition, int> selector) {
            if (db == null || selector == null) return 0;
            int sum = 0;
            foreach (var kv in Equipped) {
                var def = db(kv.Value);
                if (def == null) continue;
                sum += selector(def);
            }
            return sum;
        }

        // bonuses
        public static int TotalHpBonus(Func<string, ItemDefinition> db) {
            if (db == null) return 0;
            int sum = 0;
            foreach (var kv in Equipped) sum += db(kv.Value)?.hpBonus ?? 0;
            return sum;
        }

        public static int TotalDamageBonus(Func<string, ItemDefinition> db) {
            if (db == null) return 0;
            int sum = 0;
            foreach (var kv in Equipped) sum += db(kv.Value)?.damageBonus ?? 0;
            return sum;
        }

        public static int TotalStrengthBonus(Func<string, ItemDefinition> db) => TotalBonus(db, d => d.strengthBonus);
        public static int TotalDexterityBonus(Func<string, ItemDefinition> db) => TotalBonus(db, d => d.dexterityBonus);
        public static int TotalIntelligenceBonus(Func<string, ItemDefinition> db) => TotalBonus(db, d => d.intelligenceBonus);

        public static void ClearAll() {
            var profile = Profile;
            if (profile == null) return;
            profile.inventoryItems.Clear();
            foreach (var stack in profile.inventoryStacks) stack?.Clear();
            InventoryAlgorithms.EnsureSlots(profile.inventoryStacks, profile.inventoryCapacity);
            profile.equippedItems.Clear();
            SaveAndNotify(Enumerable.Range(0, profile.inventoryStacks.Count));
        }
    }
}

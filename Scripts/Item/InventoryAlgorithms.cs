using System;
using System.Collections.Generic;
using Begin.PlayerData;

namespace Begin.Items {
    public static class InventoryAlgorithms {
        public static void EnsureSlots(List<PlayerProfile.InventoryItemRecord> slots, int capacity) {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            capacity = Math.Max(0, capacity);

            for (int i = slots.Count; i < capacity; i++) {
                slots.Add(new PlayerProfile.InventoryItemRecord());
            }
            if (slots.Count > capacity) {
                slots.RemoveRange(capacity, slots.Count - capacity);
            }

            for (int i = 0; i < slots.Count; i++) {
                var record = slots[i];
                if (record == null) {
                    slots[i] = new PlayerProfile.InventoryItemRecord();
                    continue;
                }

                if (string.IsNullOrEmpty(record.itemId) || record.quantity <= 0) {
                    record.Clear();
                } else {
                    record.quantity = Math.Max(1, record.quantity);
                }
            }
        }

        public static bool TryAdd(List<PlayerProfile.InventoryItemRecord> slots,
                                   int capacity,
                                   ItemDefinition def,
                                   int quantity,
                                   string metadata,
                                   out List<int> affectedSlots) {
            affectedSlots = new List<int>();
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (def == null || quantity <= 0) return false;

            EnsureSlots(slots, capacity);

            int remaining = quantity;
            bool canStack = def.stackable;
            int maxStack = Math.Max(1, canStack ? def.maxStack : 1);
            var originalStates = new Dictionary<int, PlayerProfile.InventoryItemRecord>();

            void CacheOriginal(int idx) {
                if (originalStates.ContainsKey(idx)) return;
                var current = slots[idx];
                if (current == null) {
                    originalStates[idx] = null;
                } else {
                    originalStates[idx] = new PlayerProfile.InventoryItemRecord {
                        itemId = current.itemId,
                        quantity = current.quantity,
                        metadataJson = current.metadataJson
                    };
                }
            }

            if (canStack) {
                for (int i = 0; i < slots.Count && remaining > 0; i++) {
                    var record = slots[i];
                    if (record == null || string.IsNullOrEmpty(record.itemId)) continue;
                    if (record.itemId != def.id) continue;
                    if (!string.Equals(record.metadataJson, metadata, StringComparison.Ordinal)) continue;

                    int space = maxStack - record.quantity;
                    if (space <= 0) continue;

                    CacheOriginal(i);
                    int add = Math.Min(space, remaining);
                    record.quantity += add;
                    remaining -= add;
                    affectedSlots.Add(i);
                }
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++) {
                var record = slots[i];
                if (record != null && !string.IsNullOrEmpty(record.itemId)) continue;

                int add = canStack ? Math.Min(maxStack, remaining) : 1;
                if (record == null) {
                    record = new PlayerProfile.InventoryItemRecord();
                    slots[i] = record;
                }
                CacheOriginal(i);
                record.itemId = def.id;
                record.quantity = add;
                record.metadataJson = metadata;
                affectedSlots.Add(i);
                remaining -= add;
                if (!canStack) break;
            }

            if (remaining > 0) {
                foreach (var kv in originalStates) {
                    var record = slots[kv.Key];
                    if (record == null) {
                        record = new PlayerProfile.InventoryItemRecord();
                        slots[kv.Key] = record;
                    }

                    if (kv.Value == null) {
                        record.Clear();
                    } else {
                        record.itemId = kv.Value.itemId;
                        record.quantity = kv.Value.quantity;
                        record.metadataJson = kv.Value.metadataJson;
                    }
                }
                affectedSlots.Clear();
                return false;
            }

            return true;
        }

        public static bool TryRemove(List<PlayerProfile.InventoryItemRecord> slots,
                                      int capacity,
                                      string itemId,
                                      int quantity,
                                      string metadata,
                                      out List<int> affectedSlots) {
            affectedSlots = new List<int>();
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;

            EnsureSlots(slots, capacity);

            int remaining = quantity;
            var originalStates = new Dictionary<int, PlayerProfile.InventoryItemRecord>();

            void CacheOriginal(int idx) {
                if (originalStates.ContainsKey(idx)) return;
                var current = slots[idx];
                if (current == null) {
                    originalStates[idx] = null;
                } else {
                    originalStates[idx] = new PlayerProfile.InventoryItemRecord {
                        itemId = current.itemId,
                        quantity = current.quantity,
                        metadataJson = current.metadataJson
                    };
                }
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++) {
                var record = slots[i];
                if (record == null || string.IsNullOrEmpty(record.itemId)) continue;
                if (record.itemId != itemId) continue;
                if (metadata != null && !string.Equals(record.metadataJson, metadata, StringComparison.Ordinal)) continue;

                CacheOriginal(i);
                int take = Math.Min(record.quantity, remaining);
                record.quantity -= take;
                remaining -= take;
                affectedSlots.Add(i);

                if (record.quantity <= 0) {
                    record.Clear();
                }
            }

            if (remaining > 0) {
                foreach (var kv in originalStates) {
                    var record = slots[kv.Key];
                    if (record == null) {
                        record = new PlayerProfile.InventoryItemRecord();
                        slots[kv.Key] = record;
                    }

                    if (kv.Value == null) {
                        record.Clear();
                    } else {
                        record.itemId = kv.Value.itemId;
                        record.quantity = kv.Value.quantity;
                        record.metadataJson = kv.Value.metadataJson;
                    }
                }
                affectedSlots.Clear();
                return false;
            }

            return true;
        }

        public static bool TryRemoveAt(List<PlayerProfile.InventoryItemRecord> slots,
                                        int capacity,
                                        int index,
                                        int quantity,
                                        out PlayerProfile.InventoryItemRecord snapshot) {
            snapshot = null;
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (index < 0 || index >= capacity || quantity <= 0) return false;

            EnsureSlots(slots, capacity);

            var record = slots[index];
            if (record == null || string.IsNullOrEmpty(record.itemId) || record.quantity <= 0) return false;

            int take = Math.Min(quantity, record.quantity);
            int newQuantity = record.quantity - take;
            string id = record.itemId;
            string meta = record.metadataJson;

            if (newQuantity <= 0) {
                record.Clear();
                snapshot = new PlayerProfile.InventoryItemRecord {
                    itemId = id,
                    quantity = 0,
                    metadataJson = meta
                };
            } else {
                record.quantity = newQuantity;
                snapshot = new PlayerProfile.InventoryItemRecord {
                    itemId = id,
                    quantity = newQuantity,
                    metadataJson = meta
                };
            }
            return true;
        }
    }
}

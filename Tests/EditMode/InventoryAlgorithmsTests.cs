using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Begin.Items;
using Begin.PlayerData;

namespace Begin.Tests.EditMode {
    public class InventoryAlgorithmsTests {
        ItemDefinition CreateItem(string id, bool stackable = false, int maxStack = 1) {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.stackable = stackable;
            item.maxStack = maxStack;
            return item;
        }

        [Test]
        public void AddsStackedItemsUntilFull() {
            var slots = new List<PlayerProfile.InventoryItemRecord>();
            var def = CreateItem("potion", true, 5);

            InventoryAlgorithms.EnsureSlots(slots, 3);
            bool added = InventoryAlgorithms.TryAdd(slots, 3, def, 7, null, out var affected);

            Assert.IsTrue(added, "Items should fit into available slots");
            Assert.That(affected, Is.Not.Empty);
            Assert.AreEqual(5, slots[0].quantity);
            Assert.AreEqual("potion", slots[0].itemId);
            Assert.AreEqual(2, slots[1].quantity);

            Object.DestroyImmediate(def);
        }

        [Test]
        public void MetadataSeparatesStacks() {
            var slots = new List<PlayerProfile.InventoryItemRecord>();
            var def = CreateItem("gem", true, 10);

            InventoryAlgorithms.TryAdd(slots, 4, def, 1, "A", out _);
            InventoryAlgorithms.TryAdd(slots, 4, def, 1, "B", out _);

            Assert.AreEqual("A", slots[0].metadataJson);
            Assert.AreEqual("B", slots[1].metadataJson);

            Object.DestroyImmediate(def);
        }

        [Test]
        public void RemoveRespectsQuantity() {
            var slots = new List<PlayerProfile.InventoryItemRecord>();
            var def = CreateItem("ore", true, 4);

            InventoryAlgorithms.TryAdd(slots, 2, def, 4, null, out _);
            bool removed = InventoryAlgorithms.TryRemove(slots, 2, "ore", 3, null, out var affected);

            Assert.IsTrue(removed);
            Assert.That(affected, Is.Not.Empty);
            Assert.AreEqual(1, slots[0].quantity);
            Assert.False(string.IsNullOrEmpty(slots[0].itemId));

            Object.DestroyImmediate(def);
        }
    }
}

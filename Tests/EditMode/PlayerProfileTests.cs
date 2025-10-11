using NUnit.Framework;
using Begin.PlayerData;

namespace Begin.Tests.EditMode {
    public class PlayerProfileTests {
        [Test]
        public void MigratesLegacyInventoryToStacks() {
            var profile = new PlayerProfile();
            profile.inventoryItems.Add("sword");
            profile.inventoryItems.Add("potion");
            profile.inventoryCapacity = 4;
            profile.inventoryStacks.Clear();

            profile.EnsureIntegrity();

            Assert.AreEqual(4, profile.inventoryStacks.Count);
            Assert.AreEqual("sword", profile.inventoryStacks[0].itemId);
            Assert.AreEqual(1, profile.inventoryStacks[0].quantity);
            Assert.AreEqual("potion", profile.inventoryStacks[1].itemId);
        }

        [Test]
        public void EnsuresCapacityAndClearsInvalidEntries() {
            var profile = new PlayerProfile();
            profile.inventoryCapacity = 2;
            profile.inventoryStacks.Add(new PlayerProfile.InventoryItemRecord { itemId = "", quantity = -5 });
            profile.inventoryStacks.Add(null);
            profile.inventoryStacks.Add(new PlayerProfile.InventoryItemRecord { itemId = "amulet", quantity = 3 });

            profile.EnsureIntegrity();

            Assert.AreEqual(2, profile.inventoryStacks.Count);
            Assert.IsTrue(string.IsNullOrEmpty(profile.inventoryStacks[0].itemId));
            Assert.AreEqual("amulet", profile.inventoryStacks[1].itemId);
            Assert.AreEqual(3, profile.inventoryStacks[1].quantity);
        }
    }
}

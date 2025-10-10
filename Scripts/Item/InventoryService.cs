using System;
using System.Collections.Generic;
using UnityEngine;

namespace Begin.Items {
    [Serializable] class SaveData {
        public List<string> items = new();
        public Dictionary<string,string> equipped = new(); // slot -> itemId
    }

    public static class InventoryService {
        const string KEY = "begin_inventory";
        static SaveData _data;

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

        static void Save() {
            PlayerPrefs.SetString(KEY, JsonUtility.ToJson(Data));
            OnChanged?.Invoke();
        }

        public static IReadOnlyList<string> Items => Data.items;
        public static IReadOnlyDictionary<string,string> Equipped => Data.equipped;

        public static void Give(string itemId) {
            if (!Data.items.Contains(itemId)) { Data.items.Add(itemId); Save(); }
        }
        public static void Remove(string itemId) {
            Data.items.Remove(itemId);
            var keys = new List<string>(Data.equipped.Keys);
            foreach (var k in keys) if (Data.equipped[k] == itemId) Data.equipped.Remove(k);
            Save();
        }
        public static void Equip(EquipmentSlot slot, string itemId) {
            var s = slot.ToString();
            Data.equipped[s] = itemId;
            Save();
        }
        public static void Unequip(EquipmentSlot slot) {
            var s = slot.ToString();
            if (Data.equipped.ContainsKey(s)) { Data.equipped.Remove(s); Save(); }
        }

        // bonuses
        public static int TotalHpBonus(Func<string,ItemDefinition> db) {
            int sum = 0; foreach (var id in Data.equipped.Values) sum += db(id)?.hpBonus ?? 0; return sum;
        }
        public static int TotalDamageBonus(Func<string,ItemDefinition> db) {
            int sum = 0; foreach (var id in Data.equipped.Values) sum += db(id)?.damageBonus ?? 0; return sum;
        }

        public static void ClearAll() { _data = new SaveData(); Save(); }
    }
}

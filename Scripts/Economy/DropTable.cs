using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Begin.Items;

namespace Begin.Economy {
    [CreateAssetMenu(menuName = "Begin/Economy/Drop Table", fileName = "DropTable")]
    public class DropTable : ScriptableObject {
        [Serializable]
        public class Entry {
            public string itemId;
            public ItemDefinition definition;
            [Min(0f)] public float weight = 1f;
            public int minQuantity = 1;
            public int maxQuantity = 1;
            [Range(0f, 1f)] public float dropChance = 1f;

            public void SyncIdFromDefinition() {
                if (definition != null) itemId = definition.id;
            }
        }

        [Serializable]
        public struct RollResult {
            public ItemDefinition definition;
            public string itemId;
            public int quantity;
        }

        public List<Entry> entries = new List<Entry>();

        public RollResult? Roll() {
            if (entries == null || entries.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var e in entries) {
                if (e.dropChance <= 0f || e.weight <= 0f) continue;
                totalWeight += e.weight;
            }

            if (totalWeight <= 0f) return null;

            float pick = UnityEngine.Random.value * totalWeight;
            foreach (var e in entries) {
                if (e.dropChance <= 0f || e.weight <= 0f) continue;

                pick -= e.weight;
                if (pick > 0f) continue;

                if (UnityEngine.Random.value > e.dropChance) return null;

                var def = Resolve(e);
                if (!def) return null;
                int qty = Mathf.Clamp(UnityEngine.Random.Range(e.minQuantity, e.maxQuantity + 1), 1, int.MaxValue);
                return new RollResult { definition = def, itemId = def.id, quantity = qty };
            }

            return null;
        }

        ItemDefinition Resolve(Entry e) {
            if (e.definition != null) return e.definition;
            if (!string.IsNullOrEmpty(e.itemId)) return ItemDB.Get(e.itemId);
            return null;
        }

        public void ImportCsv(TextAsset csv) {
            if (!csv) throw new ArgumentNullException(nameof(csv));
            using (var reader = new StringReader(csv.text)) {
                ImportCsv(reader);
            }
        }

        public void ImportCsv(TextReader reader) {
            entries.Clear();
            string line;
            int lineIndex = 0;
            while ((line = reader.ReadLine()) != null) {
                lineIndex++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (lineIndex == 1) {
                    if (line.StartsWith("#")) continue; // allow header comments
                    var headerProbe = line.Trim().ToLowerInvariant();
                    if (headerProbe.Contains("item") && headerProbe.Contains("weight")) continue;
                }

                var parts = line.Split(',');
                if (parts.Length < 2) {
                    Debug.LogWarning($"DropTable CSV line {lineIndex} is invalid: '{line}'");
                    continue;
                }

                var entry = new Entry { itemId = parts[0].Trim() };
                float weight = 1f;
                int min = 1;
                int max = 1;
                float dropChance = 1f;

                if (parts.Length > 1) float.TryParse(parts[1], out weight);
                if (parts.Length > 2) int.TryParse(parts[2], out min);
                if (parts.Length > 3) int.TryParse(parts[3], out max);
                if (parts.Length > 4) float.TryParse(parts[4], out dropChance);

                entry.weight = Mathf.Max(0f, weight);
                entry.minQuantity = Mathf.Max(1, min);
                entry.maxQuantity = Mathf.Max(entry.minQuantity, max);
                entry.dropChance = Mathf.Clamp01(dropChance);
                entry.definition = ItemDB.Get(entry.itemId);
                entries.Add(entry);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Begin.Items {
    public static class ItemDB {
        static Dictionary<string, ItemDefinition> _map;

        public static void Warmup() {
            if (_map != null) return;
            _map = Resources.LoadAll<ItemDefinition>("Items").ToDictionary(i => i.id, i => i);
        }

        public static ItemDefinition Get(string id) {
            Warmup();
            return _map != null && _map.TryGetValue(id, out var so) ? so : null;
        }

        public static IEnumerable<ItemDefinition> All() { Warmup(); return _map.Values; }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Begin.PlayerData {
    public static class CharacterClassRegistry {
        static Dictionary<string, CharacterClass> _cache;
        static readonly object _lock = new();

        static void EnsureCache() {
            if (_cache != null) return;
            lock (_lock) {
                if (_cache != null) return;
                _cache = new Dictionary<string, CharacterClass>(System.StringComparer.OrdinalIgnoreCase);
                var classes = Resources.LoadAll<CharacterClass>("Classes");
                foreach (var cc in classes) {
                    if (cc == null || string.IsNullOrEmpty(cc.id)) continue;
                    _cache[cc.id] = cc;
                }
            }
        }

        public static CharacterClass Get(string id) {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureCache();
            return _cache.TryGetValue(id, out var cc) ? cc : null;
        }

        public static IReadOnlyCollection<CharacterClass> All {
            get {
                EnsureCache();
                return _cache.Values;
            }
        }

        public static void ClearCache() {
            _cache = null;
        }
    }
}

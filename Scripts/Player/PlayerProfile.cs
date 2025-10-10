using System.Collections.Generic;
using Begin.Util;

namespace Begin.PlayerData {
    [System.Serializable]
    public class PlayerProfile {
        public string playerName;
        public string classId;

        // Новые поля
        public int gold = 0;
        public int seed = 0;
        public Dictionary<string, int> unlockedTalents = new();

        // --- сохранение / загрузка ---
        public static PlayerProfile Load() => JsonUtil.Load<PlayerProfile>("begin_profile", null);

        public static void Save(PlayerProfile p) => JsonUtil.Save("begin_profile", p);

        public static void Clear() => UnityEngine.PlayerPrefs.DeleteKey("begin_profile");

        // --- служебные ---
        public static PlayerProfile CreateNew(string name, string classId) {
            return new PlayerProfile {
                playerName = name,
                classId = classId,
                gold = 0,
                seed = UnityEngine.Random.Range(100000, 999999),
                unlockedTalents = new Dictionary<string, int>()
            };
        }

        public static void ResetProfile() {
            Clear();
        }
    }
}

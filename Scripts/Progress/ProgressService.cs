using System;
using UnityEngine;
using Begin.Talents;

namespace Begin.Progress {
    [Serializable] class SaveData {
        public int level = 1;
        public int xp = 0;
    }

    public static class ProgressService {
        const string KEY = "begin_progress";
        public static int pointsPerLevel = 1;     // сколько очков талантов давать за уровень

        static SaveData _data;
        public static event Action OnChanged;
        public static event Action<int> OnLevelUp; // аргумент — новый уровень

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

        public static int Level => Mathf.Max(1, Data.level);
        public static int XP => Mathf.Max(0, Data.xp);
        public static int XPNeeded => XpRequiredFor(Level);

        // Формула требования XP: мягкая кривая роста
        public static int XpRequiredFor(int level) {
            // level 1→2: ~100, 2→3: ~283, 3→4: ~519 … (можно поменять под себя)
            return Mathf.Max(20, Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f)));
        }

        public static void AddXP(int amount) {
            if (amount <= 0) return;
            Data.xp += amount;

            // Поднимаем уровни, пока хватает опыта
            bool leveled = false;
            while (Data.xp >= XpRequiredFor(Data.level)) {
                Data.xp -= XpRequiredFor(Data.level);
                Data.level++;
                leveled = true;

                // Награда за уровень: очки талантов
                TalentService.AddPoints(pointsPerLevel);
                OnLevelUp?.Invoke(Data.level);
            }

            Save();

            // небольшая страховка: если переполнение XP — обнулить хвост
            if (Data.xp < 0) { Data.xp = 0; Save(); }
            if (leveled) OnChanged?.Invoke();
        }

        public static void ResetAll() {
            _data = new SaveData();
            Save();
        }
    }
}

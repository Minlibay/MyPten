using System;
using UnityEngine;
using Begin.Core;
using Begin.PlayerData;
using Begin.Talents;

namespace Begin.Progress {
    public static class ProgressService {
        public static int pointsPerLevel = 1;     // сколько очков талантов давать за уровень

        public static event Action OnChanged;
        public static event Action<int> OnLevelUp; // аргумент — новый уровень

        static ProgressService() {
            GameManager.OnProfileChanged += _ => OnChanged?.Invoke();
        }

        static PlayerProfile Profile => GameManager.I ? GameManager.I.CurrentProfile : null;

        static void Save() {
            var profile = Profile;
            if (profile == null) return;
            PlayerProfile.Save(profile);
            OnChanged?.Invoke();
        }

        public static int Level => Mathf.Max(1, Profile?.level ?? 1);
        public static int XP => Mathf.Max(0, Profile?.xp ?? 0);
        public static int XPNeeded => XpRequiredFor(Level);

        // Формула требования XP: мягкая кривая роста
        public static int XpRequiredFor(int level) {
            // level 1→2: ~100, 2→3: ~283, 3→4: ~519 … (можно поменять под себя)
            return Mathf.Max(20, Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f)));
        }

        public static void AddXP(int amount) {
            if (amount <= 0) return;
            var profile = Profile;
            if (profile == null) return;

            profile.xp += amount;

            // Поднимаем уровни, пока хватает опыта
            bool leveled = false;
            while (profile.xp >= XpRequiredFor(profile.level)) {
                profile.xp -= XpRequiredFor(profile.level);
                profile.level++;
                leveled = true;

                // Награда за уровень: очки талантов
                TalentService.AddPoints(pointsPerLevel);
                OnLevelUp?.Invoke(profile.level);
            }

            Save();

            // небольшая страховка: если переполнение XP — обнулить хвост
            if (profile.xp < 0) { profile.xp = 0; Save(); }
            if (leveled) OnChanged?.Invoke();
        }

        public static void ResetAll() {
            var profile = Profile;
            if (profile == null) return;
            profile.level = 1;
            profile.xp = 0;
            Save();
        }
    }
}

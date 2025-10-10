using UnityEngine;
using Begin.Core;
using Begin.PlayerData;

namespace Begin.Economy {
    public static class Currency {
        static PlayerProfile Profile => GameManager.I ? GameManager.I.CurrentProfile : null;

        public static int Gold => Profile?.gold ?? 0;

        public static void Give(int amount) {
            if (amount <= 0) return;
            var profile = Profile;
            if (profile == null) return;
            profile.gold = Mathf.Max(0, profile.gold + amount);
            PlayerProfile.Save(profile);
        }

        public static bool TrySpend(int amount) {
            if (amount <= 0) return true;
            var profile = Profile;
            if (profile == null) return false;
            if (profile.gold >= amount) {
                profile.gold -= amount;
                if (profile.gold < 0) profile.gold = 0;
                PlayerProfile.Save(profile);
                return true;
            }
            return false;
        }
    }
}

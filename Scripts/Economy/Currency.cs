using UnityEngine;

namespace Begin.Economy {
    public static class Currency {
        public static int Gold {
            get => PlayerPrefs.GetInt("begin_gold", 0);
            private set => PlayerPrefs.SetInt("begin_gold", Mathf.Max(0, value));
        }

        public static void Give(int amount) {
            Gold = Gold + Mathf.Max(0, amount);
        }

        public static bool TrySpend(int amount) {
            if (Gold >= amount) { Gold = Gold - amount; return true; }
            return false;
        }
    }
}

using UnityEngine;
using System;

namespace Begin.Util {
    public static class JsonUtil {
        public static void Save(string key, object obj) {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(obj));
        }
        public static T Load<T>(string key, T fallback = default) {
            if (!PlayerPrefs.HasKey(key)) return fallback;
            try { return JsonUtility.FromJson<T>(PlayerPrefs.GetString(key)); }
            catch (Exception) { return fallback; }
        }
    }
}

using UnityEngine;
using Begin.PlayerData;

namespace Begin.Core {
    public class GameManager : MonoBehaviour {
        public static GameManager I;
        public PlayerProfile CurrentProfile;

        void Awake() {
            if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }
    }
}

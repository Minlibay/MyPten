using System;
using UnityEngine;
using Begin.PlayerData;

namespace Begin.Core {
    public class GameManager : MonoBehaviour {
        public static GameManager I;
        public static event Action<PlayerProfile> OnProfileChanged;

        PlayerProfile _currentProfile;
        CharacterClass _currentClass;

        public PlayerProfile CurrentProfile {
            get => _currentProfile;
            set {
                _currentProfile = value;
                _currentProfile?.EnsureIntegrity();
                _currentClass = CharacterClassRegistry.Get(_currentProfile?.classId);
                OnProfileChanged?.Invoke(_currentProfile);
            }
        }

        public CharacterClass CurrentClass => _currentClass;

        void Awake() {
            if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }
    }
}

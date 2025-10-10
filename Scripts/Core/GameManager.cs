using System;
using UnityEngine;
using Begin.PlayerData;

namespace Begin.Core {
    public class GameManager : MonoBehaviour {
        public static GameManager I;
        public static event Action<PlayerProfile> OnProfileChanged;

        static PlayerProfile _cachedProfile;

        PlayerProfile _currentProfile;
        CharacterClass _currentClass;

        public PlayerProfile CurrentProfile {
            get => _currentProfile;
            set {
                _currentProfile = value;
                _cachedProfile = value;
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

        public static PlayerProfile GetOrLoadProfile() {
            if (I && I._currentProfile != null) return I._currentProfile;
            if (_cachedProfile == null) _cachedProfile = PlayerProfile.Load();
            if (I && I._currentProfile == null && _cachedProfile != null)
                I.CurrentProfile = _cachedProfile;
            return _cachedProfile;
        }
    }
}

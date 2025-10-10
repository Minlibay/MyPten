using UnityEngine;
using Begin.PlayerData;

namespace Begin.Core {
    public class BootLoader : MonoBehaviour {
        void Start() {
            var profile = PlayerProfile.Load();
            GameManager.I.CurrentProfile = profile;
            if (profile == null || string.IsNullOrEmpty(profile.playerName) || string.IsNullOrEmpty(profile.classId)) {
                SceneLoader.Load("CharacterCreation");
            } else {
                SceneLoader.Load("Hub");
            }
        }
    }
}

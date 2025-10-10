using UnityEngine;
using UnityEngine.SceneManagement;

namespace Begin.Core {
    public static class SceneLoader {
        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}

using UnityEngine;
using Begin.Combat;
using Begin.Control;

namespace Begin.Player {
    [CreateAssetMenu(menuName = "Begin/Player Avatar Settings", fileName = "AvatarSettings")]
    public class PlayerAvatarSettings : ScriptableObject {
        private const string VisualRootName = "__AvatarRoot";
        private const string PlaceholderName = "__PlaceholderCapsule";

        [Header("Visual Prefab")]
        [Tooltip("Prefab with the warrior mesh/rig to instantiate under the player root.")]
        public GameObject prefab;

        [Tooltip("Local position offset applied after the prefab is instantiated.")]
        public Vector3 prefabOffset = Vector3.zero;

        [Header("Character Controller")]
        [Tooltip("Center for the CharacterController component once the avatar is installed.")]
        public Vector3 controllerCenter = new Vector3(0f, 1f, 0f);

        [Tooltip("Radius for the CharacterController component once the avatar is installed.")]
        public float controllerRadius = 0.5f;

        [Tooltip("Height for the CharacterController component once the avatar is installed.")]
        public float controllerHeight = 2f;

        [Header("Animator Overrides (optional)")]
        [Tooltip("Animator controller to assign to the spawned prefab (leave empty to keep prefab default).")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("Humanoid avatar to assign to the spawned prefab (leave empty to keep prefab default).")]
        public Avatar humanoidAvatar;

        void OnEnable() {
            PlayerAvatarUtility.ResetCachedSettings(this);
        }

#if UNITY_EDITOR
        void OnValidate() {
            PlayerAvatarUtility.ResetCachedSettings(this);
        }
#endif

        public void Apply(GameObject playerRoot) {
            if (!playerRoot) return;

            var cc = playerRoot.GetComponent<CharacterController>() ?? playerRoot.AddComponent<CharacterController>();
            cc.center = controllerCenter;
            cc.radius = controllerRadius;
            cc.height = controllerHeight;

            // make sure there are no leftover primitive renderers on the root
            var meshFilter = playerRoot.GetComponent<MeshFilter>();
            if (meshFilter) Object.Destroy(meshFilter);
            var meshRenderer = playerRoot.GetComponent<MeshRenderer>();
            if (meshRenderer) Object.Destroy(meshRenderer);

            var placeholder = playerRoot.transform.Find(PlaceholderName);
            if (placeholder) Object.Destroy(placeholder.gameObject);

            var visualRoot = playerRoot.transform.Find(VisualRootName);
            if (!visualRoot) {
                var visualRootGO = new GameObject(VisualRootName);
                visualRootGO.transform.SetParent(playerRoot.transform, false);
                visualRoot = visualRootGO.transform;
            }

            if (prefab) {
                // clear previous visuals so we do not stack duplicates
                for (int i = visualRoot.childCount - 1; i >= 0; i--) {
                    Object.Destroy(visualRoot.GetChild(i).gameObject);
                }

                var instance = Object.Instantiate(prefab, visualRoot);
                instance.name = prefab.name;
                instance.transform.localPosition = prefabOffset;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                var animator = instance.GetComponentInChildren<Animator>();
                if (!animator) animator = instance.GetComponent<Animator>();
                if (!animator) animator = instance.AddComponent<Animator>();

                if (animatorController) animator.runtimeAnimatorController = animatorController;
                if (humanoidAvatar) animator.avatar = humanoidAvatar;
            } else if (visualRoot.childCount == 0) {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = PlaceholderName;
                capsule.transform.SetParent(playerRoot.transform, false);
                var capsuleCollider = capsule.GetComponent<Collider>();
                if (capsuleCollider) Object.Destroy(capsuleCollider);
            }
        }
    }

    public static class PlayerAvatarUtility {
        private const string ResourcePath = "Player/AvatarSettings";
        private static PlayerAvatarSettings _cachedSettings;

        public static GameObject EnsurePlayerRoot(Camera mainCamera) {
            var playerGO = GameObject.FindWithTag("Player");
            if (!playerGO) {
                playerGO = new GameObject("Player") { tag = "Player" };
            }

            var controller = playerGO.GetComponent<PlayerController>() ?? playerGO.AddComponent<PlayerController>();
            if (!controller.cam && mainCamera) controller.cam = mainCamera;

            var health = playerGO.GetComponent<PlayerHealth>() ?? playerGO.AddComponent<PlayerHealth>();
            var attack = playerGO.GetComponent<SimpleAttack>() ?? playerGO.AddComponent<SimpleAttack>();
            _ = health; // silence unused warnings when not referenced directly
            _ = attack;

            var settings = LoadSettings();
            if (settings) settings.Apply(playerGO);
            else EnsureFallbackVisual(playerGO.transform);

            return playerGO;
        }

        public static PlayerAvatarSettings LoadSettings() {
            if (!_cachedSettings) _cachedSettings = Resources.Load<PlayerAvatarSettings>(ResourcePath);
            return _cachedSettings;
        }

        public static void ResetCachedSettings(PlayerAvatarSettings asset) {
            if (asset && asset == _cachedSettings) _cachedSettings = null;
        }

        private static void EnsureFallbackVisual(Transform root) {
            if (!root) return;
            var meshFilter = root.GetComponent<MeshFilter>();
            if (!meshFilter && root.GetComponentInChildren<Renderer>()) return;

            var placeholder = root.Find("__PlaceholderCapsule");
            if (placeholder) return;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "__PlaceholderCapsule";
            capsule.transform.SetParent(root, false);
            var capsuleCollider = capsule.GetComponent<Collider>();
            if (capsuleCollider) Object.Destroy(capsuleCollider);
        }
    }
}

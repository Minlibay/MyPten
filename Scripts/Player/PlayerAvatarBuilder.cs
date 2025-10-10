using UnityEngine;
using Begin.Combat;
using Begin.Control;

namespace Begin.Player {
    public static class PlayerAvatarBuilder {
        const string VisualRootName = "__Visual";
        const string FallbackName = "__CapsuleFallback";

        public static GameObject EnsurePlayerRoot(GameObject visualPrefab, Vector3 visualOffset, Camera mainCamera) {
            var playerGO = GameObject.FindWithTag("Player");
            if (!playerGO) {
                playerGO = new GameObject("Player") { tag = "Player" };
            }

            EnsureCoreComponents(playerGO, mainCamera, out var animationDriver);
            InstallVisual(playerGO.transform, visualPrefab, visualOffset, animationDriver);

            return playerGO;
        }

        static void EnsureCoreComponents(GameObject playerGO, Camera mainCamera, out PlayerAnimationDriver animationDriver) {
            if (!playerGO.TryGetComponent(out CharacterController cc)) {
                cc = playerGO.AddComponent<CharacterController>();
                cc.height = 2f;
                cc.center = new Vector3(0f, 1f, 0f);
                cc.radius = 0.5f;
            }

            var controller = playerGO.GetComponent<PlayerController>() ?? playerGO.AddComponent<PlayerController>();
            if (!controller.cam && mainCamera) controller.cam = mainCamera;

            if (!playerGO.GetComponent<PlayerHealth>()) playerGO.AddComponent<PlayerHealth>();
            if (!playerGO.GetComponent<SimpleAttack>()) playerGO.AddComponent<SimpleAttack>();

            animationDriver = playerGO.GetComponent<PlayerAnimationDriver>();
            if (!animationDriver) animationDriver = playerGO.AddComponent<PlayerAnimationDriver>();

            // удалить возможные остатки меша капсулы на самом корне
            var meshFilter = playerGO.GetComponent<MeshFilter>();
            if (meshFilter) Object.Destroy(meshFilter);
            var meshRenderer = playerGO.GetComponent<MeshRenderer>();
            if (meshRenderer) Object.Destroy(meshRenderer);
        }

        static void InstallVisual(Transform root, GameObject prefab, Vector3 offset, PlayerAnimationDriver animationDriver) {
            var visualRoot = root.Find(VisualRootName);
            if (!visualRoot) {
                var visualGO = new GameObject(VisualRootName);
                visualGO.transform.SetParent(root, false);
                visualRoot = visualGO.transform;
            }

            for (int i = visualRoot.childCount - 1; i >= 0; i--) {
                Object.Destroy(visualRoot.GetChild(i).gameObject);
            }

            if (prefab) {
                var instance = Object.Instantiate(prefab, visualRoot);
                instance.name = prefab.name;
                instance.transform.localPosition = offset;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                Animator animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>();
                animationDriver?.BindAnimator(animator);
            } else {
                CreateFallback(visualRoot);
                animationDriver?.BindAnimator(null);
            }
        }

        static void CreateFallback(Transform parent) {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = FallbackName;
            capsule.transform.SetParent(parent, false);
            var collider = capsule.GetComponent<Collider>();
            if (collider) Object.Destroy(collider);
        }
    }
}

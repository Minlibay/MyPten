using UnityEngine;
using Begin.Combat;
using Begin.Control;

namespace Begin.Player {
    public static class PlayerAvatarBuilder {
        const string VisualRootName = "__Visual";
        const string FallbackName = "__CapsuleFallback";

        public static GameObject EnsurePlayerRoot(GameObject visualPrefab, Vector3 visualOffset, bool autoAlignToController, Camera mainCamera) {
            var playerGO = GameObject.FindWithTag("Player");
            if (!playerGO) {
                playerGO = new GameObject("Player") { tag = "Player" };
            }

            EnsureCoreComponents(playerGO, mainCamera, out var animationDriver, out var characterController);
            InstallVisual(playerGO.transform, visualPrefab, visualOffset, autoAlignToController, animationDriver, characterController);

            return playerGO;
        }

        static void EnsureCoreComponents(GameObject playerGO, Camera mainCamera, out PlayerAnimationDriver animationDriver, out CharacterController characterController) {
            if (!playerGO.TryGetComponent(out characterController)) {
                characterController = playerGO.AddComponent<CharacterController>();
                characterController.height = 2f;
                characterController.center = new Vector3(0f, 1f, 0f);
                characterController.radius = 0.5f;
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

        static void InstallVisual(Transform root, GameObject prefab, Vector3 offset, bool autoAlignToController, PlayerAnimationDriver animationDriver, CharacterController characterController) {
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

                if (autoAlignToController && characterController) {
                    var adjust = CalculateAutoAlignment(visualRoot, instance.transform, characterController);
                    if (adjust.sqrMagnitude > 0f) {
                        instance.transform.localPosition += adjust;
                    }
                }

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

        static Vector3 CalculateAutoAlignment(Transform visualRoot, Transform instance, CharacterController characterController) {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return Vector3.zero;

            var hasBounds = false;
            Bounds totalBounds = default;
            foreach (var renderer in renderers) {
                if (!renderer) continue;
                if (!hasBounds) {
                    totalBounds = renderer.bounds;
                    hasBounds = true;
                } else {
                    totalBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds) return Vector3.zero;

            var bottomWorld = new Vector3(totalBounds.center.x, totalBounds.min.y, totalBounds.center.z);
            var bottomLocal = visualRoot.InverseTransformPoint(bottomWorld).y;
            var targetBottom = characterController.center.y - characterController.height * 0.5f;
            var deltaY = targetBottom - bottomLocal;

            if (Mathf.Approximately(deltaY, 0f)) return Vector3.zero;

            return new Vector3(0f, deltaY, 0f);
        }
    }
}

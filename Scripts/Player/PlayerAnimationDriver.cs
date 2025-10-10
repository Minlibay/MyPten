using UnityEngine;
using Begin.Control;

namespace Begin.Player {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationDriver : MonoBehaviour {
        [Tooltip("Animator, который управляет визуалом игрока. Если не задан, возьмём первый в дочерних объектах визуала.")]
        [SerializeField] Animator animator;

        [Tooltip("Имя float-параметра, отражающего нормализованную скорость перемещения.")]
        [SerializeField] string speedParameter = "Speed";

        [Tooltip("Имя bool-параметра, включающего состояние движения в аниматоре.")]
        [SerializeField] string movingParameter = "IsMoving";

        [Tooltip("Сколько времени занимает сглаживание параметра Speed в аниматоре.")]
        [SerializeField, Range(0.01f, 0.5f)] float speedDampTime = 0.12f;

        [Tooltip("Порог, ниже которого персонаж считается стоящим на месте (значение Speed).")]
        [SerializeField, Range(0f, 0.5f)] float movingThreshold = 0.05f;

        int speedHash;
        int movingHash;

        PlayerController controller;
        CharacterController characterController;

        void Awake() {
            controller = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            RebuildHashes();
        }

        void OnEnable() {
            PrepareAnimator();
        }

        void OnValidate() {
            RebuildHashes();
        }

        public void BindAnimator(Animator targetAnimator) {
            animator = targetAnimator ? targetAnimator : null;
            PrepareAnimator();
        }

        void PrepareAnimator() {
            if (!animator) return;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        void Update() {
            if (!animator || !characterController) return;

            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;

            float currentSpeed = velocity.magnitude;
            float maxSpeed = Mathf.Max(0.01f, controller ? controller.moveSpeed : 1f);
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);

            animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
            animator.SetBool(movingHash, normalizedSpeed > movingThreshold);
        }

        void RebuildHashes() {
            speedHash = Animator.StringToHash(string.IsNullOrEmpty(speedParameter) ? "Speed" : speedParameter);
            movingHash = Animator.StringToHash(string.IsNullOrEmpty(movingParameter) ? "IsMoving" : movingParameter);
        }
    }
}

using UnityEngine;
using Begin.PlayerData;
using Begin.Player;

namespace Begin.Control {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour {
        [Header("Movement")]
        public float moveSpeed = 6f;
        [SerializeField, Min(0.1f)] float acceleration = 18f;
        [SerializeField, Min(0.1f)] float rotationSpeed = 720f;
        [SerializeField] float gravity = -30f;
        [SerializeField, Min(0f)] float jumpHeight = 1.5f;
        [SerializeField] KeyCode jumpKey = KeyCode.Space;

        [Header("Camera Alignment")]
        public Camera cam;

        [Header("Animation Actions")]
        [SerializeField] PlayerAnimationDriver animationDriver;
        [SerializeField] PlayerAnimationDriver.AnimationKey jumpFromIdle = PlayerAnimationDriver.AnimationKey.Jump;
        [SerializeField] PlayerAnimationDriver.AnimationKey jumpWhileMoving = PlayerAnimationDriver.AnimationKey.JumpWhileRunning;
        [SerializeField] PlayerAnimationDriver.AnimationKey attackAction = PlayerAnimationDriver.AnimationKey.OneHandedMeleeAttack;
        [SerializeField] KeyCode attackKey = KeyCode.Mouse0;

        CharacterController cc;
        float _baseMoveSpeed;
        Vector3 planarVelocity;
        float verticalVelocity;

        void Awake() {
            cc = GetComponent<CharacterController>();
            animationDriver = animationDriver ? animationDriver : GetComponent<PlayerAnimationDriver>();
            cam = cam ? cam : Camera.main;
            _baseMoveSpeed = moveSpeed;
            ClampTunableParameters();
        }

        void OnEnable() {
            PlayerStatService.OnChanged += HandleStatsChanged;
            HandleStatsChanged(PlayerStatService.Current);
        }

        void OnDisable() {
            PlayerStatService.OnChanged -= HandleStatsChanged;
        }

        void OnValidate() {
            ClampTunableParameters();
        }

        void ClampTunableParameters() {
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            acceleration = Mathf.Max(0.01f, acceleration);
            rotationSpeed = Mathf.Max(0.01f, rotationSpeed);
            if (gravity >= 0f) {
                gravity = -Mathf.Max(0.01f, gravity);
            }
            jumpHeight = Mathf.Max(0f, jumpHeight);
        }

        void HandleStatsChanged(PlayerDerivedStats stats) {
            moveSpeed = Mathf.Max(_baseMoveSpeed, stats.MoveSpeed);
        }

        void Update() {
            UpdatePlanarMovement();
            ApplyGravityAndJump();
            HandleActions();
            ApplyMovement();
            RotateTowardsVelocity();
        }

        void UpdatePlanarMovement() {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 desiredDirection = CalculateCameraAlignedDirection(h, v);
            float maxSpeed = moveSpeed;
            Vector3 desiredVelocity = desiredDirection * maxSpeed;

            float maxDelta = acceleration * Time.deltaTime;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, maxDelta);
        }

        Vector3 CalculateCameraAlignedDirection(float h, float v) {
            Vector3 input = new Vector3(h, 0f, v);
            if (input.sqrMagnitude <= 0.0001f) {
                return Vector3.zero;
            }

            if (!cam) {
                return input.normalized;
            }

            Vector3 camForward = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 worldDirection = camForward * input.z + camRight * input.x;
            if (worldDirection.sqrMagnitude <= 0.0001f) {
                return Vector3.zero;
            }

            return worldDirection.normalized;
        }

        void ApplyGravityAndJump() {
            if (cc.isGrounded && verticalVelocity < 0f) {
                verticalVelocity = -2f;
            }

            if (cc.isGrounded && Input.GetKeyDown(jumpKey)) {
                if (jumpHeight > 0f) {
                    verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
                }
                TriggerJumpAnimation();
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        void HandleActions() {
            if (Input.GetKeyDown(attackKey)) {
                TriggerAttackAnimation();
            }
        }

        void ApplyMovement() {
            Vector3 displacement = (planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            cc.Move(displacement);
        }

        void RotateTowardsVelocity() {
            Vector3 horizontalVelocity = planarVelocity;
            horizontalVelocity.y = 0f;
            if (horizontalVelocity.sqrMagnitude <= 0.0001f) {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        void TriggerJumpAnimation() {
            if (!animationDriver) return;

            bool movingFast = planarVelocity.sqrMagnitude > (moveSpeed * moveSpeed * 0.25f);
            var key = movingFast ? jumpWhileMoving : jumpFromIdle;
            animationDriver.PlayAction(key);
        }

        void TriggerAttackAnimation() {
            if (!animationDriver) return;
            animationDriver.PlayAction(attackAction);
        }

#if UNITY_EDITOR
        void Reset() {
            cam = Camera.main;
            animationDriver = GetComponent<PlayerAnimationDriver>();
        }
#endif
    }
}

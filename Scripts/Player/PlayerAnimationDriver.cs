using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Begin.Control;

namespace Begin.Player {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationDriver : MonoBehaviour {
        enum LocomotionMode {
            AnimatorParameters,
            BlinkClipMixer
        }

        [Header("Animator Source")]
        [Tooltip("Animator, который управляет визуалом игрока. Если не задан, возьмём первый в дочерних объектах визуала.")]
        [SerializeField] Animator animator;

        [Header("Режим")] 
        [SerializeField] LocomotionMode locomotionMode = LocomotionMode.BlinkClipMixer;

        [Header("Animator Parameters (Fallback)")]
        [Tooltip("Имя float-параметра, отражающего нормализованную скорость перемещения.")]
        [SerializeField] string speedParameter = "Speed";

        [Tooltip("Имя bool-параметра, включающего состояние движения в аниматоре.")]
        [SerializeField] string movingParameter = "IsMoving";

        [Tooltip("Сколько времени занимает сглаживание параметра Speed в аниматоре.")]
        [SerializeField, Range(0.01f, 0.5f)] float speedDampTime = 0.12f;

        [Tooltip("Порог, ниже которого персонаж считается стоящим на месте (значение Speed).")]
        [SerializeField, Range(0f, 0.5f)] float movingThreshold = 0.05f;

        [Header("Blink Clip Mixer")]
        [Tooltip("Автоматически подгружать анимации из Resources/Blink.")]
        [SerializeField] bool autoLoadBlinkClips = true;

        [Tooltip("Анимация ожидания (Idle).")]
        [SerializeField] AnimationClip idleClip;

        [Tooltip("Анимация медленного движения / шага.")]
        [SerializeField] AnimationClip walkClip;

        [Tooltip("Анимация быстрого движения / бега.")]
        [SerializeField] AnimationClip runClip;

        [Tooltip("Насколько быстро сглаживается скорость для плавного микса анимаций.")]
        [SerializeField, Range(1f, 15f)] float locomotionBlendResponsiveness = 6f;

        [Tooltip("Граница между Idle и Walk в нормализованной скорости.")]
        [SerializeField, Range(0.05f, 0.6f)] float walkThreshold = 0.25f;

        [Tooltip("Граница между Walk и Run в нормализованной скорости.")]
        [SerializeField, Range(0.3f, 1f)] float runThreshold = 0.65f;

        int speedHash;
        int movingHash;

        PlayerController controller;
        CharacterController characterController;

        PlayableGraph locomotionGraph;
        AnimationMixerPlayable locomotionMixer;
        AnimationClipPlayable idlePlayable;
        AnimationClipPlayable walkPlayable;
        AnimationClipPlayable runPlayable;
        float smoothedSpeed;

        void Awake() {
            controller = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            RebuildHashes();
            if (autoLoadBlinkClips) TryAutoAssignBlinkClips();
        }

        void OnEnable() {
            PrepareAnimator();
            RebuildGraph();
        }

        void OnDisable() {
            TearDownGraph();
        }

        void OnDestroy() {
            TearDownGraph();
        }

        void OnValidate() {
            RebuildHashes();
            if (autoLoadBlinkClips) TryAutoAssignBlinkClips();
            if (locomotionMode == LocomotionMode.BlinkClipMixer) {
                ValidateThresholds();
            }
        }

        public void BindAnimator(Animator targetAnimator) {
            animator = targetAnimator ? targetAnimator : null;
            PrepareAnimator();
            RebuildGraph();
        }

        void PrepareAnimator() {
            if (!animator) {
                TearDownGraph();
                return;
            }

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        void Update() {
            if (!characterController) return;

            float currentSpeed = ExtractPlanarSpeed();
            float maxSpeed = Mathf.Max(0.01f, controller ? controller.moveSpeed : 1f);
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);

            switch (locomotionMode) {
                case LocomotionMode.BlinkClipMixer:
                    if (!UpdateBlinkMixer(normalizedSpeed)) {
                        UpdateAnimatorParameters(normalizedSpeed);
                    }
                    break;
                default:
                    UpdateAnimatorParameters(normalizedSpeed);
                    break;
            }
        }

        float ExtractPlanarSpeed() {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        void UpdateAnimatorParameters(float normalizedSpeed) {
            if (!animator) return;
            animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
            animator.SetBool(movingHash, normalizedSpeed > movingThreshold);
        }

        bool UpdateBlinkMixer(float normalizedSpeed) {
            if (!animator || !locomotionGraph.IsValid() || !locomotionMixer.IsValid()) return false;

            smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, normalizedSpeed, locomotionBlendResponsiveness * Time.deltaTime);

            float idleWeight;
            float walkWeight;
            float runWeight;

            if (smoothedSpeed <= walkThreshold) {
                float t = walkThreshold <= Mathf.Epsilon ? 1f : smoothedSpeed / walkThreshold;
                idleWeight = 1f - t;
                walkWeight = t;
                runWeight = 0f;
            } else {
                idleWeight = 0f;
                float normalizedRunSpan = Mathf.Max(0.0001f, 1f - runThreshold);
                float t = Mathf.Clamp01((smoothedSpeed - runThreshold) / normalizedRunSpan);
                walkWeight = Mathf.Clamp01(1f - t);
                runWeight = t;
            }

            float total = idleWeight + walkWeight + runWeight;
            if (total <= Mathf.Epsilon) {
                idleWeight = 1f;
                walkWeight = runWeight = 0f;
                total = 1f;
            }

            locomotionMixer.SetInputWeight(0, idleWeight / total);
            locomotionMixer.SetInputWeight(1, walkWeight / total);
            locomotionMixer.SetInputWeight(2, runWeight / total);

            return true;
        }

        void RebuildGraph() {
            TearDownGraph();

            if (!animator || locomotionMode != LocomotionMode.BlinkClipMixer) return;

            if (!idleClip && !walkClip && !runClip) {
                // нечего воспроизводить – оставляем стандартный режим аниматора
                return;
            }

            ValidateThresholds();

            locomotionGraph = PlayableGraph.Create($"PlayerLocomotionGraph_{name}");
            locomotionGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            locomotionMixer = AnimationMixerPlayable.Create(locomotionGraph, 3, true);

            idlePlayable = CreateClipPlayable(idleClip, locomotionMixer, 0);
            walkPlayable = CreateClipPlayable(walkClip ? walkClip : runClip, locomotionMixer, 1);
            runPlayable = CreateClipPlayable(runClip ? runClip : walkClip, locomotionMixer, 2);

            var output = AnimationPlayableOutput.Create(locomotionGraph, "Locomotion", animator);
            output.SetSourcePlayable(locomotionMixer);

            locomotionGraph.Play();
        }

        AnimationClipPlayable CreateClipPlayable(AnimationClip clip, AnimationMixerPlayable mixer, int port) {
            if (clip) {
                var playable = AnimationClipPlayable.Create(locomotionGraph, clip);
                playable.SetApplyFootIK(true);
                playable.SetApplyPlayableIK(true);
                playable.SetSpeed(clip.isLooping ? 1f : 1f);
                locomotionGraph.Connect(playable, 0, mixer, port);
                mixer.SetInputWeight(port, port == 0 ? 1f : 0f);
                return playable;
            }

            // создаём пустой заглушечный плейбл, чтобы сохранить количество входов
            var emptyPlayable = AnimationClipPlayable.Create(locomotionGraph, new AnimationClip());
            emptyPlayable.SetApplyFootIK(false);
            emptyPlayable.SetApplyPlayableIK(false);
            emptyPlayable.SetSpeed(0f);
            locomotionGraph.Connect(emptyPlayable, 0, mixer, port);
            mixer.SetInputWeight(port, port == 0 ? 1f : 0f);
            return emptyPlayable;
        }

        void TearDownGraph() {
            if (locomotionGraph.IsValid()) {
                locomotionGraph.Destroy();
            }

            locomotionMixer = default;
            idlePlayable = default;
            walkPlayable = default;
            runPlayable = default;
            smoothedSpeed = 0f;
        }

        void TryAutoAssignBlinkClips() {
            if (idleClip && walkClip && runClip) return;

            var clips = Resources.LoadAll<AnimationClip>("Blink");
            if (clips == null || clips.Length == 0) return;

            foreach (var clip in clips) {
                if (!clip) continue;
                string name = clip.name.ToLowerInvariant();

                if (!idleClip && (name.Contains("idle") || name.Contains("stand"))) {
                    idleClip = clip;
                    continue;
                }

                if (!runClip && (name.Contains("run") || name.Contains("sprint"))) {
                    runClip = clip;
                    continue;
                }

                if (!walkClip && (name.Contains("walk") || name.Contains("move") || name.Contains("locomotion"))) {
                    walkClip = clip;
                    continue;
                }
            }
        }

        void RebuildHashes() {
            speedHash = Animator.StringToHash(string.IsNullOrEmpty(speedParameter) ? "Speed" : speedParameter);
            movingHash = Animator.StringToHash(string.IsNullOrEmpty(movingParameter) ? "IsMoving" : movingParameter);
        }

        void ValidateThresholds() {
            if (walkThreshold < 0.01f) walkThreshold = 0.01f;
            if (runThreshold <= walkThreshold + 0.05f) runThreshold = walkThreshold + 0.05f;
            if (runThreshold >= 1f) runThreshold = 0.95f;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Begin.Control;

namespace Begin.Player {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationDriver : MonoBehaviour {
        public enum AnimationKey {
            Idle,
            CombatIdle,
            RunForward,
            Sprint,
            RunLeft,
            RunRight,
            StrafeLeft,
            StrafeRight,
            RunBackward,
            RunBackwardLeft,
            RunBackwardRight,
            Jump,
            JumpWhileRunning,
            FallingLoop,
            RollForward,
            RollLeft,
            RollRight,
            RollBackward,
            PunchLeft,
            PunchRight,
            OneHandedMeleeAttack,
            TwoHandedMeleeAttack,
            MagicAttack,
            SpellCastingLoop,
            BowShot,
            BuffOrBoost,
            GetHit,
            BlockingLoop,
            StunnedLoop,
            Death,
            Gathering,
            Mining
        }

        [Serializable]
        class BlinkAnimationLibrary {
            [Tooltip("Idle - стоим на месте")] public AnimationClip idle;
            [Tooltip("Combat Idle - стоим с оружием")] public AnimationClip combatIdle;
            [Tooltip("Run Forward - бег вперёд")] public AnimationClip runForward;
            [Tooltip("Sprint - ускорение бега")] public AnimationClip sprint;
            [Tooltip("Run Left - бег влево")] public AnimationClip runLeft;
            [Tooltip("Run Right - бег вправо")] public AnimationClip runRight;
            [Tooltip("Strafe Left - стрейф влево")] public AnimationClip strafeLeft;
            [Tooltip("Strafe Right - стрейф вправо")] public AnimationClip strafeRight;
            [Tooltip("Run Backward - бег назад")] public AnimationClip runBackward;
            [Tooltip("Run Backward Left - бег назад влево")] public AnimationClip runBackwardLeft;
            [Tooltip("Run Backward Right - бег назад вправо")] public AnimationClip runBackwardRight;
            [Tooltip("Jump - вертикальный прыжок")] public AnimationClip jump;
            [Tooltip("Jump while running - прыжок на бегу")] public AnimationClip jumpWhileRunning;
            [Tooltip("Falling Loop - петля падения")] public AnimationClip fallingLoop;
            [Tooltip("Roll Forward - кувырок вперёд")] public AnimationClip rollForward;
            [Tooltip("Roll Left - кувырок влево")] public AnimationClip rollLeft;
            [Tooltip("Roll Right - кувырок вправо")] public AnimationClip rollRight;
            [Tooltip("Roll Backward - кувырок назад")] public AnimationClip rollBackward;
            [Tooltip("Punch Left - удар левой рукой")] public AnimationClip punchLeft;
            [Tooltip("Punch Right - удар правой рукой")] public AnimationClip punchRight;
            [Tooltip("One-Handed Melee Attack")] public AnimationClip oneHandedMeleeAttack;
            [Tooltip("Two-Handed Melee Attack")] public AnimationClip twoHandedMeleeAttack;
            [Tooltip("Magic Attack")] public AnimationClip magicAttack;
            [Tooltip("Spell Casting Loop")] public AnimationClip spellCastingLoop;
            [Tooltip("Bow Shot")] public AnimationClip bowShot;
            [Tooltip("Buff / Boost")] public AnimationClip buffOrBoost;
            [Tooltip("Get Hit")] public AnimationClip getHit;
            [Tooltip("Blocking Loop")] public AnimationClip blockingLoop;
            [Tooltip("Stunned Loop")] public AnimationClip stunnedLoop;
            [Tooltip("Death")] public AnimationClip death;
            [Tooltip("Gathering")] public AnimationClip gathering;
            [Tooltip("Mining")] public AnimationClip mining;
        }

        static readonly AnimationKey[] kLocomotionKeys = new AnimationKey[] {
            AnimationKey.Idle,
            AnimationKey.CombatIdle,
            AnimationKey.RunForward,
            AnimationKey.Sprint,
            AnimationKey.RunLeft,
            AnimationKey.RunRight,
            AnimationKey.StrafeLeft,
            AnimationKey.StrafeRight,
            AnimationKey.RunBackward,
            AnimationKey.RunBackwardLeft,
            AnimationKey.RunBackwardRight,
            AnimationKey.FallingLoop
        };

        static readonly HashSet<AnimationKey> kLoopingActions = new HashSet<AnimationKey> {
            AnimationKey.SpellCastingLoop,
            AnimationKey.BlockingLoop,
            AnimationKey.StunnedLoop,
            AnimationKey.Gathering,
            AnimationKey.Mining
        };

        struct BlinkAutoName {
            public AnimationKey key;
            public string[] variants;

            public BlinkAutoName(AnimationKey key, params string[] variants) {
                this.key = key;
                this.variants = variants;
            }
        }

        static readonly BlinkAutoName[] kAutoNameHints = new BlinkAutoName[] {
            new BlinkAutoName(AnimationKey.Idle, "idle", "locomotionidle"),
            new BlinkAutoName(AnimationKey.CombatIdle, "combatidle", "idlecombat"),
            new BlinkAutoName(AnimationKey.RunForward, "runforward", "run", "moveforward"),
            new BlinkAutoName(AnimationKey.Sprint, "sprint", "runfast", "dashforward"),
            new BlinkAutoName(AnimationKey.RunLeft, "runleft", "runstrafeleft"),
            new BlinkAutoName(AnimationKey.RunRight, "runright", "runstraferight"),
            new BlinkAutoName(AnimationKey.StrafeLeft, "strafeleft"),
            new BlinkAutoName(AnimationKey.StrafeRight, "straferight"),
            new BlinkAutoName(AnimationKey.RunBackward, "runback", "runbackward"),
            new BlinkAutoName(AnimationKey.RunBackwardLeft, "runbackwardleft", "runbackleft"),
            new BlinkAutoName(AnimationKey.RunBackwardRight, "runbackwardright", "runbackright"),
            new BlinkAutoName(AnimationKey.Jump, "jump", "idlejump"),
            new BlinkAutoName(AnimationKey.JumpWhileRunning, "jumprun", "runjump", "jumpforward"),
            new BlinkAutoName(AnimationKey.FallingLoop, "fallingloop", "fall"),
            new BlinkAutoName(AnimationKey.RollForward, "rollforward", "dodgeforward"),
            new BlinkAutoName(AnimationKey.RollLeft, "rollleft", "dodgeleft"),
            new BlinkAutoName(AnimationKey.RollRight, "rollright", "dodgeright"),
            new BlinkAutoName(AnimationKey.RollBackward, "rollbackward", "dodgeback"),
            new BlinkAutoName(AnimationKey.PunchLeft, "punchleft"),
            new BlinkAutoName(AnimationKey.PunchRight, "punchright"),
            new BlinkAutoName(AnimationKey.OneHandedMeleeAttack, "onehanded", "attack1h"),
            new BlinkAutoName(AnimationKey.TwoHandedMeleeAttack, "twohanded", "attack2h"),
            new BlinkAutoName(AnimationKey.MagicAttack, "magicattack", "spellattack"),
            new BlinkAutoName(AnimationKey.SpellCastingLoop, "spellloop", "castloop"),
            new BlinkAutoName(AnimationKey.BowShot, "bow", "arrow", "shoot"),
            new BlinkAutoName(AnimationKey.BuffOrBoost, "buff", "boost"),
            new BlinkAutoName(AnimationKey.GetHit, "gethit", "hit"),
            new BlinkAutoName(AnimationKey.BlockingLoop, "block", "guard"),
            new BlinkAutoName(AnimationKey.StunnedLoop, "stun", "stagger"),
            new BlinkAutoName(AnimationKey.Death, "death", "die"),
            new BlinkAutoName(AnimationKey.Gathering, "gather", "harvest"),
            new BlinkAutoName(AnimationKey.Mining, "mine", "pickaxe")
        };

        enum LocomotionFallbackMode {
            AnimatorParameters,
            BlinkGraph
        }

        [Header("Animator Source")]
        [SerializeField] Animator animator;

        [Header("Режим работы")]
        [SerializeField] LocomotionFallbackMode locomotionMode = LocomotionFallbackMode.BlinkGraph;

        [Header("Animator Parameters (Fallback)")]
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string movingParameter = "IsMoving";
        [SerializeField, Range(0.01f, 0.5f)] float speedDampTime = 0.12f;
        [SerializeField, Range(0f, 0.5f)] float movingThreshold = 0.05f;

        [Header("Blink Animation Set")]
        [SerializeField] bool autoLoadBlinkClips = true;
        [SerializeField] BlinkAnimationLibrary blinkAnimations = new BlinkAnimationLibrary();

        [Header("Locomotion Blending")]
        [Tooltip("Порог скорости для перехода в Idle/Combat Idle.")]
        [SerializeField, Range(0.01f, 0.3f)] float idleThreshold = 0.12f;
        [Tooltip("Порог поперечного смещения для включения стрейфа.")]
        [SerializeField, Range(0.05f, 0.6f)] float strafeThreshold = 0.28f;
        [Tooltip("Порог нормализованной скорости для спринта.")]
        [SerializeField, Range(0.3f, 1f)] float sprintThreshold = 0.78f;
        [Tooltip("Скорость сглаживания веса локомоции.")]
        [SerializeField, Range(1f, 20f)] float locomotionResponsiveness = 9f;

        [Header("Combat Idle")]
        [Tooltip("Стартовать ли в боевой стойке.")]
        [SerializeField] bool startInCombatIdle = false;

        [Header("Action Layer")]
        [Tooltip("Время нарастания веса экшн-анимации.")]
        [SerializeField, Range(0.01f, 1f)] float actionFadeIn = 0.15f;
        [Tooltip("Время затухания веса экшн-анимации.")]
        [SerializeField, Range(0.01f, 1f)] float actionFadeOut = 0.25f;

        int speedHash;
        int movingHash;

        PlayerController controller;
        CharacterController characterController;

        PlayableGraph locomotionGraph;
        AnimationLayerMixerPlayable layerMixer;
        AnimationMixerPlayable baseMixer;
        readonly Dictionary<AnimationKey, AnimationClipPlayable> basePlayables = new Dictionary<AnimationKey, AnimationClipPlayable>();
        readonly Dictionary<AnimationKey, float> baseWeights = new Dictionary<AnimationKey, float>();

        AnimationClipPlayable actionPlayable;
        AnimationKey? activeAction;
        bool actionLooping;
        float actionWeight;
        float actionTargetWeight;
        float actionTimer;
        float runtimeActionFadeIn;
        float runtimeActionFadeOut;

        float smoothedSpeed;
        Vector3 smoothedLocalVelocity;
        bool combatMode;

        readonly Dictionary<AnimationKey, int> basePorts = new Dictionary<AnimationKey, int>();

        void Awake() {
            controller = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            combatMode = startInCombatIdle;
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
            if (isActiveAndEnabled) {
                PrepareAnimator();
                RebuildGraph();
            }
        }

        public void BindAnimator(Animator targetAnimator) {
            animator = targetAnimator ? targetAnimator : null;
            PrepareAnimator();
            RebuildGraph();
        }

        public void SetCombatMode(bool enabled) {
            combatMode = enabled;
        }

        public void PlayAction(AnimationKey key, float customFadeIn = -1f, float customFadeOut = -1f) {
            if (locomotionMode != LocomotionFallbackMode.BlinkGraph || !layerMixer.IsValid()) return;
            var clip = GetClip(key);
            if (!clip) return;

            runtimeActionFadeIn = customFadeIn > 0f ? customFadeIn : actionFadeIn;
            runtimeActionFadeOut = customFadeOut > 0f ? customFadeOut : actionFadeOut;

            SetupActionPlayable(clip, kLoopingActions.Contains(key));
            activeAction = key;
        }

        public void StopAction(AnimationKey key) {
            if (locomotionMode != LocomotionFallbackMode.BlinkGraph || !layerMixer.IsValid()) return;
            if (activeAction.HasValue && activeAction.Value == key) {
                actionTargetWeight = 0f;
                actionLooping = false;
                activeAction = null;
            }
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

            var localVelocity = ExtractLocalPlanarVelocity();
            smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, normalizedSpeed, locomotionResponsiveness * Time.deltaTime);
            var targetLocal = localVelocity.sqrMagnitude > 0.0001f ? localVelocity.normalized * smoothedSpeed : Vector3.zero;
            smoothedLocalVelocity = Vector3.MoveTowards(smoothedLocalVelocity, targetLocal, locomotionResponsiveness * Time.deltaTime);

            bool graphActive = locomotionMode == LocomotionFallbackMode.BlinkGraph && layerMixer.IsValid();

            if (graphActive) {
                UpdateLocomotionState();
                UpdateActionLayer();
            } else {
                UpdateAnimatorParameters(normalizedSpeed);
            }
        }

        float ExtractPlanarSpeed() {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        Vector3 ExtractLocalPlanarVelocity() {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return transform.InverseTransformDirection(velocity);
        }

        void UpdateLocomotionState() {
            AnimationKey target = SelectLocomotionKey();

            foreach (var key in kLocomotionKeys) {
                if (!basePlayables.TryGetValue(key, out var playable)) continue;
                if (!basePorts.TryGetValue(key, out int port)) continue;

                float targetWeight = key == target ? 1f : 0f;
                float weight = baseWeights.TryGetValue(key, out float current) ? current : 0f;
                weight = Mathf.MoveTowards(weight, targetWeight, locomotionResponsiveness * Time.deltaTime);
                baseWeights[key] = weight;
                if (baseMixer.IsValid()) baseMixer.SetInputWeight(port, weight);
            }
        }

        AnimationKey SelectLocomotionKey() {
            if (!characterController.isGrounded) {
                var fallingClip = GetClip(AnimationKey.FallingLoop);
                if (fallingClip) return AnimationKey.FallingLoop;
            }

            if (smoothedSpeed < idleThreshold) {
                return EnsureAvailable(combatMode ? AnimationKey.CombatIdle : AnimationKey.Idle);
            }

            Vector3 local = smoothedLocalVelocity;
            float forward = local.z;
            float right = local.x;

            if (forward > 0.1f) {
                if (smoothedSpeed >= sprintThreshold && GetClip(AnimationKey.Sprint)) {
                    return AnimationKey.Sprint;
                }

                if (Mathf.Abs(right) > strafeThreshold) {
                    var sideKey = right > 0f ? AnimationKey.RunRight : AnimationKey.RunLeft;
                    return EnsureAvailable(sideKey);
                }

                return EnsureAvailable(AnimationKey.RunForward);
            }

            if (forward < -0.1f) {
                if (Mathf.Abs(right) > strafeThreshold) {
                    var backSide = right > 0f ? AnimationKey.RunBackwardRight : AnimationKey.RunBackwardLeft;
                    return EnsureAvailable(backSide);
                }

                return EnsureAvailable(AnimationKey.RunBackward);
            }

            if (Mathf.Abs(right) > 0.05f) {
                return EnsureAvailable(right > 0f ? AnimationKey.StrafeRight : AnimationKey.StrafeLeft);
            }

            return EnsureAvailable(combatMode ? AnimationKey.CombatIdle : AnimationKey.Idle);
        }

        AnimationKey EnsureAvailable(AnimationKey desired) {
            if (GetClip(desired)) return desired;
            if (GetClip(AnimationKey.Idle)) return AnimationKey.Idle;
            foreach (var key in kLocomotionKeys) {
                if (GetClip(key)) return key;
            }

            return AnimationKey.Idle;
        }

        void UpdateActionLayer() {
            if (!layerMixer.IsValid()) return;

            if (activeAction.HasValue) {
                if (!actionLooping) {
                    actionTimer -= Time.deltaTime;
                    if (actionTimer <= 0f) {
                        actionTargetWeight = 0f;
                        activeAction = null;
                    }
                }
            } else {
                actionTargetWeight = 0f;
            }

            float fadeIn = runtimeActionFadeIn > 0f ? runtimeActionFadeIn : actionFadeIn;
            float fadeOut = runtimeActionFadeOut > 0f ? runtimeActionFadeOut : actionFadeOut;
            float fadeSpeed = actionTargetWeight > actionWeight ? (1f / Mathf.Max(0.0001f, fadeIn)) : (1f / Mathf.Max(0.0001f, fadeOut));
            actionWeight = Mathf.MoveTowards(actionWeight, actionTargetWeight, fadeSpeed * Time.deltaTime);
            layerMixer.SetInputWeight(1, actionWeight);

            if (actionWeight <= 0f && actionPlayable.IsValid()) {
                actionPlayable.Pause();
                runtimeActionFadeIn = 0f;
                runtimeActionFadeOut = 0f;
            }
        }

        void UpdateAnimatorParameters(float normalizedSpeed) {
            if (!animator) return;
            animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
            animator.SetBool(movingHash, normalizedSpeed > movingThreshold);
        }

        void SetupActionPlayable(AnimationClip clip, bool looping) {
            if (!layerMixer.IsValid() || clip == null) return;

            if (actionPlayable.IsValid()) {
                layerMixer.DisconnectInput(1);
                actionPlayable.Destroy();
                actionPlayable = default;
            }

            actionPlayable = AnimationClipPlayable.Create(locomotionGraph, clip);
            actionPlayable.SetApplyFootIK(true);
            actionPlayable.SetApplyPlayableIK(true);
            actionPlayable.SetTime(0f);
            actionPlayable.SetSpeed(1f);

            actionLooping = looping;
            if (looping) {
                actionPlayable.SetDuration(double.PositiveInfinity);
                actionTimer = float.PositiveInfinity;
            } else {
                actionPlayable.SetDuration(clip.length);
                actionTimer = clip.length;
            }

            locomotionGraph.Connect(actionPlayable, 0, layerMixer, 1);
            layerMixer.SetInputWeight(1, 0f);
            actionTargetWeight = 1f;
            actionWeight = 0f;
        }

        void RebuildGraph() {
            TearDownGraph();

            if (!animator || locomotionMode != LocomotionFallbackMode.BlinkGraph) return;

            locomotionGraph = PlayableGraph.Create($"PlayerLocomotionGraph_{name}");
            locomotionGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            baseMixer = AnimationMixerPlayable.Create(locomotionGraph, kLocomotionKeys.Length, true);
            layerMixer = AnimationLayerMixerPlayable.Create(locomotionGraph, 2);

            basePlayables.Clear();
            baseWeights.Clear();
            basePorts.Clear();

            for (int i = 0; i < kLocomotionKeys.Length; i++) {
                var key = kLocomotionKeys[i];
                var clip = GetClip(key);
                var playable = CreateClipPlayable(clip, true);
                basePlayables[key] = playable;
                baseWeights[key] = key == AnimationKey.Idle ? 1f : 0f;
                locomotionGraph.Connect(playable, 0, baseMixer, i);
                baseMixer.SetInputWeight(i, baseWeights[key]);
                basePorts[key] = i;
            }

            locomotionGraph.Connect(baseMixer, 0, layerMixer, 0);
            layerMixer.SetInputWeight(0, 1f);
            layerMixer.SetInputWeight(1, 0f);
            layerMixer.SetLayerAdditive(1, false);

            actionPlayable = AnimationClipPlayable.Create(locomotionGraph, new AnimationClip());
            actionPlayable.SetSpeed(0f);
            locomotionGraph.Connect(actionPlayable, 0, layerMixer, 1);

            var output = AnimationPlayableOutput.Create(locomotionGraph, "PlayerLocomotion", animator);
            output.SetSourcePlayable(layerMixer);

            locomotionGraph.Play();
        }

        AnimationClipPlayable CreateClipPlayable(AnimationClip clip, bool loop) {
            AnimationClip source = clip ? clip : new AnimationClip();
            var playable = AnimationClipPlayable.Create(locomotionGraph, source);
            playable.SetApplyFootIK(true);
            playable.SetApplyPlayableIK(true);
            playable.SetTime(0f);
            if (loop || (clip && clip.isLooping)) {
                playable.SetDuration(double.PositiveInfinity);
            } else if (clip) {
                playable.SetDuration(clip.length);
            } else {
                playable.SetDuration(double.PositiveInfinity);
            }

            if (!clip) playable.SetSpeed(0f);
            else playable.SetSpeed(1f);

            return playable;
        }

        void TearDownGraph() {
            if (locomotionGraph.IsValid()) {
                locomotionGraph.Destroy();
            }

            baseMixer = default;
            layerMixer = default;
            basePlayables.Clear();
            baseWeights.Clear();
            actionPlayable = default;
            actionWeight = 0f;
            actionTargetWeight = 0f;
            actionTimer = 0f;
            activeAction = null;
        }

        void RebuildHashes() {
            speedHash = Animator.StringToHash(string.IsNullOrEmpty(speedParameter) ? "Speed" : speedParameter);
            movingHash = Animator.StringToHash(string.IsNullOrEmpty(movingParameter) ? "IsMoving" : movingParameter);
        }

        void TryAutoAssignBlinkClips() {
            var clips = Resources.LoadAll<AnimationClip>("Blink");
            if (clips == null || clips.Length == 0) return;

            foreach (var clip in clips) {
                if (!clip) continue;
                string sanitized = SanitizeName(clip.name);

                foreach (var hint in kAutoNameHints) {
                    if (HasClip(hint.key)) continue;

                    foreach (var variant in hint.variants) {
                        if (MatchesVariant(sanitized, variant)) {
                            SetClip(hint.key, clip);
                            goto Assigned;
                        }
                    }
                }

            Assigned:
                continue;
            }
        }

        bool HasClip(AnimationKey key) {
            return GetClip(key) != null;
        }

        bool MatchesVariant(string sanitizedName, string variant) {
            if (string.IsNullOrEmpty(sanitizedName) || string.IsNullOrEmpty(variant)) return false;
            if (sanitizedName == variant) return true;
            return sanitizedName.Contains(variant);
        }

        string SanitizeName(string name) {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            char[] buffer = new char[name.Length];
            int index = 0;
            foreach (char c in name) {
                if (char.IsLetterOrDigit(c)) {
                    buffer[index++] = char.ToLowerInvariant(c);
                }
            }

            return new string(buffer, 0, index);
        }

        AnimationClip GetClip(AnimationKey key) {
            return key switch {
                AnimationKey.Idle => blinkAnimations.idle,
                AnimationKey.CombatIdle => blinkAnimations.combatIdle,
                AnimationKey.RunForward => blinkAnimations.runForward,
                AnimationKey.Sprint => blinkAnimations.sprint,
                AnimationKey.RunLeft => blinkAnimations.runLeft,
                AnimationKey.RunRight => blinkAnimations.runRight,
                AnimationKey.StrafeLeft => blinkAnimations.strafeLeft,
                AnimationKey.StrafeRight => blinkAnimations.strafeRight,
                AnimationKey.RunBackward => blinkAnimations.runBackward,
                AnimationKey.RunBackwardLeft => blinkAnimations.runBackwardLeft,
                AnimationKey.RunBackwardRight => blinkAnimations.runBackwardRight,
                AnimationKey.Jump => blinkAnimations.jump,
                AnimationKey.JumpWhileRunning => blinkAnimations.jumpWhileRunning,
                AnimationKey.FallingLoop => blinkAnimations.fallingLoop,
                AnimationKey.RollForward => blinkAnimations.rollForward,
                AnimationKey.RollLeft => blinkAnimations.rollLeft,
                AnimationKey.RollRight => blinkAnimations.rollRight,
                AnimationKey.RollBackward => blinkAnimations.rollBackward,
                AnimationKey.PunchLeft => blinkAnimations.punchLeft,
                AnimationKey.PunchRight => blinkAnimations.punchRight,
                AnimationKey.OneHandedMeleeAttack => blinkAnimations.oneHandedMeleeAttack,
                AnimationKey.TwoHandedMeleeAttack => blinkAnimations.twoHandedMeleeAttack,
                AnimationKey.MagicAttack => blinkAnimations.magicAttack,
                AnimationKey.SpellCastingLoop => blinkAnimations.spellCastingLoop,
                AnimationKey.BowShot => blinkAnimations.bowShot,
                AnimationKey.BuffOrBoost => blinkAnimations.buffOrBoost,
                AnimationKey.GetHit => blinkAnimations.getHit,
                AnimationKey.BlockingLoop => blinkAnimations.blockingLoop,
                AnimationKey.StunnedLoop => blinkAnimations.stunnedLoop,
                AnimationKey.Death => blinkAnimations.death,
                AnimationKey.Gathering => blinkAnimations.gathering,
                AnimationKey.Mining => blinkAnimations.mining,
                _ => null
            };
        }

        void SetClip(AnimationKey key, AnimationClip clip) {
            switch (key) {
                case AnimationKey.Idle: blinkAnimations.idle = clip; break;
                case AnimationKey.CombatIdle: blinkAnimations.combatIdle = clip; break;
                case AnimationKey.RunForward: blinkAnimations.runForward = clip; break;
                case AnimationKey.Sprint: blinkAnimations.sprint = clip; break;
                case AnimationKey.RunLeft: blinkAnimations.runLeft = clip; break;
                case AnimationKey.RunRight: blinkAnimations.runRight = clip; break;
                case AnimationKey.StrafeLeft: blinkAnimations.strafeLeft = clip; break;
                case AnimationKey.StrafeRight: blinkAnimations.strafeRight = clip; break;
                case AnimationKey.RunBackward: blinkAnimations.runBackward = clip; break;
                case AnimationKey.RunBackwardLeft: blinkAnimations.runBackwardLeft = clip; break;
                case AnimationKey.RunBackwardRight: blinkAnimations.runBackwardRight = clip; break;
                case AnimationKey.Jump: blinkAnimations.jump = clip; break;
                case AnimationKey.JumpWhileRunning: blinkAnimations.jumpWhileRunning = clip; break;
                case AnimationKey.FallingLoop: blinkAnimations.fallingLoop = clip; break;
                case AnimationKey.RollForward: blinkAnimations.rollForward = clip; break;
                case AnimationKey.RollLeft: blinkAnimations.rollLeft = clip; break;
                case AnimationKey.RollRight: blinkAnimations.rollRight = clip; break;
                case AnimationKey.RollBackward: blinkAnimations.rollBackward = clip; break;
                case AnimationKey.PunchLeft: blinkAnimations.punchLeft = clip; break;
                case AnimationKey.PunchRight: blinkAnimations.punchRight = clip; break;
                case AnimationKey.OneHandedMeleeAttack: blinkAnimations.oneHandedMeleeAttack = clip; break;
                case AnimationKey.TwoHandedMeleeAttack: blinkAnimations.twoHandedMeleeAttack = clip; break;
                case AnimationKey.MagicAttack: blinkAnimations.magicAttack = clip; break;
                case AnimationKey.SpellCastingLoop: blinkAnimations.spellCastingLoop = clip; break;
                case AnimationKey.BowShot: blinkAnimations.bowShot = clip; break;
                case AnimationKey.BuffOrBoost: blinkAnimations.buffOrBoost = clip; break;
                case AnimationKey.GetHit: blinkAnimations.getHit = clip; break;
                case AnimationKey.BlockingLoop: blinkAnimations.blockingLoop = clip; break;
                case AnimationKey.StunnedLoop: blinkAnimations.stunnedLoop = clip; break;
                case AnimationKey.Death: blinkAnimations.death = clip; break;
                case AnimationKey.Gathering: blinkAnimations.gathering = clip; break;
                case AnimationKey.Mining: blinkAnimations.mining = clip; break;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Begin.Items;
using Begin.Player;
using Begin.PlayerData;
using Begin.UI;

namespace Begin.Combat {
    /// <summary>
    /// Player melee combat controller. Handles animation triggering, stat-driven cooldowns,
    /// and distinct hit shapes for one-handed and two-handed weapons.
    /// </summary>
    public class SimpleAttack : MonoBehaviour {
        const int kMaxAreaTargets = 32;

        [System.Serializable]
        struct AttackProfile {
            public PlayerAnimationDriver.AnimationKey animationKey;
            public float baseCooldown;
            public float hitDelay;
            public float recovery;
            public float reach;
            public float rayRadius;
            public float arcRadius;
            [Range(10f, 180f)] public float arcAngle;

            public static AttackProfile DefaultOneHanded => new AttackProfile {
                animationKey = PlayerAnimationDriver.AnimationKey.OneHandedMeleeAttack,
                baseCooldown = 0.7f,
                hitDelay = 0.14f,
                recovery = 0.28f,
                reach = 2.6f,
                rayRadius = 0.35f,
                arcRadius = 2.4f,
                arcAngle = 80f
            };

            public static AttackProfile DefaultTwoHanded => new AttackProfile {
                animationKey = PlayerAnimationDriver.AnimationKey.TwoHandedMeleeAttack,
                baseCooldown = 1.1f,
                hitDelay = 0.24f,
                recovery = 0.42f,
                reach = 3.2f,
                rayRadius = 0.5f,
                arcRadius = 3.6f,
                arcAngle = 120f
            };
        }

        [Header("Weapon Detection")]
        [SerializeField] bool autoDetectFromEquipment = true;
        [SerializeField] WeaponArchetype fallbackArchetype = WeaponArchetype.OneHandedBlade;

        [Header("Profiles")]
        [SerializeField] AttackProfile oneHandedProfile = AttackProfile.DefaultOneHanded;
        [SerializeField] AttackProfile twoHandedProfile = AttackProfile.DefaultTwoHanded;

        [Header("Hit Origin")]
        [Tooltip("Optional transform used as the origin point for melee swings (e.g. weapon bone).")]
        [SerializeField] Transform attackOrigin;
        [Tooltip("Local-space offset applied when no explicit origin transform is assigned.")]
        [SerializeField] Vector3 localOriginOffset = new Vector3(0f, 1.1f, 0.6f);

        [Header("Hit Filters")]
        [SerializeField] LayerMask hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Feedback")]
        [SerializeField] Color normalHitColor = new Color32(255, 221, 79, 255);
        [SerializeField] Color critHitColor = new Color32(255, 86, 67, 255);

        PlayerAnimationDriver animationDriver;
        CharacterController characterController;
        WeaponArchetype currentArchetype;
        PlayerDerivedStats stats;

        readonly Collider[] areaBuffer = new Collider[kMaxAreaTargets];
        readonly HashSet<Health> uniqueHealthHits = new HashSet<Health>();

        Coroutine attackRoutine;
        float cooldownTimer;
        bool isAttacking;

        public WeaponArchetype CurrentArchetype {
            get => currentArchetype;
            set => currentArchetype = value;
        }

        void Awake() {
            characterController = GetComponent<CharacterController>();
            animationDriver = GetComponent<PlayerAnimationDriver>();
            ResolveWeaponArchetypeFromEquipment();
        }

        void OnEnable() {
            PlayerStatService.OnChanged += HandleStatsChanged;
            InventoryService.OnChanged += HandleInventoryChanged;
            HandleStatsChanged(PlayerStatService.Current);
            ResolveWeaponArchetypeFromEquipment();
        }

        void OnDisable() {
            PlayerStatService.OnChanged -= HandleStatsChanged;
            InventoryService.OnChanged -= HandleInventoryChanged;
            if (attackRoutine != null) {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isAttacking = false;
            cooldownTimer = 0f;
        }

        void Update() {
            if (cooldownTimer > 0f) {
                cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            }
        }

        void HandleStatsChanged(PlayerDerivedStats updatedStats) {
            stats = updatedStats;
        }

        void HandleInventoryChanged() {
            ResolveWeaponArchetypeFromEquipment();
        }

        void ResolveWeaponArchetypeFromEquipment() {
            if (!autoDetectFromEquipment) {
                currentArchetype = fallbackArchetype;
                return;
            }

            var equipped = InventoryService.Equipped;
            string weaponSlot = EquipmentSlot.Weapon.ToString();
            if (equipped != null && equipped.TryGetValue(weaponSlot, out var itemId) && !string.IsNullOrEmpty(itemId)) {
                var definition = ItemDB.Get(itemId);
                if (definition != null && definition.weaponArchetype != WeaponArchetype.None) {
                    currentArchetype = definition.weaponArchetype;
                    return;
                }
            }

            currentArchetype = fallbackArchetype;
        }

        AttackProfile GetActiveProfile() {
            return currentArchetype == WeaponArchetype.TwoHandedAxe ? twoHandedProfile : oneHandedProfile;
        }

        public void ConfigureAnimationDriver(PlayerAnimationDriver driver) {
            animationDriver = driver;
        }

        public void ConfigureAnimationKeys(PlayerAnimationDriver.AnimationKey oneHanded, PlayerAnimationDriver.AnimationKey twoHanded) {
            oneHandedProfile.animationKey = oneHanded;
            twoHandedProfile.animationKey = twoHanded;
        }

        public bool TryAttack() {
            if (!isActiveAndEnabled) return false;
            if (isAttacking) return false;
            if (cooldownTimer > 0f) return false;

            attackRoutine = StartCoroutine(AttackSequence());
            return true;
        }

        IEnumerator AttackSequence() {
            isAttacking = true;
            var profile = GetActiveProfile();

            float finalCooldown = ComputeCooldown(profile.baseCooldown);
            cooldownTimer = Mathf.Max(finalCooldown, 0.05f);

            PlayAttackAnimation(profile.animationKey);

            float delay = AdjustForSpeed(profile.hitDelay);
            if (delay > 0f) {
                yield return new WaitForSeconds(delay);
            } else {
                yield return null;
            }

            ApplyDamage(profile);

            float recovery = AdjustForSpeed(profile.recovery);
            if (recovery > 0f) {
                yield return new WaitForSeconds(recovery);
            }

            isAttacking = false;
            attackRoutine = null;
        }

        float ComputeCooldown(float baseValue) {
            float speedMultiplier = Mathf.Max(0.2f, stats.AttackSpeedMultiplier);
            float cooldownMultiplier = Mathf.Max(0.1f, 1f - stats.CooldownReduction);
            return Mathf.Max(0.1f, baseValue * cooldownMultiplier / speedMultiplier);
        }

        float AdjustForSpeed(float value) {
            if (value <= 0f) return 0f;
            float speedMultiplier = Mathf.Max(0.2f, stats.AttackSpeedMultiplier);
            return Mathf.Max(0f, value / speedMultiplier);
        }

        void PlayAttackAnimation(PlayerAnimationDriver.AnimationKey key) {
            if (animationDriver) {
                animationDriver.PlayAction(key);
            }
        }

        void ApplyDamage(AttackProfile profile) {
            float damage = Mathf.Max(1f, stats.AttackPower);
            bool crit = Random.value < stats.CritChance;
            if (crit) damage *= 1.5f;

            Vector3 origin = GetAttackOrigin();
            Vector3 forward = GetAttackDirection();

            if (currentArchetype == WeaponArchetype.TwoHandedAxe) {
                ApplyArcDamage(origin, forward, profile, damage, crit);
            } else {
                ApplySingleTargetDamage(origin, forward, profile, damage, crit);
            }
        }

        Vector3 GetAttackOrigin() {
            if (attackOrigin) {
                return attackOrigin.position;
            }

            if (characterController) {
                Vector3 worldCenter = transform.TransformPoint(characterController.center);
                Vector3 offsetWorld = transform.TransformVector(localOriginOffset);
                return worldCenter + offsetWorld;
            }

            return transform.TransformPoint(localOriginOffset);
        }

        Vector3 GetAttackDirection() {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) {
                forward = transform.forward;
            }
            return forward.normalized;
        }

        void ApplySingleTargetDamage(Vector3 origin, Vector3 forward, AttackProfile profile, float damage, bool crit) {
            RaycastHit hit;
            bool hasHit;

            if (profile.rayRadius > 0.01f) {
                hasHit = Physics.SphereCast(origin, profile.rayRadius, forward, out hit, profile.reach, hitMask, triggerInteraction);
            } else {
                hasHit = Physics.Raycast(origin, forward, out hit, profile.reach, hitMask, triggerInteraction);
            }

            if (!hasHit) return;

            var health = hit.collider.GetComponentInParent<Health>();
            if (!IsValidTarget(health, hit.collider)) return;

            health.Take(damage);
            DamageNumbers.Show(hit.point, Mathf.RoundToInt(damage), crit ? critHitColor : normalHitColor, 1f);
        }

        void ApplyArcDamage(Vector3 origin, Vector3 forward, AttackProfile profile, float damage, bool crit) {
            float radius = Mathf.Max(profile.arcRadius, profile.reach);
            int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, areaBuffer, hitMask, triggerInteraction);
            if (hitCount <= 0) return;

            float halfAngle = Mathf.Clamp(profile.arcAngle * 0.5f, 0f, 180f);
            uniqueHealthHits.Clear();

            for (int i = 0; i < hitCount; i++) {
                var col = areaBuffer[i];
                if (!col) continue;
                var health = col.GetComponentInParent<Health>();
                if (!IsValidTarget(health, col)) continue;
                if (!uniqueHealthHits.Add(health)) continue;

                Vector3 targetPoint = col.bounds.ClosestPoint(origin);
                Vector3 toTarget = targetPoint - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude <= 0.0001f) continue;

                float angle = Vector3.Angle(forward, toTarget.normalized);
                if (angle > halfAngle) continue;

                health.Take(damage);
                DamageNumbers.Show(targetPoint, Mathf.RoundToInt(damage), crit ? critHitColor : normalHitColor, 1.1f);
            }
        }

        bool IsValidTarget(Health target, Collider sourceCollider) {
            if (!target) return false;
            if (!target.isActiveAndEnabled) return false;
            if (sourceCollider && sourceCollider.transform.IsChildOf(transform)) return false;
            return true;
        }

#if UNITY_EDITOR
        void OnValidate() {
            oneHandedProfile.baseCooldown = Mathf.Max(0.1f, oneHandedProfile.baseCooldown);
            oneHandedProfile.hitDelay = Mathf.Max(0f, oneHandedProfile.hitDelay);
            oneHandedProfile.recovery = Mathf.Max(0f, oneHandedProfile.recovery);
            oneHandedProfile.reach = Mathf.Max(0.5f, oneHandedProfile.reach);
            oneHandedProfile.rayRadius = Mathf.Max(0f, oneHandedProfile.rayRadius);
            oneHandedProfile.arcRadius = Mathf.Max(0.5f, oneHandedProfile.arcRadius);

            twoHandedProfile.baseCooldown = Mathf.Max(0.1f, twoHandedProfile.baseCooldown);
            twoHandedProfile.hitDelay = Mathf.Max(0f, twoHandedProfile.hitDelay);
            twoHandedProfile.recovery = Mathf.Max(0f, twoHandedProfile.recovery);
            twoHandedProfile.reach = Mathf.Max(0.5f, twoHandedProfile.reach);
            twoHandedProfile.rayRadius = Mathf.Max(0f, twoHandedProfile.rayRadius);
            twoHandedProfile.arcRadius = Mathf.Max(0.5f, twoHandedProfile.arcRadius);
        }
#endif
    }
}

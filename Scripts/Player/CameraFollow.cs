using UnityEngine;

namespace Begin.Control {
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour {
        [SerializeField] Transform followTarget;
        [SerializeField] Vector3 offset = new Vector3(0f, 20f, -20f);
        [SerializeField] Vector3 focusOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField, Min(0f)] float positionResponsiveness = 10f;
        [SerializeField, Min(0f)] float rotationResponsiveness = 12f;
        [SerializeField, Min(0f)] float maxLagDistance = 0.5f;
        [SerializeField] LayerMask collisionMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] float collisionRadius = 0.35f;
        [SerializeField, Min(0f)] float collisionBuffer = 0.05f;

        bool _initialized;

        public Transform target {
            get => followTarget;
            set {
                if (followTarget == value) {
                    return;
                }

                followTarget = value;
                _initialized = false;
            }
        }

        void OnValidate() {
            maxLagDistance = Mathf.Max(0.01f, maxLagDistance);
            collisionRadius = Mathf.Max(0f, collisionRadius);
            collisionBuffer = Mathf.Max(0f, collisionBuffer);
        }

        void LateUpdate() {
            var currentTarget = followTarget;
            if (!currentTarget) {
                return;
            }

            Vector3 focusPoint = currentTarget.position + focusOffset;
            Vector3 desiredPosition = focusPoint + offset;
            desiredPosition = ResolveCollisions(currentTarget, focusPoint, desiredPosition);

            if (!_initialized) {
                transform.position = desiredPosition;
                transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
                _initialized = true;
                return;
            }

            transform.position = DampPosition(transform.position, desiredPosition);
            EnforceLagLimit(focusPoint, desiredPosition);

            Quaternion desiredRotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
            transform.rotation = DampRotation(transform.rotation, desiredRotation);
        }

        Vector3 DampPosition(Vector3 current, Vector3 desired) {
            if (positionResponsiveness <= 0f) {
                return desired;
            }

            float t = 1f - Mathf.Exp(-positionResponsiveness * Time.deltaTime);
            return Vector3.Lerp(current, desired, t);
        }

        Quaternion DampRotation(Quaternion current, Quaternion desired) {
            if (rotationResponsiveness <= 0f) {
                return desired;
            }

            float t = 1f - Mathf.Exp(-rotationResponsiveness * Time.deltaTime);
            return Quaternion.Slerp(current, desired, t);
        }

        void EnforceLagLimit(Vector3 focusPoint, Vector3 desiredPosition) {
            Vector3 delta = transform.position - desiredPosition;
            float maxLagSqr = maxLagDistance * maxLagDistance;
            if (delta.sqrMagnitude > maxLagSqr) {
                transform.position = desiredPosition + delta.normalized * maxLagDistance;
            }

            Vector3 fromFocus = transform.position - focusPoint;
            float desiredDistance = (desiredPosition - focusPoint).magnitude;
            float maxDistance = desiredDistance + maxLagDistance;
            float currentDistance = fromFocus.magnitude;
            if (currentDistance > maxDistance) {
                transform.position = focusPoint + fromFocus.normalized * maxDistance;
            }
        }

        Vector3 ResolveCollisions(Transform currentTarget, Vector3 focusPoint, Vector3 desiredPosition) {
            Vector3 toCamera = desiredPosition - focusPoint;
            float distance = toCamera.magnitude;
            if (distance <= 0.001f) {
                return desiredPosition;
            }

            if (collisionMask.value == 0) {
                return desiredPosition;
            }

            Vector3 direction = toCamera / distance;
            var hits = Physics.SphereCastAll(focusPoint, collisionRadius, direction, distance, collisionMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) {
                return desiredPosition;
            }

            float closest = distance;
            bool foundHit = false;
            foreach (var hit in hits) {
                if (!hit.transform || hit.distance <= 0f) {
                    continue;
                }

                if (currentTarget && hit.transform.IsChildOf(currentTarget)) {
                    continue;
                }

                if (hit.distance < closest) {
                    closest = hit.distance;
                    foundHit = true;
                }
            }

            if (!foundHit) {
                return desiredPosition;
            }

            float safeDistance = Mathf.Max(closest - collisionBuffer, 0f);
            return focusPoint + direction * safeDistance;
        }

#if UNITY_EDITOR
        void Reset() {
            if (!followTarget) {
                var player = FindObjectOfType<Player.PlayerController>();
                if (player) {
                    followTarget = player.transform;
                }
            }
        }
#endif
    }
}

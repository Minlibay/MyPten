using UnityEngine;

namespace Begin.Control {
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour {
        [SerializeField] Transform followTarget;
        [SerializeField] Vector3 offset = new Vector3(0f, 20f, -20f);
        [SerializeField] Vector3 focusOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField, Min(0f)] float positionResponsiveness = 10f;
        [SerializeField, Min(0f)] float rotationResponsiveness = 12f;
        [SerializeField] bool hardLockToTarget = true;
        [SerializeField, Min(0f)] float maxLagDistance = 0.5f;
        [SerializeField] LayerMask collisionMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] float collisionRadius = 0.35f;
        [SerializeField, Min(0f)] float collisionBuffer = 0.05f;

        [Header("Orbit Controls")]
        [SerializeField] bool enableOrbitControls = true;
        [SerializeField] bool requireRightMouseToOrbit = true;
        [SerializeField, Range(10f, 720f)] float orbitSensitivity = 240f;
        [SerializeField] Vector2 pitchLimits = new Vector2(-35f, 65f);
        [SerializeField] bool invertY = false;

        [Header("Zoom")]
        [SerializeField] bool enableZoom = true;
        [SerializeField, Min(0.1f)] float zoomSensitivity = 4f;
        [SerializeField, Min(0.1f)] float minDistance = 2.5f;
        [SerializeField, Min(0.1f)] float maxDistance = 16f;

        bool _initialized;
        bool _orbitInitialized;
        float _orbitYaw;
        float _orbitPitch;
        float _currentDistance;
        Vector3 _runtimeOffset;

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
            zoomSensitivity = Mathf.Max(0.01f, zoomSensitivity);
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            pitchLimits.x = Mathf.Clamp(pitchLimits.x, -89f, 89f);
            pitchLimits.y = Mathf.Clamp(pitchLimits.y, -89f, 89f);
            if (pitchLimits.x > pitchLimits.y) {
                (pitchLimits.x, pitchLimits.y) = (pitchLimits.y, pitchLimits.x);
            }
            _runtimeOffset = offset;
            SyncOrbitWithOffset();
        }

        void Awake() {
            _runtimeOffset = offset;
            SyncOrbitWithOffset();
        }

        void LateUpdate() {
            var currentTarget = followTarget;
            if (!currentTarget) {
                return;
            }

            if (enableOrbitControls) {
                UpdateOrbitInput();
            }

            Vector3 focusPoint = currentTarget.position + focusOffset;
            Vector3 desiredOffset = enableOrbitControls ? ComputeOrbitOffset() : offset;
            _runtimeOffset = desiredOffset;
            Vector3 desiredPosition = focusPoint + desiredOffset;
            desiredPosition = ResolveCollisions(currentTarget, focusPoint, desiredPosition);

            Quaternion desiredRotation = Quaternion.LookRotation(focusPoint - desiredPosition, Vector3.up);

            if (hardLockToTarget) {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                _initialized = true;
                return;
            }

            if (!_initialized) {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                _initialized = true;
                return;
            }

            transform.position = DampPosition(transform.position, desiredPosition);
            EnforceLagLimit(focusPoint, desiredPosition);

            transform.rotation = DampRotation(transform.rotation, desiredRotation);
        }

        void SyncOrbitWithOffset() {
            Vector3 sourceOffset = _runtimeOffset.sqrMagnitude > 0.0001f ? _runtimeOffset : offset;
            _currentDistance = Mathf.Max(0.01f, sourceOffset.magnitude);
            if (_currentDistance <= 0.01f) {
                _currentDistance = 5f;
                sourceOffset = new Vector3(0f, 2f, -_currentDistance);
            }

            Vector3 direction = sourceOffset / _currentDistance;
            direction = direction.sqrMagnitude > 0.0001f ? direction : new Vector3(0f, 0f, -1f);
            _orbitPitch = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            _orbitYaw = Mathf.Atan2(direction.x, -direction.z) * Mathf.Rad2Deg;
            _orbitPitch = Mathf.Clamp(_orbitPitch, pitchLimits.x, pitchLimits.y);
            _orbitInitialized = true;
        }

        void UpdateOrbitInput() {
            if (!_orbitInitialized) {
                SyncOrbitWithOffset();
            }

            bool canRotate = !requireRightMouseToOrbit || Input.GetMouseButton(1);
            float deltaTime = Time.unscaledDeltaTime;

            if (canRotate) {
                float mouseX = Input.GetAxisRaw("Mouse X");
                float mouseY = Input.GetAxisRaw("Mouse Y");
                if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f) {
                    _orbitYaw += mouseX * orbitSensitivity * deltaTime;
                    float yDelta = invertY ? mouseY : -mouseY;
                    _orbitPitch += yDelta * orbitSensitivity * deltaTime;
                    _orbitPitch = Mathf.Clamp(_orbitPitch, pitchLimits.x, pitchLimits.y);
                }
            }

            if (enableZoom) {
                float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f) {
                    _currentDistance -= scroll * zoomSensitivity;
                    _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
                }
            }
        }

        Vector3 ComputeOrbitOffset() {
            if (!_orbitInitialized) {
                SyncOrbitWithOffset();
            }

            float yawRad = _orbitYaw * Mathf.Deg2Rad;
            float pitchRad = _orbitPitch * Mathf.Deg2Rad;
            float cosPitch = Mathf.Cos(pitchRad);

            Vector3 result;
            result.x = Mathf.Sin(yawRad) * cosPitch * _currentDistance;
            result.y = Mathf.Sin(pitchRad) * _currentDistance;
            result.z = -Mathf.Cos(yawRad) * cosPitch * _currentDistance;
            return result;
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
                var player = FindObjectOfType<PlayerController>();
                if (player) {
                    followTarget = player.transform;
                }
            }
        }
#endif
    }
}

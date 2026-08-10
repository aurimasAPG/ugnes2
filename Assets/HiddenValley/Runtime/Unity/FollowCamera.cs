using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// The follow camera the Phase 1 gate interrogates: five minutes of adversarial play —
    /// backing into corners, spinning against walls, running down slopes — and it must never
    /// clip geometry and never lose the player.
    ///
    /// Two decisions carry that gate. First, there is no free orbit: the camera always looks
    /// at the player, so it cannot lose them by construction. Second, occlusion is handled by
    /// a sphere cast from the pivot toward the desired position, pulled in fast and released
    /// slowly — snapping in hides a wall clip within one frame, easing out prevents the
    /// pumping that fast in/fast out produces when strafing along a fence.
    ///
    /// Yaw deliberately does NOT track the player's facing. Camera-relative movement +
    /// "yaw follows player yaw" + "player faces camera-relative wish" is a positive-feedback
    /// spin (observed on Mac, 2026-08-07). Yaw only gently reframes toward recent travel
    /// when the player has been moving for a beat, and never when idle.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Framing")]
        [SerializeField] private float distance = 6f;
        [SerializeField] private float pitchDegrees = 22f;
        [SerializeField] private float pivotHeight = 1.5f;

        [Header("Feel")]
        [SerializeField] private float followLag = 8f;
        [SerializeField] private float travelYawLag = 1.2f;
        [SerializeField] private float travelYawMinSpeed = 1.2f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float releaseLag = 4f;
        [SerializeField] private LayerMask collisionMask = ~0;

        [Header("Speed feel")]
        [SerializeField] private float sprintFovKick = 5f;
        [SerializeField] private float sprintDistanceKick = 0.5f;
        [SerializeField] private float kickResponse = 3f;
        [SerializeField] private float lookAheadMeters = 0.6f;
        [SerializeField] private float lookAheadResponse = 2f;

        private Vector3 _pivot;
        private float _yaw;
        private float _currentDistance;

        private Camera _camera;
        private PlayerController _playerSource;
        private float _baseFov;
        private float _kick;                 // 0..1, eased toward sprinting
        private Vector3 _lookAhead;          // damped world-space travel offset
        private Vector3 _lastTargetPos;
        private bool _hasLastPos;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Start()
        {
            if (target == null) return;
            _camera = GetComponent<Camera>();
            _baseFov = _camera != null ? _camera.fieldOfView : 60f;
            _playerSource = target.GetComponent<PlayerController>();
            _pivot = Pivot();
            _yaw = target.eulerAngles.y;
            _currentDistance = distance;
            _lastTargetPos = target.position;
            _hasLastPos = true;
            Apply(1f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float followT = 1f - Mathf.Exp(-followLag * Time.deltaTime);
            _pivot = Vector3.Lerp(_pivot, Pivot(), followT);

            // Independent yaw: only ease toward the direction the player *travelled*,
            // and only when they are actually moving. Holding a strafe no longer spins
            // the whole camera around them.
            if (_hasLastPos)
            {
                Vector3 delta = target.position - _lastTargetPos;
                delta.y = 0f;
                float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                float speed = delta.magnitude / dt;
                if (speed >= travelYawMinSpeed && delta.sqrMagnitude > 0.0001f)
                {
                    float desiredYaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                    float yawT = 1f - Mathf.Exp(-travelYawLag * Time.deltaTime);
                    _yaw = Mathf.LerpAngle(_yaw, desiredYaw, yawT);
                }

                // Speed feel: FOV/boom kick near top speed, plus a small damped
                // look-ahead so the camera shows where you're going. Both feed the same
                // pivot/boom the occlusion cast guards — the Phase 1 never-clips /
                // never-loses invariants are untouched.
                float speed01 = _playerSource != null
                    ? Mathf.Clamp01(_playerSource.NormalizedSpeed) : 0f;
                float kickTarget = speed01 > 0.72f ? 1f : 0f;
                _kick = Mathf.Lerp(_kick, kickTarget, 1f - Mathf.Exp(-kickResponse * dt));
                if (_camera != null) _camera.fieldOfView = _baseFov + sprintFovKick * _kick;

                Vector3 lookTarget = speed >= travelYawMinSpeed
                    ? delta.normalized * (lookAheadMeters * speed01)
                    : Vector3.zero;
                _lookAhead = Vector3.Lerp(
                    _lookAhead, lookTarget, 1f - Mathf.Exp(-lookAheadResponse * dt));
            }

            _lastTargetPos = target.position;
            _hasLastPos = true;

            Apply(1f - Mathf.Exp(-releaseLag * Time.deltaTime));
        }

        private Vector3 Pivot() => target.position + Vector3.up * pivotHeight;

        private void Apply(float releaseT)
        {
            var rotation = Quaternion.Euler(pitchDegrees, _yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Look-ahead shifts the pivot the cast starts from, so the offset view is
            // exactly as occlusion-guarded as the centered one.
            Vector3 pivot = _pivot + _lookAhead;
            float boom = distance + sprintDistanceKick * _kick;

            float free = boom;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, boom,
                                   collisionMask, QueryTriggerInteraction.Ignore))
            {
                free = hit.distance;
            }

            _currentDistance = free < _currentDistance
                ? free
                : Mathf.Lerp(_currentDistance, free, releaseT);

            transform.position = pivot + back * _currentDistance;
            transform.rotation = Quaternion.LookRotation(pivot - transform.position);
        }
    }
}

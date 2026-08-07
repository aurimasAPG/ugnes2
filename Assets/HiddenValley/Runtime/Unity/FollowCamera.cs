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
        [SerializeField] private float yawFollowLag = 3.5f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float releaseLag = 4f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private Vector3 _pivot;
        private float _yaw;
        private float _currentDistance;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Start()
        {
            if (target == null) return;
            _pivot = Pivot();
            _yaw = target.eulerAngles.y;
            _currentDistance = distance;
            Apply(1f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Exponential damping, framerate-independent. Time.deltaTime * lag is the
            // naive version and behaves differently at 30 and 60 fps — on a project whose
            // first quality dimension is frame budget, the camera must not change feel
            // when the budget slips during development.
            float followT = 1f - Mathf.Exp(-followLag * Time.deltaTime);
            float yawT = 1f - Mathf.Exp(-yawFollowLag * Time.deltaTime);

            _pivot = Vector3.Lerp(_pivot, Pivot(), followT);
            _yaw = Mathf.LerpAngle(_yaw, target.eulerAngles.y, yawT);

            Apply(1f - Mathf.Exp(-releaseLag * Time.deltaTime));
        }

        private Vector3 Pivot() => target.position + Vector3.up * pivotHeight;

        private void Apply(float releaseT)
        {
            var rotation = Quaternion.Euler(pitchDegrees, _yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // The cast starts at the pivot, so geometry between player and camera is what
            // shortens the boom — never geometry behind the player.
            float free = distance;
            if (Physics.SphereCast(_pivot, collisionRadius, back, out var hit, distance,
                                   collisionMask, QueryTriggerInteraction.Ignore))
            {
                free = hit.distance;
            }

            // In: immediately. Out: eased.
            _currentDistance = free < _currentDistance
                ? free
                : Mathf.Lerp(_currentDistance, free, releaseT);

            transform.position = _pivot + back * _currentDistance;
            transform.rotation = Quaternion.LookRotation(_pivot - transform.position);
        }
    }
}

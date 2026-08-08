using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Character movement for the Phase 0/1 done-states: touch joystick in, capsule motion
    /// out. Acceleration and deceleration are explicit fields rather than a blend tree or
    /// physics material, because Phase 1 is tuned by feel on device and every number that
    /// matters must be reachable from the inspector without touching code.
    ///
    /// Movement is camera-relative: stick-up walks away from the camera. The player faces
    /// the *wish* direction (stick × camera), not post-collision velocity — facing velocity
    /// while the camera also yaws toward the player creates a feedback spin (fixed 2026-08-07).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private TouchControls controls;
        [SerializeField] private Transform cameraTransform;

        [Header("Feel — the Phase 1 tuning surface")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private float acceleration = 24f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float turnDegreesPerSecond = 540f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -28f;
        [SerializeField] private float faceStickThreshold = 0.05f;

        private CharacterController _cc;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private Vector3 _lastWish;

        /// <summary>Planar speed as a fraction of sprint speed — for animation / footsteps.</summary>
        public float NormalizedSpeed =>
            _planarVelocity.magnitude / Mathf.Max(0.01f, walkSpeed * sprintMultiplier);

        /// <summary>True while grounded and moving faster than a crawl.</summary>
        public bool IsMovingOnGround =>
            _cc != null && _cc.isGrounded && _planarVelocity.sqrMagnitude > 0.05f;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 stick = controls != null ? controls.Move : Vector2.zero;
            bool sprint = controls != null && controls.Sprint;
            bool jump = controls != null && controls.ConsumeJump();

            // Camera-relative basis, flattened. Camera yaw is independent of player facing
            // (see FollowCamera), so this basis is stable while the stick is held.
            Vector3 fwd = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)
                : transform.forward;
            if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            Vector3 wish = (fwd * stick.y + right * stick.x);
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            _lastWish = wish;

            float topSpeed = walkSpeed * (sprint ? sprintMultiplier : 1f);
            Vector3 target = wish * topSpeed;

            // Accelerate toward the stick, decelerate toward zero. Two rates, because
            // stopping crisply matters more to feel than starting crisply.
            float rate = target.sqrMagnitude > _planarVelocity.sqrMagnitude
                ? acceleration
                : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, target, rate * Time.deltaTime);

            // Snap residual crawl to zero so we don't slide forever after release.
            if (target.sqrMagnitude < 0.0001f && _planarVelocity.sqrMagnitude < 0.01f)
                _planarVelocity = Vector3.zero;

            if (_cc.isGrounded)
            {
                // A small constant downward push keeps isGrounded stable on slopes.
                _verticalVelocity = -2f;
                if (jump)
                {
                    _verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
                    GameAudio.Instance?.Jump();
                }
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            _cc.Move((_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);

            // Face the stick wish while moving; hold last facing when idle. Never face
            // post-collision velocity — that plus a yaw-following camera is a spin loop.
            if (wish.sqrMagnitude > faceStickThreshold)
            {
                var look = Quaternion.LookRotation(wish.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, look, turnDegreesPerSecond * Time.deltaTime);
            }
        }
    }
}

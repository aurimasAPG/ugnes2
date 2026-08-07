using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Character movement for the Phase 0/1 done-states: touch joystick in, capsule motion
    /// out. Acceleration and deceleration are explicit fields rather than a blend tree or
    /// physics material, because Phase 1 is tuned by feel on device and every number that
    /// matters must be reachable from the inspector without touching code.
    ///
    /// Movement is camera-relative: stick-up walks away from the camera. Slope and step
    /// handling are delegated to the CharacterController's slopeLimit and stepOffset, which
    /// is the cheapest correct answer on mobile — no physics ticks, no rigidbody sleep bugs.
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
        [SerializeField] private float turnDegreesPerSecond = 720f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -28f;

        private CharacterController _cc;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;

        /// <summary>Planar speed as a fraction of sprint speed — for animation later.</summary>
        public float NormalizedSpeed =>
            _planarVelocity.magnitude / (walkSpeed * sprintMultiplier);

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 stick = controls != null ? controls.Move : Vector2.zero;
            bool sprint = controls != null && controls.Sprint;
            bool jump = controls != null && controls.ConsumeJump();

            // Camera-relative basis, flattened. If the camera ever pitches straight down
            // the projected forward degenerates, so fall back to the player's own forward.
            Vector3 fwd = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)
                : transform.forward;
            if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            Vector3 wish = (fwd * stick.y + right * stick.x);
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            float topSpeed = walkSpeed * (sprint ? sprintMultiplier : 1f);
            Vector3 target = wish * topSpeed;

            // Accelerate toward the stick, decelerate toward zero. Two rates, because
            // stopping crisply matters more to feel than starting crisply.
            float rate = target.sqrMagnitude > _planarVelocity.sqrMagnitude
                ? acceleration
                : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, target, rate * Time.deltaTime);

            if (_cc.isGrounded)
            {
                // A small constant downward push keeps isGrounded stable on slopes.
                _verticalVelocity = -2f;
                if (jump) _verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            _cc.Move((_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);

            // Face travel direction, not stick direction — after collisions they differ,
            // and facing the wall you are sliding along reads as broken.
            Vector3 face = Vector3.ProjectOnPlane(_planarVelocity, Vector3.up);
            if (face.sqrMagnitude > 0.04f)
            {
                var look = Quaternion.LookRotation(face);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, look, turnDegreesPerSecond * Time.deltaTime);
            }
        }
    }
}

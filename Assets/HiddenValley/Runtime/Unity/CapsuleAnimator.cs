using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Procedural life for capsule-era characters: lean into acceleration, a speed-driven
    /// gait bob with roll, jump stretch and landing squash, and idle breathing. A capsule
    /// that translates rigidly is the loudest grey-box tell there is; this component is
    /// also the animation contract for the rigged characters later — the same parameters
    /// ride on top of a real rig, so the feel tuned now survives the art pass.
    ///
    /// Attached at runtime (PlayerController/NpcBinder), never serialized into a scene —
    /// scene-serialized additions are how the level0 corruption was earned (phase log,
    /// 2026-08-07). It derives velocity from transform deltas so it works identically for
    /// the CharacterController-driven player and the MoveTowards-driven NPCs.
    /// </summary>
    [DefaultExecutionOrder(100)] // after every mover has finished the frame
    public sealed class CapsuleAnimator : MonoBehaviour
    {
        [Header("Gait")]
        [SerializeField] private float strideCyclesPerMeter = 0.7f;
        [SerializeField] private float bobAmplitude = 0.045f;
        [SerializeField] private float rollDegrees = 2.2f;
        [SerializeField] private float fullSpeedReference = 7.2f;

        [Header("Lean")]
        [SerializeField] private float leanMaxDegrees = 8f;
        [SerializeField] private float leanPerMetersPerSecond2 = 0.9f;
        [SerializeField] private float leanResponse = 6f;

        [Header("Jump / land")]
        [SerializeField] private float landSquash = 0.85f;
        [SerializeField] private float jumpStretch = 1.12f;
        [SerializeField] private float squashRecover = 6f;

        [Header("Idle")]
        [SerializeField] private float breathScale = 0.015f;
        [SerializeField] private float breathHz = 0.25f;

        /// <summary>Fires once per gait cycle at foot-plant. GameAudio hooks footsteps
        /// here so sound and visible motion share one clock.</summary>
        public event System.Action Footstep;

        private Transform _visual;
        private PlayerController _playerSource; // optional; null for NPCs

        private Vector3 _baseScale, _baseLocalPos;
        private Vector3 _prevPos, _prevPlanarVelocity, _smoothedAccel;
        private float _phase, _squash = 1f, _lastStepSin;
        private bool _wasGrounded = true;
        private float _prevVy;

        public static CapsuleAnimator Attach(Transform visual, PlayerController source = null)
        {
            if (visual == null || visual.parent == null) return null;

            var animator = visual.parent.gameObject.AddComponent<CapsuleAnimator>();
            animator._visual = visual;
            animator._playerSource = source;
            animator._baseScale = visual.localScale;
            animator._baseLocalPos = visual.localPosition;
            animator._prevPos = visual.parent.position;
            return animator;
        }

        private void LateUpdate()
        {
            if (_visual == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 position = transform.position;
            Vector3 velocity = (position - _prevPos) / dt;
            _prevPos = position;

            Vector3 planar = velocity;
            planar.y = 0;
            float speed = planar.magnitude;
            float speed01 = Mathf.Clamp01(speed / fullSpeedReference);

            // Jump / land edges. Only the player has real airtime; NPCs stay grounded.
            bool grounded = _playerSource == null || _playerSource.IsGrounded;
            if (_playerSource != null)
            {
                if (!_wasGrounded && grounded && _prevVy < -3f) _squash = landSquash;
                else if (_wasGrounded && !grounded && velocity.y > 1f) _squash = jumpStretch;
            }
            _wasGrounded = grounded;
            _prevVy = velocity.y;
            _squash = Mathf.MoveTowards(_squash, 1f, squashRecover * dt);

            // Gait phase advances with distance, not time — stride length stays constant.
            _phase += speed * dt * strideCyclesPerMeter * Mathf.PI * 2f;
            float stepSin = Mathf.Sin(_phase);
            if (grounded && speed01 > 0.15f && _lastStepSin <= 0f && stepSin > 0f)
                Footstep?.Invoke();
            _lastStepSin = stepSin;

            float bob = stepSin * bobAmplitude * speed01;
            float gaitRoll = Mathf.Sin(_phase) * rollDegrees * speed01;

            // Lean into acceleration, expressed in local space so it reads as effort.
            Vector3 accel = (planar - _prevPlanarVelocity) / dt;
            _prevPlanarVelocity = planar;
            _smoothedAccel = Vector3.Lerp(
                _smoothedAccel, accel, 1f - Mathf.Exp(-leanResponse * dt));

            Vector3 localAccel = transform.InverseTransformDirection(_smoothedAccel);
            float leanPitch = Mathf.Clamp(
                localAccel.z * leanPerMetersPerSecond2, -leanMaxDegrees, leanMaxDegrees);
            float leanRoll = Mathf.Clamp(
                -localAccel.x * leanPerMetersPerSecond2, -leanMaxDegrees, leanMaxDegrees);

            // Idle breathing only when actually idle.
            float breath = (grounded && speed01 < 0.05f)
                ? Mathf.Sin(Time.time * breathHz * Mathf.PI * 2f) * breathScale
                : 0f;

            // Squash about the feet, roughly volume-preserving.
            float scaleY = _squash * (1f + breath);
            float scaleXz = 1f + (1f - scaleY) * 0.5f;

            _visual.localScale = new Vector3(
                _baseScale.x * scaleXz, _baseScale.y * scaleY, _baseScale.z * scaleXz);
            _visual.localPosition = new Vector3(
                _baseLocalPos.x, _baseLocalPos.y * scaleY + bob, _baseLocalPos.z);
            _visual.localRotation = Quaternion.Euler(leanPitch, 0f, gaitRoll + leanRoll);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Pip. Follows, and dims near the residue.
    ///
    /// The dimming is deliberately analog and deliberately unexplained (world bible §6). It
    /// is not a marker, not a highlight, and not a ping — there is no UI here at all, and
    /// nothing snaps. The player is meant to notice a correlation over several minutes and
    /// then start using it, which only works if the signal is continuous enough to read by
    /// standing still and watching.
    ///
    /// The failure mode to avoid in tuning: if the falloff is sharp, this becomes a
    /// proximity beep with extra steps and the discovery stops being the player's.
    /// </summary>
    public sealed class PipCompanion : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform follow;
        [SerializeField] private Vector3 offset = new Vector3(-0.35f, 1.55f, -0.1f);
        [SerializeField] private float followSmoothing = 3.5f;
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float bobFrequency = 1.3f;

        [Header("Light")]
        [SerializeField] private Light lamp;
        [SerializeField] private float brightIntensity = 1.5f;
        [SerializeField] private float dimIntensity = 0.15f;
        [SerializeField] private float responseSmoothing = 1.2f;

        [Header("Residue")]
        [SerializeField] private float falloffRadius = 14f;
        [SerializeField] private List<Transform> residueSources = new List<Transform>();

        private Vector3 _velocity;
        private float _dimness;

        /// <summary>0 = bright and steady, 1 = as dim as Pip gets. Read by audio and VFX.</summary>
        public float Dimness => _dimness;

        /// <summary>Runtime wiring for <see cref="LayoutSpawner"/>.</summary>
        public void Configure(Transform followTarget, Light lampLight)
        {
            follow = followTarget;
            lamp = lampLight;
        }

        private void LateUpdate()
        {
            if (follow != null)
            {
                var target = follow.TransformPoint(offset);
                target.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;

                transform.position = Vector3.SmoothDamp(
                    transform.position, target, ref _velocity, 1f / Mathf.Max(0.01f, followSmoothing));
            }

            float targetDimness = ResidueProximity();

            // Smoothed both ways, so Pip fades rather than flickers when the player crosses
            // a boundary. A flicker reads as a bug; a fade reads as a creature.
            _dimness = Mathf.MoveTowards(_dimness, targetDimness, Time.deltaTime * responseSmoothing);

            if (lamp != null)
                lamp.intensity = Mathf.Lerp(brightIntensity, dimIntensity, _dimness);
        }

        /// <summary>Nearest residue source, normalised. 1 at the source, 0 at the falloff radius.</summary>
        private float ResidueProximity()
        {
            float nearest = float.MaxValue;

            foreach (var source in residueSources)
            {
                if (source == null || !source.gameObject.activeInHierarchy) continue;

                float distance = Vector3.Distance(transform.position, source.position);
                if (distance < nearest) nearest = distance;
            }

            if (nearest >= falloffRadius) return 0f;

            // Squared falloff: almost nothing at the edge, unmistakable close in. Linear
            // reads as a gauge; this reads as a reaction.
            float t = 1f - (nearest / falloffRadius);
            return t * t;
        }
    }
}

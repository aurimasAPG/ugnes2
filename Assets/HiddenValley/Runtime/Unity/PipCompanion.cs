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
        private Vector3 _wander;
        private float _nextWanderAt;
        private float _flutterSeed;
        private Vector3 _lastFollowPos;
        private Transform _halo;
        private Material _haloMaterial;

        /// <summary>0 = bright and steady, 1 = as dim as Pip gets. Read by audio and VFX.</summary>
        public float Dimness => _dimness;

        /// <summary>True during dialogue: Pip settles to a calm hover beside the two speakers.</summary>
        public bool Calm { get; set; }

        /// <summary>Runtime wiring for <see cref="LayoutSpawner"/>.</summary>
        public void Configure(Transform followTarget, Light lampLight)
        {
            follow = followTarget;
            lamp = lampLight;
            _flutterSeed = Random.value * 100f;
            SpawnHalo();
        }

        private void LateUpdate()
        {
            float time = Time.time;

            if (follow != null)
            {
                float speed = ((follow.position - _lastFollowPos) / Mathf.Max(Time.deltaTime, 0.001f)).magnitude;
                _lastFollowPos = follow.position;
                float speed01 = Mathf.Clamp01(speed / 7f);

                // Fast player: Pip trails behind the shoulder. Idle: it drifts on a slow
                // wander, occasionally repositioning — a creature, not a hood ornament.
                Vector3 dynamicOffset = offset + new Vector3(0, 0, -0.6f * speed01);
                if (!Calm && speed01 < 0.05f && time >= _nextWanderAt)
                {
                    _nextWanderAt = time + Random.Range(4f, 8f);
                    _wander = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.25f, 0.35f), Random.Range(-0.4f, 0.8f));
                }
                if (Calm) _wander = Vector3.zero;

                var target = follow.TransformPoint(dynamicOffset + _wander);

                // Moth, not metronome: two incommensurate sines plus drift — never a clean
                // loop. Flutter roughens as the light dims (distress you can see).
                float flutterAmp = Calm ? 0.015f : bobAmplitude * (1f + _dimness * 1.6f);
                target.y += Mathf.Sin(time * bobFrequency) * flutterAmp
                          + Mathf.Sin(time * (bobFrequency * 2.71f) + _flutterSeed) * flutterAmp * 0.45f;
                target.x += Mathf.Sin(time * 1.93f + _flutterSeed) * flutterAmp * 0.6f;

                transform.position = Vector3.SmoothDamp(
                    transform.position, target, ref _velocity, 1f / Mathf.Max(0.01f, followSmoothing));
            }

            float targetDimness = ResidueProximity();

            // Smoothed both ways, so Pip fades rather than flickers when the player crosses
            // a boundary. A flicker reads as a bug; a fade reads as a creature.
            _dimness = Mathf.MoveTowards(_dimness, targetDimness, Time.deltaTime * responseSmoothing);

            if (lamp != null)
            {
                // Base intensity from dimness, plus a gutter whose amplitude grows as the
                // light thins — "the light goes thin, comes back, goes thin again",
                // finally true of the actual light.
                float baseIntensity = Mathf.Lerp(brightIntensity, dimIntensity, _dimness);
                float gutter = (Mathf.PerlinNoise(_flutterSeed, time * 2.2f) - 0.5f)
                               * (0.12f + 0.55f * _dimness);
                lamp.intensity = Mathf.Max(0.02f, baseIntensity * (1f + gutter));
            }

            UpdateHalo();
            FlapWings(time);
        }

        private Transform _wingL, _wingR;

        /// <summary>Wing beat quickens as the light thins — distress you can see.</summary>
        private void FlapWings(float time)
        {
            if (_wingL == null)
            {
                var visual = transform.Find("Visual");
                if (visual == null) return;
                _wingL = visual.Find("Wing.L");
                _wingR = visual.Find("Wing.R");
                if (_wingL == null) return;
            }

            float rate = 9f + 11f * _dimness;
            float flap = Mathf.Sin(time * rate) * 38f;
            _wingL.localRotation = Quaternion.Euler(90, 0, 20f + flap);
            _wingR.localRotation = Quaternion.Euler(90, 0, -20f - flap);
        }

        // ---- glow halo --------------------------------------------------------

        /// <summary>An additive soft disc that makes the lamp's state readable at a
        /// glance without HDR bloom. Scale and alpha follow the light.</summary>
        private void SpawnHalo()
        {
            if (_halo != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Halo";
            go.layer = gameObject.layer;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _haloMaterial = new Material(shader);
            _haloMaterial.mainTexture = SoftCircle();
            _haloMaterial.SetOverrideTag("RenderType", "Transparent");
            _haloMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _haloMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additive
            _haloMaterial.SetInt("_ZWrite", 0);
            if (_haloMaterial.HasProperty("_Surface")) _haloMaterial.SetFloat("_Surface", 1f);
            _haloMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            go.GetComponent<MeshRenderer>().material = _haloMaterial;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<Billboard>();

            _halo = go.transform;
        }

        private void UpdateHalo()
        {
            if (_halo == null || lamp == null) return;
            float glow = Mathf.InverseLerp(dimIntensity, brightIntensity, lamp.intensity);
            _halo.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.5f, glow);
            if (_haloMaterial != null)
            {
                var color = new Color(1f, 0.9f, 0.68f, Mathf.Lerp(0.04f, 0.38f, glow));
                if (_haloMaterial.HasProperty("_BaseColor")) _haloMaterial.SetColor("_BaseColor", color);
                else _haloMaterial.color = color;
            }
        }

        private static Texture2D _softCircle;

        private static Texture2D SoftCircle()
        {
            if (_softCircle != null) return _softCircle;
            const int size = 64;
            _softCircle = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float a = Mathf.Clamp01(1f - d);
                    _softCircle.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            _softCircle.Apply();
            return _softCircle;
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

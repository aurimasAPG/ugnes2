using HiddenValley.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenValley.Unity
{
    /// <summary>
    /// The whole atmosphere in one component: gradient sky, fog, trilight ambient, a sun
    /// driven by the Core clock, and the post volume (grading + vignette). Spawned from
    /// code by GameBootstrap — never serialized into a scene (the level0 landmine).
    ///
    /// One palette per day phase; everything lerps through phase boundaries over a
    /// transition window. The single load-bearing trick: fog color == sky horizon color,
    /// so geometry dissolves into the sky it stands under. Dawn must LOOK like dawn —
    /// the head-gate puzzle only works then, and until this component existed the whole
    /// day happened under one noon light.
    ///
    /// Look: the world bible's "wet stone and low cloud… never twee" — day is overcast
    /// neutral (no postcard noon), warmth belongs to dawn/dusk and man-made light.
    /// </summary>
    public sealed class AtmosphereRig : MonoBehaviour
    {
        [System.Serializable]
        private struct PhasePalette
        {
            public Color skyTop, skyHorizon, skyGround;
            public Color ambientSky, ambientEquator, ambientGround;
            public Color sunColor;
            public float sunIntensity;
            public float fogDensity;
            public float sunElevation; // degrees above horizon at this phase's midpoint
        }

        // night / dawn / day / dusk — boundaries come from the content clock settings.
        private static readonly PhasePalette Night = new PhasePalette
        {
            skyTop = new Color(0.09f, 0.11f, 0.17f),
            skyHorizon = new Color(0.16f, 0.18f, 0.24f),
            skyGround = new Color(0.06f, 0.07f, 0.09f),
            ambientSky = new Color(0.16f, 0.18f, 0.26f),
            ambientEquator = new Color(0.10f, 0.11f, 0.16f),
            ambientGround = new Color(0.05f, 0.06f, 0.08f),
            sunColor = new Color(0.55f, 0.62f, 0.80f),
            sunIntensity = 0.18f,
            fogDensity = 0.014f,
            sunElevation = 18f, // the "moon": a dim cool key so night is readable
        };

        private static readonly PhasePalette Dawn = new PhasePalette
        {
            skyTop = new Color(0.34f, 0.40f, 0.52f),
            skyHorizon = new Color(0.82f, 0.72f, 0.58f),
            skyGround = new Color(0.30f, 0.31f, 0.33f),
            ambientSky = new Color(0.52f, 0.54f, 0.62f),
            ambientEquator = new Color(0.55f, 0.50f, 0.46f),
            ambientGround = new Color(0.24f, 0.24f, 0.25f),
            sunColor = new Color(1.00f, 0.83f, 0.62f),
            sunIntensity = 0.85f,
            fogDensity = 0.016f, // dawn is the misty one — it is also the puzzle hour
            sunElevation = 14f,
        };

        private static readonly PhasePalette Day = new PhasePalette
        {
            skyTop = new Color(0.42f, 0.51f, 0.62f),
            skyHorizon = new Color(0.72f, 0.75f, 0.78f),
            skyGround = new Color(0.40f, 0.42f, 0.44f),
            ambientSky = new Color(0.58f, 0.63f, 0.70f),
            ambientEquator = new Color(0.50f, 0.52f, 0.55f),
            ambientGround = new Color(0.28f, 0.29f, 0.30f),
            sunColor = new Color(1.00f, 0.97f, 0.90f),
            sunIntensity = 1.15f,
            fogDensity = 0.009f,
            sunElevation = 52f,
        };

        private static readonly PhasePalette Dusk = new PhasePalette
        {
            skyTop = new Color(0.26f, 0.28f, 0.40f),
            skyHorizon = new Color(0.86f, 0.62f, 0.46f),
            skyGround = new Color(0.22f, 0.22f, 0.26f),
            ambientSky = new Color(0.42f, 0.40f, 0.50f),
            ambientEquator = new Color(0.52f, 0.42f, 0.38f),
            ambientGround = new Color(0.18f, 0.17f, 0.19f),
            sunColor = new Color(1.00f, 0.72f, 0.48f),
            sunIntensity = 0.70f,
            fogDensity = 0.012f,
            sunElevation = 10f,
        };

        [SerializeField] private float transitionGameMinutes = 60f;
        [SerializeField] private float farClipPlane = 140f;
        [SerializeField] private float postSaturation = -10f;
        [SerializeField] private float postContrast = 8f;
        [SerializeField] private float vignetteIntensity = 0.25f;

        private Light _sun;
        private Material _skybox;
        private GameBootstrap _boot;
        private int _lastMinute = -1;

        private PhasePalette _palette = Day;
        private ZoneAmbience _zone;
        private float _zoneWeight;
        private float _muteTarget;
        private ParticleSystem _ashfall;

        /// <summary>Extra mute for Quietday and similar authored states (0 = normal).</summary>
        public float Mute { get; set; }

        public void EnterZone(ZoneAmbience zone) => _zone = zone;
        public void ExitZone(ZoneAmbience zone) { if (_zone == zone) _zone = null; }

        public static AtmosphereRig Spawn()
        {
            var existing = FindFirstObjectByType<AtmosphereRig>();
            if (existing != null) return existing;

            var go = new GameObject("Atmosphere");
            DontDestroyOnLoad(go);
            return go.AddComponent<AtmosphereRig>();
        }

        private void Start()
        {
            _boot = GameBootstrap.Instance;

            var sunGo = GameObject.Find("Sun");
            _sun = sunGo != null ? sunGo.GetComponent<Light>() : FindFirstObjectByType<Light>();

            var source = Resources.Load<Material>("Art/SkyboxGradient");
            if (source != null)
            {
                _skybox = new Material(source); // never mutate the asset
                RenderSettings.skybox = _skybox;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Trilight;

            var camera = Camera.main;
            if (camera != null)
            {
                // Fog owns the distance now; reclaim the culling time.
                camera.farClipPlane = farClipPlane;

                var data = camera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
                SpawnPostVolume();
            }

            ApplyNow();
        }

        private void SpawnPostVolume()
        {
            var go = new GameObject("PostVolume");
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var color = profile.Add<ColorAdjustments>();
            color.saturation.Override(postSaturation);
            color.contrast.Override(postContrast);

            // Lifted, slightly cool shadows — the "wet stone" grade.
            var lift = profile.Add<LiftGammaGain>();
            lift.lift.Override(new Vector4(0.99f, 1.00f, 1.03f, 0.01f));

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(vignetteIntensity);

            volume.sharedProfile = profile;
        }

        private void Update()
        {
            var clock = _boot?.Game?.State.Clock;
            if (clock == null) return;

            // The sun (and its shadow re-render) steps per game-minute; colors and fog
            // are plain RenderSettings writes and lerp every frame for smooth zone and
            // Quietday transitions.
            if (clock.Minute != _lastMinute)
            {
                _lastMinute = clock.Minute;
                _palette = Blend(clock.Minute, clock.Settings);
                UpdateQuietday();
                ApplySun(clock);
            }

            float dt = Time.deltaTime;
            _zoneWeight = Mathf.MoveTowards(_zoneWeight, _zone != null ? 1f : 0f, dt * 0.8f);
            Mute = Mathf.MoveTowards(Mute, _muteTarget, dt * 0.25f);
            UpdateAshfall();
            ApplyFrame();
        }

        /// <summary>
        /// The valley feels wrong while the question is open: once the scorched bark is
        /// read and until the mystery resolves, the light goes flat and grey. This is the
        /// world bible's Quietday made visible without a single line of UI.
        /// </summary>
        private void UpdateQuietday()
        {
            var state = _boot?.Game?.State;
            if (state == null) return;
            bool open = state.GetBoolFlag("bark.read") && !state.GetBoolFlag("mystery.resolved");
            _muteTarget = open ? 0.6f : 0f;
        }

        private void UpdateAshfall()
        {
            bool wanted = Mute > 0.3f;
            if (wanted && _ashfall == null) _ashfall = SpawnAshfall();
            if (_ashfall == null) return;

            var emission = _ashfall.emission;
            emission.enabled = wanted;
        }

        /// <summary>Sparse ash drifting onto the eastern leaves — clue 1, visible.</summary>
        private ParticleSystem SpawnAshfall()
        {
            var go = new GameObject("Ashfall");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(12f, 14f, 80f);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 14f;
            main.startSpeed = 0.35f;
            main.startSize = 0.07f;
            main.startColor = new Color(0.72f, 0.70f, 0.68f, 0.8f);
            main.maxParticles = 200;
            main.gravityModifier = 0.015f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 1f, 34f);

            var emission = system.emission;
            emission.rateOverTime = 9f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null) renderer.material = new Material(shader);

            return system;
        }

        private void ApplyNow()
        {
            var clock = _boot?.Game?.State.Clock;
            if (clock == null) return;
            _lastMinute = clock.Minute;
            _palette = Blend(clock.Minute, clock.Settings);
            UpdateQuietday();
            ApplySun(clock);
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            PhasePalette palette = _palette;

            float mute = Mathf.Clamp01(Mute);
            if (mute > 0.001f)
            {
                Color grey = new Color(0.62f, 0.63f, 0.64f);
                palette.skyTop = Color.Lerp(palette.skyTop, grey * 0.9f, mute);
                palette.skyHorizon = Color.Lerp(palette.skyHorizon, grey, mute);
                palette.sunColor = Color.Lerp(palette.sunColor, grey, mute * 0.8f);
                palette.sunIntensity = Mathf.Lerp(palette.sunIntensity, 0.55f, mute);
                palette.fogDensity = Mathf.Lerp(palette.fogDensity, 0.02f, mute);

                if (_sun != null)
                {
                    _sun.color = palette.sunColor;
                    _sun.intensity = palette.sunIntensity;
                }
            }

            // Zone tint (upper shelf cool, lower shelf warm) rides on fog + ambient only.
            if (_zoneWeight > 0.001f && _zone != null)
            {
                float w = _zoneWeight;
                Color tint = Color.Lerp(Color.white, _zone.FogTint, w);
                palette.skyHorizon *= tint;
                palette.fogDensity *= Mathf.Lerp(1f, _zone.FogDensityMul, w);
                Color ambient = Color.Lerp(Color.white, _zone.AmbientTint, w);
                palette.ambientSky *= ambient;
                palette.ambientEquator *= ambient;
                palette.ambientGround *= ambient;
            }

            if (_skybox != null)
            {
                _skybox.SetColor("_Top", palette.skyTop);
                _skybox.SetColor("_Horizon", palette.skyHorizon);
                _skybox.SetColor("_Bottom", palette.skyGround);
            }

            RenderSettings.fogColor = palette.skyHorizon;
            RenderSettings.fogDensity = palette.fogDensity;
            RenderSettings.ambientSkyColor = palette.ambientSky;
            RenderSettings.ambientEquatorColor = palette.ambientEquator;
            RenderSettings.ambientGroundColor = palette.ambientGround;
        }

        private void ApplySun(Clock clock)
        {
            if (_sun == null) return;

            _sun.color = _palette.sunColor;
            _sun.intensity = _palette.sunIntensity;

            // Azimuth sweeps the day arc east→west; elevation comes from the palette.
            float t = clock.NormalisedTime;
            float azimuth = Mathf.Lerp(70f, 290f, t);
            _sun.transform.rotation = Quaternion.Euler(_palette.sunElevation, azimuth, 0f);
        }

        private static PhasePalette Blend(float minute, ClockSettings s)
        {
            // Ordered boundaries; each starts its palette, blending from the previous.
            (float at, PhasePalette pal)[] keys =
            {
                (s.DawnStart, Dawn), (s.DayStart, Day), (s.DuskStart, Dusk), (s.NightStart, Night)
            };

            PhasePalette from = Night, to = Night;
            float blend = 1f;
            const float window = 60f;

            foreach (var (at, pal) in keys)
            {
                if (minute < at) break;
                from = to;
                to = pal;
                blend = Mathf.Clamp01((minute - at) / window);
            }

            return Lerp(from, to, blend);
        }

        private static PhasePalette Lerp(PhasePalette a, PhasePalette b, float t)
        {
            return new PhasePalette
            {
                skyTop = Color.Lerp(a.skyTop, b.skyTop, t),
                skyHorizon = Color.Lerp(a.skyHorizon, b.skyHorizon, t),
                skyGround = Color.Lerp(a.skyGround, b.skyGround, t),
                ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t),
                ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t),
                ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t),
                sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
                sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                sunElevation = Mathf.Lerp(a.sunElevation, b.sunElevation, t),
            };
        }
    }
}

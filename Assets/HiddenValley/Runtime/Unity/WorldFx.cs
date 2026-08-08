using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>Scrolls the water texture along an authored flow direction. The channel
    /// visibly runs; the pool drifts. Cheap: one material offset write per frame.</summary>
    public sealed class WaterFlow : MonoBehaviour
    {
        private Material _material;
        private Vector2 _flow = new Vector2(0, -1);
        private float _speed = 0.03f;
        private Vector2 _offset;

        public static void Attach(GameObject host, Vector2 flow, float speed = 0.03f)
        {
            var renderer = host.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            var component = host.AddComponent<WaterFlow>();
            component._material = renderer.material; // instance, not shared — each slab scrolls alone
            component._flow = flow.sqrMagnitude > 0.001f ? flow.normalized : new Vector2(0, -1);
            component._speed = speed;
        }

        private void Update()
        {
            if (_material == null) return;
            _offset += _flow * (_speed * Time.deltaTime);
            _material.mainTextureOffset = _offset;
        }
    }

    /// <summary>Warm gutter for the ember light on the second kiln — Perlin flicker,
    /// never a strobe. "Someone is keeping it warm" should be feelable before stated.</summary>
    public sealed class FlickerLight : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private float _seed;

        public static void Attach(GameObject host, Color color, float intensity, float range)
        {
            var go = new GameObject("Ember");
            go.transform.SetParent(host.transform, false);
            go.transform.localPosition = Vector3.up * 0.8f;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;

            var flicker = go.AddComponent<FlickerLight>();
            flicker._light = light;
            flicker._baseIntensity = intensity;
            flicker._seed = Random.value * 100f;
        }

        private void Update()
        {
            if (_light == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * 1.7f);
            _light.intensity = _baseIntensity * (0.82f + 0.36f * n);
        }
    }

    /// <summary>
    /// A trigger volume that tints the atmosphere while the player is inside — the upper
    /// shelf goes cool and hushed, the lower shelf goes warm. Registered with the
    /// AtmosphereRig, which owns all RenderSettings writes.
    /// </summary>
    public sealed class ZoneAmbience : MonoBehaviour
    {
        public Color FogTint = Color.white;
        public float FogDensityMul = 1f;
        public Color AmbientTint = Color.white;

        public static void Spawn(string id, Vector3 center, Vector3 size,
            Color fogTint, float fogDensityMul, Color ambientTint)
        {
            var go = new GameObject(id) { layer = 2 };
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            var zone = go.AddComponent<ZoneAmbience>();
            zone.FogTint = fogTint;
            zone.FogDensityMul = fogDensityMul;
            zone.AmbientTint = ambientTint;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var rig = FindFirstObjectByType<AtmosphereRig>();
            if (rig != null) rig.EnterZone(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var rig = FindFirstObjectByType<AtmosphereRig>();
            if (rig != null) rig.ExitZone(this);
        }
    }
}

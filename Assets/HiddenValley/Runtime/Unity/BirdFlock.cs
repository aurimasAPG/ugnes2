using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Swifts over the village: a handful of tiny dark wedges on layered elliptical
    /// orbits with wing-flick bobbing. The sky is the largest empty surface in every
    /// screenshot, and distant motion is the cheapest life a world can have.
    ///
    /// On Quietdays the birds are gone — the world bible's "the wind stops, the birds
    /// stop" — so their absence is information, exactly like the still grass. They fade
    /// with the AtmosphereRig's Mute rather than popping out of existence.
    /// </summary>
    public sealed class BirdFlock : MonoBehaviour
    {
        private const int Count = 7;

        private Transform[] _birds;
        private float[] _phase, _radius, _height, _speed;
        private Vector3 _center = new Vector3(0, 16f, 0);
        private AtmosphereRig _rig;

        public static void Spawn()
        {
            var go = new GameObject("BirdFlock");
            go.AddComponent<BirdFlock>();
        }

        private void Start()
        {
            _rig = FindFirstObjectByType<AtmosphereRig>();
            _birds = new Transform[Count];
            _phase = new float[Count];
            _radius = new float[Count];
            _height = new float[Count];
            _speed = new float[Count];

            var random = new System.Random(23);
            var material = RuntimeArt.MaterialFor("slate");

            for (int i = 0; i < Count; i++)
            {
                var bird = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bird.name = "swift";
                Destroy(bird.GetComponent<Collider>());
                bird.transform.SetParent(transform, false);
                bird.transform.localScale = new Vector3(0.55f, 0.045f, 0.16f); // a wing line
                var renderer = bird.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                _birds[i] = bird.transform;
                _phase[i] = (float)random.NextDouble() * Mathf.PI * 2f;
                _radius[i] = 14f + (float)random.NextDouble() * 18f;
                _height[i] = 13f + (float)random.NextDouble() * 8f;
                _speed[i] = 0.25f + (float)random.NextDouble() * 0.2f;
            }
        }

        private void Update()
        {
            float mute = _rig != null ? Mathf.Clamp01(_rig.Mute / 0.6f) : 0f;
            bool visible = mute < 0.65f;
            float time = Time.time;

            for (int i = 0; i < Count; i++)
            {
                if (_birds[i] == null) continue;
                if (_birds[i].gameObject.activeSelf != visible)
                    _birds[i].gameObject.SetActive(visible);
                if (!visible) continue;

                float a = _phase[i] + time * _speed[i];
                // Elliptical orbit + wing-flick bob + banking into the turn.
                Vector3 position = _center + new Vector3(
                    Mathf.Cos(a) * _radius[i],
                    _height[i] + Mathf.Sin(time * 2.3f + _phase[i] * 3f) * 0.6f,
                    Mathf.Sin(a) * _radius[i] * 0.7f);

                _birds[i].position = position;
                _birds[i].rotation = Quaternion.LookRotation(
                    new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a) * 0.7f), Vector3.up)
                    * Quaternion.Euler(0, 0, 18f * Mathf.Sin(a));
            }
        }
    }
}

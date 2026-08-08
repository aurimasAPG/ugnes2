using System.Collections.Generic;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Ambient motion at rest: the grass tufts sway. Transform-based rather than a vertex
    /// shader — measurable, simple, and honest about cost: updates are staggered across
    /// three frames, so ~500 tufts cost ~170 rotation writes per frame.
    ///
    /// On Quietdays the wind stops (world bible: "the wind stops, the birds stop") —
    /// sway amplitude follows the AtmosphereRig's Mute toward zero, which makes the
    /// stillness of a wrong morning something the player can see in the grass.
    /// </summary>
    public sealed class WindSway : MonoBehaviour
    {
        [SerializeField] private float amplitudeDegrees = 4f;
        [SerializeField] private float frequency = 1.25f;

        private struct Blade
        {
            public Transform transform;
            public Quaternion baseRotation;
            public float phase;
        }

        private readonly List<Blade> _blades = new List<Blade>();
        private AtmosphereRig _rig;
        private int _cursor;

        private void Start()
        {
            foreach (Transform child in transform)
            {
                if (child.name != "tuft") continue;
                _blades.Add(new Blade
                {
                    transform = child,
                    baseRotation = child.rotation,
                    phase = (child.position.x + child.position.z) * 0.7f,
                });
            }
            _rig = FindFirstObjectByType<AtmosphereRig>();
        }

        private void Update()
        {
            if (_blades.Count == 0) return;

            float still = _rig != null ? Mathf.Clamp01(_rig.Mute / 0.6f) : 0f;
            float amplitude = amplitudeDegrees * (1f - still);
            float time = Time.time * frequency * Mathf.PI * 2f;

            // A fifth of the field per frame — sway at 12 Hz per blade still reads as
            // continuous motion and trims the write cost (pass 1 found the Mac 1%-high
            // creeping toward budget).
            int slice = Mathf.Max(1, _blades.Count / 5);
            for (int n = 0; n < slice; n++)
            {
                _cursor = (_cursor + 1) % _blades.Count;
                var blade = _blades[_cursor];
                float lean = Mathf.Sin(time + blade.phase) * amplitude;
                blade.transform.rotation = blade.baseRotation * Quaternion.Euler(lean, 0, lean * 0.6f);
            }
        }
    }
}

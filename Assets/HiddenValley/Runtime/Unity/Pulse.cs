using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// A one-shot squash pulse on a transform — the world answering the player's tap.
    /// Sound and haptics already respond; this is the missing visual third. Attaches,
    /// plays ~0.2 s, restores the exact original scale, self-destructs.
    /// </summary>
    public sealed class Pulse : MonoBehaviour
    {
        private Vector3 _baseScale;
        private float _startedAt;
        private const float Duration = 0.2f;
        private const float Amount = 0.12f;

        public static void At(Transform target)
        {
            if (target == null) return;
            if (target.GetComponent<Pulse>() != null) return; // one at a time
            var pulse = target.gameObject.AddComponent<Pulse>();
            pulse._baseScale = target.localScale;
            pulse._startedAt = Time.unscaledTime;
        }

        private void Update()
        {
            float t = (Time.unscaledTime - _startedAt) / Duration;
            if (t >= 1f)
            {
                transform.localScale = _baseScale;
                Destroy(this);
                return;
            }

            // Out fast, back with a slight overshoot — reads as touched, not resized.
            float wave = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.4f);
            transform.localScale = _baseScale * (1f + Amount * wave);
        }
    }
}

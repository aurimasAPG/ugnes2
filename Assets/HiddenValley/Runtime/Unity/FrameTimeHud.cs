using UnityEngine;
using UnityEngine.Profiling;

namespace HiddenValley.Unity
{
    /// <summary>
    /// The on-screen frame-time readout required by the Phase 0 done-state, and the thing
    /// the device pass reads its numbers off.
    ///
    /// It reports the <em>worst</em> frame time, not the average, because the device pass is
    /// explicitly defined that way: an average hides exactly the hitches that make a game
    /// feel bad, and a 60fps average with a 90ms spike every few seconds is a game nobody
    /// wants to hold. Worst-in-window and 1%-high are both shown, since a single stray frame
    /// during a scene load should not condemn a scene forever.
    ///
    /// Press and hold three fingers (or F1 in the editor) to reset the window.
    /// </summary>
    public sealed class FrameTimeHud : MonoBehaviour
    {
        // Dev builds show the readout; release builds hide it behind the 3-finger
        // gesture so testers never see debug text but the device pass can always
        // summon it.
        [SerializeField] private bool visible = true;

        private void Awake()
        {
            if (!UnityEngine.Debug.isDebugBuild) visible = false;
        }
        [SerializeField] private float windowSeconds = 10f;
        [SerializeField] private int targetFps = 60;

        private const int Capacity = 1024;
        private readonly float[] _samples = new float[Capacity];
        private int _count;
        private int _next;

        private float _worst;
        private float _windowElapsed;
        private bool _gestureHeld;
        private float _nextTelemetryAt = 20f; // let load hitches wash out first
        private float _sessionWorstAfterWarmup;

        public float WorstMs => _worst * 1000f;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            _samples[_next] = dt;
            _next = (_next + 1) % Capacity;
            if (_count < Capacity) _count++;

            if (dt > _worst) _worst = dt;

            _windowElapsed += dt;
            if (_windowElapsed >= windowSeconds) ResetWindow();

            bool gesture = Input.touchCount >= 3;
#if UNITY_EDITOR
            gesture |= Input.GetKeyDown(KeyCode.F1);
#endif
            if (gesture && !_gestureHeld)
            {
                visible = !visible; // 3-finger tap toggles; the reset rides along
                ResetWindow();
            }
            _gestureHeld = gesture;

            // Telemetry: the device pass's numbers, written down by the device itself.
            // Appends a line every 15 s after a 20 s warmup (load hitches excluded), so
            // an unattended run on the phone still produces real measurements that can
            // be pulled off via devicectl and recorded in the phase log.
            if (Time.unscaledTime > 20f && dt > _sessionWorstAfterWarmup)
                _sessionWorstAfterWarmup = dt;

            if (Time.unscaledTime >= _nextTelemetryAt)
            {
                _nextTelemetryAt = Time.unscaledTime + 15f;
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Application.persistentDataPath, "frame-telemetry.txt"),
                        $"{System.DateTime.UtcNow:HH:mm:ss} t={Time.unscaledTime:0}s " +
                        $"now={Time.unscaledDeltaTime * 1000f:0.0}ms worst={WorstMs:0.0}ms " +
                        $"p99={OnePercentHighMs():0.0}ms sessionWorst={_sessionWorstAfterWarmup * 1000f:0.0}ms " +
                        $"mem={Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)}MB\n");
                }
                catch { /* telemetry must never take the game down */ }
            }
        }

        private void ResetWindow()
        {
            _worst = 0f;
            _windowElapsed = 0f;
            _count = 0;
            _next = 0;
        }

        /// <summary>The 99th-percentile frame time — the hitch you feel, minus the one-off outlier.</summary>
        private float OnePercentHighMs()
        {
            if (_count == 0) return 0f;

            var copy = new float[_count];
            System.Array.Copy(_samples, copy, _count);
            System.Array.Sort(copy);

            int index = Mathf.Clamp(Mathf.FloorToInt(_count * 0.99f), 0, _count - 1);
            return copy[index] * 1000f;
        }

        private void OnGUI()
        {
            if (!visible) return;

            float budgetMs = 1000f / targetFps;
            float currentMs = Time.unscaledDeltaTime * 1000f;
            float worstMs = WorstMs;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.022f),
                alignment = TextAnchor.UpperLeft
            };

            style.normal.textColor = worstMs > budgetMs ? Color.red : Color.green;

            float memMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

            var text =
                $"now  {currentMs:00.0} ms\n" +
                $"worst {worstMs:00.0} ms   (budget {budgetMs:00.0})\n" +
                $"1% hi {OnePercentHighMs():00.0} ms\n" +
                $"mem  {memMb:0} MB";

            var rect = new Rect(Screen.width * 0.02f, Screen.height * 0.02f,
                                Screen.width * 0.5f, Screen.height * 0.3f);

            GUI.Label(rect, text, style);
        }
    }
}

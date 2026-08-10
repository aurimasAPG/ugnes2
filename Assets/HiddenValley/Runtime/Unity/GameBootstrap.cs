using System;
using System.IO;
using HiddenValley.Core;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Loads content, builds the Game, and drives its clock. Everything else in the Unity
    /// layer is a view onto this.
    ///
    /// Change notification is a revision counter rather than a web of events: views poll
    /// <see cref="GameState.Revision"/> once per frame and refresh when it moves. With a
    /// village's worth of objects that is a single integer compare per frame, and it cannot
    /// leak subscriptions across a scene load — which is where event-based state mirroring
    /// usually starts costing frames on mobile.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [SerializeField] private string contentFolder = "Content";
        [SerializeField] private bool validateOnLoad = true;
        [SerializeField] private bool advanceClock = true;

        public Game Game { get; private set; }

        /// <summary>Raised once after content is loaded and the Game exists.</summary>
        public event Action Ready;

        /// <summary>Raised on any frame in which game state changed.</summary>
        public event Action Changed;

        /// <summary>Raised once per presentation cue emitted by content (see CueEffect).</summary>
        public event Action<string> Cue;

        private int _lastRevision = -1;
        private bool _restored;

        public string ContentPath => Path.Combine(Application.streamingAssetsPath, contentFolder);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Unity's iOS default is a 30fps cap; the mission's first quality dimension
            // is 60 on device. Found by the on-device telemetry: a perfectly steady
            // 33.3 ms is a cap, not a struggle.
            Application.targetFrameRate = 60;

            try
            {
                Debug.Log($"[HiddenValley] Loading content from {ContentPath}");
                var content = ContentDatabase.LoadFromDirectory(ContentPath);

                if (validateOnLoad)
                {
                    var report = ContentValidator.Validate(content);
                    if (!report.Ok) Debug.LogError(report.ToString());
                    else if (report.Warnings.Count > 0) Debug.LogWarning(report.ToString());
                }

                Game = Game.NewGame(content);
                Debug.Log(
                    $"[HiddenValley] Content ready: " +
                    $"{content.Npcs.Count} npcs, {content.Quests.Count} quests, " +
                    $"{content.WorldObjects.Count} world objects.");

                // A save that exists is a session in progress. Restore it silently before
                // anyone hears Ready — the write half was always wired (pause/quit); the
                // read half was forgotten until 2026-08-08, so every launch was New Game.
                if (File.Exists(SavePath))
                {
                    try
                    {
                        Game = Game.FromSave(File.ReadAllText(SavePath), content);
                        _restored = true;
                        Debug.Log("[HiddenValley] Save restored from slot0.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[HiddenValley] Save unreadable, starting fresh: {e.Message}");
                    }
                }
            }
            catch (ContentException e)
            {
                // Content is authored data, so this is a content bug, not a crash. Say which.
                Debug.LogError($"[HiddenValley] Content failed to load: {e.Message}");
                enabled = false;
                return;
            }

            _lastRevision = Game.State.Revision;

            // Dev hook: "-hvminute 1200" starts the clock at a chosen minute — exists so
            // dusk and night can be screenshot-verified without playing to them.
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-hvminute" && int.TryParse(args[i + 1], out var minute))
                    Game.State.SetClock(new Clock(Game.Content.Settings, 0, minute));

            Ready?.Invoke();
        }

        private void Start()
        {
            // The atmosphere is code-spawned like every other runtime addition — the
            // scene shell stays minimal (level0 landmine).
            AtmosphereRig.Spawn();
            BirdFlock.Spawn();

            StartCoroutine(ScreenshotHook());

            // Reposition the player where they were saved. Start, not Awake: every scene
            // object exists by now, and the CharacterController must be toggled around a
            // teleport or it snaps the transform back.
            if (!_restored) return;

            var saved = Game.State.GetFlag("player.pos");
            var player = GameObject.FindWithTag("Player");
            if (string.IsNullOrEmpty(saved) || player == null) return;

            var parts = saved.Split(',');
            if (parts.Length != 3
                || !float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture, out var x)
                || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture, out var y)
                || !float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture, out var z)) return;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = new Vector3(x, y, z);
            if (cc != null) cc.enabled = true;
        }

        /// <summary>
        /// Dev hook: "-hvshot &lt;path&gt; [seconds]" photographs the running game from
        /// inside it and quits. Exists because the alternative — driving macOS
        /// `screencapture` at the player window — depends on window focus and on
        /// accessibility permission to send keystrokes, neither of which a headless
        /// verification run can rely on. A pass that cannot be photographed cannot be
        /// scored, so the game takes its own picture.
        /// </summary>
        private System.Collections.IEnumerator ScreenshotHook()
        {
            string path = null;
            float delay = 6f;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-hvshot")
                {
                    path = args[i + 1];
                    if (i + 2 < args.Length
                        && float.TryParse(args[i + 2], System.Globalization.NumberStyles.Float,
                                          System.Globalization.CultureInfo.InvariantCulture, out var s))
                        delay = s;
                }

            if (path == null) yield break;

            // Without this the player freezes the moment it loses focus — Update stops,
            // the coroutine never resumes, and the capture silently never happens. The
            // verification run is exactly the case where the window is behind a terminal.
            Application.runInBackground = true;

            // Realtime: the pause panel sets timeScale to 0, and a scaled wait there
            // would never return.
            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitForEndOfFrame();

            // Written by hand as an uncompressed TGA rather than via
            // ScreenCapture/ImageConversion: this project trims the built-in modules
            // (see Packages/manifest.json), and a verification hook is not worth putting
            // two modules back into every shipped build. Convert on the host with
            // `sips -s format png shot.tga --out shot.png`.
            var shot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            shot.Apply();

            try
            {
                File.WriteAllBytes(path, EncodeTga(shot));
                Debug.Log($"[HiddenValley] Screenshot written: {path} ({Screen.width}x{Screen.height}).");
            }
            catch (Exception e)
            {
                Debug.LogError($"[HiddenValley] Screenshot failed: {e.Message}");
            }
            Destroy(shot);

            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        /// <summary>Uncompressed 24-bit bottom-up TGA — the simplest format that needs no
        /// encoder module. ReadPixels already hands us bottom-up BGR-order-friendly rows.</summary>
        private static byte[] EncodeTga(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            var pixels = tex.GetPixels32();
            var bytes = new byte[18 + w * h * 3];

            bytes[2] = 2;                        // uncompressed true-colour
            bytes[12] = (byte)(w & 0xFF);
            bytes[13] = (byte)((w >> 8) & 0xFF);
            bytes[14] = (byte)(h & 0xFF);
            bytes[15] = (byte)((h >> 8) & 0xFF);
            bytes[16] = 24;                      // bits per pixel

            for (int i = 0; i < pixels.Length; i++)
            {
                int o = 18 + i * 3;
                bytes[o] = pixels[i].b;          // TGA is BGR
                bytes[o + 1] = pixels[i].g;
                bytes[o + 2] = pixels[i].r;
            }
            return bytes;
        }

        private void Update()
        {
            if (Game == null) return;

            if (advanceClock) Game.Tick(Time.deltaTime);

            if (Game.State.Revision == _lastRevision) return;

            _lastRevision = Game.State.Revision;
            Changed?.Invoke();
            DrainCues();
        }

        private void DrainCues()
        {
            var cues = Game.State.PendingCues;
            if (cues.Count == 0) return;

            // Handlers can emit state changes of their own; drain from a copy-by-index so
            // a re-entrant cue lands in the next frame instead of mutating this loop.
            for (int i = 0; i < cues.Count; i++) Cue?.Invoke(cues[i]);
            cues.Clear();
        }

        // ---- save / load -------------------------------------------------------

        public string SavePath => Path.Combine(Application.persistentDataPath, "slot0.json");

        public void SaveToDisk()
        {
            if (Game == null) return;

            // Player position rides in a flag so the save format stays pure Core state.
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var p = player.transform.position;
                Game.State.SetFlag("player.pos", string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:0.##},{1:0.##},{2:0.##}", p.x, p.y, p.z));
            }

            // Write beside the target and move into place, so a kill mid-write cannot leave
            // a truncated save. Testers close the app at the worst possible moment.
            var temp = SavePath + ".tmp";
            File.WriteAllText(temp, Game.Save());

            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(temp, SavePath);
        }

        public bool LoadFromDisk()
        {
            if (Game == null || !File.Exists(SavePath)) return false;

            try
            {
                Game = Game.FromSave(File.ReadAllText(SavePath), Game.Content);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HiddenValley] Save failed to load, starting fresh: {e.Message}");
                return false;
            }

            _lastRevision = -1;
            Ready?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveToDisk();
        }

        private void OnApplicationQuit() => SaveToDisk();
    }
}

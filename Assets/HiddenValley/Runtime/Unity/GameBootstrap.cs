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
            Ready?.Invoke();
        }

        private void Start()
        {
            // The atmosphere is code-spawned like every other runtime addition — the
            // scene shell stays minimal (level0 landmine).
            AtmosphereRig.Spawn();

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

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

        private int _lastRevision = -1;

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
                var content = ContentDatabase.LoadFromDirectory(ContentPath);

                if (validateOnLoad)
                {
                    var report = ContentValidator.Validate(content);
                    if (!report.Ok) Debug.LogError(report.ToString());
                    else if (report.Warnings.Count > 0) Debug.LogWarning(report.ToString());
                }

                Game = Game.NewGame(content);
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

        private void Update()
        {
            if (Game == null) return;

            if (advanceClock) Game.Tick(Time.deltaTime);

            if (Game.State.Revision == _lastRevision) return;

            _lastRevision = Game.State.Revision;
            Changed?.Invoke();
        }

        // ---- save / load -------------------------------------------------------

        public string SavePath => Path.Combine(Application.persistentDataPath, "slot0.json");

        public void SaveToDisk()
        {
            if (Game == null) return;

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

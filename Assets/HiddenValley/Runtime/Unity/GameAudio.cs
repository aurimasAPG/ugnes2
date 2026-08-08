using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HiddenValley.Core;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// SFX + dialogue VO bus. Short cues live under <c>Resources/Audio</c>; full ElevenLabs
    /// line reads live under <c>Resources/Audio/Voice</c> and are resolved via
    /// <c>StreamingAssets/Audio/voice_map.json</c> (speaker|line → clip id).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [SerializeField] private float masterVolume = 0.85f;
        [SerializeField] private float voiceVolume = 1f;
        [SerializeField] private float ambienceVolume = 0.28f;
        [SerializeField] private float musicVolume = 0.4f;
        [SerializeField] private float footstepInterval = 0.38f;

        private AudioSource _oneshot;
        private AudioSource _feet;
        private AudioSource _voice;
        private AudioSource _ambience;
        private AudioSource _music;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private Dictionary<string, string> _voiceMap = new Dictionary<string, string>();
        private float _nextFootstep;
        private PlayerController _player;
        private AtmosphereRig _atmosphere;
        private int _lastInventoryCount = -1;
        private int _lastCompletedQuests = -1;

        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("GameAudio");
            go.AddComponent<GameAudio>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _oneshot = gameObject.AddComponent<AudioSource>();
            _oneshot.playOnAwake = false;
            _oneshot.spatialBlend = 0f;
            _oneshot.volume = masterVolume;

            _feet = gameObject.AddComponent<AudioSource>();
            _feet.playOnAwake = false;
            _feet.spatialBlend = 0f;
            _feet.volume = masterVolume * 0.55f;

            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            _voice.volume = masterVolume * voiceVolume;

            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.playOnAwake = false;
            _ambience.spatialBlend = 0f;
            _ambience.loop = true;
            _ambience.volume = masterVolume * ambienceVolume;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.loop = false;
            _music.volume = masterVolume * musicVolume;

            LoadVoiceMap();
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            var boot = GameBootstrap.Instance;
            if (boot != null)
            {
                boot.Changed += OnGameChanged;
                boot.Cue += OnCue;
                if (boot.Game != null)
                {
                    _lastInventoryCount = CountInventory(boot.Game);
                    _lastCompletedQuests = CountCompletedQuests(boot.Game);
                }
            }

            PrewarmShortClips();
            StartAmbience();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var boot = GameBootstrap.Instance;
            if (boot != null)
            {
                boot.Changed -= OnGameChanged;
                boot.Cue -= OnCue;
            }
        }

        /// <summary>
        /// Content-authored presentation cues (see Core CueEffect). Convention:
        /// "sting.x" → music sting "mus_sting_x", "sfx.x" → one-shot "sfx_x".
        /// </summary>
        private void OnCue(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return;
            if (cue.StartsWith("sting.")) PlayMusicSting("mus_sting_" + cue.Substring(6));
            else if (cue.StartsWith("sfx.")) Play("sfx_" + cue.Substring(4));
        }

        /// <summary>
        /// The short cue set is a few seconds of audio total; loading it at boot removes
        /// the first-play hitch from the exact moments feedback matters most.
        /// </summary>
        private void PrewarmShortClips()
        {
            string[] names =
            {
                "sfx_footstep", "sfx_pickup", "sfx_interact", "sfx_jump",
                "sfx_ui_open", "sfx_ui_close", "sfx_ui_click", "sfx_dialogue_advance",
                "sfx_dialogue_default", "sfx_dialogue_vesk", "sfx_dialogue_coll",
                "sfx_dialogue_orrel", "sfx_dialogue_pip",
                "mus_sting_quest", "mus_sting_mystery"
            };
            foreach (var n in names) Load(n);
        }

        private readonly HashSet<string> _prewarmedSpeakers = new HashSet<string>();

        /// <summary>
        /// Async-loads every VO clip mapped to a speaker, called when their talk prompt
        /// first appears — by the time the player taps, the lines are in memory instead
        /// of decoding on the main thread mid-dialogue.
        /// </summary>
        public void PrewarmSpeaker(string speaker)
        {
            if (string.IsNullOrEmpty(speaker) || !_prewarmedSpeakers.Add(speaker)) return;

            string prefix = speaker + "|";
            foreach (var pair in _voiceMap)
            {
                if (!pair.Key.StartsWith(prefix)) continue;
                string clipId = pair.Value;
                if (_clips.ContainsKey(clipId)) continue;

                var request = Resources.LoadAsync<AudioClip>("Audio/Voice/" + clipId);
                request.completed += _ =>
                {
                    if (!_clips.ContainsKey(clipId))
                        _clips[clipId] = request.asset as AudioClip;
                };
            }
        }

        private bool _gaitHooked;

        private void Update()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;

            // Prefer footsteps clocked by the visible gait (CapsuleAnimator) — sound and
            // motion sharing one clock is what "feels solid" is made of. The timer path
            // stays as fallback for a player without an animator.
            if (!_gaitHooked)
            {
                var animator = _player.GetComponent<CapsuleAnimator>();
                if (animator != null)
                {
                    animator.Footstep += OnFootstep;
                    _gaitHooked = true;
                }
            }
            if (_gaitHooked) return;

            if (!_player.IsMovingOnGround) return;
            if (Time.time < _nextFootstep) return;

            OnFootstep();
            float pace = Mathf.Lerp(footstepInterval * 1.15f, footstepInterval * 0.72f,
                Mathf.Clamp01(_player.NormalizedSpeed));
            _nextFootstep = Time.time + pace;
        }

        private void OnFootstep()
            => Play("sfx_footstep", _feet, 0.7f + Random.Range(-0.05f, 0.05f));

        private void OnGameChanged()
        {
            var boot = GameBootstrap.Instance;
            if (boot?.Game == null) return;
            int count = CountInventory(boot.Game);
            if (_lastInventoryCount >= 0 && count > _lastInventoryCount)
                Play("sfx_pickup");
            _lastInventoryCount = count;

            int done = CountCompletedQuests(boot.Game);
            if (_lastCompletedQuests >= 0 && done > _lastCompletedQuests)
                PlayMusicSting("mus_sting_quest");
            _lastCompletedQuests = done;
        }

        private static int CountInventory(Game game)
        {
            int n = 0;
            foreach (var pair in game.State.Inventory) n += pair.Value;
            return n;
        }

        private static int CountCompletedQuests(Game game)
        {
            int n = 0;
            foreach (var q in game.State.Quests.Values)
                if (q.State == QuestState.Completed) n++;
            return n;
        }

        private AudioSource _water;

        public void StartAmbience()
        {
            var clip = Load("amb_valley");
            if (clip == null || _ambience == null) return;
            if (_ambience.isPlaying && _ambience.clip == clip) return;
            _ambience.clip = clip;
            _ambience.volume = masterVolume * ambienceVolume;
            _ambience.Play();

            // Water bed: a second loop whose volume follows distance to the channel
            // spine (x = -2, z -28..26). The river gets louder as you approach — the
            // cheapest spatial audio there is.
            var water = Load("sfx_water");
            if (water != null && _water == null)
            {
                _water = gameObject.AddComponent<AudioSource>();
                _water.clip = water;
                _water.loop = true;
                _water.playOnAwake = false;
                _water.spatialBlend = 0f;
                _water.volume = 0f;
                _water.Play();
            }

            Debug.Log("[HiddenValley] Ambience started: amb_valley");
        }

        /// <summary>Day-phase ambience: full by day, hushed at night with the synthesized
        /// cricket bed crossfaded in; water bed by proximity to the channel; occasional
        /// wind gusts that respect Quietday stillness. Lerps only, no allocation.</summary>
        private AudioSource _night;
        private float _nextGustAt = 30f;

        private void LateUpdateAmbience()
        {
            var boot = GameBootstrap.Instance;
            if (boot?.Game == null || _ambience == null) return;

            string phase = boot.Game.State.Clock.PhaseName;
            float phaseScale = phase switch
            {
                "night" => 0.35f,
                "dusk" => 0.8f,
                "dawn" => 0.9f,
                _ => 1f
            };
            float target = masterVolume * ambienceVolume * phaseScale;
            // Only ease when not ducked by VO (the duck path owns the volume then).
            if (_voice == null || !_voice.isPlaying)
                _ambience.volume = Mathf.MoveTowards(_ambience.volume, target, Time.deltaTime * 0.15f);

            // Night bed: crickets fade up as the valley loop fades down.
            if (_night == null)
            {
                var clip = Load("amb_night");
                if (clip != null)
                {
                    _night = gameObject.AddComponent<AudioSource>();
                    _night.clip = clip;
                    _night.loop = true;
                    _night.playOnAwake = false;
                    _night.volume = 0f;
                    _night.Play();
                }
            }
            if (_night != null)
            {
                bool dark = phase == "night" || phase == "dusk";
                float nightTarget = dark ? masterVolume * ambienceVolume * (phase == "night" ? 0.9f : 0.3f) : 0f;
                _night.volume = Mathf.MoveTowards(_night.volume, nightTarget, Time.deltaTime * 0.1f);
            }

            // Gusts: sparse, daytime-weighted, and silenced entirely while the world is
            // muted — Quietday stillness is also an audio statement.
            var rig = _atmosphere != null ? _atmosphere : (_atmosphere = FindFirstObjectByType<AtmosphereRig>());
            bool still = rig != null && rig.Mute > 0.2f;
            if (!still && Time.time >= _nextGustAt && phase != "night")
            {
                _nextGustAt = Time.time + Random.Range(18f, 45f);
                Play("sfx_gust", 0.5f);
            }

            if (_water != null && _player != null)
            {
                Vector3 p = _player.transform.position;
                float dz = Mathf.Max(0f, Mathf.Max(-28f - p.z, p.z - 26f));
                float dx = Mathf.Abs(p.x + 2f);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float proximity = Mathf.Clamp01(1f - distance / 14f);
                _water.volume = Mathf.MoveTowards(
                    _water.volume, masterVolume * ambienceVolume * 0.9f * proximity * proximity,
                    Time.deltaTime * 0.3f);
            }
        }

        public void PlayMusicSting(string name)
        {
            var clip = Load(name);
            if (clip == null || _music == null) return;
            _music.Stop();
            _music.PlayOneShot(clip, Mathf.Clamp01(masterVolume * musicVolume));
        }

        public void PlayMysterySting() => PlayMusicSting("mus_sting_mystery");

        public void Play(string clipName, float volumeScale = 1f)
            => Play(clipName, _oneshot, volumeScale);

        public void Play(string clipName, AudioSource source, float volumeScale)
        {
            if (source == null || string.IsNullOrEmpty(clipName)) return;
            var clip = Load(clipName);
            if (clip == null) return;
            source.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
        }

        /// <summary>
        /// Plays the ElevenLabs line VO when mapped; otherwise falls back to a short
        /// speaker-identifying cue so dialogue is never silent.
        /// </summary>
        public void PlayDialogueLine(string speaker, string lineText)
        {
            if (_voice != null && _voice.isPlaying) _voice.Stop();

            string clipId = ResolveVoiceClipId(speaker, lineText);
            if (!string.IsNullOrEmpty(clipId))
            {
                var vo = LoadVoice(clipId);
                if (vo != null)
                {
                    // Duck ambience only when real VO plays — the short speaker cue on the
                    // fallback path used to duck and instantly restore, an audible flap.
                    if (_ambience != null)
                        _ambience.volume = masterVolume * ambienceVolume * 0.45f;

                    _voice.clip = vo;
                    _voice.volume = Mathf.Clamp01(masterVolume * voiceVolume);
                    _voice.Play();
                    return;
                }
            }

            Play(DialogueClipForSpeaker(speaker));
        }

        public void StopDialogueVoice()
        {
            if (_voice != null && _voice.isPlaying) _voice.Stop();
            if (_ambience != null)
                _ambience.volume = masterVolume * ambienceVolume;
        }

        private void LateUpdate()
        {
            // Restore ambience after VO ends.
            if (_voice != null && !_voice.isPlaying && _ambience != null && _ambience.isPlaying)
            {
                float target = masterVolume * ambienceVolume;
                if (_ambience.volume < target - 0.01f)
                    _ambience.volume = Mathf.MoveTowards(_ambience.volume, target, Time.deltaTime * 0.5f);
            }

            LateUpdateAmbience();
        }

        public void PlayDialogueForSpeaker(string speaker)
            => Play(DialogueClipForSpeaker(speaker));

        /// <summary>Maps a dialogue speaker name to a short cue Resources key.</summary>
        public static string DialogueClipForSpeaker(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return "sfx_dialogue_default";
            var s = speaker.Trim().ToLowerInvariant();
            if (s.Contains("vesk")) return "sfx_dialogue_vesk";
            if (s.Contains("coll")) return "sfx_dialogue_coll";
            if (s.Contains("orrel")) return "sfx_dialogue_orrel";
            if (s.Contains("pip")) return "sfx_dialogue_pip";
            return "sfx_dialogue_default";
        }

        /// <summary>
        /// Pure helper: build the map key used in voice_map.json. Prefer exact raw line;
        /// also try cleaned quoted speech (matches TTS generation).
        /// </summary>
        public static string VoiceMapKey(string speaker, string lineText)
            => (speaker ?? "") + "|" + (lineText ?? "");

        public static string CleanLineForVoice(string lineText)
        {
            if (string.IsNullOrEmpty(lineText)) return "";
            var matches = Regex.Matches(lineText, "\"([^\"]+)\"");
            if (matches.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (Match m in matches)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(m.Groups[1].Value);
                }
                return sb.ToString();
            }
            return lineText.Trim();
        }

        public void UiOpen() => Play("sfx_ui_open");
        public void UiClose() => Play("sfx_ui_close");
        public void UiClick() => Play("sfx_ui_click");
        public void Interact() => Play("sfx_interact");
        public void Jump() => Play("sfx_jump");
        public void DialogueAdvance() => Play("sfx_dialogue_advance", 0.7f);

        private string ResolveVoiceClipId(string speaker, string lineText)
        {
            if (_voiceMap == null || _voiceMap.Count == 0) return null;
            string rawKey = VoiceMapKey(speaker, lineText);
            if (_voiceMap.TryGetValue(rawKey, out var id)) return id;
            string cleaned = CleanLineForVoice(lineText);
            string cleanKey = VoiceMapKey(speaker, cleaned);
            if (_voiceMap.TryGetValue(cleanKey, out id)) return id;
            return null;
        }

        private void LoadVoiceMap()
        {
            _voiceMap = new Dictionary<string, string>();
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Audio", "voice_map.json");
#if UNITY_ANDROID && !UNITY_EDITOR
                // StreamingAssets on Android is inside jar — not used for this Mac/iOS slice.
#endif
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[HiddenValley] voice_map.json missing at " + path);
                    return;
                }
                string json = File.ReadAllText(path);
                // Simple JSON object parse without depending on Unity JsonUtility dictionary gaps.
                // Format: { "speaker|line": "vo_id", ... }
                var root = Newtonsoft.Json.Linq.JObject.Parse(json);
                foreach (var prop in root.Properties())
                    _voiceMap[prop.Name] = prop.Value.ToString();
                Debug.Log($"[HiddenValley] Voice map loaded: {_voiceMap.Count} entries.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[HiddenValley] Voice map failed: " + e.Message);
            }
        }

        private AudioClip Load(string name)
        {
            if (_clips.TryGetValue(name, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            _clips[name] = clip;
            if (clip == null)
                Debug.LogWarning($"[HiddenValley] Missing audio Resources/Audio/{name}");
            return clip;
        }

        private AudioClip LoadVoice(string clipId)
        {
            if (_clips.TryGetValue(clipId, out var cached)) return cached;
            // Resources path: Audio/Voice/vo_vesk_xxx
            var clip = Resources.Load<AudioClip>("Audio/Voice/" + clipId);
            _clips[clipId] = clip;
            if (clip == null)
                Debug.LogWarning($"[HiddenValley] Missing VO Resources/Audio/Voice/{clipId}");
            return clip;
        }
    }
}

using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Player-facing options, persisted outside the save file.
    ///
    /// Deliberately not in Core: none of this is world state, and a New Game must not
    /// reset the volume someone chose. PlayerPrefs is the right store for exactly this —
    /// it survives reinstall-over-the-top and costs nothing at boot.
    ///
    /// Everything here is read live by its consumer (GameAudio scales its beds by
    /// <see cref="Master"/> every frame; Haptics checks <see cref="HapticsOn"/> per
    /// call), so a change takes effect without a restart or a re-apply pass.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMaster = "hv.audio.master";
        private const string KeyVoice = "hv.audio.voice";
        private const string KeyHaptics = "hv.haptics";
        private const string KeyFrameHud = "hv.framehud";

        private static bool _loaded;

        private static float _master = 1f;
        private static float _voice = 1f;
        private static bool _haptics = true;
        private static bool _frameHud;

        /// <summary>Scales every audio bed. 1 = the mix as authored.</summary>
        public static float Master
        {
            get { Load(); return _master; }
            set { Load(); _master = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyMaster, _master); GameAudio.Instance?.ApplyVolumes(); }
        }

        /// <summary>Scales dialogue voice on top of <see cref="Master"/>.</summary>
        public static float Voice
        {
            get { Load(); return _voice; }
            set { Load(); _voice = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyVoice, _voice); GameAudio.Instance?.ApplyVolumes(); }
        }

        public static bool HapticsOn
        {
            get { Load(); return _haptics; }
            set { Load(); _haptics = value; PlayerPrefs.SetInt(KeyHaptics, value ? 1 : 0); }
        }

        /// <summary>The frame-time readout. Reachable from Settings so the owner's device
        /// pass does not depend on remembering a three-finger gesture.</summary>
        public static bool FrameHud
        {
            get { Load(); return _frameHud; }
            set { Load(); _frameHud = value; PlayerPrefs.SetInt(KeyFrameHud, value ? 1 : 0); }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _master = PlayerPrefs.GetFloat(KeyMaster, 1f);
            _voice = PlayerPrefs.GetFloat(KeyVoice, 1f);
            _haptics = PlayerPrefs.GetInt(KeyHaptics, 1) != 0;
            _frameHud = PlayerPrefs.GetInt(KeyFrameHud, 0) != 0;
        }

        /// <summary>Flushes to disk. Called when the pause panel closes and on background —
        /// per-drag writes during a slider would hit the disk every frame.</summary>
        public static void Flush()
        {
            if (_loaded) PlayerPrefs.Save();
        }
    }
}

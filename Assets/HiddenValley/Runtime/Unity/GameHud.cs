using System.Collections.Generic;
using HiddenValley.Core;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// The play-facing UI: interaction prompt, dialogue, quest tracker, bag, the account
    /// (the clue screen), readables, and the clock. Until this existed the engine could run
    /// a quest but the player had no way to see one — the slice was a black box.
    ///
    /// IMGUI, deliberately, same as the rest of the debug layer: grey-box builds need no
    /// canvas, no prefabs and no font assets, so the whole HUD survives scene regeneration.
    /// Phase 4 replaces the skin, not the wiring.
    ///
    /// Input model is one context button (see TouchControls.ContextAction): a tap talks to
    /// the NPC in front of you, else interacts with the object in front of you, else
    /// advances dialogue, else jumps. Choices and panel toggles are the only targets that
    /// need aimed taps, and dialogue locks movement so those taps are single-touch.
    ///
    /// Spawned at runtime by <see cref="SpawnOn"/> rather than serialized into the scene.
    /// The Heartwood scene already ships without a NavMesh for the level0-corruption reason
    /// (phase log, 2026-08-07); keeping the HUD code-only means regenerating the scene never
    /// depends on a MonoBehaviour that was mid-bisect and deleted from Assets.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private TouchControls controls;
        [SerializeField] private InteractionController interaction;
        [SerializeField] private Transform player;
        [SerializeField] private float talkRange = 2.6f;
        [SerializeField] private float talkMaxAngle = 110f;

        private GameBootstrap _boot;
        private NpcBinder[] _npcs = System.Array.Empty<NpcBinder>();

        private DialogueSession _session;
        private int _lineIndex;

        private string _readableTitle;
        private string _readableText;

        private bool _bagOpen;
        private bool _accountOpen;

        // Typewriter for dialogue lines (presentation polish).
        private string _typeFull = "";
        private int _typeChars;
        private float _typeNext;
        private const float TypeCharsPerSecond = 42f;

        /// <summary>
        /// Attach a GameHud to <paramref name="host"/> (normally the Controls object) and
        /// wire the three references it needs. Called from <see cref="TouchControls.Start"/>.
        /// </summary>
        public static GameHud SpawnOn(TouchControls controls, InteractionController interaction, Transform player)
        {
            if (controls == null) return null;
            if (controls.GetComponent<GameHud>() != null) return controls.GetComponent<GameHud>();

            var hud = controls.gameObject.AddComponent<GameHud>();
            hud.controls = controls;
            hud.interaction = interaction;
            hud.player = player;
            return hud;
        }

        private void Start()
        {
            _boot = GameBootstrap.Instance;
            RefreshNpcs();
            if (controls != null) controls.ContextAction = ContextAction;
        }

        private void Update()
        {
            // NPCs/Pip are runtime-spawned; re-scan if the first Start ran empty.
            if (_npcs.Length == 0) RefreshNpcs();

            if (controls != null)
                controls.Locked = _session != null || _readableText != null || _bagOpen || _accountOpen;

            if (_session != null && _typeChars < _typeFull.Length && Time.unscaledTime >= _typeNext)
            {
                _typeChars++;
                _typeNext = Time.unscaledTime + 1f / TypeCharsPerSecond;
            }
        }

        private void RefreshNpcs()
            => _npcs = FindObjectsByType<NpcBinder>(FindObjectsSortMode.None);

        // ---- the one button ---------------------------------------------------

        private bool ContextAction()
        {
            if (_readableText != null)
            {
                _readableText = null;
                _readableTitle = null;
                GameAudio.Instance?.UiClose();
                return true;
            }
            if (_bagOpen || _accountOpen)
            {
                _bagOpen = _accountOpen = false;
                GameAudio.Instance?.UiClose();
                return true;
            }
            if (_session != null)
            {
                // First tap finishes typewriter; second advances.
                if (_typeChars < _typeFull.Length)
                {
                    _typeChars = _typeFull.Length;
                    return true;
                }
                AdvanceDialogue();
                return true;
            }

            var npc = NearestNpc();
            if (npc != null) { BeginTalk(npc); return true; }

            if (interaction != null && interaction.Current != null)
            {
                var binder = interaction.Current;
                GameAudio.Instance?.Interact();
                if (binder.Interact() && !string.IsNullOrEmpty(binder.Text))
                {
                    _readableTitle = binder.Label;
                    _readableText = binder.Text;
                    GameAudio.Instance?.UiOpen();
                }
                return true;
            }

            return false; // nothing claimed the tap — it becomes a jump
        }

        private NpcBinder NearestNpc()
        {
            if (player == null) return null;

            NpcBinder best = null;
            float bestSqr = talkRange * talkRange;

            foreach (var npc in _npcs)
            {
                if (npc == null || !npc.isActiveAndEnabled || npc.Def == null) continue;

                var offset = npc.transform.position - player.position;
                offset.y = 0;
                if (offset.sqrMagnitude > bestSqr) continue;
                if (Vector3.Angle(player.forward, offset) > talkMaxAngle) continue;

                bestSqr = offset.sqrMagnitude;
                best = npc;
            }

            return best;
        }

        // ---- dialogue ---------------------------------------------------------

        private void BeginTalk(NpcBinder npc)
        {
            var session = npc.BeginConversation();
            if (session == null || session.Finished) return;
            _session = session;
            _lineIndex = 0;
            PlayDialogueLine();
        }

        private void AdvanceDialogue()
        {
            var node = _session?.Node;
            if (node == null) { EndDialogue(); return; }

            GameAudio.Instance?.StopDialogueVoice();

            if (_lineIndex < node.Lines.Count - 1)
            {
                _lineIndex++;
                PlayDialogueLine();
                return;
            }

            // Past the last line. If choices are showing, the tap does nothing — the
            // player must aim at one, and movement is locked so mis-taps cost nothing.
            if (_boot.Game.Dialogue.AvailableChoices(_session).Count > 0) return;

            _boot.Game.Dialogue.Continue(_session);
            _lineIndex = 0;
            if (_session.Finished)
            {
                EndDialogue();
                GameAudio.Instance?.DialogueAdvance();
            }
            else
            {
                PlayDialogueLine();
            }
        }

        private void Choose(int index)
        {
            GameAudio.Instance?.UiClick();
            GameAudio.Instance?.StopDialogueVoice();
            _boot.Game.Dialogue.Choose(_session, index);
            _lineIndex = 0;
            if (_session != null && _session.Finished) EndDialogue();
            else PlayDialogueLine();
        }

        private void EndDialogue()
        {
            _session = null;
            _typeFull = "";
            _typeChars = 0;
            GameAudio.Instance?.StopDialogueVoice();
        }

        private void PlayDialogueLine()
        {
            var node = _session?.Node;
            if (node == null) return;
            string line = _lineIndex < node.Lines.Count ? node.Lines[_lineIndex] : "";
            _typeFull = line ?? "";
            _typeChars = 0;
            _typeNext = Time.unscaledTime;
            // Prefer full ElevenLabs VO for this exact line; falls back to speaker cue.
            GameAudio.Instance?.PlayDialogueLine(node.Speaker, line);

            // Mystery-thread stings when Quietday clues enter conversation text.
            if (!string.IsNullOrEmpty(line) &&
                (line.IndexOf("Quietday", System.StringComparison.OrdinalIgnoreCase) >= 0
                 || line.IndexOf("kiln ash", System.StringComparison.OrdinalIgnoreCase) >= 0
                 || line.IndexOf("warm", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && line.IndexOf("channel", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                GameAudio.Instance?.PlayMysterySting();
            }
        }

        // ---- drawing ----------------------------------------------------------

        private GUIStyle _label, _prompt, _title, _button;
        private int _builtForHeight;

        private void EnsureStyles()
        {
            if (_label != null && _builtForHeight == Screen.height) return;
            _builtForHeight = Screen.height;

            int baseSize = Mathf.RoundToInt(Screen.height * 0.026f);

            _label = new GUIStyle(GUI.skin.label) { fontSize = baseSize, wordWrap = true };
            _label.normal.textColor = Color.white;

            _prompt = new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter };

            _title = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.25f),
                fontStyle = FontStyle.Bold
            };

            _button = new GUIStyle(GUI.skin.button) { fontSize = baseSize, wordWrap = true };
        }

        private void OnGUI()
        {
            if (_boot == null || _boot.Game == null) return;
            EnsureStyles();

            float w = Screen.width, h = Screen.height;

            DrawClockAndTracker(w, h);
            DrawToggles(w, h);

            if (_session != null) DrawDialogue(w, h);
            else if (_readableText != null) DrawReadable(w, h);
            else if (_bagOpen) DrawBag(w, h);
            else if (_accountOpen) DrawAccount(w, h);
            else DrawPrompt(w, h);
        }

        private void DrawClockAndTracker(float w, float h)
        {
            var clock = _boot.Game.State.Clock;
            string time = $"Day {clock.Day + 1} · {clock.PhaseName} · {clock.Minute / 60:00}:{clock.Minute % 60:00}";

            var right = new Rect(w * 0.55f, h * 0.02f, w * 0.43f, h * 0.5f);
            GUILayout.BeginArea(right);
            GUILayout.BeginVertical();

            var alignRight = new GUIStyle(_label) { alignment = TextAnchor.UpperRight };
            GUILayout.Label(time, alignRight);

            foreach (var line in _boot.Game.Quests.ActiveTrackerLines())
                GUILayout.Label(line, alignRight);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawToggles(float w, float h)
        {
            if (_session != null) return;

            float bw = w * 0.11f, bh = h * 0.07f;
            if (GUI.Button(new Rect(w * 0.30f, h * 0.02f, bw, bh), "Bag", _button))
            {
                bool opening = !_bagOpen;
                _bagOpen = !_bagOpen;
                _accountOpen = false;
                if (_bagOpen && opening) GameAudio.Instance?.UiOpen();
                else if (!_bagOpen) GameAudio.Instance?.UiClose();
            }
            if (GUI.Button(new Rect(w * 0.42f, h * 0.02f, bw, bh), "Account", _button))
            {
                bool opening = !_accountOpen;
                _accountOpen = !_accountOpen;
                _bagOpen = false;
                if (_accountOpen && opening) GameAudio.Instance?.UiOpen();
                else if (!_accountOpen) GameAudio.Instance?.UiClose();
            }
        }

        private void DrawPrompt(float w, float h)
        {
            string label = null;

            var npc = NearestNpc();
            if (npc != null) label = $"Talk to {npc.Def.Name}";
            else if (interaction != null && interaction.Current != null)
                label = interaction.Current.Label ?? "Interact";

            if (label == null) return;

            var rect = new Rect(w * 0.25f, h * 0.78f, w * 0.5f, h * 0.08f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, label, _prompt);
        }

        private void DrawDialogue(float w, float h)
        {
            var node = _session.Node;
            if (node == null) { _session = null; return; }

            var panel = new Rect(w * 0.08f, h * 0.62f, w * 0.84f, h * 0.34f);
            GUI.Box(panel, GUIContent.none);

            var inner = new Rect(panel.x + w * 0.02f, panel.y + h * 0.02f,
                                 panel.width - w * 0.04f, panel.height - h * 0.04f);
            GUILayout.BeginArea(inner);
            GUILayout.BeginVertical();

            // Prefer character display name over content id when present.
            string speaker = node.Speaker;
            if (!string.IsNullOrEmpty(speaker) && speaker.StartsWith("npc."))
            {
                var def = _boot.Game.Content.Npc(speaker);
                if (def != null && !string.IsNullOrEmpty(def.Name)) speaker = def.Name;
            }
            if (!string.IsNullOrEmpty(speaker)) GUILayout.Label(speaker, _title);

            string visible = _typeFull;
            if (_typeChars < _typeFull.Length)
                visible = _typeFull.Substring(0, Mathf.Clamp(_typeChars, 0, _typeFull.Length));
            GUILayout.Label(visible, _label);

            GUILayout.FlexibleSpace();

            bool lastLine = _lineIndex >= node.Lines.Count - 1;
            var choices = _boot.Game.Dialogue.AvailableChoices(_session);

            if (lastLine && choices.Count > 0)
            {
                for (int i = 0; i < choices.Count; i++)
                    if (GUILayout.Button(choices[i].Text, _button, GUILayout.MinHeight(h * 0.07f)))
                        Choose(i);
            }
            else
            {
                var hint = new GUIStyle(_label) { alignment = TextAnchor.LowerRight };
                GUILayout.Label("▸", hint);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawReadable(float w, float h)
        {
            var panel = new Rect(w * 0.15f, h * 0.15f, w * 0.7f, h * 0.7f);
            GUI.Box(panel, GUIContent.none);

            var inner = new Rect(panel.x + w * 0.02f, panel.y + h * 0.02f,
                                 panel.width - w * 0.04f, panel.height - h * 0.04f);
            GUILayout.BeginArea(inner);
            if (!string.IsNullOrEmpty(_readableTitle)) GUILayout.Label(_readableTitle, _title);
            GUILayout.Label(_readableText, _label);
            GUILayout.FlexibleSpace();
            GUILayout.Label("tap to close", new GUIStyle(_label) { alignment = TextAnchor.LowerCenter });
            GUILayout.EndArea();
        }

        private void DrawBag(float w, float h)
        {
            var panel = new Rect(w * 0.2f, h * 0.15f, w * 0.6f, h * 0.7f);
            GUI.Box(panel, GUIContent.none);

            var inner = new Rect(panel.x + w * 0.02f, panel.y + h * 0.02f,
                                 panel.width - w * 0.04f, panel.height - h * 0.04f);
            GUILayout.BeginArea(inner);
            GUILayout.Label("Bag", _title);

            var inventory = _boot.Game.State.Inventory;
            if (inventory.Count == 0) GUILayout.Label("Nothing carried.", _label);
            else
            {
                float icon = h * 0.07f;
                foreach (var pair in inventory)
                {
                    var item = _boot.Game.Content.Item(pair.Key);
                    GUILayout.BeginHorizontal();
                    var tex = RuntimeArt.Icon(pair.Key);
                    var iconRect = GUILayoutUtility.GetRect(icon, icon, GUILayout.Width(icon), GUILayout.Height(icon));
                    if (tex != null) GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit);
                    else GUI.Box(iconRect, GUIContent.none);
                    GUILayout.Label($"{item?.Name ?? pair.Key} × {pair.Value}", _label, GUILayout.Height(icon));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndArea();
        }

        private void DrawAccount(float w, float h)
        {
            var panel = new Rect(w * 0.12f, h * 0.1f, w * 0.76f, h * 0.8f);
            GUI.Box(panel, GUIContent.none);

            var inner = new Rect(panel.x + w * 0.02f, panel.y + h * 0.02f,
                                 panel.width - w * 0.04f, panel.height - h * 0.04f);
            GUILayout.BeginArea(inner);
            GUILayout.Label("The Account", _title);

            var clues = _boot.Game.KnownClues();
            if (clues.Count == 0) GUILayout.Label("Nothing written yet.", _label);
            else
                foreach (var clue in clues)
                {
                    GUILayout.Space(h * 0.015f);
                    GUILayout.Label(clue.Title, _title);
                    GUILayout.Label(clue.Text, _label);
                }

            GUILayout.EndArea();
        }
    }
}

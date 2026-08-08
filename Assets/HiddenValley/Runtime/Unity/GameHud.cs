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

        /// <summary>Station id while the crafting panel is open; null when closed.
        /// Opened by content setting the ui.open_crafting flag (e.g. the kiln).</summary>
        private string _craftingStation;

        // Typewriter for dialogue lines (presentation polish).
        private string _typeFull = "";
        private int _typeChars;
        private float _typeNext;
        private const float TypeCharsPerSecond = 42f;
        private float _panelOpenedAt;
        private static Texture2D _dimTexture;

        // Revision-gated caches. OnGUI runs at least twice a frame; anything rebuilt per
        // pass was allocating ~2-6 KB/frame and buying a GC spike every half minute.
        private readonly List<string> _trackerCache = new List<string>();
        private string _clockText = "";
        private int _clockMinute = -1, _clockDay = -1;
        private List<DialogueChoice> _choiceCache = new List<DialogueChoice>();
        private readonly List<string> _bagCache = new List<string>();
        private readonly List<string> _bagIcons = new List<string>();

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
            if (_boot != null)
            {
                _boot.Changed += OnGameChanged;
                OnGameChanged();
            }
        }

        private void OnDestroy()
        {
            if (_boot != null) _boot.Changed -= OnGameChanged;
        }

        private void OnGameChanged()
        {
            if (_boot?.Game == null) return;

            _trackerCache.Clear();
            _trackerCache.AddRange(_boot.Game.Quests.ActiveTrackerLines());

            if (_session != null)
                _choiceCache = _boot.Game.Dialogue.AvailableChoices(_session);

            RebuildBagCache();
            _recipeCacheStation = "\0"; // inputs/known recipes may have changed mid-panel
            DiffForToasts();

            // Content asks for the crafting panel by flag (e.g. the kiln's on_interact).
            var station = _boot.Game.State.GetFlag("ui.open_crafting");
            if (!string.IsNullOrEmpty(station))
            {
                _boot.Game.State.SetFlag("ui.open_crafting", "");
                _craftingStation = station;
                _bagOpen = _accountOpen = false;
                GameAudio.Instance?.UiOpen();
            }
        }

        private void RebuildBagCache()
        {
            _bagCache.Clear();
            _bagIcons.Clear();
            foreach (var pair in _boot.Game.State.Inventory)
            {
                var item = _boot.Game.Content.Item(pair.Key);
                _bagCache.Add($"{item?.Name ?? pair.Key} × {pair.Value}");
                _bagIcons.Add(pair.Key);
            }
        }

        private void Update()
        {
            // NPCs/Pip are runtime-spawned; re-scan if the first Start ran empty.
            if (_npcs.Length == 0) RefreshNpcs();

            if (controls != null)
                controls.Locked = _session != null || _readableText != null
                    || _bagOpen || _accountOpen || _craftingStation != null;

            if (_session != null && _typeChars < _typeFull.Length && Time.unscaledTime >= _typeNext)
            {
                char typed = _typeFull[_typeChars];
                _typeChars++;

                // Punctuation breathes: a beat after clause and sentence ends is 80% of
                // what makes a typewriter read as spoken language instead of teletype.
                float interval = 1f / TypeCharsPerSecond;
                if (typed is '.' or '!' or '?') interval *= 5f;
                else if (typed is ',' or ';' or '—') interval *= 2.5f;
                _typeNext = Time.unscaledTime + interval;
            }

            // Expire toasts.
            while (_toasts.Count > 0 && Time.unscaledTime > _toasts.Peek().until)
                _toasts.Dequeue();
        }

        // ---- toasts -----------------------------------------------------------

        private readonly Queue<(string text, float until)> _toasts = new Queue<(string, float)>();
        private int _lastClueCount = -1, _lastCompletedQuests = -1;
        private readonly List<string> _prevTracker = new List<string>();

        private void Toast(string text)
        {
            if (_toasts.Count > 4) _toasts.Dequeue();
            _toasts.Enqueue((text, Time.unscaledTime + 3.2f));
        }

        /// <summary>State changes become visible beats: one moment hits ear (sting, via
        /// GameAudio), eye (toast) and hand (haptic) on the same frame.</summary>
        private void DiffForToasts()
        {
            var game = _boot.Game;

            int done = 0;
            foreach (var q in game.State.Quests.Values)
                if (q.State == QuestState.Completed) done++;
            if (_lastCompletedQuests >= 0 && done > _lastCompletedQuests)
            {
                Toast("Quest complete");
                Haptics.Success();
            }
            _lastCompletedQuests = done;

            int clues = game.State.Clues.Count;
            if (_lastClueCount >= 0 && clues > _lastClueCount)
            {
                Toast("Written into the account");
                Haptics.Medium();
            }
            _lastClueCount = clues;

            foreach (var line in _trackerCache)
                if (!_prevTracker.Contains(line))
                    Toast(line);
            _prevTracker.Clear();
            _prevTracker.AddRange(_trackerCache);
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
            if (_bagOpen || _accountOpen || _craftingStation != null)
            {
                _bagOpen = _accountOpen = false;
                _craftingStation = null;
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
                Haptics.Light();
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

        /// <summary>Recipes for the open station, resolved once per panel state change.</summary>
        private readonly List<RecipeDef> _recipeCache = new List<RecipeDef>();
        private string _recipeCacheStation;

        private List<RecipeDef> StationRecipes()
        {
            if (_recipeCacheStation != _craftingStation)
            {
                _recipeCacheStation = _craftingStation;
                _recipeCache.Clear();
                if (_craftingStation != null)
                    _recipeCache.AddRange(_boot.Game.Crafting.KnownRecipes(_craftingStation));
            }
            return _recipeCache;
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

        private NpcBinder _talkingNpc;
        private PipCompanion _pip;

        private void BeginTalk(NpcBinder npc)
        {
            var session = npc.BeginConversation();
            if (session == null || session.Finished) return;
            _session = session;
            _lineIndex = 0;
            _panelOpenedAt = Time.unscaledTime;

            // Both parties turn to face each other; Pip settles to a calm hover.
            _talkingNpc = npc;
            npc.FaceTarget = player;
            if (player != null)
            {
                Vector3 flat = npc.transform.position - player.position;
                flat.y = 0;
                if (flat.sqrMagnitude > 0.01f)
                    player.rotation = Quaternion.LookRotation(flat);
            }
            if (_pip == null) _pip = FindFirstObjectByType<PipCompanion>();
            if (_pip != null) _pip.Calm = true;

            Haptics.Light();
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
            Haptics.Light();
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
            if (_talkingNpc != null) _talkingNpc.FaceTarget = null;
            _talkingNpc = null;
            if (_pip != null) _pip.Calm = false;
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
            _choiceCache = _boot.Game.Dialogue.AvailableChoices(_session);
            // Prefer full ElevenLabs VO for this exact line; falls back to speaker cue.
            // Mystery stings are no longer string-matched here — content authors them
            // with cue effects (see Core CueEffect), routed through GameAudio.OnCue.
            GameAudio.Instance?.PlayDialogueLine(node.Speaker, line);
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

            // Derived styles hoisted here — building them inside Draw* allocated per pass.
            _labelRight = new GUIStyle(_label) { alignment = TextAnchor.UpperRight };
            _hintRight = new GUIStyle(_label) { alignment = TextAnchor.LowerRight };
            _footCenter = new GUIStyle(_label) { alignment = TextAnchor.LowerCenter };
        }

        private GUIStyle _labelRight, _hintRight, _footCenter;

        private void OnGUI()
        {
            if (_boot == null || _boot.Game == null) return;
            EnsureStyles();

            float w = Screen.width, h = Screen.height;

            // Respect the notch: everything anchored to the top shifts below the safe
            // area; landscape phones put the clock straight under the sensor housing.
            var safe = Screen.safeArea;
            GUI.BeginGroup(new Rect(safe.x, h - safe.yMax, safe.width, safe.height));
            w = safe.width;
            h = safe.height;

            DrawClockAndTracker(w, h);
            DrawToggles(w, h);

            if (_session != null) DrawDialogue(w, h);
            else if (_readableText != null) DrawReadable(w, h);
            else if (_craftingStation != null) DrawCrafting(w, h);
            else if (_bagOpen) DrawBag(w, h);
            else if (_accountOpen) DrawAccount(w, h);
            else DrawPrompt(w, h);

            DrawToasts(w, h);
            GUI.EndGroup();
        }

        private void DrawToasts(float w, float h)
        {
            if (_toasts.Count == 0) return;

            float y = h * 0.10f;
            foreach (var toast in _toasts)
            {
                float life = toast.until - Time.unscaledTime;
                float alpha = Mathf.Clamp01(life / 0.4f); // fade the last 0.4 s
                var prev = GUI.color;
                GUI.color = new Color(1, 1, 1, alpha);

                var rect = new Rect(w * 0.28f, y, w * 0.44f, h * 0.065f);
                GUI.Box(rect, GUIContent.none);
                GUI.Label(rect, toast.text, _prompt);

                GUI.color = prev;
                y += h * 0.075f;
            }
        }

        private void DrawClockAndTracker(float w, float h)
        {
            var clock = _boot.Game.State.Clock;
            if (clock.Minute != _clockMinute || clock.Day != _clockDay)
            {
                _clockMinute = clock.Minute;
                _clockDay = clock.Day;
                _clockText = $"Day {clock.Day + 1} · {clock.PhaseName} · {clock.Minute / 60:00}:{clock.Minute % 60:00}";
            }

            var right = new Rect(w * 0.55f, h * 0.02f, w * 0.43f, h * 0.5f);
            GUILayout.BeginArea(right);
            GUILayout.BeginVertical();

            GUILayout.Label(_clockText, _labelRight);

            for (int i = 0; i < _trackerCache.Count; i++)
                GUILayout.Label(_trackerCache[i], _labelRight);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawCrafting(float w, float h)
        {
            var panel = new Rect(w * 0.15f, h * 0.1f, w * 0.7f, h * 0.8f);
            GUI.Box(panel, GUIContent.none);

            var inner = new Rect(panel.x + w * 0.02f, panel.y + h * 0.02f,
                                 panel.width - w * 0.04f, panel.height - h * 0.04f);
            GUILayout.BeginArea(inner);
            GUILayout.Label("Craft — " + _craftingStation, _title);

            var recipes = StationRecipes();
            if (recipes.Count == 0)
                GUILayout.Label("You don't know any recipes for this station yet.", _label);

            var crafting = _boot.Game.Crafting;
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label(recipe.Name, _label);

                foreach (var input in recipe.Inputs)
                {
                    var item = _boot.Game.Content.Item(input.Key);
                    int have = _boot.Game.State.ItemCount(input.Key);
                    GUILayout.Label($"  {item?.Name ?? input.Key}: {have}/{input.Value}", _label);
                }
                GUILayout.EndVertical();

                GUI.enabled = crafting.CanCraft(recipe.Id);
                if (GUILayout.Button("Craft", _button,
                        GUILayout.Width(w * 0.16f), GUILayout.MinHeight(h * 0.08f)))
                {
                    if (crafting.Craft(recipe.Id)) GameAudio.Instance?.Play("sfx_pickup");
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(h * 0.015f);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("tap anywhere to close", _footCenter);
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
            if (npc != null)
            {
                label = $"Talk to {npc.Def.Name}";
                // Their VO decodes now, off-frame, instead of at the first tapped line.
                GameAudio.Instance?.PrewarmSpeaker(npc.Id);
            }
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

            // Dialogue is a mode: dim the world behind it.
            if (_dimTexture == null)
            {
                _dimTexture = new Texture2D(1, 1);
                _dimTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.45f));
                _dimTexture.Apply();
            }
            GUI.DrawTexture(new Rect(0, 0, w, h), _dimTexture);

            // Ease the panel up over ~0.12 s when it opens.
            float ease = Mathf.Clamp01((Time.unscaledTime - _panelOpenedAt) / 0.12f);
            float slide = (1f - ease * ease) * h * 0.06f;

            var panel = new Rect(w * 0.08f, h * 0.62f + slide, w * 0.84f, h * 0.34f);
            GUI.Box(panel, GUIContent.none);

            // Portrait beside the speaker name when art exists.
            string portraitKey = node.Speaker != null && node.Speaker.StartsWith("npc.")
                ? node.Speaker.Substring(4) : node.Speaker;
            var portrait = RuntimeArt.Character(portraitKey);
            if (portrait != null)
            {
                float side = h * 0.16f;
                GUI.DrawTexture(
                    new Rect(panel.x + w * 0.012f, panel.y - side * 0.55f, side, side),
                    portrait, ScaleMode.ScaleToFit);
            }

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

            if (lastLine && _choiceCache.Count > 0)
            {
                for (int i = 0; i < _choiceCache.Count; i++)
                    if (GUILayout.Button(_choiceCache[i].Text, _button, GUILayout.MinHeight(h * 0.07f)))
                        Choose(i);
            }
            else
            {
                GUILayout.Label("▸", _hintRight);
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
            GUILayout.Label("tap to close", _footCenter);
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

            if (_bagCache.Count == 0) GUILayout.Label("Nothing carried.", _label);
            else
            {
                float icon = h * 0.07f;
                for (int i = 0; i < _bagCache.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    var tex = RuntimeArt.Icon(_bagIcons[i]);
                    var iconRect = GUILayoutUtility.GetRect(icon, icon, GUILayout.Width(icon), GUILayout.Height(icon));
                    if (tex != null) GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit);
                    else GUI.Box(iconRect, GUIContent.none);
                    GUILayout.Label(_bagCache[i], _label, GUILayout.Height(icon));
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

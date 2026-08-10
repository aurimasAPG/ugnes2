using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// The touch joystick required by the Phase 0 done-state, plus jump and sprint for
    /// Phase 1. Raw <see cref="Input.touches"/> rather than uGUI, for two reasons: the rest
    /// of the debug layer (FrameTimeHud) already draws with IMGUI so grey-box builds need no
    /// canvas at all, and GUI buttons do not compose with multitouch — walking while jumping
    /// is the very first thing a tester does.
    ///
    /// Layout: a touch beginning in the left 45% of the screen anchors a floating joystick
    /// where the finger lands. On the right half, a short tap is Jump and a held touch is
    /// Sprint. In the editor, WASD/arrows, Space and LeftShift map to the same three outputs,
    /// so play-mode testing exercises the identical consumer code path.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        [SerializeField] private float radiusScreenFraction = 0.12f;
        [SerializeField] private float tapMaxSeconds = 0.25f;
        [SerializeField] private float sprintHoldSeconds = 0.3f;
        [SerializeField] private bool drawOverlay = true;

        /// <summary>Stick deflection, each axis in [-1, 1].</summary>
        public Vector2 Move { get; private set; }

        /// <summary>True while a right-half touch has been held past the sprint threshold.</summary>
        public bool Sprint { get; private set; }

        /// <summary>
        /// While true (dialogue, bag, the account), the stick reads zero and taps neither
        /// jump nor sprint — but <see cref="ContextAction"/> still fires, because a tap
        /// during dialogue means "next line", not "jump behind the UI".
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// One thumb, one button: a right-half tap offers itself here first — talk,
        /// interact, advance dialogue — and only becomes a jump if nothing claims it.
        /// Wired by GameHud; null (the grey-box scene) means taps just jump.
        /// </summary>
        public System.Func<bool> ContextAction { get; set; }

        private bool _jumpQueued;

        private int _stickFinger = -1;
        private Vector2 _stickOrigin;

        private int _rightFinger = -1;
        private float _rightHeld;
        private Vector2 _rightStart;

        private void Start()
        {
            // HUD is attached at runtime so VillageSetup never serializes a GameHud into
            // the scene. Wire InteractionController + player by finding them once.
            GameAudio.EnsureExists();
            var interaction = FindFirstObjectByType<InteractionController>();
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            GameHud.SpawnOn(this, interaction, playerGo != null ? playerGo.transform : null);
        }

        /// <summary>Jump is an event, not a state — consuming it clears it, so one tap is one jump.</summary>
        public bool ConsumeJump()
        {
            bool queued = _jumpQueued;
            _jumpQueued = false;
            return queued;
        }

        private void Update()
        {
            ReadTouches();
#if UNITY_EDITOR || UNITY_STANDALONE
            ReadDesktopFallback();
#endif
            if (Locked)
            {
                Move = Vector2.zero;
                Sprint = false;
                _jumpQueued = false;
            }
        }

        private void Tap()
        {
            if (ContextAction != null && ContextAction()) return;
            if (!Locked) _jumpQueued = true;
        }

        private void ReadTouches()
        {
            float radius = Screen.height * radiusScreenFraction;
            bool stickAlive = false;
            bool rightAlive = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.fingerId == _stickFinger)
                {
                    stickAlive = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                    Move = stickAlive
                        ? Vector2.ClampMagnitude((touch.position - _stickOrigin) / radius, 1f)
                        : Vector2.zero;
                    continue;
                }

                if (touch.fingerId == _rightFinger)
                {
                    rightAlive = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                    if (rightAlive)
                    {
                        _rightHeld += touch.deltaTime;
                        Sprint = _rightHeld >= sprintHoldSeconds;
                    }
                    else
                    {
                        // Short and stationary is a tap; anything else was a hold or a swipe.
                        bool moved = (touch.position - _rightStart).magnitude > radius * 0.5f;
                        if (_rightHeld < tapMaxSeconds && !moved) Tap();
                        Sprint = false;
                    }
                    continue;
                }

                if (touch.phase != TouchPhase.Began) continue;

                if (_stickFinger < 0 && touch.position.x < Screen.width * 0.45f)
                {
                    _stickFinger = touch.fingerId;
                    _stickOrigin = touch.position;
                    stickAlive = true;
                }
                else if (_rightFinger < 0 && touch.position.x >= Screen.width * 0.45f)
                {
                    _rightFinger = touch.fingerId;
                    _rightStart = touch.position;
                    _rightHeld = 0f;
                    rightAlive = true;
                }
            }

            if (!stickAlive && _stickFinger >= 0)
            {
                _stickFinger = -1;
                Move = Vector2.zero;
            }

            if (!rightAlive && _rightFinger >= 0)
            {
                _rightFinger = -1;
                _rightHeld = 0f;
                Sprint = false;
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private void ReadDesktopFallback()
        {
            // Always assign Move from keys — including zero. The old path only wrote when
            // keys were down, so releasing WASD left Move stuck and the player never stopped.
            var keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (keys.sqrMagnitude > 0.01f)
                Move = Vector2.ClampMagnitude(keys, 1f);
            else if (_stickFinger < 0)
                Move = Vector2.zero;

            if (Input.GetKeyDown(KeyCode.Space)) Tap();
            // Sprint is held-state: set true while shift is down, clear when released
            // (unless a right-half touch is already sprinting).
            if (_rightFinger < 0)
                Sprint = Input.GetKey(KeyCode.LeftShift);
        }
#endif

        // ---- overlay ----------------------------------------------------------

        private static Texture2D _circle;

        private void OnGUI()
        {
            if (!drawOverlay || _stickFinger < 0) return;

            float radius = Screen.height * radiusScreenFraction;
            DrawCircle(_stickOrigin, radius, new Color(1f, 1f, 1f, 0.15f));
            DrawCircle(_stickOrigin + Move * radius, radius * 0.4f, new Color(1f, 1f, 1f, 0.35f));
        }

        private static void DrawCircle(Vector2 screenPos, float radius, Color color)
        {
            if (_circle == null) _circle = MakeCircleTexture(64);

            // Touch positions are bottom-left origin; GUI is top-left.
            var rect = new Rect(screenPos.x - radius, Screen.height - screenPos.y - radius,
                                radius * 2f, radius * 2f);
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _circle);
            GUI.color = previous;
        }

        private static Texture2D MakeCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.85f, 1f, d));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            return texture;
        }
    }
}

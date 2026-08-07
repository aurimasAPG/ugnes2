using System.Collections.Generic;

namespace HiddenValley.Core
{
    /// <summary>
    /// The whole game as one object, with no Unity dependency anywhere beneath it.
    ///
    /// That constraint is not tidiness: it means the rules can be compiled and tested on a
    /// build machine with no engine installed, which is how this repo's engine logic gets
    /// verified at all given the toolchain blocker (phase log B1).
    /// </summary>
    public sealed class Game
    {
        public ContentDatabase Content { get; }
        public GameState State { get; private set; }
        public QuestEngine Quests { get; private set; }
        public DialogueEngine Dialogue { get; private set; }
        public WorldView World { get; private set; }
        public Crafting Crafting { get; private set; }

        private float _secondsCarried;

        private Game(ContentDatabase content, GameState state)
        {
            Content = content;
            Bind(state);
        }

        private void Bind(GameState state)
        {
            State = state;
            Quests = new QuestEngine(state);
            Dialogue = new DialogueEngine(state, Quests);
            World = new WorldView(state, Quests);
            Crafting = new Crafting(state, Quests);
        }

        public static Game NewGame(ContentDatabase content)
        {
            var game = new Game(content, new GameState(content));
            game.Quests.Settle();
            return game;
        }

        public static Game FromSave(string json, ContentDatabase content)
        {
            var game = new Game(content, SaveSystem.Load(json, content));
            game.Quests.Settle();
            return game;
        }

        public string Save() => SaveSystem.Save(State);

        /// <summary>Applies effects and settles. The single entry point for anything that changes the world.</summary>
        public void Apply(IEnumerable<Effect> effects)
        {
            Effect.ApplyAll(effects, State);
            Quests.Settle();
        }

        public void Apply(Effect effect)
        {
            effect?.Apply(State);
            Quests.Settle();
        }

        /// <summary>
        /// Advances the clock by real elapsed time. Fractions are carried rather than
        /// dropped, so a 60fps device and a 30fps device see the same in-game day length.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            var perMinute = Content.Settings.SecondsPerMinute;
            if (perMinute <= 0f) return;

            _secondsCarried += deltaSeconds;

            int minutes = (int)(_secondsCarried / perMinute);
            if (minutes <= 0) return;

            _secondsCarried -= minutes * perMinute;
            State.Clock.Advance(minutes);
            Quests.Settle();
        }

        /// <summary>Clues the player has found, ordered for the account (the clue screen).</summary>
        public List<ClueDef> KnownClues(string thread = null)
        {
            var list = new List<ClueDef>();
            foreach (var clue in Content.Clues)
            {
                if (thread != null && clue.Thread != thread) continue;
                if (State.KnowsClue(clue.Id)) list.Add(clue);
            }
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            return list;
        }
    }
}

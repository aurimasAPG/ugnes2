using System.Collections.Generic;

namespace HiddenValley.Core
{
    /// <summary>
    /// The closed effect vocabulary — the write half of the content language.
    ///
    /// Note what is deliberately absent: there is no "open shop", no "unlock area", no
    /// "spawn resource node". Those are not effects, they are <em>conditions on world
    /// objects</em>. An NPC who changes what the world contains sets a flag; the world
    /// objects watch the flag. That inversion is what makes NPC #4 free.
    /// </summary>
    public abstract class Effect
    {
        public abstract void Apply(GameState state);

        public static void ApplyAll(IEnumerable<Effect> effects, GameState state)
        {
            if (effects == null) return;
            foreach (var e in effects)
                e?.Apply(state);
        }
    }

    public sealed class GiveItemEffect : Effect
    {
        public string Item;
        public int Count = 1;
        public override void Apply(GameState state) => state.AddItem(Item, Count);
    }

    public sealed class TakeItemEffect : Effect
    {
        public string Item;
        public int Count = 1;
        public override void Apply(GameState state) => state.RemoveItem(Item, Count);
    }

    public sealed class SetFlagEffect : Effect
    {
        public string Key;
        public string Value = "true";
        public override void Apply(GameState state) => state.SetFlag(Key, Value);
    }

    /// <summary>
    /// Steps a flag through a list of values, wrapping at the end.
    ///
    /// Added for the Undersluice's paddle gates, where the player cycles each gate through
    /// four positions. It stays a primitive rather than a puzzle: nothing here knows what a
    /// gate is, and the same effect drives any multi-state switch, lever or dial the slice
    /// needs later. A flag whose value is not in the list snaps to the first entry, so a
    /// mistyped starting value degrades to a working switch instead of a dead one.
    /// </summary>
    public sealed class CycleFlagEffect : Effect
    {
        public string Key;
        public List<string> Values = new List<string>();

        public override void Apply(GameState state)
        {
            if (string.IsNullOrEmpty(Key) || Values.Count == 0) return;

            var current = state.GetFlag(Key);
            int index = Values.IndexOf(current);
            int next = index < 0 ? 0 : (index + 1) % Values.Count;

            state.SetFlag(Key, Values[next]);
        }
    }

    public sealed class StartQuestEffect : Effect
    {
        public string Quest;
        public override void Apply(GameState state) => state.StartQuest(Quest);
    }

    public sealed class CompleteQuestEffect : Effect
    {
        public string Quest;
        public override void Apply(GameState state) => state.ForceCompleteQuest(Quest);
    }

    /// <summary>
    /// Pushes a quest past its current step regardless of completion conditions. For
    /// steps that are finished by a conversation rather than by world state.
    /// </summary>
    public sealed class AdvanceQuestEffect : Effect
    {
        public string Quest;
        public override void Apply(GameState state) => state.AdvanceQuest(Quest);
    }

    /// <summary>
    /// Grants either a single recipe or an entire family. The family form is what makes
    /// Vesk a capability rather than a vending machine (world bible §5.1).
    /// </summary>
    public sealed class LearnRecipeEffect : Effect
    {
        public string Recipe;
        public string Family;

        public override void Apply(GameState state)
        {
            if (!string.IsNullOrEmpty(Recipe))
                state.Recipes.Add(Recipe);

            if (string.IsNullOrEmpty(Family)) return;

            foreach (var r in state.Content.Recipes)
                if (r.Family == Family) state.Recipes.Add(r.Id);
        }
    }

    public sealed class LearnClueEffect : Effect
    {
        public string Clue;
        public override void Apply(GameState state) => state.Clues.Add(Clue);
    }

    /// <summary>Advances the clock to the next occurrence of a phase. Sleeping, waiting.</summary>
    public sealed class SetTimeEffect : Effect
    {
        public string Phase = "dawn";
        public override void Apply(GameState state) => state.Clock.AdvanceToPhase(Phase);
    }

    public sealed class AdvanceTimeEffect : Effect
    {
        public int Minutes = 60;
        public override void Apply(GameState state) => state.Clock.Advance(Minutes);
    }
}

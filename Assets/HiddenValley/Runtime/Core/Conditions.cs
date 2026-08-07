using System.Collections.Generic;

namespace HiddenValley.Core
{
    public enum QuestState
    {
        NotStarted = 0,
        Active = 1,
        Completed = 2
    }

    /// <summary>
    /// Everything a condition is allowed to ask about the world. Deliberately narrow:
    /// content can only interrogate state that the save file round-trips, which is what
    /// keeps save/load honest as content grows.
    /// </summary>
    public interface IEvalContext
    {
        ContentDatabase Content { get; }
        string GetFlag(string key);
        int ItemCount(string itemId);
        QuestState QuestStateOf(string questId);
        int QuestStepIndex(string questId);
        bool KnowsClue(string clueId);
        bool KnowsRecipe(string recipeId);
        Clock Clock { get; }
    }

    /// <summary>
    /// The closed condition vocabulary. This set is intentionally small and closed:
    /// every gate in the game — dialogue branches, quest completion, whether a shop
    /// exists, whether a cliff is passable — composes from these. Adding a content item
    /// never adds a condition type, which is the whole architecture in one sentence.
    /// </summary>
    public abstract class Condition
    {
        public abstract bool Evaluate(IEvalContext ctx);

        /// <summary>Null conditions mean "always true". Centralised so callers stop null-checking.</summary>
        public static bool Test(Condition c, IEvalContext ctx) => c == null || c.Evaluate(ctx);
    }

    public sealed class AlwaysCondition : Condition
    {
        public bool Value = true;
        public override bool Evaluate(IEvalContext ctx) => Value;
    }

    public sealed class FlagCondition : Condition
    {
        public string Key;

        /// <summary>Compared as a string, so flags can carry puzzle settings, not just booleans.</summary>
        public string Value = "true";

        public override bool Evaluate(IEvalContext ctx) => ctx.GetFlag(Key) == Value;
    }

    public sealed class HasItemCondition : Condition
    {
        public string Item;
        public int Count = 1;
        public override bool Evaluate(IEvalContext ctx) => ctx.ItemCount(Item) >= Count;
    }

    public sealed class QuestStateCondition : Condition
    {
        public string Quest;
        public QuestState State;
        public override bool Evaluate(IEvalContext ctx) => ctx.QuestStateOf(Quest) == State;
    }

    public enum StepCompare
    {
        At = 0,
        AtOrPast = 1,
        Past = 2
    }

    /// <summary>
    /// Compares against a step by <em>id</em> rather than index, so inserting a step in
    /// the middle of a quest does not silently re-point every condition that referenced it.
    /// </summary>
    public sealed class QuestStepCondition : Condition
    {
        public string Quest;
        public string Step;
        public StepCompare Compare = StepCompare.At;

        public override bool Evaluate(IEvalContext ctx)
        {
            var quest = ctx.Content.Quest(Quest);
            if (quest == null) return false;

            int target = -1;
            for (int i = 0; i < quest.Steps.Count; i++)
                if (quest.Steps[i].Id == Step) { target = i; break; }
            if (target < 0) return false;

            if (ctx.QuestStateOf(Quest) == QuestState.NotStarted) return false;

            // A completed quest counts as past every one of its steps.
            int current = ctx.QuestStateOf(Quest) == QuestState.Completed
                ? quest.Steps.Count
                : ctx.QuestStepIndex(Quest);

            switch (Compare)
            {
                case StepCompare.At: return current == target;
                case StepCompare.AtOrPast: return current >= target;
                case StepCompare.Past: return current > target;
                default: return false;
            }
        }
    }

    public sealed class ClueKnownCondition : Condition
    {
        public string Clue;
        public override bool Evaluate(IEvalContext ctx) => ctx.KnowsClue(Clue);
    }

    public sealed class RecipeKnownCondition : Condition
    {
        public string Recipe;
        public override bool Evaluate(IEvalContext ctx) => ctx.KnowsRecipe(Recipe);
    }

    /// <summary>
    /// True when the clock is in any of the named phases. The Undersluice puzzle's third
    /// step is the one load-bearing use of this in the slice (world bible §8).
    /// </summary>
    public sealed class TimeOfDayCondition : Condition
    {
        public List<string> Phases = new List<string>();

        public override bool Evaluate(IEvalContext ctx)
        {
            var now = ctx.Clock.PhaseName;
            foreach (var p in Phases)
                if (p == now) return true;
            return false;
        }
    }

    public sealed class AllCondition : Condition
    {
        public List<Condition> Of = new List<Condition>();

        public override bool Evaluate(IEvalContext ctx)
        {
            foreach (var c in Of)
                if (!Test(c, ctx)) return false;
            return true;
        }
    }

    public sealed class AnyCondition : Condition
    {
        public List<Condition> Of = new List<Condition>();

        public override bool Evaluate(IEvalContext ctx)
        {
            foreach (var c in Of)
                if (Test(c, ctx)) return true;
            return false;
        }
    }

    public sealed class NotCondition : Condition
    {
        public Condition Of;
        public override bool Evaluate(IEvalContext ctx) => !Test(Of, ctx);
    }
}

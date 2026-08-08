using System.Collections.Generic;

namespace HiddenValley.Core
{
    public sealed class QuestProgress
    {
        public QuestState State = QuestState.NotStarted;
        public int StepIndex;
    }

    /// <summary>
    /// The entire mutable game. Every field is a generic collection keyed by content id —
    /// there is no per-quest or per-NPC field anywhere. That is deliberate and load-bearing:
    /// it means the save format does not change when content is added, so a save written
    /// before NPC #4 existed still loads after.
    /// </summary>
    public sealed class GameState : IEvalContext
    {
        public ContentDatabase Content { get; }
        public Clock Clock { get; private set; }

        public readonly Dictionary<string, string> Flags = new Dictionary<string, string>();
        public readonly Dictionary<string, int> Inventory = new Dictionary<string, int>();
        public readonly Dictionary<string, QuestProgress> Quests = new Dictionary<string, QuestProgress>();
        public readonly HashSet<string> Clues = new HashSet<string>();
        public readonly HashSet<string> Recipes = new HashSet<string>();

        /// <summary>Bumped by every mutation, so systems can cheaply detect "something changed".</summary>
        public int Revision { get; private set; }

        public GameState(ContentDatabase content, Clock clock = null)
        {
            Content = content;
            Clock = clock ?? new Clock(content?.Settings ?? new ClockSettings());
        }

        public void Touch() => Revision++;

        // ---- flags -------------------------------------------------------------

        public string GetFlag(string key)
        {
            if (key == null) return null;
            return Flags.TryGetValue(key, out var v) ? v : null;
        }

        public void SetFlag(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            Flags[key] = value;
            Touch();
        }

        public bool GetBoolFlag(string key) => GetFlag(key) == "true";

        // ---- inventory ---------------------------------------------------------

        public int ItemCount(string itemId)
        {
            if (itemId == null) return 0;
            return Inventory.TryGetValue(itemId, out var n) ? n : 0;
        }

        public void AddItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;
            Inventory[itemId] = ItemCount(itemId) + count;
            Touch();
        }

        /// <summary>Removes up to <paramref name="count"/>; returns how many were actually removed.</summary>
        public int RemoveItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return 0;

            int have = ItemCount(itemId);
            int taken = have < count ? have : count;
            if (taken <= 0) return 0;

            int left = have - taken;
            if (left > 0) Inventory[itemId] = left;
            else Inventory.Remove(itemId);

            Touch();
            return taken;
        }

        // ---- quests ------------------------------------------------------------

        public QuestProgress Progress(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            if (!Quests.TryGetValue(questId, out var p))
            {
                p = new QuestProgress();
                Quests[questId] = p;
            }
            return p;
        }

        public QuestState QuestStateOf(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return QuestState.NotStarted;
            return Quests.TryGetValue(questId, out var p) ? p.State : QuestState.NotStarted;
        }

        public int QuestStepIndex(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return 0;
            return Quests.TryGetValue(questId, out var p) ? p.StepIndex : 0;
        }

        public void StartQuest(string questId)
        {
            if (Content?.Quest(questId) == null) return;

            var p = Progress(questId);
            if (p.State != QuestState.NotStarted) return;

            p.State = QuestState.Active;
            p.StepIndex = 0;
            Touch();
        }

        public void AdvanceQuest(string questId)
        {
            var quest = Content?.Quest(questId);
            if (quest == null) return;

            var p = Progress(questId);
            if (p.State != QuestState.Active) return;

            var step = p.StepIndex >= 0 && p.StepIndex < quest.Steps.Count
                ? quest.Steps[p.StepIndex]
                : null;

            p.StepIndex++;
            Touch();

            if (step != null) Effect.ApplyAll(step.OnComplete, this);

            if (p.StepIndex >= quest.Steps.Count) CompleteQuest(questId);
        }

        public void ForceCompleteQuest(string questId)
        {
            var quest = Content?.Quest(questId);
            if (quest == null) return;

            var p = Progress(questId);
            if (p.State == QuestState.Completed) return;

            p.State = QuestState.Active;
            p.StepIndex = quest.Steps.Count;
            CompleteQuest(questId);
        }

        private void CompleteQuest(string questId)
        {
            var quest = Content.Quest(questId);
            var p = Progress(questId);
            if (p.State == QuestState.Completed) return;

            p.State = QuestState.Completed;
            p.StepIndex = quest.Steps.Count;
            Touch();

            Effect.ApplyAll(quest.OnComplete, this);
        }

        public QuestStep CurrentStep(string questId)
        {
            var quest = Content?.Quest(questId);
            if (quest == null) return null;

            var p = Progress(questId);
            if (p.State != QuestState.Active) return null;
            if (p.StepIndex < 0 || p.StepIndex >= quest.Steps.Count) return null;

            return quest.Steps[p.StepIndex];
        }

        // ---- knowledge ---------------------------------------------------------

        public bool KnowsClue(string clueId) => clueId != null && Clues.Contains(clueId);
        public bool KnowsRecipe(string recipeId) => recipeId != null && Recipes.Contains(recipeId);

        /// <summary>Adds and touches. Bare Clues.Add skipped Revision, so views never heard
        /// about new clues until something else changed — found 2026-08-08.</summary>
        public void LearnClue(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return;
            if (Clues.Add(clueId)) Touch();
        }

        public void LearnRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return;
            if (Recipes.Add(recipeId)) Touch();
        }

        // ---- presentation cues -------------------------------------------------

        /// <summary>
        /// Transient, never saved: cues emitted by content effects for the presentation
        /// layer (stingers, Pip reactions, screen moments). The Unity side drains this
        /// after each settle. Content authors moments; code never string-matches text.
        /// </summary>
        public readonly List<string> PendingCues = new List<string>();

        public void EmitCue(string cueId)
        {
            if (string.IsNullOrEmpty(cueId)) return;
            PendingCues.Add(cueId);
            Touch();
        }

        public void SetClock(Clock clock)
        {
            Clock = clock;
            Touch();
        }
    }
}

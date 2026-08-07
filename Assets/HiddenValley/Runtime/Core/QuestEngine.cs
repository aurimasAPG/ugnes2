using System.Collections.Generic;

namespace HiddenValley.Core
{
    /// <summary>
    /// Drives every quest in the game with one loop and no per-quest code.
    ///
    /// After any state change, <see cref="Settle"/> starts any quest whose start condition
    /// now passes and completes any step whose completion condition now passes, repeating
    /// until nothing else changes. A quest that finishes a step and thereby satisfies the
    /// next one resolves in the same settle, which is what makes chained content feel
    /// instant rather than one-interaction-behind.
    /// </summary>
    public sealed class QuestEngine
    {
        private const int MaxIterations = 64;

        private readonly GameState _state;

        /// <summary>Non-fatal problems found while settling. Surfaced by the validator and the debug HUD.</summary>
        public readonly List<string> Warnings = new List<string>();

        public QuestEngine(GameState state)
        {
            _state = state;
        }

        public void Settle()
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                bool changed = AutoStart() || AdvanceSteps();
                if (!changed) return;
            }

            Warnings.Add(
                "Quest settle did not converge in " + MaxIterations + " iterations. " +
                "This usually means two quests' effects re-trigger each other — check on_complete chains.");
        }

        private bool AutoStart()
        {
            foreach (var quest in _state.Content.Quests)
            {
                if (quest.StartWhen == null) continue;
                if (_state.QuestStateOf(quest.Id) != QuestState.NotStarted) continue;
                if (!quest.StartWhen.Evaluate(_state)) continue;

                _state.StartQuest(quest.Id);
                return true;
            }
            return false;
        }

        private bool AdvanceSteps()
        {
            foreach (var quest in _state.Content.Quests)
            {
                if (_state.QuestStateOf(quest.Id) != QuestState.Active) continue;

                var step = _state.CurrentStep(quest.Id);
                if (step == null || step.Completion.Count == 0) continue;

                foreach (var set in step.Completion)
                {
                    if (!Condition.Test(set.When, _state)) continue;

                    // Record which route the player took. Content can react to it, and the
                    // play-feel pass can tell whether anyone ever found the undocumented one.
                    if (!string.IsNullOrEmpty(set.Id))
                        _state.SetFlag($"solved.{quest.Id}.{step.Id}", set.Id);

                    _state.AdvanceQuest(quest.Id);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Tracker lines for every active quest, in content order.</summary>
        public List<string> ActiveTrackerLines()
        {
            var lines = new List<string>();
            foreach (var quest in _state.Content.Quests)
            {
                if (_state.QuestStateOf(quest.Id) != QuestState.Active) continue;

                var step = _state.CurrentStep(quest.Id);
                if (step != null) lines.Add($"{quest.Title}: {step.Tracker}");
            }
            return lines;
        }

        public List<QuestDef> ActiveQuests()
        {
            var list = new List<QuestDef>();
            foreach (var quest in _state.Content.Quests)
                if (_state.QuestStateOf(quest.Id) == QuestState.Active) list.Add(quest);
            return list;
        }
    }
}

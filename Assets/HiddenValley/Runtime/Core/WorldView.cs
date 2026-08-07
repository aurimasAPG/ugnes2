using System.Collections.Generic;

namespace HiddenValley.Core
{
    /// <summary>
    /// Answers "what is in the world right now, and what can the player do with it".
    ///
    /// This is where the brief's hardest architectural requirement is actually satisfied.
    /// Orrel "changes what the world contains" and Brann "opens a space that was closed",
    /// and neither is a code path — both are world objects whose conditions flipped. The
    /// Unity layer just mirrors this into GameObject.SetActive and NavMesh obstacles.
    /// </summary>
    public sealed class WorldView
    {
        private readonly GameState _state;
        private readonly QuestEngine _quests;

        public WorldView(GameState state, QuestEngine quests)
        {
            _state = state;
            _quests = quests;
        }

        public bool IsVisible(WorldObjectDef def)
            => def != null && Condition.Test(def.VisibleWhen, _state);

        public bool IsVisible(string id) => IsVisible(_state.Content.WorldObject(id));

        /// <summary>Barriers block only while their condition holds. Draining the pool flips one.</summary>
        public bool Blocks(WorldObjectDef def)
        {
            if (def?.BlocksWhen == null) return false;
            return IsVisible(def) && def.BlocksWhen.Evaluate(_state);
        }

        public bool CanInteract(WorldObjectDef def)
        {
            if (def == null || def.OnInteract.Count == 0) return false;
            if (!IsVisible(def)) return false;
            return Condition.Test(def.InteractWhen, _state);
        }

        /// <summary>Runs an object's interaction and settles quests. Returns false if it was not interactable.</summary>
        public bool Interact(string id)
        {
            var def = _state.Content.WorldObject(id);
            if (!CanInteract(def)) return false;

            Effect.ApplyAll(def.OnInteract, _state);
            _quests.Settle();
            return true;
        }

        public List<WorldObjectDef> VisibleObjects(string region = null)
        {
            var list = new List<WorldObjectDef>();
            foreach (var def in _state.Content.WorldObjects)
            {
                if (region != null && def.Region != region) continue;
                if (IsVisible(def)) list.Add(def);
            }
            return list;
        }

        /// <summary>
        /// Where an NPC should be standing right now. First schedule block whose phase
        /// window contains the current phase and whose condition passes.
        /// </summary>
        public string WaypointFor(string npcId)
        {
            var npc = _state.Content.Npc(npcId);
            if (npc == null) return null;

            var phase = _state.Clock.PhaseName;
            foreach (var block in npc.Schedule)
            {
                if (!Condition.Test(block.When, _state)) continue;
                if (PhaseInWindow(phase, block.From, block.To)) return block.Waypoint;
            }
            return null;
        }

        private static readonly string[] PhaseOrder = { "dawn", "day", "dusk", "night" };

        private static int PhaseIndex(string phase)
        {
            for (int i = 0; i < PhaseOrder.Length; i++)
                if (PhaseOrder[i] == phase) return i;
            return -1;
        }

        /// <summary>Inclusive window that may wrap past night, e.g. dusk→dawn.</summary>
        internal static bool PhaseInWindow(string phase, string from, string to)
        {
            int p = PhaseIndex(phase), f = PhaseIndex(from), t = PhaseIndex(to);
            if (p < 0 || f < 0 || t < 0) return false;

            if (f <= t) return p >= f && p <= t;
            return p >= f || p <= t;
        }
    }
}

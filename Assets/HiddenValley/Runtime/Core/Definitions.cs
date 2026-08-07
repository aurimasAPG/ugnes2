using System.Collections.Generic;

namespace HiddenValley.Core
{
    /// <summary>
    /// Content definitions. Every one of these is authored as JSON and loaded at
    /// runtime. Nothing here is ever subclassed per-content-item: a new NPC is a new
    /// row of data, not a new type. That property is the Phase 2 gate.
    /// </summary>
    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int MaxStack = 99;
    }

    public sealed class RecipeDef
    {
        public string Id;
        public string Name;

        /// <summary>
        /// Recipes are grouped into families so an NPC can grant a whole branch of
        /// crafting at once (see world bible §5.1). The family is data, not a type.
        /// </summary>
        public string Family;

        public string Station;
        public Dictionary<string, int> Inputs = new Dictionary<string, int>();
        public string OutputItem;
        public int OutputCount = 1;
    }

    public sealed class ClueDef
    {
        public string Id;
        public string Title;
        public string Text;

        /// <summary>Which thread this clue belongs to, for the investigation board.</summary>
        public string Thread;

        /// <summary>Ordering hint within the thread. Not a gate — clues may be found out of order.</summary>
        public int Order;
    }

    public sealed class DialogueChoice
    {
        public string Text;

        /// <summary>Choice is hidden unless this passes. Null means always shown.</summary>
        public Condition When;

        public List<Effect> Effects = new List<Effect>();

        /// <summary>Node to move to. Null or empty ends the conversation.</summary>
        public string Goto;
    }

    public sealed class DialogueNode
    {
        public string Id;
        public string Speaker;
        public List<string> Lines = new List<string>();
        public List<DialogueChoice> Choices = new List<DialogueChoice>();

        /// <summary>Effects applied once, when the node is entered.</summary>
        public List<Effect> OnEnter = new List<Effect>();

        /// <summary>If set and there are no choices, the conversation continues here.</summary>
        public string Goto;
    }

    public sealed class DialogueDef
    {
        public string Id;
        public List<DialogueNode> Nodes = new List<DialogueNode>();

        public DialogueNode Node(string id)
        {
            foreach (var n in Nodes)
                if (n.Id == id) return n;
            return null;
        }
    }

    /// <summary>
    /// An NPC's conversation changes with world state by picking the first entry whose
    /// condition passes. This is why a talkative, quest-stage-aware NPC needs no code.
    /// </summary>
    public sealed class DialogueEntry
    {
        public Condition When;
        public string Dialogue;
        public string Node = "start";
    }

    public sealed class ScheduleBlock
    {
        public string From = "dawn";
        public string To = "night";
        public string Waypoint;

        /// <summary>Optional: block only applies when this passes.</summary>
        public Condition When;
    }

    public sealed class NpcDef
    {
        public string Id;
        public string Name;
        public string Region;
        public List<ScheduleBlock> Schedule = new List<ScheduleBlock>();
        public List<DialogueEntry> DialogueEntries = new List<DialogueEntry>();

        /// <summary>
        /// Declared capability kind, used by the content validator to enforce the
        /// brief's rule that NPCs differ at the systems level. One of:
        /// recipe_family, world_contents, traversal, none.
        /// </summary>
        public string CapabilityKind = "none";
    }

    /// <summary>
    /// One alternative way to finish a step. A step with more than one of these has an
    /// undocumented solution — the tracker text describes only the first.
    /// </summary>
    public sealed class CompletionSet
    {
        public string Id;
        public Condition When;

        /// <summary>
        /// False for solutions the quest text does not describe. The validator uses this
        /// to assert the brief's "exactly one of the eight" rule.
        /// </summary>
        public bool Documented = true;
    }

    public sealed class QuestStep
    {
        public string Id;

        /// <summary>Player-facing tracker line for this step.</summary>
        public string Tracker;

        /// <summary>Step completes when ANY of these passes.</summary>
        public List<CompletionSet> Completion = new List<CompletionSet>();

        public List<Effect> OnComplete = new List<Effect>();
    }

    public sealed class QuestDef
    {
        public string Id;
        public string Title;
        public string Summary;

        /// <summary>
        /// One of: fetch, repair, investigate, craft-to-spec, observe-and-report,
        /// escort, trade, unlock. The validator enforces that no more than two quests
        /// share a shape.
        /// </summary>
        public string Shape;

        public string Giver;

        /// <summary>Quest starts by itself when this passes. Null means it must be started explicitly.</summary>
        public Condition StartWhen;

        public List<QuestStep> Steps = new List<QuestStep>();
        public List<Effect> OnComplete = new List<Effect>();
    }

    /// <summary>
    /// Anything placed in the world whose presence or behaviour depends on state:
    /// shops, resource nodes, barriers, interactables, puzzle mechanisms.
    ///
    /// This type is what lets an NPC "change what the world contains" and "open a
    /// closed space" without a single new line of C#. Both are conditions on data.
    /// </summary>
    public sealed class WorldObjectDef
    {
        public string Id;

        /// <summary>Free-form tag the Unity layer maps to a prefab or a scene object.</summary>
        public string Kind;

        public string Region;

        /// <summary>Present in the world only when this passes. Null means always.</summary>
        public Condition VisibleWhen;

        /// <summary>Blocks navigation only when this passes. Null means never blocks.</summary>
        public Condition BlocksWhen;

        /// <summary>Interaction is offered only when this passes. Null means always (if visible).</summary>
        public Condition InteractWhen;

        public string InteractLabel;

        /// <summary>Body text shown when the object is read — plates, notices, carvings.</summary>
        public string Text;

        public List<Effect> OnInteract = new List<Effect>();

        /// <summary>Optional spawn position for objects with no authored scene placement.</summary>
        public float[] Position;
    }

    public sealed class ClockSettings
    {
        public int DayLengthMinutes = 1440;
        public int DawnStart = 300;
        public int DayStart = 480;
        public int DuskStart = 1140;
        public int NightStart = 1260;

        /// <summary>Real seconds per in-game minute.</summary>
        public float SecondsPerMinute = 0.5f;

        public int StartMinute = 420;
    }
}

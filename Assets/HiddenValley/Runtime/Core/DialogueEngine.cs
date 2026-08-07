using System.Collections.Generic;

namespace HiddenValley.Core
{
    public sealed class DialogueSession
    {
        public DialogueDef Def;
        public DialogueNode Node;
        public bool Finished => Node == null;
    }

    /// <summary>
    /// Runs conversations. An NPC's current conversation is chosen by walking their
    /// dialogue entries and taking the first whose condition passes, so "this character
    /// says something different once you've found the scorched bark" is a data edit.
    /// </summary>
    public sealed class DialogueEngine
    {
        private readonly GameState _state;
        private readonly QuestEngine _quests;

        public DialogueEngine(GameState state, QuestEngine quests)
        {
            _state = state;
            _quests = quests;
        }

        public DialogueEntry ResolveEntry(string npcId)
        {
            var npc = _state.Content.Npc(npcId);
            if (npc == null) return null;

            foreach (var entry in npc.DialogueEntries)
                if (Condition.Test(entry.When, _state)) return entry;

            return null;
        }

        public DialogueSession Begin(string npcId)
        {
            var entry = ResolveEntry(npcId);
            if (entry == null) return null;

            var def = _state.Content.Dialogue(entry.Dialogue);
            if (def == null)
                throw new ContentException($"NPC '{npcId}' points at missing dialogue '{entry.Dialogue}'.");

            var node = def.Node(entry.Node);
            if (node == null)
                throw new ContentException($"Dialogue '{def.Id}' has no node '{entry.Node}'.");

            var session = new DialogueSession { Def = def, Node = node };
            EnterNode(session, node);
            return session;
        }

        private void EnterNode(DialogueSession session, DialogueNode node)
        {
            session.Node = node;
            if (node == null) return;

            Effect.ApplyAll(node.OnEnter, _state);
            _quests.Settle();
        }

        /// <summary>Choices whose conditions currently pass, in author order.</summary>
        public List<DialogueChoice> AvailableChoices(DialogueSession session)
        {
            var list = new List<DialogueChoice>();
            if (session?.Node == null) return list;

            foreach (var choice in session.Node.Choices)
                if (Condition.Test(choice.When, _state)) list.Add(choice);

            return list;
        }

        public void Choose(DialogueSession session, int index)
        {
            var choices = AvailableChoices(session);
            if (index < 0 || index >= choices.Count) return;

            var choice = choices[index];
            Effect.ApplyAll(choice.Effects, _state);
            _quests.Settle();

            GoTo(session, choice.Goto);
        }

        /// <summary>Advances a node that has no choices. Ends the session when there is nowhere to go.</summary>
        public void Continue(DialogueSession session)
        {
            if (session?.Node == null) return;
            GoTo(session, session.Node.Goto);
        }

        private void GoTo(DialogueSession session, string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                session.Node = null;
                return;
            }

            var next = session.Def.Node(nodeId);
            if (next == null)
                throw new ContentException($"Dialogue '{session.Def.Id}' has no node '{nodeId}'.");

            EnterNode(session, next);
        }
    }
}

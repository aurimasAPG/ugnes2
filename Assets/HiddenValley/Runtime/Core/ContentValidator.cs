using System.Collections.Generic;

namespace HiddenValley.Core
{
    public sealed class ValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public bool Ok => Errors.Count == 0;

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Ok ? "CONTENT OK" : $"CONTENT FAILED — {Errors.Count} error(s)");
            foreach (var e in Errors) sb.AppendLine("  ERROR   " + e);
            foreach (var w in Warnings) sb.AppendLine("  warning " + w);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Checks two different things, deliberately in one place.
    ///
    /// The first is ordinary reference integrity — a dangling item id in a quest is a
    /// soft-lock waiting for a tester to find it, and content authored in JSON has no
    /// compiler to catch it.
    ///
    /// The second is unusual and is the point: the design constraints from the build brief
    /// are asserted here as code. "No more than two quests share a shape" and "the three
    /// NPCs differ at the systems level" are the kind of rule that silently rots as content
    /// grows, because nothing fails when it breaks. Now something fails.
    /// </summary>
    public static class ContentValidator
    {
        private static readonly HashSet<string> AllowedShapes = new HashSet<string>
        {
            "fetch", "repair", "investigate", "craft-to-spec",
            "observe-and-report", "escort", "trade", "unlock"
        };

        private static readonly HashSet<string> CapabilityKinds = new HashSet<string>
        {
            "recipe_family", "world_contents", "traversal", "none"
        };

        public static ValidationReport Validate(ContentDatabase db)
        {
            var r = new ValidationReport();

            CheckReferences(db, r);
            CheckQuestShapes(db, r);
            CheckUndocumentedSolution(db, r);
            CheckNpcCapabilities(db, r);
            CheckDialogue(db, r);
            CheckReachability(db, r);

            return r;
        }

        // ---- brief constraints -------------------------------------------------

        private static void CheckQuestShapes(ContentDatabase db, ValidationReport r)
        {
            var counts = new Dictionary<string, int>();

            foreach (var q in db.Quests)
            {
                if (string.IsNullOrEmpty(q.Shape))
                {
                    r.Errors.Add($"Quest '{q.Id}' declares no shape.");
                    continue;
                }
                if (!AllowedShapes.Contains(q.Shape))
                {
                    r.Errors.Add($"Quest '{q.Id}' has shape '{q.Shape}', which is not one of the eight allowed shapes.");
                    continue;
                }
                counts.TryGetValue(q.Shape, out var n);
                counts[q.Shape] = n + 1;
            }

            foreach (var kv in counts)
                if (kv.Value > 2)
                    r.Errors.Add(
                        $"{kv.Value} quests share the shape '{kv.Key}'. The brief allows at most two. " +
                        "Re-shape one of them rather than renaming it.");
        }

        private static void CheckUndocumentedSolution(ContentDatabase db, ValidationReport r)
        {
            var questsWithAlternates = new List<string>();

            foreach (var q in db.Quests)
            {
                bool hasUndocumented = false;
                foreach (var step in q.Steps)
                {
                    int undocumented = 0;
                    foreach (var set in step.Completion)
                        if (!set.Documented) undocumented++;

                    if (undocumented > 0) hasUndocumented = true;

                    if (step.Completion.Count > 1 && undocumented == 0)
                        r.Warnings.Add(
                            $"Quest '{q.Id}' step '{step.Id}' has several completion routes but marks them all " +
                            "documented. If one is genuinely not described by the tracker text, mark it " +
                            "\"documented\": false so it counts.");
                }

                if (hasUndocumented) questsWithAlternates.Add(q.Id);
            }

            if (questsWithAlternates.Count != 1)
                r.Errors.Add(
                    $"The brief requires exactly one quest solvable in a way its text does not describe; " +
                    $"found {questsWithAlternates.Count} ({string.Join(", ", questsWithAlternates)}).");
        }

        private static void CheckNpcCapabilities(ContentDatabase db, ValidationReport r)
        {
            var kinds = new Dictionary<string, List<string>>();

            foreach (var npc in db.Npcs)
            {
                if (!CapabilityKinds.Contains(npc.CapabilityKind))
                {
                    r.Errors.Add($"NPC '{npc.Id}' has unknown capability_kind '{npc.CapabilityKind}'.");
                    continue;
                }
                if (npc.CapabilityKind == "none") continue;

                if (!kinds.TryGetValue(npc.CapabilityKind, out var list))
                    kinds[npc.CapabilityKind] = list = new List<string>();
                list.Add(npc.Id);
            }

            foreach (var required in new[] { "recipe_family", "world_contents", "traversal" })
                if (!kinds.ContainsKey(required))
                    r.Errors.Add(
                        $"No NPC provides the '{required}' capability. The brief requires all three kinds — " +
                        "three NPCs who differ only in personality is the stated failure state.");

            foreach (var kv in kinds)
                if (kv.Value.Count > 1)
                    r.Warnings.Add(
                        $"NPCs {string.Join(", ", kv.Value)} all provide '{kv.Key}'. That is allowed, but check " +
                        "they are not becoming the same character twice.");

            // A declared capability that nothing in the data actually delivers is worse than
            // no declaration, because it passes the check above while lying.
            foreach (var npc in db.Npcs)
            {
                if (npc.CapabilityKind == "recipe_family" && !GrantsRecipeFamily(db, npc))
                    r.Errors.Add(
                        $"NPC '{npc.Id}' declares capability 'recipe_family' but no quest they give and no " +
                        "dialogue they own grants a recipe family.");
            }
        }

        private static bool GrantsRecipeFamily(ContentDatabase db, NpcDef npc)
        {
            foreach (var q in db.Quests)
            {
                if (q.Giver != npc.Id) continue;
                foreach (var e in AllEffectsOf(q))
                    if (e is LearnRecipeEffect lr && !string.IsNullOrEmpty(lr.Family)) return true;
            }

            foreach (var entry in npc.DialogueEntries)
            {
                var def = db.Dialogue(entry.Dialogue);
                if (def == null) continue;
                foreach (var e in AllEffectsOf(def))
                    if (e is LearnRecipeEffect lr && !string.IsNullOrEmpty(lr.Family)) return true;
            }

            return false;
        }

        // ---- integrity ---------------------------------------------------------

        private static void CheckDialogue(ContentDatabase db, ValidationReport r)
        {
            foreach (var def in db.Dialogues)
            {
                var ids = new HashSet<string>();
                foreach (var n in def.Nodes)
                {
                    if (string.IsNullOrEmpty(n.Id))
                        r.Errors.Add($"Dialogue '{def.Id}' has a node with no id.");
                    else if (!ids.Add(n.Id))
                        r.Errors.Add($"Dialogue '{def.Id}' has duplicate node id '{n.Id}'.");
                }

                foreach (var n in def.Nodes)
                {
                    if (!string.IsNullOrEmpty(n.Goto) && def.Node(n.Goto) == null)
                        r.Errors.Add($"Dialogue '{def.Id}' node '{n.Id}' goes to missing node '{n.Goto}'.");

                    foreach (var c in n.Choices)
                        if (!string.IsNullOrEmpty(c.Goto) && def.Node(c.Goto) == null)
                            r.Errors.Add($"Dialogue '{def.Id}' node '{n.Id}' has a choice to missing node '{c.Goto}'.");
                }
            }

            foreach (var npc in db.Npcs)
            {
                if (npc.DialogueEntries.Count == 0)
                    r.Warnings.Add($"NPC '{npc.Id}' has no dialogue entries and cannot be talked to.");

                foreach (var entry in npc.DialogueEntries)
                {
                    var def = db.Dialogue(entry.Dialogue);
                    if (def == null)
                    {
                        r.Errors.Add($"NPC '{npc.Id}' points at missing dialogue '{entry.Dialogue}'.");
                        continue;
                    }
                    if (def.Node(entry.Node) == null)
                        r.Errors.Add($"NPC '{npc.Id}' points at missing node '{entry.Node}' in dialogue '{def.Id}'.");
                }

                // An NPC whose entries are all conditional can fall through to nothing.
                bool hasFallback = false;
                foreach (var entry in npc.DialogueEntries)
                    if (entry.When == null) hasFallback = true;

                if (npc.DialogueEntries.Count > 0 && !hasFallback)
                    r.Warnings.Add(
                        $"NPC '{npc.Id}' has no unconditional dialogue entry. If every condition fails the " +
                        "player walks up and nothing happens, which reads as a bug.");
            }
        }

        private static void CheckReachability(ContentDatabase db, ValidationReport r)
        {
            var started = new HashSet<string>();
            foreach (var e in AllEffects(db))
                if (e is StartQuestEffect s && s.Quest != null) started.Add(s.Quest);

            foreach (var q in db.Quests)
            {
                if (q.StartWhen == null && !started.Contains(q.Id))
                    r.Errors.Add(
                        $"Quest '{q.Id}' has no start_when and nothing starts it. It is unreachable content.");

                if (q.Steps.Count == 0)
                    r.Errors.Add($"Quest '{q.Id}' has no steps.");

                foreach (var step in q.Steps)
                {
                    if (string.IsNullOrEmpty(step.Id))
                        r.Errors.Add($"Quest '{q.Id}' has a step with no id.");

                    if (step.Completion.Count == 0 && !AdvancedByEffect(db, q.Id))
                        r.Errors.Add(
                            $"Quest '{q.Id}' step '{step.Id}' has no completion conditions and no " +
                            "advance_quest effect anywhere targets this quest. It is a dead end.");

                    if (string.IsNullOrEmpty(step.Tracker))
                        r.Warnings.Add($"Quest '{q.Id}' step '{step.Id}' has no tracker text.");

                    // A null condition tests as true, so this would complete the instant the
                    // step opened — a silent skip rather than an error at runtime.
                    foreach (var set in step.Completion)
                        if (set.When == null)
                            r.Errors.Add(
                                $"Quest '{q.Id}' step '{step.Id}' has a completion set with no condition. " +
                                "It would complete instantly.");
                }
            }
        }

        private static bool AdvancedByEffect(ContentDatabase db, string questId)
        {
            foreach (var e in AllEffects(db))
            {
                if (e is AdvanceQuestEffect a && a.Quest == questId) return true;
                if (e is CompleteQuestEffect c && c.Quest == questId) return true;
            }
            return false;
        }

        private static void CheckReferences(ContentDatabase db, ValidationReport r)
        {
            foreach (var e in AllEffects(db))
            {
                switch (e)
                {
                    case GiveItemEffect g when db.Item(g.Item) == null:
                        r.Errors.Add($"Effect give_item references missing item '{g.Item}'."); break;
                    case TakeItemEffect t when db.Item(t.Item) == null:
                        r.Errors.Add($"Effect take_item references missing item '{t.Item}'."); break;
                    case StartQuestEffect s when db.Quest(s.Quest) == null:
                        r.Errors.Add($"Effect start_quest references missing quest '{s.Quest}'."); break;
                    case AdvanceQuestEffect a when db.Quest(a.Quest) == null:
                        r.Errors.Add($"Effect advance_quest references missing quest '{a.Quest}'."); break;
                    case CompleteQuestEffect c when db.Quest(c.Quest) == null:
                        r.Errors.Add($"Effect complete_quest references missing quest '{c.Quest}'."); break;
                    case LearnClueEffect l when db.Clue(l.Clue) == null:
                        r.Errors.Add($"Effect learn_clue references missing clue '{l.Clue}'."); break;
                    case LearnRecipeEffect lr:
                        if (!string.IsNullOrEmpty(lr.Recipe) && db.Recipe(lr.Recipe) == null)
                            r.Errors.Add($"Effect learn_recipe references missing recipe '{lr.Recipe}'.");
                        if (!string.IsNullOrEmpty(lr.Family) && !FamilyExists(db, lr.Family))
                            r.Errors.Add($"Effect learn_recipe references empty family '{lr.Family}'.");
                        break;
                }
            }

            foreach (var c in AllConditions(db))
            {
                switch (c)
                {
                    case HasItemCondition h when db.Item(h.Item) == null:
                        r.Errors.Add($"Condition has_item references missing item '{h.Item}'."); break;
                    case QuestStateCondition q when db.Quest(q.Quest) == null:
                        r.Errors.Add($"Condition quest_state references missing quest '{q.Quest}'."); break;
                    case ClueKnownCondition cl when db.Clue(cl.Clue) == null:
                        r.Errors.Add($"Condition clue_known references missing clue '{cl.Clue}'."); break;
                    case RecipeKnownCondition rk when db.Recipe(rk.Recipe) == null:
                        r.Errors.Add($"Condition recipe_known references missing recipe '{rk.Recipe}'."); break;
                    case QuestStepCondition qs:
                        {
                            var quest = db.Quest(qs.Quest);
                            if (quest == null)
                            {
                                r.Errors.Add($"Condition quest_step references missing quest '{qs.Quest}'.");
                                break;
                            }
                            bool found = false;
                            foreach (var s in quest.Steps)
                                if (s.Id == qs.Step) { found = true; break; }
                            if (!found)
                                r.Errors.Add($"Condition quest_step references missing step '{qs.Step}' in quest '{qs.Quest}'.");
                            break;
                        }
                }
            }

            foreach (var recipe in db.Recipes)
            {
                if (db.Item(recipe.OutputItem) == null)
                    r.Errors.Add($"Recipe '{recipe.Id}' outputs missing item '{recipe.OutputItem}'.");
                foreach (var kv in recipe.Inputs)
                    if (db.Item(kv.Key) == null)
                        r.Errors.Add($"Recipe '{recipe.Id}' consumes missing item '{kv.Key}'.");
            }

            foreach (var q in db.Quests)
                if (!string.IsNullOrEmpty(q.Giver) && db.Npc(q.Giver) == null)
                    r.Errors.Add($"Quest '{q.Id}' has giver '{q.Giver}', who does not exist.");
        }

        private static bool FamilyExists(ContentDatabase db, string family)
        {
            foreach (var recipe in db.Recipes)
                if (recipe.Family == family) return true;
            return false;
        }

        // ---- traversal helpers -------------------------------------------------

        private static IEnumerable<Effect> AllEffects(ContentDatabase db)
        {
            foreach (var q in db.Quests)
                foreach (var e in AllEffectsOf(q)) yield return e;

            foreach (var d in db.Dialogues)
                foreach (var e in AllEffectsOf(d)) yield return e;

            foreach (var w in db.WorldObjects)
                foreach (var e in w.OnInteract) yield return e;
        }

        private static IEnumerable<Effect> AllEffectsOf(QuestDef q)
        {
            foreach (var e in q.OnComplete) yield return e;
            foreach (var s in q.Steps)
                foreach (var e in s.OnComplete) yield return e;
        }

        private static IEnumerable<Effect> AllEffectsOf(DialogueDef d)
        {
            foreach (var n in d.Nodes)
            {
                foreach (var e in n.OnEnter) yield return e;
                foreach (var c in n.Choices)
                    foreach (var e in c.Effects) yield return e;
            }
        }

        private static IEnumerable<Condition> AllConditions(ContentDatabase db)
        {
            foreach (var q in db.Quests)
            {
                foreach (var c in Expand(q.StartWhen)) yield return c;
                foreach (var s in q.Steps)
                    foreach (var set in s.Completion)
                        foreach (var c in Expand(set.When)) yield return c;
            }

            foreach (var n in db.Npcs)
            {
                foreach (var entry in n.DialogueEntries)
                    foreach (var c in Expand(entry.When)) yield return c;
                foreach (var b in n.Schedule)
                    foreach (var c in Expand(b.When)) yield return c;
            }

            foreach (var d in db.Dialogues)
                foreach (var node in d.Nodes)
                    foreach (var choice in node.Choices)
                        foreach (var c in Expand(choice.When)) yield return c;

            foreach (var w in db.WorldObjects)
            {
                foreach (var c in Expand(w.VisibleWhen)) yield return c;
                foreach (var c in Expand(w.BlocksWhen)) yield return c;
                foreach (var c in Expand(w.InteractWhen)) yield return c;
            }
        }

        /// <summary>Flattens composite conditions so nested references are checked too.</summary>
        private static IEnumerable<Condition> Expand(Condition c)
        {
            if (c == null) yield break;
            yield return c;

            switch (c)
            {
                case AllCondition all:
                    foreach (var sub in all.Of)
                        foreach (var x in Expand(sub)) yield return x;
                    break;
                case AnyCondition any:
                    foreach (var sub in any.Of)
                        foreach (var x in Expand(sub)) yield return x;
                    break;
                case NotCondition not:
                    foreach (var x in Expand(not.Of)) yield return x;
                    break;
            }
        }
    }
}

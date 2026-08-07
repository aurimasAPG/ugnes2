using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace HiddenValley.Core
{
    public sealed class ContentException : Exception
    {
        public ContentException(string message) : base(message) { }
    }

    /// <summary>
    /// All authored content, loaded from JSON at startup.
    ///
    /// Every file may contain any mix of top-level arrays — "items", "recipes", "clues",
    /// "npcs", "quests", "dialogue", "world" — so an author can put one NPC and
    /// everything that NPC needs in a single file, rather than editing seven registries.
    /// That is the difference between NPC #4 costing an afternoon and costing a sprint.
    /// </summary>
    public sealed class ContentDatabase
    {
        public readonly List<ItemDef> Items = new List<ItemDef>();
        public readonly List<RecipeDef> Recipes = new List<RecipeDef>();
        public readonly List<ClueDef> Clues = new List<ClueDef>();
        public readonly List<NpcDef> Npcs = new List<NpcDef>();
        public readonly List<QuestDef> Quests = new List<QuestDef>();
        public readonly List<DialogueDef> Dialogues = new List<DialogueDef>();
        public readonly List<WorldObjectDef> WorldObjects = new List<WorldObjectDef>();

        public ClockSettings Settings = new ClockSettings();

        private readonly Dictionary<string, ItemDef> _items = new Dictionary<string, ItemDef>();
        private readonly Dictionary<string, RecipeDef> _recipes = new Dictionary<string, RecipeDef>();
        private readonly Dictionary<string, ClueDef> _clues = new Dictionary<string, ClueDef>();
        private readonly Dictionary<string, NpcDef> _npcs = new Dictionary<string, NpcDef>();
        private readonly Dictionary<string, QuestDef> _quests = new Dictionary<string, QuestDef>();
        private readonly Dictionary<string, DialogueDef> _dialogues = new Dictionary<string, DialogueDef>();
        private readonly Dictionary<string, WorldObjectDef> _world = new Dictionary<string, WorldObjectDef>();

        public ItemDef Item(string id) => Lookup(_items, id);
        public RecipeDef Recipe(string id) => Lookup(_recipes, id);
        public ClueDef Clue(string id) => Lookup(_clues, id);
        public NpcDef Npc(string id) => Lookup(_npcs, id);
        public QuestDef Quest(string id) => Lookup(_quests, id);
        public DialogueDef Dialogue(string id) => Lookup(_dialogues, id);
        public WorldObjectDef WorldObject(string id) => Lookup(_world, id);

        private static T Lookup<T>(Dictionary<string, T> map, string id) where T : class
            => id != null && map.TryGetValue(id, out var v) ? v : null;

        // ---- loading -----------------------------------------------------------

        /// <summary>Loads every *.json under a directory, recursively.</summary>
        public static ContentDatabase LoadFromDirectory(string root)
        {
            if (!Directory.Exists(root))
                throw new ContentException($"Content directory not found: {root}");

            var db = new ContentDatabase();
            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);

            foreach (var file in files)
            {
                try
                {
                    db.MergeJson(File.ReadAllText(file));
                }
                catch (ContentException e)
                {
                    throw new ContentException($"{Path.GetFileName(file)}: {e.Message}");
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    throw new ContentException($"{Path.GetFileName(file)}: malformed JSON — {e.Message}");
                }
            }

            db.Index();
            return db;
        }

        public static ContentDatabase LoadFromTexts(IEnumerable<string> jsonTexts)
        {
            var db = new ContentDatabase();
            foreach (var text in jsonTexts) db.MergeJson(text);
            db.Index();
            return db;
        }

        public void MergeJson(string json)
        {
            var root = JObject.Parse(json);

            foreach (var t in Arr(root, "items")) Items.Add(ParseItem(t));
            foreach (var t in Arr(root, "recipes")) Recipes.Add(ParseRecipe(t));
            foreach (var t in Arr(root, "clues")) Clues.Add(ParseClue(t));
            foreach (var t in Arr(root, "npcs")) Npcs.Add(ParseNpc(t));
            foreach (var t in Arr(root, "quests")) Quests.Add(ParseQuest(t));
            foreach (var t in Arr(root, "dialogue")) Dialogues.Add(ParseDialogue(t));
            foreach (var t in Arr(root, "world")) WorldObjects.Add(ParseWorldObject(t));

            if (root["settings"] is JObject s) Settings = ParseSettings(s);
        }

        /// <summary>Rebuilds id lookups. Called after loading; safe to call again.</summary>
        public void Index()
        {
            Fill(_items, Items, d => d.Id, "item");
            Fill(_recipes, Recipes, d => d.Id, "recipe");
            Fill(_clues, Clues, d => d.Id, "clue");
            Fill(_npcs, Npcs, d => d.Id, "npc");
            Fill(_quests, Quests, d => d.Id, "quest");
            Fill(_dialogues, Dialogues, d => d.Id, "dialogue");
            Fill(_world, WorldObjects, d => d.Id, "world object");
        }

        private static void Fill<T>(Dictionary<string, T> map, List<T> list, Func<T, string> id, string label)
        {
            map.Clear();
            foreach (var item in list)
            {
                var key = id(item);
                if (string.IsNullOrEmpty(key))
                    throw new ContentException($"A {label} has no id.");
                if (map.ContainsKey(key))
                    throw new ContentException($"Duplicate {label} id: {key}");
                map[key] = item;
            }
        }

        private static IEnumerable<JToken> Arr(JObject root, string key)
        {
            if (root[key] is JArray arr) return arr;
            return System.Linq.Enumerable.Empty<JToken>();
        }

        // ---- def parsers -------------------------------------------------------

        private static string Str(JToken t, string key, string fallback = null)
            => t?[key] != null && t[key].Type != JTokenType.Null ? t[key].Value<string>() : fallback;

        private static int Int(JToken t, string key, int fallback)
            => t?[key] != null && t[key].Type != JTokenType.Null ? t[key].Value<int>() : fallback;

        private static bool Bool(JToken t, string key, bool fallback)
            => t?[key] != null && t[key].Type != JTokenType.Null ? t[key].Value<bool>() : fallback;

        private static ItemDef ParseItem(JToken t) => new ItemDef
        {
            Id = Str(t, "id"),
            Name = Str(t, "name"),
            Description = Str(t, "description"),
            MaxStack = Int(t, "max_stack", 99)
        };

        private static RecipeDef ParseRecipe(JToken t)
        {
            var r = new RecipeDef
            {
                Id = Str(t, "id"),
                Name = Str(t, "name"),
                Family = Str(t, "family"),
                Station = Str(t, "station"),
                OutputItem = Str(t, "output_item"),
                OutputCount = Int(t, "output_count", 1)
            };

            if (t["inputs"] is JObject inputs)
                foreach (var p in inputs.Properties())
                    r.Inputs[p.Name] = p.Value.Value<int>();

            return r;
        }

        private static ClueDef ParseClue(JToken t) => new ClueDef
        {
            Id = Str(t, "id"),
            Title = Str(t, "title"),
            Text = Str(t, "text"),
            Thread = Str(t, "thread"),
            Order = Int(t, "order", 0)
        };

        private static NpcDef ParseNpc(JToken t)
        {
            var n = new NpcDef
            {
                Id = Str(t, "id"),
                Name = Str(t, "name"),
                Region = Str(t, "region"),
                CapabilityKind = Str(t, "capability_kind", "none")
            };

            if (t["schedule"] is JArray sched)
                foreach (var b in sched)
                    n.Schedule.Add(new ScheduleBlock
                    {
                        From = Str(b, "from", "dawn"),
                        To = Str(b, "to", "night"),
                        Waypoint = Str(b, "waypoint"),
                        When = ParseCondition(b["when"])
                    });

            if (t["dialogue_entries"] is JArray entries)
                foreach (var e in entries)
                    n.DialogueEntries.Add(new DialogueEntry
                    {
                        When = ParseCondition(e["when"]),
                        Dialogue = Str(e, "dialogue"),
                        Node = Str(e, "node", "start")
                    });

            return n;
        }

        private static QuestDef ParseQuest(JToken t)
        {
            var q = new QuestDef
            {
                Id = Str(t, "id"),
                Title = Str(t, "title"),
                Summary = Str(t, "summary"),
                Shape = Str(t, "shape"),
                Giver = Str(t, "giver"),
                StartWhen = ParseCondition(t["start_when"]),
                OnComplete = ParseEffects(t["on_complete"])
            };

            if (t["steps"] is JArray steps)
                foreach (var s in steps)
                {
                    var step = new QuestStep
                    {
                        Id = Str(s, "id"),
                        Tracker = Str(s, "tracker"),
                        OnComplete = ParseEffects(s["on_complete"])
                    };

                    if (s["completion"] is JArray sets)
                        foreach (var c in sets)
                            step.Completion.Add(new CompletionSet
                            {
                                Id = Str(c, "id"),
                                Documented = Bool(c, "documented", true),
                                When = ParseCondition(c["when"])
                            });

                    q.Steps.Add(step);
                }

            return q;
        }

        private static DialogueDef ParseDialogue(JToken t)
        {
            var d = new DialogueDef { Id = Str(t, "id") };

            if (t["nodes"] is JArray nodes)
                foreach (var n in nodes)
                {
                    var node = new DialogueNode
                    {
                        Id = Str(n, "id"),
                        Speaker = Str(n, "speaker"),
                        Goto = Str(n, "goto"),
                        OnEnter = ParseEffects(n["on_enter"])
                    };

                    if (n["lines"] is JArray lines)
                        foreach (var l in lines) node.Lines.Add(l.Value<string>());

                    if (n["choices"] is JArray choices)
                        foreach (var c in choices)
                            node.Choices.Add(new DialogueChoice
                            {
                                Text = Str(c, "text"),
                                When = ParseCondition(c["when"]),
                                Effects = ParseEffects(c["effects"]),
                                Goto = Str(c, "goto")
                            });

                    d.Nodes.Add(node);
                }

            return d;
        }

        private static WorldObjectDef ParseWorldObject(JToken t)
        {
            var w = new WorldObjectDef
            {
                Id = Str(t, "id"),
                Kind = Str(t, "kind"),
                Region = Str(t, "region"),
                VisibleWhen = ParseCondition(t["visible_when"]),
                BlocksWhen = ParseCondition(t["blocks_when"]),
                InteractWhen = ParseCondition(t["interact_when"]),
                InteractLabel = Str(t, "interact_label"),
                Text = Str(t, "text"),
                OnInteract = ParseEffects(t["on_interact"])
            };

            if (t["position"] is JArray p && p.Count == 3)
                w.Position = new[] { p[0].Value<float>(), p[1].Value<float>(), p[2].Value<float>() };

            return w;
        }

        private static ClockSettings ParseSettings(JObject s) => new ClockSettings
        {
            DayLengthMinutes = Int(s, "day_length_minutes", 1440),
            DawnStart = Int(s, "dawn_start", 300),
            DayStart = Int(s, "day_start", 480),
            DuskStart = Int(s, "dusk_start", 1140),
            NightStart = Int(s, "night_start", 1260),
            StartMinute = Int(s, "start_minute", 420),
            SecondsPerMinute = s["seconds_per_minute"] != null ? s["seconds_per_minute"].Value<float>() : 0.5f
        };

        // ---- condition / effect parsers ---------------------------------------

        public static List<Effect> ParseEffects(JToken t)
        {
            var list = new List<Effect>();
            if (t is JArray arr)
                foreach (var e in arr) list.Add(ParseEffect(e));
            return list;
        }

        public static Condition ParseCondition(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;

            if (t.Type == JTokenType.Boolean)
                return new AlwaysCondition { Value = t.Value<bool>() };

            var type = Str(t, "type");
            switch (type)
            {
                case "always":
                    return new AlwaysCondition { Value = Bool(t, "value", true) };

                case "flag":
                    return new FlagCondition { Key = Str(t, "key"), Value = Str(t, "value", "true") };

                case "has_item":
                    return new HasItemCondition { Item = Str(t, "item"), Count = Int(t, "count", 1) };

                case "quest_state":
                    return new QuestStateCondition
                    {
                        Quest = Str(t, "quest"),
                        State = ParseQuestState(Str(t, "state", "completed"))
                    };

                case "quest_step":
                    return new QuestStepCondition
                    {
                        Quest = Str(t, "quest"),
                        Step = Str(t, "step"),
                        Compare = ParseCompare(Str(t, "compare", "at"))
                    };

                case "clue_known":
                    return new ClueKnownCondition { Clue = Str(t, "clue") };

                case "recipe_known":
                    return new RecipeKnownCondition { Recipe = Str(t, "recipe") };

                case "time_of_day":
                    {
                        var c = new TimeOfDayCondition();
                        if (t["phases"] is JArray phases)
                            foreach (var p in phases) c.Phases.Add(p.Value<string>());
                        else if (Str(t, "phase") != null)
                            c.Phases.Add(Str(t, "phase"));
                        return c;
                    }

                case "all":
                    {
                        var c = new AllCondition();
                        foreach (var sub in SubConditions(t)) c.Of.Add(sub);
                        return c;
                    }

                case "any":
                    {
                        var c = new AnyCondition();
                        foreach (var sub in SubConditions(t)) c.Of.Add(sub);
                        return c;
                    }

                case "not":
                    return new NotCondition { Of = ParseCondition(t["of"]) };

                default:
                    throw new ContentException(
                        $"Unknown condition type '{type}'. The condition vocabulary is closed by design — " +
                        "see docs/systems-README.md before adding one.");
            }
        }

        private static IEnumerable<Condition> SubConditions(JToken t)
        {
            var list = new List<Condition>();
            if (t["of"] is JArray arr)
                foreach (var sub in arr) list.Add(ParseCondition(sub));
            return list;
        }

        public static Effect ParseEffect(JToken t)
        {
            var type = Str(t, "type");
            switch (type)
            {
                case "give_item":
                    return new GiveItemEffect { Item = Str(t, "item"), Count = Int(t, "count", 1) };

                case "take_item":
                    return new TakeItemEffect { Item = Str(t, "item"), Count = Int(t, "count", 1) };

                case "set_flag":
                    return new SetFlagEffect { Key = Str(t, "key"), Value = Str(t, "value", "true") };

                case "cycle_flag":
                    {
                        var e = new CycleFlagEffect { Key = Str(t, "key") };
                        if (t["values"] is JArray values)
                            foreach (var v in values) e.Values.Add(v.Value<string>());
                        if (e.Values.Count == 0)
                            throw new ContentException($"cycle_flag on '{e.Key}' has no values.");
                        return e;
                    }

                case "start_quest":
                    return new StartQuestEffect { Quest = Str(t, "quest") };

                case "advance_quest":
                    return new AdvanceQuestEffect { Quest = Str(t, "quest") };

                case "complete_quest":
                    return new CompleteQuestEffect { Quest = Str(t, "quest") };

                case "learn_recipe":
                    return new LearnRecipeEffect { Recipe = Str(t, "recipe"), Family = Str(t, "family") };

                case "learn_clue":
                    return new LearnClueEffect { Clue = Str(t, "clue") };

                case "set_time":
                    return new SetTimeEffect { Phase = Str(t, "phase", "dawn") };

                case "advance_time":
                    return new AdvanceTimeEffect { Minutes = Int(t, "minutes", 60) };

                default:
                    throw new ContentException(
                        $"Unknown effect type '{type}'. The effect vocabulary is closed by design — " +
                        "see docs/systems-README.md before adding one.");
            }
        }

        private static QuestState ParseQuestState(string s)
        {
            switch (s)
            {
                case "notstarted":
                case "not_started": return QuestState.NotStarted;
                case "active": return QuestState.Active;
                case "completed": return QuestState.Completed;
                default: throw new ContentException($"Unknown quest state '{s}'.");
            }
        }

        private static StepCompare ParseCompare(string s)
        {
            switch (s)
            {
                case "at": return StepCompare.At;
                case "at_or_past": return StepCompare.AtOrPast;
                case "past": return StepCompare.Past;
                default: throw new ContentException($"Unknown step compare '{s}'.");
            }
        }
    }
}

using Newtonsoft.Json.Linq;

namespace HiddenValley.Core
{
    /// <summary>
    /// Save and load. The format is a flat set of id-keyed maps and nothing else — no
    /// field is named after a specific quest, NPC or item.
    ///
    /// The consequence worth stating: adding content never changes this format, so a save
    /// written before NPC #4 existed loads afterwards without migration. Content that
    /// disappeared is skipped on load rather than throwing, so removing a quest mid-project
    /// does not brick a tester's save.
    /// </summary>
    public static class SaveSystem
    {
        public const int Version = 1;

        public static string Save(GameState state)
        {
            var flags = new JObject();
            foreach (var kv in state.Flags) flags[kv.Key] = kv.Value;

            var inventory = new JObject();
            foreach (var kv in state.Inventory) inventory[kv.Key] = kv.Value;

            var quests = new JObject();
            foreach (var kv in state.Quests)
            {
                if (kv.Value.State == QuestState.NotStarted) continue;
                quests[kv.Key] = new JObject
                {
                    ["state"] = StateName(kv.Value.State),
                    ["step"] = kv.Value.StepIndex
                };
            }

            var clues = new JArray();
            foreach (var c in state.Clues) clues.Add(c);

            var recipes = new JArray();
            foreach (var r in state.Recipes) recipes.Add(r);

            var root = new JObject
            {
                ["version"] = Version,
                ["day"] = state.Clock.Day,
                ["minute"] = state.Clock.Minute,
                ["flags"] = flags,
                ["inventory"] = inventory,
                ["quests"] = quests,
                ["clues"] = clues,
                ["recipes"] = recipes
            };

            return root.ToString();
        }

        public static GameState Load(string json, ContentDatabase content)
        {
            var root = JObject.Parse(json);

            int version = root["version"]?.Value<int>() ?? 0;
            if (version > Version)
                throw new ContentException(
                    $"Save file is version {version}; this build understands up to {Version}.");

            var clock = new Clock(
                content.Settings,
                root["day"]?.Value<int>() ?? 0,
                root["minute"]?.Value<int>() ?? -1);

            var state = new GameState(content, clock);

            if (root["flags"] is JObject flags)
                foreach (var p in flags.Properties())
                    state.Flags[p.Name] = p.Value.Value<string>();

            if (root["inventory"] is JObject inventory)
                foreach (var p in inventory.Properties())
                {
                    // Skip items this build no longer defines rather than failing the load.
                    if (content.Item(p.Name) == null) continue;
                    state.Inventory[p.Name] = p.Value.Value<int>();
                }

            if (root["quests"] is JObject quests)
                foreach (var p in quests.Properties())
                {
                    var quest = content.Quest(p.Name);
                    if (quest == null) continue;

                    int step = p.Value["step"]?.Value<int>() ?? 0;
                    if (step < 0) step = 0;
                    if (step > quest.Steps.Count) step = quest.Steps.Count;

                    state.Quests[p.Name] = new QuestProgress
                    {
                        State = ParseState(p.Value["state"]?.Value<string>()),
                        StepIndex = step
                    };
                }

            if (root["clues"] is JArray clues)
                foreach (var c in clues)
                {
                    var id = c.Value<string>();
                    if (content.Clue(id) != null) state.Clues.Add(id);
                }

            if (root["recipes"] is JArray recipes)
                foreach (var r in recipes)
                {
                    var id = r.Value<string>();
                    if (content.Recipe(id) != null) state.Recipes.Add(id);
                }

            return state;
        }

        private static string StateName(QuestState s)
        {
            switch (s)
            {
                case QuestState.Active: return "active";
                case QuestState.Completed: return "completed";
                default: return "notstarted";
            }
        }

        private static QuestState ParseState(string s)
        {
            switch (s)
            {
                case "active": return QuestState.Active;
                case "completed": return QuestState.Completed;
                default: return QuestState.NotStarted;
            }
        }
    }
}

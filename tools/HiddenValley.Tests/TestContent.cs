using System;
using System.IO;
using HiddenValley.Core;
using Xunit;

namespace HiddenValley.Tests
{
    /// <summary>Loads the real shipped content, so these tests fail when authored data breaks.</summary>
    public static class TestContent
    {
        private static readonly Lazy<ContentDatabase> Lazy =
            new Lazy<ContentDatabase>(() => ContentDatabase.LoadFromDirectory(ContentPath));

        public static ContentDatabase Db => Lazy.Value;

        public static string RepoRoot
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets")))
                    dir = dir.Parent;

                if (dir == null)
                    throw new DirectoryNotFoundException("Could not find the repo root (no Assets/ above the test binary).");

                return dir.FullName;
            }
        }

        public static string ContentPath => Path.Combine(RepoRoot, "Assets", "StreamingAssets", "Content");

        /// <summary>
        /// Talks to an NPC and picks the first available choice whose text starts with the
        /// given prefix. Throws with the available options listed, so a content edit that
        /// removes a line fails loudly rather than silently skipping a step.
        /// </summary>
        public static void Say(Game game, string npcId, string choicePrefix)
        {
            var session = game.Dialogue.Begin(npcId);
            Assert.True(session != null, $"{npcId} had no dialogue entry that matched current state.");

            var choices = game.Dialogue.AvailableChoices(session);
            for (int i = 0; i < choices.Count; i++)
            {
                if (!choices[i].Text.StartsWith(choicePrefix, StringComparison.Ordinal)) continue;
                game.Dialogue.Choose(session, i);
                return;
            }

            var offered = choices.Count == 0 ? "(none)" : string.Join(" | ", choices.ConvertAll(c => c.Text));
            throw new Xunit.Sdk.XunitException(
                $"{npcId} did not offer a choice starting with \"{choicePrefix}\". Offered: {offered}");
        }

        /// <summary>Opens a conversation and closes it, which is enough to fire on_enter effects.</summary>
        public static void Greet(Game game, string npcId)
        {
            var session = game.Dialogue.Begin(npcId);
            Assert.True(session != null, $"{npcId} had no dialogue entry that matched current state.");
        }

        public static void Interact(Game game, string worldObjectId)
        {
            Assert.True(game.World.Interact(worldObjectId),
                $"'{worldObjectId}' was not interactable when the playthrough expected it to be.");
        }
    }
}

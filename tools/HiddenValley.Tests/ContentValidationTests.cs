using HiddenValley.Core;
using Xunit;

namespace HiddenValley.Tests
{
    /// <summary>
    /// These assert the build brief's design constraints against the shipped content.
    /// They are not unit tests of the engine — they are the brief, executable.
    /// </summary>
    public class ContentValidationTests
    {
        [Fact]
        public void Shipped_content_passes_the_validator()
        {
            var report = ContentValidator.Validate(TestContent.Db);
            Assert.True(report.Ok, report.ToString());
        }

        [Fact]
        public void Slice_has_exactly_the_scope_the_brief_allows()
        {
            var db = TestContent.Db;

            Assert.Equal(8, db.Quests.Count);

            // Three named NPCs, plus Pip the companion, who grants no capability.
            Assert.Equal(3, db.Npcs.FindAll(n => n.CapabilityKind != "none").Count);
            Assert.Single(db.Npcs.FindAll(n => n.Id == "npc.pip"));
        }

        [Fact]
        public void No_more_than_two_quests_share_a_shape()
        {
            var counts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var q in TestContent.Db.Quests)
            {
                counts.TryGetValue(q.Shape, out var n);
                counts[q.Shape] = n + 1;
            }

            foreach (var kv in counts)
                Assert.True(kv.Value <= 2, $"{kv.Value} quests share shape '{kv.Key}'.");
        }

        [Fact]
        public void Exactly_one_quest_is_solvable_in_a_way_its_text_does_not_describe()
        {
            var found = new System.Collections.Generic.List<string>();

            foreach (var q in TestContent.Db.Quests)
                foreach (var step in q.Steps)
                    foreach (var set in step.Completion)
                        if (!set.Documented && !found.Contains(q.Id)) found.Add(q.Id);

            Assert.Single(found);
        }

        [Fact]
        public void The_three_npcs_differ_at_the_systems_level()
        {
            var kinds = new System.Collections.Generic.HashSet<string>();
            foreach (var npc in TestContent.Db.Npcs)
                if (npc.CapabilityKind != "none") kinds.Add(npc.CapabilityKind);

            Assert.Contains("recipe_family", kinds);
            Assert.Contains("world_contents", kinds);
            Assert.Contains("traversal", kinds);
            Assert.Equal(3, kinds.Count);
        }
    }
}

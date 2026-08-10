using HiddenValley.Core;
using Xunit;

namespace HiddenValley.Tests
{
    /// <summary>
    /// The cue effect is the channel content uses to author presentation moments
    /// (stingers, Pip reactions). It must mutate nothing but the transient list,
    /// bump the revision so views wake up, and never leak into a save.
    /// </summary>
    public class CueTests
    {
        private static ContentDatabase Db() => ContentDatabase.LoadFromTexts(new[]
        {
            @"{ ""items"": [ { ""id"": ""item.x"", ""name"": ""X"" } ] }"
        });

        [Fact]
        public void Cue_effect_parses_and_emits()
        {
            var db = ContentDatabase.LoadFromTexts(new[]
            {
                @"{ ""world"": [ {
                    ""id"": ""world.bell"", ""kind"": ""bell"", ""region"": ""r"",
                    ""interact_label"": ""Ring"",
                    ""on_interact"": [ { ""type"": ""cue"", ""id"": ""sting.mystery"" } ]
                } ] }"
            });

            var game = Game.NewGame(db);
            int before = game.State.Revision;

            Assert.True(game.World.Interact("world.bell"));
            Assert.Contains("sting.mystery", game.State.PendingCues);
            Assert.True(game.State.Revision > before, "EmitCue must bump Revision so views notice.");
        }

        [Fact]
        public void Cues_do_not_survive_a_save_round_trip()
        {
            var game = Game.NewGame(Db());
            game.State.EmitCue("sting.mystery");

            var restored = Game.FromSave(game.Save(), game.Content);
            Assert.Empty(restored.State.PendingCues);
        }

        [Fact]
        public void Learning_a_clue_bumps_revision()
        {
            var db = ContentDatabase.LoadFromTexts(new[]
            {
                @"{ ""clues"": [ { ""id"": ""clue.a"", ""title"": ""A"", ""text"": ""t"", ""thread"": ""m"" } ] }"
            });

            var game = Game.NewGame(db);
            int before = game.State.Revision;

            new LearnClueEffect { Clue = "clue.a" }.Apply(game.State);

            Assert.True(game.State.KnowsClue("clue.a"));
            Assert.True(game.State.Revision > before,
                "Clue discovery must bump Revision — bare Clues.Add hid new clues from every view.");
        }
    }
}

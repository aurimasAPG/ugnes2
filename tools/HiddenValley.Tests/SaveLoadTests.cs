using HiddenValley.Core;
using Xunit;
using static HiddenValley.Tests.TestContent;

namespace HiddenValley.Tests
{
    public class SaveLoadTests
    {
        [Fact]
        public void Save_and_reload_resumes_a_quest_at_the_correct_step()
        {
            var game = Game.NewGame(Db);

            Say(game, "npc.vesk", "What kind of sand?");
            Interact(game, "world.sand_1");
            Interact(game, "world.sand_2");

            // Mid-quest: gathered two of three, so still on step 0.
            Assert.Equal(QuestState.Active, game.State.QuestStateOf("quest.kiln_ash"));
            Assert.Equal(0, game.State.QuestStepIndex("quest.kiln_ash"));

            var reloaded = Game.FromSave(game.Save(), Db);

            Assert.Equal(QuestState.Active, reloaded.State.QuestStateOf("quest.kiln_ash"));
            Assert.Equal(0, reloaded.State.QuestStepIndex("quest.kiln_ash"));
            Assert.Equal(2, reloaded.State.ItemCount("item.white_sand"));
            Assert.Equal("gather", reloaded.State.CurrentStep("quest.kiln_ash").Id);

            // And the quest still finishes normally from the reloaded state.
            Interact(reloaded, "world.sand_3");
            Assert.Equal("deliver", reloaded.State.CurrentStep("quest.kiln_ash").Id);

            Say(reloaded, "npc.vesk", "Three measures");
            Assert.Equal(QuestState.Completed, reloaded.State.QuestStateOf("quest.kiln_ash"));
        }

        [Fact]
        public void Round_trip_preserves_flags_inventory_clues_recipes_and_clock()
        {
            var game = Game.NewGame(Db);

            game.Apply(new Effect[]
            {
                new SetFlagEffect { Key = "gate.a", Value = "2" },
                new GiveItemEffect { Item = "item.resin", Count = 4 },
                new LearnClueEffect { Clue = "clue.scorched_bark" },
                new LearnRecipeEffect { Family = "lensmithing" },
                new AdvanceTimeEffect { Minutes = 500 }
            });

            var before = game.State;
            var after = Game.FromSave(game.Save(), Db).State;

            Assert.Equal("2", after.GetFlag("gate.a"));
            Assert.Equal(4, after.ItemCount("item.resin"));
            Assert.True(after.KnowsClue("clue.scorched_bark"));
            Assert.Equal(4, after.Recipes.Count);
            Assert.Equal(before.Clock.Day, after.Clock.Day);
            Assert.Equal(before.Clock.Minute, after.Clock.Minute);
        }

        [Fact]
        public void Deep_save_survives_a_full_playthrough_state()
        {
            var game = Game.NewGame(Db);
            game.Apply(new Effect[]
            {
                new SetFlagEffect { Key = "pool.drained", Value = "true" },
                new SetFlagEffect { Key = "orrel.tradepost", Value = "true" }
            });

            var after = Game.FromSave(game.Save(), Db);

            Assert.True(after.World.IsVisible("world.trade_post"));
            Assert.False(after.World.Blocks(Db.WorldObject("world.pool_water")));
        }

        /// <summary>
        /// The property that makes the save format survive content growth: unknown ids are
        /// dropped rather than fatal. A tester's save from last week must still open after a
        /// quest is renamed or removed.
        /// </summary>
        [Fact]
        public void Content_that_no_longer_exists_is_skipped_not_fatal()
        {
            const string stale = @"{
                ""version"": 1,
                ""day"": 2,
                ""minute"": 600,
                ""flags"": { ""some.old.flag"": ""true"" },
                ""inventory"": { ""item.deleted_thing"": 5, ""item.resin"": 2 },
                ""quests"": { ""quest.deleted"": { ""state"": ""active"", ""step"": 3 } },
                ""clues"": [ ""clue.deleted"" ],
                ""recipes"": [ ""recipe.deleted"", ""recipe.reading_lens"" ]
            }";

            var state = SaveSystem.Load(stale, Db);

            Assert.Equal(0, state.ItemCount("item.deleted_thing"));
            Assert.Equal(2, state.ItemCount("item.resin"));
            Assert.Equal(QuestState.NotStarted, state.QuestStateOf("quest.deleted"));
            Assert.False(state.KnowsRecipe("recipe.deleted"));
            Assert.True(state.KnowsRecipe("recipe.reading_lens"));
            Assert.Equal("true", state.GetFlag("some.old.flag"));
            Assert.Equal(2, state.Clock.Day);
        }

        [Fact]
        public void A_step_index_beyond_the_quest_is_clamped_rather_than_crashing()
        {
            const string corrupt = @"{
                ""version"": 1,
                ""quests"": { ""quest.kiln_ash"": { ""state"": ""active"", ""step"": 99 } }
            }";

            var state = SaveSystem.Load(corrupt, Db);
            Assert.Equal(Db.Quest("quest.kiln_ash").Steps.Count, state.QuestStepIndex("quest.kiln_ash"));
            Assert.Null(state.CurrentStep("quest.kiln_ash"));
        }

        [Fact]
        public void A_save_from_a_newer_build_is_refused_with_a_clear_message()
        {
            var ex = Assert.Throws<ContentException>(
                () => SaveSystem.Load(@"{""version"": 99}", Db));

            Assert.Contains("version 99", ex.Message);
        }
    }
}

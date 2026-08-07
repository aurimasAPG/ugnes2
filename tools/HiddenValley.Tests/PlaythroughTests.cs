using HiddenValley.Core;
using Xunit;
using static HiddenValley.Tests.TestContent;

namespace HiddenValley.Tests
{
    /// <summary>
    /// Walks the whole slice start to finish through the same API the game uses.
    ///
    /// This is the cheapest soft-lock detector available before there is a device to play
    /// on: it proves the dependency graph across all eight quests, the puzzle chain and the
    /// mystery actually closes. It does not prove the slice is fun, and nothing here
    /// substitutes for the play-feel pass.
    /// </summary>
    public class PlaythroughTests
    {
        private static Game Start() => Game.NewGame(Db);

        [Fact]
        public void Whole_slice_completes_without_a_softlock()
        {
            var game = Start();

            // --- The mystery starts by being noticed, not by being given. -------------
            Interact(game, "world.scorched_bark");
            Assert.Equal(QuestState.Active, game.State.QuestStateOf("quest.quietday"));

            // --- Vesk: the recipe family ---------------------------------------------
            Say(game, "npc.vesk", "What kind of sand?");
            Interact(game, "world.sand_1");
            Interact(game, "world.sand_2");
            Interact(game, "world.sand_3");
            Say(game, "npc.vesk", "Three measures");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.kiln_ash"));
            Assert.Equal(4, game.Crafting.KnownRecipes().Count);

            // The ash conversation is available the moment both halves are known.
            Greet(game, "npc.vesk");
            Assert.True(game.State.KnowsClue("clue.kiln_ash"));

            // --- Vesk: craft to spec --------------------------------------------------
            Say(game, "npc.vesk", "So make something");
            Interact(game, "world.sand_4");
            Assert.True(game.Crafting.Craft("recipe.glass_blank"));
            Assert.True(game.Crafting.Craft("recipe.reading_lens"));
            Say(game, "npc.vesk", "Ground it");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.lens"));

            // --- Coll: repair, then traversal ---------------------------------------
            Say(game, "npc.coll", "I'll do it.");
            Interact(game, "world.grate_debris");
            Interact(game, "world.iron_scrap");
            Interact(game, "world.grate");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.grate"));
            Assert.Equal(QuestState.Active, game.State.QuestStateOf("quest.undersluice"));

            // The warm-channel clue comes from Coll without him knowing it is a clue.
            Greet(game, "npc.coll");
            Assert.True(game.State.KnowsClue("clue.warm_channel"));

            Interact(game, "world.setting_plate");

            Interact(game, "world.gate_a");
            Interact(game, "world.gate_a");
            Interact(game, "world.gate_a");
            Interact(game, "world.gate_b");
            Interact(game, "world.gate_c");
            Interact(game, "world.gate_c");
            Interact(game, "world.gate_c");
            Interact(game, "world.gate_c");

            Assert.Equal("2", game.State.GetFlag("gate.a"));
            Assert.Equal("0", game.State.GetFlag("gate.b"));
            Assert.Equal("3", game.State.GetFlag("gate.c"));

            // Dawn only. Sleeping is the intended way to get there.
            Interact(game, "world.bed");
            Assert.Equal("dawn", game.State.Clock.PhaseName);
            Interact(game, "world.head_gate");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.undersluice"));
            Assert.True(game.State.GetBoolFlag("shelf.lower.open"));

            // --- The mystery resolves into the space the puzzle opened ---------------
            Interact(game, "world.second_kiln");
            Interact(game, "world.kiln_hearth");
            Say(game, "npc.coll", "Someone is keeping");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.quietday"));
            Assert.True(game.State.GetBoolFlag("mystery.resolved"));

            // --- Orrel: the world gains contents -------------------------------------
            Say(game, "npc.orrel", "Why twice?");

            Interact(game, "world.bed");
            Interact(game, "world.moss_patch_dawn");
            Interact(game, "world.bench_dusk");
            Interact(game, "world.moss_patch_dusk");
            Say(game, "npc.orrel", "Both counted");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.countings"));
            Assert.True(game.World.IsVisible("world.trade_post"));

            Say(game, "npc.orrel", "Three slips.");
            Interact(game, "world.sunmoss_1");
            Interact(game, "world.sunmoss_2");
            Interact(game, "world.sunmoss_3");
            Interact(game, "world.trade_post");
            Interact(game, "world.trade_post");
            Interact(game, "world.trade_post");
            Say(game, "npc.orrel", "Three of them.");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.ledger"));

            // --- The escort exists only because the traversal gate opened ------------
            Assert.Equal(QuestState.Active, game.State.QuestStateOf("quest.cart"));
            Interact(game, "world.handcart");
            Interact(game, "world.cart_stand");

            foreach (var quest in Db.Quests)
                Assert.Equal(QuestState.Completed, game.State.QuestStateOf(quest.Id));
        }

        [Fact]
        public void The_undocumented_solution_completes_the_lens_quest()
        {
            var game = Start();

            Say(game, "npc.vesk", "What kind of sand?");
            Interact(game, "world.sand_1");
            Interact(game, "world.sand_2");
            Interact(game, "world.sand_3");
            Say(game, "npc.vesk", "Three measures");
            Say(game, "npc.vesk", "So make something");

            // Never crafts anything. Just looks in the pool.
            Interact(game, "world.drowned_lens");
            Say(game, "npc.vesk", "I found this in the pool.");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.lens"));
            Assert.Equal(0, game.State.ItemCount("item.reading_lens"));

            // The route the player took is recorded, so the play-feel pass can tell
            // whether any tester ever found it.
            Assert.Equal("drowned", game.State.GetFlag("solved.quest.lens.make"));

            // And it stays valid downstream — the drowned lens reads the plate too.
            Say(game, "npc.coll", "I'll do it.");
            Interact(game, "world.grate_debris");
            Interact(game, "world.iron_scrap");
            Interact(game, "world.grate");
            Interact(game, "world.setting_plate");

            Assert.True(game.State.GetBoolFlag("plate.read"));
        }

        [Fact]
        public void Traversal_is_gated_until_the_pool_is_drained()
        {
            var game = Start();

            var water = Db.WorldObject("world.pool_water");
            Assert.True(game.World.Blocks(water));

            game.Apply(new SetFlagEffect { Key = "pool.drained", Value = "true" });

            Assert.False(game.World.Blocks(water));
        }

        [Fact]
        public void Orrels_resource_nodes_do_not_exist_before_her_quest()
        {
            var game = Start();

            Assert.False(game.World.IsVisible("world.sunmoss_1"));
            Assert.False(game.World.IsVisible("world.trade_post"));

            game.Apply(new SetFlagEffect { Key = "orrel.tradepost", Value = "true" });

            Assert.True(game.World.IsVisible("world.sunmoss_1"));
            Assert.True(game.World.IsVisible("world.trade_post"));
        }

        [Fact]
        public void Head_gate_refuses_outside_dawn_even_with_the_gates_set()
        {
            var game = Start();

            game.Apply(new Effect[]
            {
                new SetFlagEffect { Key = "grate.mended", Value = "true" },
                new SetFlagEffect { Key = "gate.a", Value = "2" },
                new SetFlagEffect { Key = "gate.b", Value = "0" },
                new SetFlagEffect { Key = "gate.c", Value = "3" }
            });

            game.State.Clock.AdvanceToPhase("day");
            Assert.False(game.World.Interact("world.head_gate"));

            game.State.Clock.AdvanceToPhase("dawn");
            Assert.True(game.World.Interact("world.head_gate"));
        }
    }
}

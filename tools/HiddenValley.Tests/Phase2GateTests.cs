using HiddenValley.Core;
using Xunit;
using static HiddenValley.Tests.TestContent;

namespace HiddenValley.Tests
{
    /// <summary>
    /// The Phase 2 gate, executable.
    ///
    /// The gate asks: can a new NPC with a new two-step quest and a new inventory item be
    /// added to the running game by editing data files only, with the C# diff empty, and
    /// does save-and-reload resume that quest at the correct step?
    ///
    /// docs/phase2-gate/npc4.json is the entire change. These tests exercise it through the
    /// same engine the shipped content uses. The "empty diff" half of the gate is not
    /// something a test can assert about itself — it is checked by tools/phase2-gate.sh,
    /// which diffs runtime C# between the baseline commit and the commit that adds the file.
    /// </summary>
    public class Phase2GateTests
    {
        private static ContentDatabase Overlaid => WithOverlay("docs/phase2-gate/npc4.json");

        [Fact]
        public void A_fourth_npc_added_only_as_data_loads_and_validates()
        {
            var db = Overlaid;

            Assert.NotNull(db.Npc("npc.sedge"));
            Assert.NotNull(db.Quest("quest.thatch"));
            Assert.NotNull(db.Item("item.reed_bundle"));

            var report = ContentValidator.Validate(db);
            Assert.True(report.Ok, report.ToString());
        }

        [Fact]
        public void The_new_quest_is_playable_end_to_end()
        {
            var db = Overlaid;
            var game = Game.NewGame(db);

            Say(game, "npc.sedge", "I'll cut them.");
            Assert.Equal(QuestState.Active, game.State.QuestStateOf("quest.thatch"));

            Interact(game, "world.reeds_1");
            Interact(game, "world.reeds_2");
            Assert.Equal("deliver", game.State.CurrentStep("quest.thatch").Id);

            Say(game, "npc.sedge", "Two bundles");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.thatch"));
            Assert.Equal(0, game.State.ItemCount("item.reed_bundle"));
            Assert.Equal(1, game.State.ItemCount("item.ledger_slip"));
        }

        [Fact]
        public void The_new_quest_survives_save_and_reload_at_the_correct_step()
        {
            var db = Overlaid;
            var game = Game.NewGame(db);

            Say(game, "npc.sedge", "I'll cut them.");
            Interact(game, "world.reeds_1");

            // Mid-quest, one bundle of two: still on step 0.
            Assert.Equal(0, game.State.QuestStepIndex("quest.thatch"));

            var reloaded = Game.FromSave(game.Save(), db);

            Assert.Equal(QuestState.Active, reloaded.State.QuestStateOf("quest.thatch"));
            Assert.Equal("gather", reloaded.State.CurrentStep("quest.thatch").Id);
            Assert.Equal(1, reloaded.State.ItemCount("item.reed_bundle"));

            Interact(reloaded, "world.reeds_2");
            Assert.Equal("deliver", reloaded.State.CurrentStep("quest.thatch").Id);

            // And a save taken on step 1 comes back on step 1, not restarted.
            var again = Game.FromSave(reloaded.Save(), db);
            Assert.Equal("deliver", again.State.CurrentStep("quest.thatch").Id);

            Say(again, "npc.sedge", "Two bundles");
            Assert.Equal(QuestState.Completed, again.State.QuestStateOf("quest.thatch"));
        }

        [Fact]
        public void Adding_the_fourth_npc_does_not_disturb_the_shipped_slice()
        {
            var db = Overlaid;
            var game = Game.NewGame(db);

            // The original eight quests are untouched and the scope ceiling still reads true
            // for the shipped content on its own.
            Assert.Equal(9, db.Quests.Count);
            Assert.Equal(8, Db.Quests.Count);

            Say(game, "npc.vesk", "What kind of sand?");
            Interact(game, "world.sand_1");
            Interact(game, "world.sand_2");
            Interact(game, "world.sand_3");
            Say(game, "npc.vesk", "Three measures");

            Assert.Equal(QuestState.Completed, game.State.QuestStateOf("quest.kiln_ash"));
        }
    }
}

using System;
using HiddenValley.Core;
using Xunit;

namespace HiddenValley.Tests
{
    /// <summary>Testers close apps at the worst moment and storage fills up; a save
    /// that cannot load must mean "fresh start", never a crash or a half-state.</summary>
    public class SaveRobustnessTests
    {
        private static ContentDatabase Db() => ContentDatabase.LoadFromTexts(new[]
        {
            @"{ ""items"": [ { ""id"": ""item.x"", ""name"": ""X"" } ] }"
        });

        [Fact]
        public void Garbage_save_throws_rather_than_half_loading()
        {
            var db = Db();
            Assert.ThrowsAny<Exception>(() => Game.FromSave("{not json at all", db));
            Assert.ThrowsAny<Exception>(() => Game.FromSave("", db));
        }

        [Fact]
        public void Truncated_save_throws_rather_than_half_loading()
        {
            var game = Game.NewGame(Db());
            game.State.AddItem("item.x", 3);
            var json = game.Save();

            var truncated = json.Substring(0, json.Length / 2);
            Assert.ThrowsAny<Exception>(() => Game.FromSave(truncated, game.Content));
        }

        [Fact]
        public void Save_round_trip_preserves_player_position_flag()
        {
            var game = Game.NewGame(Db());
            game.State.SetFlag("player.pos", "1.5,0.2,-25");

            var restored = Game.FromSave(game.Save(), game.Content);
            Assert.Equal("1.5,0.2,-25", restored.State.GetFlag("player.pos"));
        }
    }
}

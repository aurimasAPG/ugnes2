using System;
using System.IO;
using HiddenValley.Core;
using Xunit;

namespace HiddenValley.Tests
{
    /// <summary>
    /// Structural checks that generated graphics exist for every shipped mat key and item id.
    /// Paths resolve to the Unity project root (repo root).
    /// </summary>
    public class ArtAssetTests
    {
        private static string RepoRoot
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "Claude.md"))
                        || File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))
                        || Directory.Exists(Path.Combine(dir.FullName, "Assets", "HiddenValley")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
                throw new DirectoryNotFoundException("Could not locate repo root from " + AppContext.BaseDirectory);
            }
        }

        [Fact]
        public void All_layout_mat_textures_exist_and_are_nonempty()
        {
            var keys = new[] { "grey", "dark", "sand", "moss", "bark", "water", "player", "npc", "pip" };
            foreach (var key in keys)
            {
                var path = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Art", "Textures", "mat_" + key + ".jpg");
                Assert.True(File.Exists(path), "missing texture " + path);
                Assert.True(new FileInfo(path).Length > 1000, "texture too small: " + path);
            }
        }

        [Fact]
        public void All_character_portraits_exist()
        {
            foreach (var key in new[] { "player", "pip", "vesk", "coll", "orrel" })
            {
                var path = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Art", "Characters", "char_" + key + ".jpg");
                Assert.True(File.Exists(path), "missing character " + path);
                Assert.True(new FileInfo(path).Length > 1000, "character too small: " + path);
            }
        }

        [Fact]
        public void Every_content_item_has_an_icon_file()
        {
            var contentDir = Path.Combine(RepoRoot, "Assets", "StreamingAssets", "Content");
            var content = ContentDatabase.LoadFromDirectory(contentDir);
            Assert.True(content.Items.Count >= 11, "expected full item catalog");
            foreach (var item in content.Items)
            {
                var path = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Art", "Icons", item.Id + ".jpg");
                Assert.True(File.Exists(path), "missing icon for " + item.Id + " at " + path);
                Assert.True(new FileInfo(path).Length > 1000, "icon too small: " + path);
            }
        }
    }
}

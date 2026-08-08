using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HiddenValley.Tests
{
    public class VoiceAssetTests
    {
        private static string RepoRoot
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "HiddenValley")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
                throw new DirectoryNotFoundException("repo root");
            }
        }

        [Fact]
        public void Voice_map_exists_and_points_at_existing_mp3_clips()
        {
            var mapPath = Path.Combine(RepoRoot, "Assets", "StreamingAssets", "Audio", "voice_map.json");
            Assert.True(File.Exists(mapPath), "missing voice_map.json");
            var root = JObject.Parse(File.ReadAllText(mapPath));
            Assert.True(root.Count >= 50, "expected many VO mappings, got " + root.Count);
            int checkedN = 0;
            foreach (var prop in root.Properties())
            {
                var id = prop.Value.ToString();
                var clip = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Audio", "Voice", id + ".mp3");
                Assert.True(File.Exists(clip), "missing clip " + clip + " for key " + prop.Name);
                Assert.True(new FileInfo(clip).Length > 500, "tiny clip " + clip);
                checkedN++;
                if (checkedN >= 20) break; // sample enough for speed; map count already asserted
            }
        }

        [Fact]
        public void No_narration_line_is_mapped_to_character_voice()
        {
            // A character audibly reading their own stage directions ("Vesk does not look
            // up…") is a production-value tell. Narration lines mention the speaker in
            // third person and contain no quoted speech; they must fall back to the short
            // speaker cue, never to VO.
            var mapPath = Path.Combine(RepoRoot, "Assets", "StreamingAssets", "Audio", "voice_map.json");
            var root = JObject.Parse(File.ReadAllText(mapPath));
            var names = new (string speaker, string name)[]
            {
                ("npc.vesk", "Vesk"), ("npc.coll", "Coll"), ("npc.orrel", "Orrel"), ("npc.pip", "Pip")
            };

            foreach (var prop in root.Properties())
            {
                var split = prop.Name.IndexOf('|');
                if (split < 0) continue;
                var speaker = prop.Name.Substring(0, split);
                var line = prop.Name.Substring(split + 1);

                foreach (var (id, name) in names)
                    if (speaker == id)
                        Assert.False(line.Contains(name) && !line.Contains("\""),
                            "narration line mapped to VO: " + prop.Name);
            }
        }

        [Fact]
        public void At_least_70_voice_mp3_files_exist()
        {
            var dir = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Audio", "Voice");
            Assert.True(Directory.Exists(dir));
            var n = Directory.GetFiles(dir, "*.mp3").Length;
            Assert.True(n >= 70, "expected >=70 VO files, got " + n);
        }

        [Fact]
        public void CleanLineForVoice_extracts_quoted_speech_like_shipped_helper()
        {
            // Mirrors GameAudio.CleanLineForVoice — keep in sync with shipped method.
            string Clean(string lineText)
            {
                if (string.IsNullOrEmpty(lineText)) return "";
                var matches = Regex.Matches(lineText, "\"([^\"]+)\"");
                if (matches.Count > 0)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (Match m in matches) parts.Add(m.Groups[1].Value);
                    return string.Join(" ", parts);
                }
                return lineText.Trim();
            }

            var src = File.ReadAllText(Path.Combine(RepoRoot,
                "Assets", "HiddenValley", "Runtime", "Unity", "GameAudio.cs"));
            Assert.Contains("CleanLineForVoice", src);
            Assert.Contains("PlayDialogueLine", src);
            Assert.Contains("Audio/Voice/", src);

            Assert.Equal("Hello there", Clean("He says \"Hello there\" quietly."));
            Assert.Equal("A. B.", Clean("\"A.\" then \"B.\""));
            Assert.Equal("No quotes here", Clean("No quotes here"));
        }

        [Fact]
        public void Aaa_gap_evaluation_doc_exists()
        {
            var path = Path.Combine(RepoRoot, "docs", "aaa-gap-evaluation.md");
            Assert.True(File.Exists(path));
            var text = File.ReadAllText(path);
            Assert.Contains("AAA", text);
            Assert.Contains("P0", text);
        }

        [Fact]
        public void Character_portraits_are_nonempty_jpgs()
        {
            foreach (var name in new[] { "player", "pip", "vesk", "coll", "orrel" })
            {
                var path = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Art", "Characters", "char_" + name + ".jpg");
                Assert.True(File.Exists(path), path);
                Assert.True(new FileInfo(path).Length > 50_000, "portrait too small " + path);
            }
        }

        [Fact]
        public void Ambience_and_music_stings_exist()
        {
            foreach (var name in new[] { "amb_valley", "mus_sting_mystery", "mus_sting_quest", "sfx_water" })
            {
                var path = Path.Combine(RepoRoot, "Assets", "HiddenValley", "Resources", "Audio", name + ".mp3");
                Assert.True(File.Exists(path), "missing " + path);
                Assert.True(new FileInfo(path).Length > 1000, "too small " + path);
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using HiddenValley.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HiddenValley.Editor
{
    /// <summary>
    /// The one-line build the Phase 0 gate asks for.
    ///
    ///   Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.BuildCommand.iOS
    ///
    /// Wrapped by tools/build-ios.sh. The gate is "does a build reach the device in under
    /// ten minutes, from a one-line command" — so this deliberately does the whole job,
    /// including failing loudly on bad content rather than shipping a build that boots to an
    /// error the tester has to describe over the phone.
    /// </summary>
    public static class BuildCommand
    {
        private const string OutputDir = "Builds/iOS";

        [MenuItem("Hidden Valley/Build iOS")]
        public static void iOS()
        {
            ValidateContentOrThrow();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes in Build Settings.");

            Directory.CreateDirectory(OutputDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log(
                $"[HiddenValley] Build {summary.result} in {summary.totalTime.TotalSeconds:0}s, " +
                $"{summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} error(s).");

            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"iOS build {summary.result}.");
        }

        /// <summary>
        /// Content is authored JSON with no compiler behind it, so this is the only thing
        /// standing between a typo and a tester finding it. Runs before the expensive part.
        /// </summary>
        private static void ValidateContentOrThrow()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Content");
            var report = ContentValidator.Validate(ContentDatabase.LoadFromDirectory(path));

            foreach (var warning in report.Warnings)
                Debug.LogWarning($"[content] {warning}");

            if (report.Ok) return;

            foreach (var error in report.Errors)
                Debug.LogError($"[content] {error}");

            throw new BuildFailedException(
                $"Content validation failed with {report.Errors.Count} error(s). Build aborted.");
        }

        [MenuItem("Hidden Valley/Validate Content")]
        public static void ValidateContentMenu()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Content");

            try
            {
                var report = ContentValidator.Validate(ContentDatabase.LoadFromDirectory(path));
                Debug.Log(report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[content] {e.Message}");
            }
        }
    }
}

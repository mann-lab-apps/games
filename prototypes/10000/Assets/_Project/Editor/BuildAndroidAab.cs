using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.Game10000.EditorTools
{
    public static class BuildAndroidAab
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/Android/10000.aab";

        public static void Build()
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {report.summary.result}");
            }
        }
    }
}


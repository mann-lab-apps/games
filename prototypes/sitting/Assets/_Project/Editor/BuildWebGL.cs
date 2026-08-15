using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MannLab.Games.Sitting.EditorTools
{
    public static class BuildWebGL
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/WebGL/sitting";

        public static void Build()
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(OutputPath);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }
        }
    }
}

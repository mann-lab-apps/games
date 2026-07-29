using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.Game10000.EditorTools
{
    public static class BuildIosXcode
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/iOS/Xcode";
        private const string BundleIdentifier = "com.mannlab.games.game10000";
        private const string AppleTeamIdEnv = "MANNLAB_APPLE_TEAM_ID";

        public static void Build()
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(OutputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "10000";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            ApplySigningHint();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"iOS Xcode project build failed: {report.summary.result}");
            }
        }

        private static void ApplySigningHint()
        {
            var teamId = Environment.GetEnvironmentVariable(AppleTeamIdEnv);
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return;
            }

            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = teamId;
        }
    }
}

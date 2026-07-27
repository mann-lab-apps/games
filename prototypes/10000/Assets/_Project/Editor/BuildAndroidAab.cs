using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.Game10000.EditorTools
{
    public static class BuildAndroidAab
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string AabOutputPath = "Builds/Android/10000.aab";
        private const string ApkOutputPath = "Builds/Android/10000.apk";
        private const string KeystorePathEnv = "MANNLAB_10000_ANDROID_KEYSTORE_PATH";
        private const string KeystorePassEnv = "MANNLAB_10000_ANDROID_KEYSTORE_PASS";
        private const string KeyAliasNameEnv = "MANNLAB_10000_ANDROID_KEYALIAS_NAME";
        private const string KeyAliasPassEnv = "MANNLAB_10000_ANDROID_KEYALIAS_PASS";

        public static void Build()
        {
            BuildAab();
        }

        public static void BuildAab()
        {
            BuildAndroid(AabOutputPath, true);
        }

        public static void BuildApk()
        {
            BuildAndroid(ApkOutputPath, false);
        }

        private static void BuildAndroid(string outputPath, bool buildAppBundle)
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = false;
            ApplyReleaseSigning();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {report.summary.result}");
            }
        }

        private static void ApplyReleaseSigning()
        {
            var keystorePath = Environment.GetEnvironmentVariable(KeystorePathEnv);
            var keystorePass = Environment.GetEnvironmentVariable(KeystorePassEnv);
            var keyAliasName = Environment.GetEnvironmentVariable(KeyAliasNameEnv);
            var keyAliasPass = Environment.GetEnvironmentVariable(KeyAliasPassEnv);

            if (string.IsNullOrWhiteSpace(keystorePath)
                || string.IsNullOrWhiteSpace(keystorePass)
                || string.IsNullOrWhiteSpace(keyAliasName)
                || string.IsNullOrWhiteSpace(keyAliasPass))
            {
                throw new InvalidOperationException(
                    $"Release signing environment variables are required: {KeystorePathEnv}, {KeystorePassEnv}, {KeyAliasNameEnv}, {KeyAliasPassEnv}");
            }

            if (!File.Exists(keystorePath))
            {
                throw new FileNotFoundException("Android release keystore not found.", keystorePath);
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keyAliasName;
            PlayerSettings.Android.keyaliasPass = keyAliasPass;
        }
    }
}

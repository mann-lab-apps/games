using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MannLab.Games.Game2048Crash.EditorTools
{
    public static class BuildAndroidAab
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string AabOutputPath = "Builds/Android/2048-crash.aab";
        private const string ApkOutputPath = "Builds/Android/2048-crash.apk";
        private const string AdMobTestApkOutputPath = "Builds/Android/2048-crash-admob-test.apk";
        private const string AppIconPath = "Assets/_Project/Art/AppStore/AppIcon-1024.png";
        private const string GoogleMobileAdsSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string ForceAdMobTestAdsDefine = "MANNLAB_ADMOB_FORCE_TEST_ADS";
        private const string AdMobAndroidAppIdEnv = "MANNLAB_2048_CRASH_ADMOB_ANDROID_APP_ID";
        private const string AdMobAndroidTestAppId = "ca-app-pub-3940256099942544~3347511713";
        private const string KeystorePathEnv = "MANNLAB_2048_CRASH_ANDROID_KEYSTORE_PATH";
        private const string KeystorePassEnv = "MANNLAB_2048_CRASH_ANDROID_KEYSTORE_PASS";
        private const string KeyAliasNameEnv = "MANNLAB_2048_CRASH_ANDROID_KEYALIAS_NAME";
        private const string KeyAliasPassEnv = "MANNLAB_2048_CRASH_ANDROID_KEYALIAS_PASS";

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

        public static void BuildAdMobTestApk()
        {
            BuildAndroid(AdMobTestApkOutputPath, false, true);
        }

        private static void BuildAndroid(string outputPath, bool buildAppBundle, bool forceAdMobTestAds = false)
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "2048 Crash";
            PlayerSettings.bundleVersion = "0.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mannlab.games.game2048crash");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            ApplyAppIcon();
            if (forceAdMobTestAds)
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }
            else
            {
                ApplyReleaseSigning();
            }

            var namedBuildTarget = NamedBuildTarget.Android;
            var previousDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            var previousGoogleMobileAdsSettings = ApplyGoogleMobileAdsAndroidAppId(GetAdMobAndroidAppId(forceAdMobTestAds));
            try
            {
                PlayerSettings.SetScriptingDefineSymbols(
                    namedBuildTarget,
                    SetScriptingDefine(previousDefines, ForceAdMobTestAdsDefine, forceAdMobTestAds));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"Android build failed: {report.summary.result}");
                }
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, previousDefines);
                RestoreGoogleMobileAdsSettings(previousGoogleMobileAdsSettings);
            }
        }

        private static string SetScriptingDefine(string currentDefines, string define, bool enabled)
        {
            var symbols = new System.Collections.Generic.List<string>();
            foreach (var rawSymbol in currentDefines.Split(';'))
            {
                var symbol = rawSymbol.Trim();
                if (string.IsNullOrEmpty(symbol) || symbol == define)
                {
                    continue;
                }

                symbols.Add(symbol);
            }

            if (enabled)
            {
                symbols.Add(define);
            }

            return string.Join(";", symbols);
        }

        private static void ApplyAppIcon()
        {
            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(AppIconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (appIcon == null)
            {
                throw new FileNotFoundException($"Android app icon not found: {AppIconPath}");
            }

            var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Android, IconKind.Application);
            var iconCount = Math.Max(iconSizes.Length, 1);
            var icons = new Texture2D[iconCount];
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = appIcon;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Application);
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

        private static string GetAdMobAndroidAppId(bool forceAdMobTestAds)
        {
            if (forceAdMobTestAds)
            {
                return AdMobAndroidTestAppId;
            }

            return Environment.GetEnvironmentVariable(AdMobAndroidAppIdEnv) ?? string.Empty;
        }

        private static string ApplyGoogleMobileAdsAndroidAppId(string appId)
        {
            if (!File.Exists(GoogleMobileAdsSettingsPath))
            {
                return null;
            }

            var contents = File.ReadAllText(GoogleMobileAdsSettingsPath);
            var updated = ReplaceSettingLine(contents, "adMobAndroidAppId:", appId ?? string.Empty);
            if (updated == contents)
            {
                return contents;
            }

            File.WriteAllText(GoogleMobileAdsSettingsPath, updated);
            AssetDatabase.ImportAsset(GoogleMobileAdsSettingsPath, ImportAssetOptions.ForceUpdate);
            return contents;
        }

        private static void RestoreGoogleMobileAdsSettings(string contents)
        {
            if (contents == null)
            {
                return;
            }

            File.WriteAllText(GoogleMobileAdsSettingsPath, contents);
            AssetDatabase.ImportAsset(GoogleMobileAdsSettingsPath, ImportAssetOptions.ForceUpdate);
        }

        private static string ReplaceSettingLine(string contents, string key, string value)
        {
            var lines = contents.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith($"  {key}", StringComparison.Ordinal))
                {
                    lines[i] = $"  {key} {value}";
                    return string.Join("\n", lines);
                }
            }

            return contents;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MannLab.Games.OnePlusOneMinusOne.EditorTools
{
    public static class BuildAndroidAab
    {
        private const string ProductName = "1 = 1";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string AabOutputPath = "Builds/Android/one-plus-one-minus-one.aab";
        private const string ApkOutputPath = "Builds/Android/one-plus-one-minus-one.apk";
        private const string AdMobTestApkOutputPath = "Builds/Android/one-plus-one-minus-one-admob-test.apk";
        private const string AppIconPath = "Assets/_Project/Art/AppIcon-1024.png";
        private const string GoogleMobileAdsSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string BundleIdentifier = "com.mannlab.games.oneplusoneminusone";
        private const string ForceAdMobTestAdsDefine = "MANNLAB_ADMOB_FORCE_TEST_ADS";
        private const string AdMobAndroidAppIdEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID";
        private const string AdMobAndroidInterstitialEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID";
        private const string AdMobAndroidTestAppId = "ca-app-pub-3940256099942544~3347511713";
        private const string AdMobAndroidTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        private const string MarketingVersionEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION";
        private const string DefaultMarketingVersion = "0.1";
        private const string VersionCodeEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE";
        private const int DefaultVersionCode = 1;
        private const string KeystorePathEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH";
        private const string KeystorePassEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS";
        private const string KeyAliasNameEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME";
        private const string KeyAliasPassEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS";

        public static void Build()
        {
            BuildAab();
        }

        public static void BuildAab()
        {
            BuildAndroid(AabOutputPath, true, false);
        }

        public static void BuildApk()
        {
            BuildAndroid(ApkOutputPath, false, false);
        }

        public static void BuildAdMobTestApk()
        {
            BuildAndroid(AdMobTestApkOutputPath, false, true);
        }

        private static void BuildAndroid(string outputPath, bool buildAppBundle, bool forceAdMobTestAds)
        {
            GenerateAppIcon.Run();
            CreateGameScene.Create();
            VerifyGoalMode.Run();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = GetMarketingVersion();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.bundleVersionCode = GetVersionCode();
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

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
            var requireProductionAds = buildAppBundle && !forceAdMobTestAds;
            var previousGoogleMobileAdsSettings = ApplyGoogleMobileAdsAndroidAppId(GetAdMobAndroidAppId(forceAdMobTestAds, requireProductionAds));
            var wroteReleaseAdMobConfig = false;
            try
            {
                wroteReleaseAdMobConfig = ReleaseAdMobConfigWriter.Write(
                    string.Empty,
                    GetAdMobAndroidInterstitialAdUnitId(forceAdMobTestAds, requireProductionAds));
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
                if (wroteReleaseAdMobConfig)
                {
                    ReleaseAdMobConfigWriter.Delete();
                }
            }
        }

        private static string SetScriptingDefine(string currentDefines, string define, bool enabled)
        {
            var symbols = new List<string>();
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
            var icons = new Texture2D[Math.Max(iconSizes.Length, 1)];
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
            if (string.IsNullOrWhiteSpace(keystorePath) ||
                string.IsNullOrWhiteSpace(keystorePass) ||
                string.IsNullOrWhiteSpace(keyAliasName) ||
                string.IsNullOrWhiteSpace(keyAliasPass))
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

        private static string GetAdMobAndroidAppId(bool forceAdMobTestAds, bool required)
        {
            if (forceAdMobTestAds)
            {
                return AdMobAndroidTestAppId;
            }

            var value = Environment.GetEnvironmentVariable(AdMobAndroidAppIdEnv);
            if (required && string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{AdMobAndroidAppIdEnv} is required for Android release builds.");
            }

            if (required && value == AdMobAndroidTestAppId)
            {
                throw new InvalidOperationException($"{AdMobAndroidAppIdEnv} must not use Google's test app ID for Android release builds.");
            }

            if (required && IsPlaceholderAdMobId(value))
            {
                throw new InvalidOperationException($"{AdMobAndroidAppIdEnv} must be a real production Android AdMob App ID, not a placeholder.");
            }

            return value ?? string.Empty;
        }

        private static string GetAdMobAndroidInterstitialAdUnitId(bool forceAdMobTestAds, bool required)
        {
            if (forceAdMobTestAds)
            {
                return string.Empty;
            }

            var value = Environment.GetEnvironmentVariable(AdMobAndroidInterstitialEnv);
            if (required && string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{AdMobAndroidInterstitialEnv} is required for Android release builds.");
            }

            if (required && value == AdMobAndroidTestInterstitialAdUnitId)
            {
                throw new InvalidOperationException($"{AdMobAndroidInterstitialEnv} must not use Google's test interstitial ad unit ID for Android release builds.");
            }

            if (required && IsPlaceholderAdMobId(value))
            {
                throw new InvalidOperationException($"{AdMobAndroidInterstitialEnv} must be a real production Android interstitial ad unit ID, not a placeholder.");
            }

            return value ?? string.Empty;
        }

        private static bool IsPlaceholderAdMobId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   (value.IndexOf("XXXX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("replace", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string GetMarketingVersion()
        {
            return Environment.GetEnvironmentVariable(MarketingVersionEnv) ?? DefaultMarketingVersion;
        }

        private static int GetVersionCode()
        {
            var rawVersionCode = Environment.GetEnvironmentVariable(VersionCodeEnv);
            if (string.IsNullOrWhiteSpace(rawVersionCode))
            {
                return DefaultVersionCode;
            }

            if (!int.TryParse(rawVersionCode, out var versionCode) || versionCode <= 0)
            {
                throw new InvalidOperationException($"{VersionCodeEnv} must be a positive integer.");
            }

            return versionCode;
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

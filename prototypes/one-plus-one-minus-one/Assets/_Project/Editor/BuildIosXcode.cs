using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MannLab.Games.OnePlusOneMinusOne.EditorTools
{
    public static class BuildIosXcode
    {
        private const string ProductName = "1 = 1";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ReleaseOutputPath = "Builds/iOS/Xcode";
        private const string CrashlyticsTestOutputPath = "Builds/iOS/CrashlyticsTestXcode";
        private const string CrashlyticsSimulatorTestOutputPath = "Builds/iOS/CrashlyticsSimulatorTestXcode";
        private const string AdMobTestOutputPath = "Builds/iOS/AdMobTestXcode";
        private const string BundleIdentifier = "com.mannlab.games.oneplusoneminusone";
        private const string ForceAdMobTestAdsDefine = "MANNLAB_ADMOB_FORCE_TEST_ADS";
        private const string AdMobIosAppIdEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID";
        private const string AdMobIosInterstitialEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID";
        private const string DefaultAdMobIosAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string AdMobIosTestAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string AdMobIosTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
        private const string MarketingVersionEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION";
        private const string DefaultMarketingVersion = "0.1";
        private const string BuildNumberEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER";
        private const string DefaultBuildNumber = "1";
        private const string AppleTeamIdEnv = "MANNLAB_APPLE_TEAM_ID";
        private const string ProvisioningProfileEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_PROFILE_SPECIFIER";
        private const string ProvisioningProfileUuidEnv = "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_PROFILE_UUID";
        private const string AppIconPath = "Assets/_Project/Art/AppIcon-1024.png";
        private const string PrivacyManifestPath = "Assets/_Project/Store/PrivacyInfo.xcprivacy";

        public static void Build()
        {
            BuildRelease();
        }

        public static void BuildRelease()
        {
            BuildIos(ReleaseOutputPath, false, iOSSdkVersion.DeviceSDK, false, true);
        }

        public static void BuildCrashlyticsTest()
        {
            BuildIos(CrashlyticsTestOutputPath, true, iOSSdkVersion.DeviceSDK);
        }

        public static void BuildCrashlyticsSimulatorTest()
        {
            BuildIos(CrashlyticsSimulatorTestOutputPath, true, iOSSdkVersion.SimulatorSDK);
        }

        public static void BuildAdMobTest()
        {
            BuildIos(AdMobTestOutputPath, false, iOSSdkVersion.DeviceSDK, true);
        }

        private static void BuildIos(
            string outputPath,
            bool developmentBuild,
            iOSSdkVersion sdkVersion,
            bool forceAdMobTestAds = false,
            bool requireProductionAds = false)
        {
            GenerateAppIcon.Run();
            CreateGameScene.Create();
            VerifyGoalMode.Run();
            Directory.CreateDirectory(outputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = GetMarketingVersion();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            var buildNumber = GetBuildNumber();
            PlayerSettings.iOS.buildNumber = buildNumber;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = sdkVersion;

            ApplyAppIcon();
            ApplySigningHint();
            var adMobIosAppId = GetAdMobIosAppId(forceAdMobTestAds, requireProductionAds);

            var namedBuildTarget = NamedBuildTarget.iOS;
            var previousDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            var wroteReleaseAdMobConfig = false;
            try
            {
                wroteReleaseAdMobConfig = ReleaseAdMobConfigWriter.Write(
                    GetAdMobIosInterstitialAdUnitId(forceAdMobTestAds, requireProductionAds),
                    string.Empty);
                PlayerSettings.SetScriptingDefineSymbols(
                    namedBuildTarget,
                    SetScriptingDefine(previousDefines, ForceAdMobTestAdsDefine, forceAdMobTestAds));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.iOS,
                    options = developmentBuild ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"iOS Xcode project build failed: {report.summary.result}");
                }
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, previousDefines);
                if (wroteReleaseAdMobConfig)
                {
                    ReleaseAdMobConfigWriter.Delete();
                }
            }

            AddMarketingIconToXcodeProject(outputPath);
            RemoveLegacyCocoaPodsSpecsSource(outputPath);
            AddSimpleLaunchScreensToXcodeProject(outputPath);
            AddPrivacyManifestToXcodeProject(outputPath);
            ConfigureInfoPlist(outputPath, buildNumber, adMobIosAppId);
            ConfigureArchiveVersion(outputPath, buildNumber);
            if (sdkVersion == iOSSdkVersion.DeviceSDK)
            {
                ConfigureArchiveSigning(outputPath);
            }
        }

        private static string SetScriptingDefine(string currentDefines, string define, bool enabled)
        {
            var symbols = new List<string>();
            foreach (var rawSymbol in currentDefines.Split(';'))
            {
                var symbol = rawSymbol.Trim();
                if (!string.IsNullOrEmpty(symbol) && symbol != define)
                {
                    symbols.Add(symbol);
                }
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
            var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (appIcon == null)
            {
                throw new FileNotFoundException($"iOS app icon not found: {AppIconPath}");
            }

            var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, IconKind.Application);
            var icons = new Texture2D[iconSizes.Length];
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = appIcon;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.iOS, icons, IconKind.Application);
        }

        private static void ApplySigningHint()
        {
            var teamId = Environment.GetEnvironmentVariable(AppleTeamIdEnv);
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return;
            }

            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.appleDeveloperTeamID = teamId;
            var profileUuid = Environment.GetEnvironmentVariable(ProvisioningProfileUuidEnv);
            if (!string.IsNullOrWhiteSpace(profileUuid))
            {
                PlayerSettings.iOS.iOSManualProvisioningProfileID = profileUuid;
                PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
            }
        }

        private static void AddMarketingIconToXcodeProject(string outputPath)
        {
            const string marketingIconFile = "Icon-AppStore-1024.png";
            var appIconSetPath = Path.Combine(
                outputPath,
                "Unity-iPhone",
                "Images.xcassets",
                "AppIcon.appiconset");
            var contentsPath = Path.Combine(appIconSetPath, "Contents.json");
            var marketingIconPath = Path.Combine(appIconSetPath, marketingIconFile);

            if (!Directory.Exists(appIconSetPath))
            {
                throw new DirectoryNotFoundException($"Xcode app icon set not found: {appIconSetPath}");
            }

            File.Copy(AppIconPath, marketingIconPath, true);
            File.WriteAllText(contentsPath, $@"{{
  ""images"" : [
    {{
      ""filename"" : ""Icon-iPhone-120.png"",
      ""idiom"" : ""iphone"",
      ""scale"" : ""2x"",
      ""size"" : ""60x60""
    }},
    {{
      ""filename"" : ""Icon-iPhone-180.png"",
      ""idiom"" : ""iphone"",
      ""scale"" : ""3x"",
      ""size"" : ""60x60""
    }},
    {{
      ""filename"" : ""Icon-iPad-76.png"",
      ""idiom"" : ""ipad"",
      ""scale"" : ""1x"",
      ""size"" : ""76x76""
    }},
    {{
      ""filename"" : ""Icon-iPad-152.png"",
      ""idiom"" : ""ipad"",
      ""scale"" : ""2x"",
      ""size"" : ""76x76""
    }},
    {{
      ""filename"" : ""Icon-iPad-167.png"",
      ""idiom"" : ""ipad"",
      ""scale"" : ""2x"",
      ""size"" : ""83.5x83.5""
    }},
    {{
      ""filename"" : ""{marketingIconFile}"",
      ""idiom"" : ""ios-marketing"",
      ""scale"" : ""1x"",
      ""size"" : ""1024x1024""
    }}
  ],
  ""info"" : {{
    ""author"" : ""xcode"",
    ""version"" : 1
  }},
  ""properties"" : {{
    ""pre-rendered"" : false
  }}
}}
");
        }

        private static void RemoveLegacyCocoaPodsSpecsSource(string outputPath)
        {
            var podfilePath = Path.Combine(outputPath, "Podfile");
            if (!File.Exists(podfilePath))
            {
                return;
            }

            var contents = File.ReadAllText(podfilePath);
            contents = contents.Replace("source 'https://github.com/CocoaPods/Specs'\n", string.Empty);
            File.WriteAllText(podfilePath, contents);
        }

        private static void AddSimpleLaunchScreensToXcodeProject(string outputPath)
        {
            WriteSimpleLaunchScreen(Path.Combine(outputPath, "LaunchScreen-iPhone.storyboard"));
            WriteSimpleLaunchScreen(Path.Combine(outputPath, "LaunchScreen-iPad.storyboard"));
        }

        private static void AddPrivacyManifestToXcodeProject(string outputPath)
        {
            if (!File.Exists(PrivacyManifestPath))
            {
                throw new FileNotFoundException($"Privacy manifest not found: {PrivacyManifestPath}");
            }

            const string privacyManifestFile = "PrivacyInfo.xcprivacy";
            var outputManifestPath = Path.Combine(outputPath, privacyManifestFile);
            File.Copy(PrivacyManifestPath, outputManifestPath, true);

            var projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            var targetGuid = project.GetUnityMainTargetGuid();
            var fileGuid = project.AddFile(privacyManifestFile, privacyManifestFile);
            project.AddFileToBuild(targetGuid, fileGuid);
            project.WriteToFile(projectPath);
        }

        private static void ConfigureInfoPlist(string outputPath, string buildNumber, string adMobIosAppId)
        {
            var plistPath = Path.Combine(outputPath, "Info.plist");
            if (!File.Exists(plistPath))
            {
                throw new FileNotFoundException($"Xcode Info.plist not found: {plistPath}");
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("CFBundleShortVersionString", GetMarketingVersion());
            plist.root.SetString("CFBundleVersion", buildNumber);
            plist.root.SetString("GADApplicationIdentifier", adMobIosAppId);
            plist.root.SetBoolean("GADIsAdManagerApp", false);
            plist.WriteToFile(plistPath);
        }

        private static void ConfigureArchiveVersion(string outputPath, string buildNumber)
        {
            var projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            ConfigureTargetVersion(project, project.ProjectGuid(), buildNumber);
            ConfigureTargetVersion(project, project.GetUnityMainTargetGuid(), buildNumber);
            ConfigureTargetVersion(project, project.GetUnityFrameworkTargetGuid(), buildNumber);

            project.WriteToFile(projectPath);
        }

        private static void ConfigureTargetVersion(PBXProject project, string targetGuid, string buildNumber)
        {
            project.SetBuildProperty(targetGuid, "MARKETING_VERSION", GetMarketingVersion());
            project.SetBuildProperty(targetGuid, "CURRENT_PROJECT_VERSION", buildNumber);
            project.SetBuildProperty(targetGuid, "VERSIONING_SYSTEM", "apple-generic");
            project.SetBuildProperty(targetGuid, "INFOPLIST_KEY_CFBundleShortVersionString", GetMarketingVersion());
            project.SetBuildProperty(targetGuid, "INFOPLIST_KEY_CFBundleVersion", buildNumber);
        }

        private static void ConfigureArchiveSigning(string outputPath)
        {
            var projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            ConfigureMainTargetSigning(project, project.GetUnityMainTargetGuid());
            ConfigureFrameworkSigning(project, project.GetUnityFrameworkTargetGuid());

            project.WriteToFile(projectPath);
            HardenIl2CppBuildScript(projectPath);
        }

        private static void ConfigureMainTargetSigning(PBXProject project, string targetGuid)
        {
            var teamId = Environment.GetEnvironmentVariable(AppleTeamIdEnv);
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return;
            }

            project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Manual");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", "Apple Distribution");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", "Apple Distribution");
            project.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", teamId);

            var profileSpecifier = Environment.GetEnvironmentVariable(ProvisioningProfileEnv);
            if (!string.IsNullOrWhiteSpace(profileSpecifier))
            {
                project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_SPECIFIER", profileSpecifier);
            }

            var profileUuid = Environment.GetEnvironmentVariable(ProvisioningProfileUuidEnv);
            if (!string.IsNullOrWhiteSpace(profileUuid))
            {
                project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE", profileUuid);
                project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_APP", profileUuid);
            }
        }

        private static void ConfigureFrameworkSigning(PBXProject project, string targetGuid)
        {
            project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Manual");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", string.Empty);
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", string.Empty);
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_SPECIFIER", string.Empty);
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE", string.Empty);

            var teamId = Environment.GetEnvironmentVariable(AppleTeamIdEnv);
            if (!string.IsNullOrWhiteSpace(teamId))
            {
                project.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", teamId);
            }
        }

        private static void HardenIl2CppBuildScript(string projectPath)
        {
            var contents = File.ReadAllText(projectPath);
            const string generatedSnippet =
                "mkdir -p \\\"$CONFIGURATION_TEMP_DIR/artifacts/arm64/buildstate/\\\"\\nmkdir -p \\\"$PROJECT_DIR/Il2CppTempDirArtifacts/$CONFIGURATION\\\"\\nln -sF  \\\"$CONFIGURATION_TEMP_DIR/artifacts\\\" \\\"$PROJECT_DIR/Il2CppTempDirArtifacts/$CONFIGURATION/artifacts\\\"\\n";
            const string hardenedSnippet =
                "mkdir -p \\\"$PROJECT_DIR/Il2CppTempDirArtifacts/$CONFIGURATION/artifacts/arm64/buildstate/\\\"\\n";

            if (contents.Contains(generatedSnippet))
            {
                contents = contents.Replace(generatedSnippet, hardenedSnippet);
            }

            contents = contents.Replace(
                "rm -rf \\\"$PROJECT_DIR/Il2CppTempDirArtifacts/$CONFIGURATION\\\"\\n",
                "# Keep Il2CppTempDirArtifacts until xcodebuild exits; Unity cleanup can race Bee clang workers.\\n");

            File.WriteAllText(projectPath, contents);
        }

        private static void WriteSimpleLaunchScreen(string path)
        {
            File.WriteAllText(path, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""15702"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" launchScreen=""YES"" useTraitCollections=""YES"" colorMatched=""YES"" initialViewController=""UnityLaunchScreen-ViewController"">
    <device id=""retina6_12"" orientation=""portrait"" appearance=""light""/>
    <dependencies>
        <deployment identifier=""iOS""/>
        <plugIn identifier=""com.apple.InterfaceBuilder.IBCocoaTouchPlugin"" version=""15704""/>
        <capability name=""documents saved in the Xcode 8 format"" minToolsVersion=""8.0""/>
    </dependencies>
    <scenes>
        <scene sceneID=""UnityLaunchScreen-Scene"">
            <objects>
                <viewController id=""UnityLaunchScreen-ViewController"" sceneMemberID=""viewController"">
                    <view key=""view"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" id=""UnityLaunchScreen-RootView"" userLabel=""RootView"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""393"" height=""852""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <color key=""backgroundColor"" red=""1"" green=""1"" blue=""1"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""UnityLaunchScreen-FirstResponder"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
            <point key=""canvasLocation"" x=""53"" y=""375""/>
        </scene>
    </scenes>
</document>
");
        }

        private static string GetAdMobIosAppId(bool forceAdMobTestAds, bool required)
        {
            if (forceAdMobTestAds)
            {
                return AdMobIosTestAppId;
            }

            var value = Environment.GetEnvironmentVariable(AdMobIosAppIdEnv);
            if (required && string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{AdMobIosAppIdEnv} is required for iOS release builds.");
            }

            if (required && value == AdMobIosTestAppId)
            {
                throw new InvalidOperationException($"{AdMobIosAppIdEnv} must not use Google's test app ID for iOS release builds.");
            }

            if (required && IsPlaceholderAdMobId(value))
            {
                throw new InvalidOperationException($"{AdMobIosAppIdEnv} must be a real production iOS AdMob App ID, not a placeholder.");
            }

            return string.IsNullOrWhiteSpace(value) ? DefaultAdMobIosAppId : value;
        }

        private static string GetAdMobIosInterstitialAdUnitId(bool forceAdMobTestAds, bool required)
        {
            if (forceAdMobTestAds)
            {
                return string.Empty;
            }

            var value = Environment.GetEnvironmentVariable(AdMobIosInterstitialEnv);
            if (required && string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{AdMobIosInterstitialEnv} is required for iOS release builds.");
            }

            if (required && value == AdMobIosTestInterstitialAdUnitId)
            {
                throw new InvalidOperationException($"{AdMobIosInterstitialEnv} must not use Google's test interstitial ad unit ID for iOS release builds.");
            }

            if (required && IsPlaceholderAdMobId(value))
            {
                throw new InvalidOperationException($"{AdMobIosInterstitialEnv} must be a real production iOS interstitial ad unit ID, not a placeholder.");
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
            return GetEnvOrDefault(MarketingVersionEnv, DefaultMarketingVersion);
        }

        private static string GetBuildNumber()
        {
            return GetEnvOrDefault(BuildNumberEnv, DefaultBuildNumber);
        }

        private static string GetEnvOrDefault(string key, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}

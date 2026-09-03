using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MannLab.Games.Walking.EditorTools
{
    public static class BuildIosXcode
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/iOS/Xcode";
        private const string CrashlyticsTestOutputPath = "Builds/iOS/CrashlyticsTestXcode";
        private const string AdMobTestOutputPath = "Builds/iOS/AdMobTestXcode";
        private const string BundleIdentifier = "com.mannlab.games.thumbwaddler";
        private const string AdMobIosAppIdEnv = "MANNLAB_THUMBWADDLE_ADMOB_IOS_APP_ID";
        private const string FirebaseIosPlistEnv = "MANNLAB_THUMBWADDLE_FIREBASE_IOS_PLIST";
        private const string FirebaseIosPlistPath = "Assets/GoogleService-Info.plist";
        private const string AdMobIosAppId = "ca-app-pub-4525914685149405~7787773444";
        private const string AdMobIosTestAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string ForceAdMobTestAdsDefine = "MANNLAB_ADMOB_FORCE_TEST_ADS";
        private const string BuildNumberEnv = "MANNLAB_WALKING_IOS_BUILD_NUMBER";
        private const string DefaultBuildNumber = "2026090301";
        private const string MarketingVersion = "1.0.8";
        private const string AppleTeamIdEnv = "MANNLAB_APPLE_TEAM_ID";
        private const string DefaultAppleTeamId = "ZRA4DHHKQ4";
        private const string ProvisioningProfileEnv = "MANNLAB_THUMBWADDLE_IOS_PROFILE_SPECIFIER";
        private const string DefaultProvisioningProfileSpecifier = "Thumbwaddle";
        private const string ProvisioningProfileUuidEnv = "MANNLAB_THUMBWADDLE_IOS_PROFILE_UUID";
        private const string DefaultProvisioningProfileUuid = "3c745d8d-b794-4204-b5f1-2fd886a0242e";
        private const string AppIconPath = "Assets/_Project/Art/AppStore/AppIcon-1024.png";

        public static void Build()
        {
            BuildRelease();
        }

        public static void BuildRelease()
        {
            BuildIos(OutputPath, false, false);
        }

        public static void BuildCrashlyticsTest()
        {
            BuildIos(CrashlyticsTestOutputPath, true, false);
        }

        public static void BuildAdMobTest()
        {
            BuildIos(AdMobTestOutputPath, false, true);
        }

        private static void BuildIos(string outputPath, bool developmentBuild, bool forceAdMobTestAds)
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(outputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;

            var buildNumber = GetBuildNumber();
            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "Thumbwaddle";
            PlayerSettings.bundleVersion = MarketingVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.buildNumber = buildNumber;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            ApplyAppIcon();
            ApplyFirebaseIosConfig();
            ApplySigningHint();

            var namedBuildTarget = NamedBuildTarget.iOS;
            var previousDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            try
            {
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
            }

            AddMarketingIconToXcodeProject(outputPath);
            RemoveLegacyCocoaPodsSpecsSource(outputPath);
            AddSimpleLaunchScreensToXcodeProject(outputPath);
            ConfigureInfoPlist(outputPath, buildNumber, forceAdMobTestAds);
            ConfigureXcodeProject(outputPath, buildNumber);
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

        private static string GetBuildNumber()
        {
            var buildNumber = Environment.GetEnvironmentVariable(BuildNumberEnv);
            return string.IsNullOrWhiteSpace(buildNumber) ? DefaultBuildNumber : buildNumber;
        }

        private static void ApplySigningHint()
        {
            var teamId = GetAppleTeamId();
            if (string.IsNullOrWhiteSpace(teamId))
            {
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                return;
            }

            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.appleDeveloperTeamID = teamId;

            var profileUuid = GetProvisioningProfileUuid();
            if (!string.IsNullOrWhiteSpace(profileUuid))
            {
                PlayerSettings.iOS.iOSManualProvisioningProfileID = profileUuid;
                PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
            }
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

        private static void ApplyFirebaseIosConfig()
        {
            if (File.Exists(FirebaseIosPlistPath))
            {
                return;
            }

            var sourcePath = Environment.GetEnvironmentVariable(FirebaseIosPlistEnv);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                Debug.LogWarning(
                    $"[Thumbwaddle] {FirebaseIosPlistPath} is missing. Set {FirebaseIosPlistEnv} to import the Firebase iOS config before build.");
                return;
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Firebase iOS config not found: {sourcePath}");
            }

            File.Copy(sourcePath, FirebaseIosPlistPath, true);
            AssetDatabase.ImportAsset(FirebaseIosPlistPath, ImportAssetOptions.ForceUpdate);
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

        private static void AddSimpleLaunchScreensToXcodeProject(string outputPath)
        {
            WriteSimpleLaunchScreen(Path.Combine(outputPath, "LaunchScreen-iPhone.storyboard"));
            WriteSimpleLaunchScreen(Path.Combine(outputPath, "LaunchScreen-iPad.storyboard"));
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
                        <color key=""backgroundColor"" red=""0.9803921569"" green=""0.9686274510"" blue=""0.9372549020"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
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

        private static void ConfigureInfoPlist(string outputPath, string buildNumber, bool forceAdMobTestAds)
        {
            var plistPath = Path.Combine(outputPath, "Info.plist");
            if (!File.Exists(plistPath))
            {
                throw new FileNotFoundException($"Xcode Info.plist not found: {plistPath}");
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("CFBundleShortVersionString", MarketingVersion);
            plist.root.SetString("CFBundleVersion", buildNumber);
            var adMobIosAppId = GetAdMobIosAppId(forceAdMobTestAds);
            if (!string.IsNullOrWhiteSpace(adMobIosAppId))
            {
                plist.root.SetString("GADApplicationIdentifier", adMobIosAppId);
                plist.root.SetBoolean("GADIsAdManagerApp", false);
            }
            else
            {
                plist.root.values.Remove("GADApplicationIdentifier");
                plist.root.values.Remove("GADIsAdManagerApp");
            }

            plist.WriteToFile(plistPath);
        }

        private static string GetAdMobIosAppId(bool forceAdMobTestAds)
        {
            if (forceAdMobTestAds)
            {
                return AdMobIosTestAppId;
            }

            var configured = Environment.GetEnvironmentVariable(AdMobIosAppIdEnv);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return AdMobIosAppId;
        }

        private static void ConfigureXcodeProject(string outputPath, string buildNumber)
        {
            var projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            var mainTargetGuid = project.GetUnityMainTargetGuid();
            var frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

            ConfigureTargetVersion(project, project.ProjectGuid(), buildNumber);
            ConfigureTargetVersion(project, mainTargetGuid, buildNumber);
            ConfigureTargetVersion(project, frameworkTargetGuid, buildNumber);
            ConfigureAppTarget(project, mainTargetGuid);
            ConfigureFrameworkTarget(project, frameworkTargetGuid);
            DisableUserScriptSandboxing(project, mainTargetGuid, frameworkTargetGuid);

            project.WriteToFile(projectPath);
        }

        private static void ConfigureTargetVersion(PBXProject project, string targetGuid, string buildNumber)
        {
            if (string.IsNullOrEmpty(targetGuid))
            {
                return;
            }

            project.SetBuildProperty(targetGuid, "MARKETING_VERSION", MarketingVersion);
            project.SetBuildProperty(targetGuid, "CURRENT_PROJECT_VERSION", buildNumber);
            project.SetBuildProperty(targetGuid, "VERSIONING_SYSTEM", "apple-generic");
        }

        private static void ConfigureAppTarget(PBXProject project, string targetGuid)
        {
            if (string.IsNullOrEmpty(targetGuid))
            {
                return;
            }

            project.SetBuildProperty(targetGuid, "PRODUCT_BUNDLE_IDENTIFIER", BundleIdentifier);
            project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Manual");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", "Apple Distribution");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", "Apple Distribution");
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_SPECIFIER", GetProvisioningProfileSpecifier());

            var profileUuid = GetProvisioningProfileUuid();
            if (!string.IsNullOrWhiteSpace(profileUuid))
            {
                project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE", profileUuid);
                project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_APP", profileUuid);
            }

            var teamId = GetAppleTeamId();
            if (!string.IsNullOrWhiteSpace(teamId))
            {
                project.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", teamId);
            }
        }

        private static void ConfigureFrameworkTarget(PBXProject project, string targetGuid)
        {
            if (string.IsNullOrEmpty(targetGuid))
            {
                return;
            }

            project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Manual");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", string.Empty);
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", string.Empty);
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_SPECIFIER", string.Empty);
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE", string.Empty);

            var teamId = GetAppleTeamId();
            if (!string.IsNullOrWhiteSpace(teamId))
            {
                project.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", teamId);
            }
        }

        private static string GetAppleTeamId()
        {
            var teamId = Environment.GetEnvironmentVariable(AppleTeamIdEnv);
            return string.IsNullOrWhiteSpace(teamId) ? DefaultAppleTeamId : teamId;
        }

        private static string GetProvisioningProfileSpecifier()
        {
            var profile = Environment.GetEnvironmentVariable(ProvisioningProfileEnv);
            return string.IsNullOrWhiteSpace(profile) ? DefaultProvisioningProfileSpecifier : profile;
        }

        private static string GetProvisioningProfileUuid()
        {
            var profileUuid = Environment.GetEnvironmentVariable(ProvisioningProfileUuidEnv);
            return string.IsNullOrWhiteSpace(profileUuid) ? DefaultProvisioningProfileUuid : profileUuid;
        }

        private static void DisableUserScriptSandboxing(PBXProject project, params string[] targetGuids)
        {
            foreach (var targetGuid in targetGuids)
            {
                if (!string.IsNullOrEmpty(targetGuid))
                {
                    project.SetBuildProperty(targetGuid, "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
                }
            }
        }
    }
}

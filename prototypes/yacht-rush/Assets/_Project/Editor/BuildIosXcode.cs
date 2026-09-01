using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MannLab.Games.YachtRush.EditorTools
{
    public static class BuildIosXcode
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/iOS/Xcode";
        private const string SimulatorOutputPath = "Builds/iOS/SimulatorXcode";
        private const string CrashlyticsTestOutputPath = "Builds/iOS/CrashlyticsTestXcode";
        private const string CrashlyticsSimulatorTestOutputPath = "Builds/iOS/CrashlyticsSimulatorTestXcode";
        private const string AdMobTestOutputPath = "Builds/iOS/AdMobTestXcode";
        private const string BundleIdentifier = "com.mannlab.games.yachtrush";
        private const string AppleTeamIdEnv = "MANNLAB_APPLE_TEAM_ID";
        private const string DefaultAppleTeamId = "ZRA4DHHKQ4";
        private const string ProvisioningProfileEnv = "MANNLAB_YACHT_RUSH_IOS_PROFILE_SPECIFIER";
        private const string DefaultProvisioningProfileSpecifier = "Yacht Rush";
        private const string ProvisioningProfileUuidEnv = "MANNLAB_YACHT_RUSH_IOS_PROFILE_UUID";
        private const string DefaultProvisioningProfileUuid = "7ac8efc3-c666-48e9-a209-816db04e5ca7";
        private const string BuildNumberEnv = "MANNLAB_YACHT_RUSH_IOS_BUILD_NUMBER";
        private const string MarketingVersionEnv = "MANNLAB_YACHT_RUSH_IOS_MARKETING_VERSION";
        private const string AdMobIosAppIdEnv = "MANNLAB_YACHT_RUSH_ADMOB_IOS_APP_ID";
        private const string FirebaseIosPlistEnv = "MANNLAB_YACHT_RUSH_FIREBASE_IOS_PLIST";
        private const string FirebaseIosPlistPath = "Assets/GoogleService-Info.plist";
        private const string AdMobIosAppId = "ca-app-pub-4525914685149405~8143053169";
        private const string AdMobIosTestAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string ForceAdMobTestAdsDefine = "MANNLAB_ADMOB_FORCE_TEST_ADS";
        private const string AppIconPath = "Assets/_Project/Art/AppStore/AppIcon-1024.png";

        public static void Build()
        {
            BuildRelease();
        }

        public static void BuildRelease()
        {
            BuildIos(OutputPath, false, iOSSdkVersion.DeviceSDK, false);
        }

        public static void BuildSimulator()
        {
            BuildIos(SimulatorOutputPath, true, iOSSdkVersion.SimulatorSDK, false);
        }

        public static void BuildCrashlyticsTest()
        {
            BuildIos(CrashlyticsTestOutputPath, true, iOSSdkVersion.DeviceSDK, false);
        }

        public static void BuildCrashlyticsSimulatorTest()
        {
            BuildIos(CrashlyticsSimulatorTestOutputPath, true, iOSSdkVersion.SimulatorSDK, false);
        }

        public static void BuildAdMobTest()
        {
            BuildIos(AdMobTestOutputPath, false, iOSSdkVersion.DeviceSDK, true);
        }

        private static void BuildIos(string outputPath, bool developmentBuild, iOSSdkVersion sdkVersion, bool forceAdMobTestAds)
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(outputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            var previousDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;

            try
            {
                SetScriptingDefine(previousDefines, ForceAdMobTestAdsDefine, forceAdMobTestAds);

                PlayerSettings.companyName = "Mann Lab";
                PlayerSettings.productName = "Yacht Rush";
                PlayerSettings.bundleVersion = GetEnvOrDefault(MarketingVersionEnv, "0.1");
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
                PlayerSettings.iOS.buildNumber = GetEnvOrDefault(BuildNumberEnv, "1");
                PlayerSettings.iOS.targetOSVersionString = "15.0";
                PlayerSettings.iOS.sdkVersion = sdkVersion;

                ApplyAppIcon();
                ApplyFirebaseIosConfig();
                ApplySigningHint();

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

                AddMarketingIconToXcodeProject(outputPath);
                AddSimpleLaunchScreensToXcodeProject(outputPath);
                ConfigureInfoPlist(outputPath, forceAdMobTestAds);
                if (sdkVersion == iOSSdkVersion.DeviceSDK)
                {
                    ConfigureArchiveSigning(outputPath);
                }
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, previousDefines);
            }
        }

        private static void ApplySigningHint()
        {
            var teamId = GetAppleTeamId();
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return;
            }

            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = teamId;
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
            for (var index = 0; index < icons.Length; index += 1)
            {
                icons[index] = appIcon;
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
                    $"[Yacht Rush] {FirebaseIosPlistPath} is missing. Set {FirebaseIosPlistEnv} to import the Firebase iOS config before build.");
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

        private static void ConfigureInfoPlist(string outputPath, bool forceAdMobTestAds)
        {
            var plistPath = Path.Combine(outputPath, "Info.plist");
            if (!File.Exists(plistPath))
            {
                throw new FileNotFoundException($"Info.plist not found: {plistPath}");
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetString("CFBundleShortVersionString", PlayerSettings.bundleVersion);
            root.SetString("CFBundleVersion", PlayerSettings.iOS.buildNumber);
            root.SetString("GADApplicationIdentifier", GetAdMobIosAppId(forceAdMobTestAds));
            root.SetBoolean("GADIsAdManagerApp", false);
            SetPortraitOnly(root, "UISupportedInterfaceOrientations");
            SetPortraitOnly(root, "UISupportedInterfaceOrientations~ipad");
            File.WriteAllText(plistPath, plist.WriteToString());
        }

        private static void SetPortraitOnly(PlistElementDict root, string key)
        {
            var orientations = root.CreateArray(key);
            orientations.AddString("UIInterfaceOrientationPortrait");
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
                        <color key=""backgroundColor"" red=""0.9725490196"" green=""0.9607843137"" blue=""0.9215686275"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
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

        private static void ConfigureArchiveSigning(string outputPath)
        {
            var projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            ConfigureDistributionSigning(project, project.GetUnityMainTargetGuid());
            ConfigureFrameworkSigning(project, project.GetUnityFrameworkTargetGuid());

            project.WriteToFile(projectPath);
        }

        private static void ConfigureDistributionSigning(PBXProject project, string targetGuid)
        {
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

        private static void ConfigureFrameworkSigning(PBXProject project, string targetGuid)
        {
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

        private static void SetScriptingDefine(string previousDefines, string symbol, bool enabled)
        {
            var defines = previousDefines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var set = new System.Collections.Generic.HashSet<string>(defines);
            if (enabled)
            {
                set.Add(symbol);
            }
            else
            {
                set.Remove(symbol);
            }

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, string.Join(";", set));
        }

        private static string GetAdMobIosAppId(bool forceAdMobTestAds)
        {
            if (forceAdMobTestAds)
            {
                return AdMobIosTestAppId;
            }

            var appId = Environment.GetEnvironmentVariable(AdMobIosAppIdEnv);
            if (!string.IsNullOrWhiteSpace(appId))
            {
                return appId;
            }

            return AdMobIosAppId;
        }

        private static string GetAppleTeamId()
        {
            return GetEnvOrDefault(AppleTeamIdEnv, DefaultAppleTeamId);
        }

        private static string GetProvisioningProfileSpecifier()
        {
            return GetEnvOrDefault(ProvisioningProfileEnv, DefaultProvisioningProfileSpecifier);
        }

        private static string GetProvisioningProfileUuid()
        {
            return GetEnvOrDefault(ProvisioningProfileUuidEnv, DefaultProvisioningProfileUuid);
        }

        private static string GetEnvOrDefault(string name, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}

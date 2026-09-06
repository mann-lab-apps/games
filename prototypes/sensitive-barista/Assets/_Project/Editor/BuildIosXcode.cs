using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MannLab.Games.SensitiveBarista.EditorTools
{
    public static class BuildIosXcode
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ReleaseOutputPath = "Builds/iOS/Xcode";
        private const string CrashlyticsTestOutputPath = "Builds/iOS/CrashlyticsTestXcode";
        private const string CrashlyticsSimulatorTestOutputPath = "Builds/iOS/CrashlyticsSimulatorTestXcode";
        private const string AdMobTestOutputPath = "Builds/iOS/AdMobTestXcode";
        private const string BundleIdentifier = "com.mannlab.games.toopickycoffee";
        private const string DisplayName = "Too Picky Coffee";
        private const string MarketingVersionEnv = "MANNLAB_TOO_PICKY_COFFEE_IOS_MARKETING_VERSION";
        private const string DefaultMarketingVersion = "1.0";
        private const string BuildNumberEnv = "MANNLAB_TOO_PICKY_COFFEE_IOS_BUILD_NUMBER";
        private const string DefaultBuildNumber = "2";
        private const string AppleTeamIdEnv = "MANNLAB_APPLE_TEAM_ID";
        private const string DefaultAppleTeamId = "ZRA4DHHKQ4";
        private const string ProvisioningProfileEnv = "MANNLAB_TOO_PICKY_COFFEE_IOS_PROFILE_SPECIFIER";
        private const string DefaultProvisioningProfileSpecifier = "Too Picky Coffee";
        private const string ProvisioningProfileUuidEnv = "MANNLAB_TOO_PICKY_COFFEE_IOS_PROFILE_UUID";
        private const string DefaultProvisioningProfileUuid = "aa78feba-c3b8-44d7-975d-8b1eae7b3c05";
        private const string ForceAdMobTestAdsDefine = "MANNLAB_ADMOB_FORCE_TEST_ADS";
        private const string AdMobIosAppIdEnv = "MANNLAB_TOO_PICKY_COFFEE_ADMOB_IOS_APP_ID";
        private const string AdMobIosProductionAppId = "ca-app-pub-4525914685149405~6759852565";
        private const string AdMobIosTestAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string SourceIconPath = "Assets/_Project/Art/TooPickyCoffeeIcon.png";
        private const string AppStoreIconPath = "Assets/_Project/Art/AppStore/AppIcon-1024.png";

        public static void Build()
        {
            BuildRelease();
        }

        [MenuItem("MannLab/Too Picky Coffee/Build iOS Release Xcode")]
        public static void BuildRelease()
        {
            BuildIos(ReleaseOutputPath, false, iOSSdkVersion.DeviceSDK);
        }

        [MenuItem("MannLab/Too Picky Coffee/Build iOS Crashlytics Test Xcode")]
        public static void BuildCrashlyticsTest()
        {
            BuildIos(CrashlyticsTestOutputPath, true, iOSSdkVersion.DeviceSDK);
        }

        [MenuItem("MannLab/Too Picky Coffee/Build iOS Crashlytics Simulator Test Xcode")]
        public static void BuildCrashlyticsSimulatorTest()
        {
            BuildIos(CrashlyticsSimulatorTestOutputPath, true, iOSSdkVersion.SimulatorSDK);
        }

        [MenuItem("MannLab/Too Picky Coffee/Build iOS AdMob Test Xcode")]
        public static void BuildAdMobTest()
        {
            BuildIos(AdMobTestOutputPath, false, iOSSdkVersion.DeviceSDK, true);
        }

        private static void BuildIos(
            string outputPath,
            bool developmentBuild,
            iOSSdkVersion sdkVersion,
            bool forceAdMobTestAds = false)
        {
            Directory.CreateDirectory(outputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = DisplayName;
            PlayerSettings.bundleVersion = GetMarketingVersion();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            var buildNumber = GetBuildNumber();
            PlayerSettings.iOS.buildNumber = buildNumber;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = sdkVersion;

            EnsureAppStoreIcon();
            ApplyAppIcon();
            ApplySigningHint(sdkVersion);

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

            RemoveLegacyCocoaPodsSpecsSource(outputPath);
            AddMarketingIconToXcodeProject(outputPath);
            AddSimpleLaunchScreensToXcodeProject(outputPath);
            ConfigureInfoPlist(outputPath, buildNumber, forceAdMobTestAds);
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

        private static void EnsureAppStoreIcon()
        {
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceIconPath);
            if (source == null)
            {
                throw new FileNotFoundException($"Source icon not found: {SourceIconPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AppStoreIconPath));
            var renderTexture = RenderTexture.GetTemporary(1024, 1024, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;
                var rgbaIcon = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
                rgbaIcon.ReadPixels(new Rect(0f, 0f, 1024f, 1024f), 0, 0);
                rgbaIcon.Apply();

                var rgbIcon = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
                var sourcePixels = rgbaIcon.GetPixels32();
                var outputPixels = new Color32[sourcePixels.Length];
                var background = new Color32(246, 242, 231, 255);
                for (var i = 0; i < sourcePixels.Length; i++)
                {
                    var pixel = sourcePixels[i];
                    var alpha = pixel.a / 255f;
                    outputPixels[i] = new Color32(
                        (byte)Mathf.RoundToInt((pixel.r * alpha) + (background.r * (1f - alpha))),
                        (byte)Mathf.RoundToInt((pixel.g * alpha) + (background.g * (1f - alpha))),
                        (byte)Mathf.RoundToInt((pixel.b * alpha) + (background.b * (1f - alpha))),
                        255);
                }

                rgbIcon.SetPixels32(outputPixels);
                rgbIcon.Apply();
                File.WriteAllBytes(AppStoreIconPath, rgbIcon.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(rgbaIcon);
                UnityEngine.Object.DestroyImmediate(rgbIcon);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            AssetDatabase.ImportAsset(AppStoreIconPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ApplyAppIcon()
        {
            AssetDatabase.ImportAsset(AppStoreIconPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(AppStoreIconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppStoreIconPath);
            if (appIcon == null)
            {
                throw new FileNotFoundException($"iOS app icon not found: {AppStoreIconPath}");
            }

            var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, IconKind.Application);
            var icons = new Texture2D[iconSizes.Length];
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = appIcon;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.iOS, icons, IconKind.Application);
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

            File.Copy(AppStoreIconPath, marketingIconPath, true);
            File.WriteAllText(contentsPath, $@"{{
  ""images"" : [
    {{ ""filename"" : ""Icon-iPhone-120.png"", ""idiom"" : ""iphone"", ""scale"" : ""2x"", ""size"" : ""60x60"" }},
    {{ ""filename"" : ""Icon-iPhone-180.png"", ""idiom"" : ""iphone"", ""scale"" : ""3x"", ""size"" : ""60x60"" }},
    {{ ""filename"" : ""Icon-iPad-76.png"", ""idiom"" : ""ipad"", ""scale"" : ""1x"", ""size"" : ""76x76"" }},
    {{ ""filename"" : ""Icon-iPad-152.png"", ""idiom"" : ""ipad"", ""scale"" : ""2x"", ""size"" : ""76x76"" }},
    {{ ""filename"" : ""Icon-iPad-167.png"", ""idiom"" : ""ipad"", ""scale"" : ""2x"", ""size"" : ""83.5x83.5"" }},
    {{ ""filename"" : ""{marketingIconFile}"", ""idiom"" : ""ios-marketing"", ""scale"" : ""1x"", ""size"" : ""1024x1024"" }}
  ],
  ""info"" : {{ ""author"" : ""xcode"", ""version"" : 1 }},
  ""properties"" : {{ ""pre-rendered"" : false }}
}}
");
        }

        private static void AddSimpleLaunchScreensToXcodeProject(string outputPath)
        {
            WriteSimpleLaunchScreen(Path.Combine(outputPath, "LaunchScreen-iPhone.storyboard"));
            WriteSimpleLaunchScreen(Path.Combine(outputPath, "LaunchScreen-iPad.storyboard"));
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
            plist.root.SetString("CFBundleDisplayName", DisplayName);
            plist.root.SetString("CFBundleName", DisplayName);
            plist.root.SetString("CFBundleShortVersionString", GetMarketingVersion());
            plist.root.SetString("CFBundleVersion", buildNumber);
            var appId = GetAdMobIosAppId(forceAdMobTestAds);
            if (string.IsNullOrWhiteSpace(appId))
            {
                plist.root.values.Remove("GADApplicationIdentifier");
                plist.root.values.Remove("GADIsAdManagerApp");
            }
            else
            {
                plist.root.SetString("GADApplicationIdentifier", appId);
                plist.root.SetBoolean("GADIsAdManagerApp", false);
            }

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

        private static void ApplySigningHint(iOSSdkVersion sdkVersion)
        {
            if (sdkVersion != iOSSdkVersion.DeviceSDK)
            {
                return;
            }

            var teamId = GetAppleTeamId();
            if (string.IsNullOrWhiteSpace(teamId))
            {
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

        private static void ConfigureArchiveSigning(string outputPath)
        {
            var projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            ConfigureMainSigning(project, project.GetUnityMainTargetGuid());
            ConfigureFrameworkSigning(project, project.GetUnityFrameworkTargetGuid());

            project.WriteToFile(projectPath);
            HardenIl2CppBuildScript(projectPath);
        }

        private static void ConfigureMainSigning(PBXProject project, string targetGuid)
        {
            project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Manual");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", "Apple Distribution");
            project.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", "Apple Distribution");
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_SPECIFIER", GetProvisioningProfileSpecifier());
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE", GetProvisioningProfileUuid());

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

        private static string GetMarketingVersion()
        {
            var version = Environment.GetEnvironmentVariable(MarketingVersionEnv);
            return string.IsNullOrWhiteSpace(version) ? DefaultMarketingVersion : version;
        }

        private static string GetBuildNumber()
        {
            var buildNumber = Environment.GetEnvironmentVariable(BuildNumberEnv);
            return string.IsNullOrWhiteSpace(buildNumber) ? DefaultBuildNumber : buildNumber;
        }

        private static string GetAppleTeamId()
        {
            var teamId = Environment.GetEnvironmentVariable(AppleTeamIdEnv);
            if (!string.IsNullOrWhiteSpace(teamId))
            {
                return teamId;
            }

            return string.IsNullOrWhiteSpace(PlayerSettings.iOS.appleDeveloperTeamID)
                ? DefaultAppleTeamId
                : PlayerSettings.iOS.appleDeveloperTeamID;
        }

        private static string GetProvisioningProfileSpecifier()
        {
            var profileSpecifier = Environment.GetEnvironmentVariable(ProvisioningProfileEnv);
            return string.IsNullOrWhiteSpace(profileSpecifier)
                ? DefaultProvisioningProfileSpecifier
                : profileSpecifier;
        }

        private static string GetProvisioningProfileUuid()
        {
            var profileUuid = Environment.GetEnvironmentVariable(ProvisioningProfileUuidEnv);
            return string.IsNullOrWhiteSpace(profileUuid) ? DefaultProvisioningProfileUuid : profileUuid;
        }

        private static string GetAdMobIosAppId(bool forceAdMobTestAds)
        {
            if (forceAdMobTestAds)
            {
                return AdMobIosTestAppId;
            }

            var appId = Environment.GetEnvironmentVariable(AdMobIosAppIdEnv);
            return string.IsNullOrWhiteSpace(appId) ? AdMobIosProductionAppId : appId;
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
    }
}

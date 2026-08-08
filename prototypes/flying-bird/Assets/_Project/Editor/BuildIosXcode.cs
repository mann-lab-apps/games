using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MannLab.Games.FlyingBird.EditorTools
{
    public static class BuildIosXcode
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/iOS/Xcode";
        private const string BundleIdentifier = "com.mannlab.games.windgull";
        private const string AppleTeamIdEnv = "MANNLAB_APPLE_TEAM_ID";
        private const string DefaultAppleTeamId = "ZRA4DHHKQ4";
        private const string ProvisioningProfileEnv = "MANNLAB_WIND_GULL_IOS_PROFILE_SPECIFIER";
        private const string DefaultProvisioningProfileSpecifier = "Wind Gull";
        private const string ProvisioningProfileUuidEnv = "MANNLAB_WIND_GULL_IOS_PROFILE_UUID";
        private const string DefaultProvisioningProfileUuid = "606bba50-83f6-4fc2-98b2-95e7b60179be";
        private const string AppIconPath = "Assets/_Project/Art/AppStore/AppIcon-1024.png";

        public static void Build()
        {
            if (!File.Exists(ScenePath))
            {
                CreateGameScene.Create();
            }

            Directory.CreateDirectory(OutputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "Wind Gull";
            PlayerSettings.bundleVersion = "0.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            ApplyAppIcon();
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

            AddMarketingIconToXcodeProject();
            AddSimpleLaunchScreensToXcodeProject();
            ConfigureArchiveSigning();
        }

        private static void ApplySigningHint()
        {
            var teamId = GetAppleTeamId();
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return;
            }

            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
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
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = appIcon;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.iOS, icons, IconKind.Application);
        }

        private static void AddMarketingIconToXcodeProject()
        {
            const string marketingIconFile = "Icon-AppStore-1024.png";
            var appIconSetPath = Path.Combine(
                OutputPath,
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

        private static void AddSimpleLaunchScreensToXcodeProject()
        {
            WriteSimpleLaunchScreen(Path.Combine(OutputPath, "LaunchScreen-iPhone.storyboard"));
            WriteSimpleLaunchScreen(Path.Combine(OutputPath, "LaunchScreen-iPad.storyboard"));
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
                        <color key=""backgroundColor"" red=""0.9843137255"" green=""0.9568627451"" blue=""0.8941176471"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
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

        private static void ConfigureArchiveSigning()
        {
            var projectPath = PBXProject.GetPBXProjectPath(OutputPath);
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
            project.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_APP", GetProvisioningProfileUuid());

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
            if (!string.IsNullOrWhiteSpace(profileSpecifier))
            {
                return profileSpecifier;
            }

            return DefaultProvisioningProfileSpecifier;
        }

        private static string GetProvisioningProfileUuid()
        {
            var profileUuid = Environment.GetEnvironmentVariable(ProvisioningProfileUuidEnv);
            if (!string.IsNullOrWhiteSpace(profileUuid))
            {
                return profileUuid;
            }

            return DefaultProvisioningProfileUuid;
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

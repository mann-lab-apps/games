using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.YachtRush.EditorTools
{
    public static class BuildWebGL
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/WebGL/yacht-rush";

        public static void Build()
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(OutputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbols = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "Yacht Rush";
            PlayerSettings.bundleVersion = "0.1";

            var compatibilityPlistPath = Path.Combine(OutputPath, "Info.plist");
            WriteAdMobWebGlCompatibilityPlist(compatibilityPlistPath);

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = OutputPath,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
            }
            finally
            {
                DeleteAdMobWebGlCompatibilityPlist(compatibilityPlistPath);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }

            PatchResponsiveTemplate();
            PatchCacheBusting();
        }

        private static void WriteAdMobWebGlCompatibilityPlist(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
  <key>CFBundleIdentifier</key>
  <string>com.mannlab.games.yachtrush.webgl</string>
</dict>
</plist>
");
        }

        private static void DeleteAdMobWebGlCompatibilityPlist(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void PatchResponsiveTemplate()
        {
            var stylePath = Path.Combine(OutputPath, "TemplateData", "style.css");
            if (!File.Exists(stylePath))
            {
                return;
            }

            const string marker = "/* Mann Lab responsive WebGL shell */";
            var style = File.ReadAllText(stylePath);
            if (style.Contains(marker))
            {
                return;
            }

            File.AppendAllText(
                stylePath,
                @"

/* Mann Lab responsive WebGL shell */
html, body {
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: #f8f5eb;
}

#unity-container,
#unity-container.unity-desktop,
#unity-container.unity-mobile {
  position: fixed;
  inset: 0;
  width: 100%;
  height: 100%;
  transform: none;
  left: 0;
  top: 0;
}

#unity-canvas,
.unity-mobile #unity-canvas {
  width: 100vw !important;
  height: 100vh !important;
  display: block;
  background: #f8f5eb;
}

#unity-footer {
  display: none;
}
");
        }

        private static void PatchCacheBusting()
        {
            var indexPath = Path.Combine(OutputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var html = File.ReadAllText(indexPath);
            html = html.Replace(
                "var loaderUrl = buildUrl + \"/yacht-rush.loader.js\";",
                $"var buildVersion = \"?v={version}\";\n      var loaderUrl = buildUrl + \"/yacht-rush.loader.js\" + buildVersion;");
            html = html.Replace(
                "dataUrl: buildUrl + \"/yacht-rush.data\",",
                "dataUrl: buildUrl + \"/yacht-rush.data\" + buildVersion,");
            html = html.Replace(
                "frameworkUrl: buildUrl + \"/yacht-rush.framework.js\",",
                "frameworkUrl: buildUrl + \"/yacht-rush.framework.js\" + buildVersion,");
            html = html.Replace(
                "codeUrl: buildUrl + \"/yacht-rush.wasm\",",
                "codeUrl: buildUrl + \"/yacht-rush.wasm\" + buildVersion,");
            File.WriteAllText(indexPath, html);
        }
    }
}

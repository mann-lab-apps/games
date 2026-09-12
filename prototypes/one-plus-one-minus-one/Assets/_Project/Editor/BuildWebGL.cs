using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.OnePlusOneMinusOne.EditorTools
{
    public static class BuildWebGL
    {
        private const string ProductName = "1 = 1";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string IconAssetPath = "Assets/_Project/Art/AppIcon-1024.png";
        private const string OutputPath = "Builds/WebGL/one-plus-one-minus-one";
        private const string QaOutputPath = "Builds/WebGL/one-plus-one-minus-one-qa";
        private const string StoreCaptureOutputPath = "Builds/WebGL/one-plus-one-minus-one-store-capture";
        private const string StoreCaptureDefine = "MANNLAB_STORE_CAPTURE";

        public static void Build()
        {
            BuildInternal(OutputPath, false);
        }

        public static void BuildDevelopmentQa()
        {
            BuildInternal(QaOutputPath, true);
        }

        public static void BuildStoreCapture()
        {
            BuildInternal(StoreCaptureOutputPath, false, true);
        }

        private static void BuildInternal(string outputPath, bool developmentBuild, bool storeCaptureBuild = false)
        {
            GenerateAppIcon.Run();
            CreateGameScene.Create();
            VerifyGoalMode.Run();
            Directory.CreateDirectory(outputPath);
            var buildName = Path.GetFileName(outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;
            PlayerSettings.stripEngineCode = false;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbols = developmentBuild;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1";

            var namedBuildTarget = NamedBuildTarget.WebGL;
            var previousDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            try
            {
                PlayerSettings.SetScriptingDefineSymbols(
                    namedBuildTarget,
                    SetScriptingDefine(previousDefines, StoreCaptureDefine, storeCaptureBuild));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    options = developmentBuild ? BuildOptions.Development : BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
                }
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, previousDefines);
            }

            PatchResponsiveTemplate(outputPath);
            PatchCacheBusting(outputPath, buildName);
            PatchPageBranding(outputPath);
            PatchPageIcon(outputPath);
        }

        private static string SetScriptingDefine(string currentDefines, string define, bool enabled)
        {
            var symbols = currentDefines.Split(';');
            var nextDefines = string.Empty;
            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i].Trim();
                if (string.IsNullOrEmpty(symbol) || symbol == define)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(nextDefines))
                {
                    nextDefines += ";";
                }

                nextDefines += symbol;
            }

            if (enabled)
            {
                if (!string.IsNullOrEmpty(nextDefines))
                {
                    nextDefines += ";";
                }

                nextDefines += define;
            }

            return nextDefines;
        }

        private static void PatchResponsiveTemplate(string outputPath)
        {
            var stylePath = Path.Combine(outputPath, "TemplateData", "style.css");
            if (!File.Exists(stylePath))
            {
                return;
            }

            const string marker = "/* Mann Lab responsive WebGL shell */";
            var style = File.ReadAllText(stylePath);
            style = style.Replace("#faf7ef", "#fffffc");
            if (style.Contains(marker))
            {
                File.WriteAllText(stylePath, style);
                return;
            }

            File.AppendAllText(
                stylePath,
                @"

/* Mann Lab responsive WebGL shell */
html, body {
  width: 100%;
  height: 100%;
  margin: 0;
  overflow: hidden;
  background: #fffffc;
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
  background: #fffffc;
}

#unity-footer {
  display: none;
}
");
        }

        private static void PatchCacheBusting(string outputPath, string buildName)
        {
            var indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var html = File.ReadAllText(indexPath);
            html = html.Replace(
                $"var loaderUrl = buildUrl + \"/{buildName}.loader.js\";",
                $"var buildVersion = \"?v={version}\";\n      var loaderUrl = buildUrl + \"/{buildName}.loader.js\" + buildVersion;");
            html = html.Replace(
                $"dataUrl: buildUrl + \"/{buildName}.data\",",
                $"dataUrl: buildUrl + \"/{buildName}.data\" + buildVersion,");
            html = html.Replace(
                $"frameworkUrl: buildUrl + \"/{buildName}.framework.js\",",
                $"frameworkUrl: buildUrl + \"/{buildName}.framework.js\" + buildVersion,");
            html = html.Replace(
                $"codeUrl: buildUrl + \"/{buildName}.wasm\",",
                $"codeUrl: buildUrl + \"/{buildName}.wasm\" + buildVersion,");
            File.WriteAllText(indexPath, html);
        }

        private static void PatchPageBranding(string outputPath)
        {
            var indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var html = File.ReadAllText(indexPath);
            html = Regex.Replace(html, "<title>.*?</title>", $"<title>{ProductName}</title>", RegexOptions.IgnoreCase);
            html = EnsureHeadTag(
                html,
                "name=\"description\"",
                "    <meta name=\"description\" content=\"A tiny stick-friend equation puzzle about making 1 equal 1.\">");
            html = EnsureHeadTag(html, "name=\"application-name\"", $"    <meta name=\"application-name\" content=\"{ProductName}\">");
            html = EnsureHeadTag(html, "name=\"apple-mobile-web-app-title\"", $"    <meta name=\"apple-mobile-web-app-title\" content=\"{ProductName}\">");
            html = EnsureHeadTag(html, "name=\"theme-color\"", "    <meta name=\"theme-color\" content=\"#fffffc\">");
            html = Regex.Replace(html, "<div id=\"unity-build-title\">.*?</div>", $"<div id=\"unity-build-title\">{ProductName}</div>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "productName: \".*?\",", $"productName: \"{ProductName}\",");
            File.WriteAllText(indexPath, html);
        }

        private static string EnsureHeadTag(string html, string marker, string tag)
        {
            if (html.Contains(marker))
            {
                return html;
            }

            return Regex.Replace(html, "</head>", $"{tag}\n</head>", RegexOptions.IgnoreCase);
        }

        private static void PatchPageIcon(string outputPath)
        {
            var indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath) || !File.Exists(IconAssetPath))
            {
                return;
            }

            const string iconFileName = "app-icon.png";
            File.Copy(IconAssetPath, Path.Combine(outputPath, iconFileName), true);

            var html = File.ReadAllText(indexPath);
            html = EnsureHeadTag(html, $"rel=\"icon\" type=\"image/png\" href=\"{iconFileName}\"", $"    <link rel=\"icon\" type=\"image/png\" href=\"{iconFileName}\">");
            html = EnsureHeadTag(html, $"rel=\"apple-touch-icon\" href=\"{iconFileName}\"", $"    <link rel=\"apple-touch-icon\" href=\"{iconFileName}\">");
            File.WriteAllText(indexPath, html);
        }
    }
}

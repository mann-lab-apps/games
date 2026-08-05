using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.Game2048Crash.EditorTools
{
    public static class BuildWebGL
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/WebGL/2048-crash";

        public static void Build()
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(OutputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "2048 Crash";
            PlayerSettings.bundleVersion = "0.1";
            ConfigureWebGLCompression();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }

            PatchResponsiveTemplate();
        }

        private static void ConfigureWebGLCompression()
        {
            var webGlSettings = typeof(PlayerSettings).GetNestedType("WebGL", BindingFlags.Public | BindingFlags.NonPublic);
            if (webGlSettings == null)
            {
                throw new InvalidOperationException("Unity PlayerSettings.WebGL API was not found.");
            }

            var compressionFormat = webGlSettings.GetProperty("compressionFormat", BindingFlags.Public | BindingFlags.Static);
            var decompressionFallback = webGlSettings.GetProperty("decompressionFallback", BindingFlags.Public | BindingFlags.Static);
            if (compressionFormat == null || decompressionFallback == null)
            {
                throw new InvalidOperationException("Unity WebGL compression settings API was not found.");
            }

            compressionFormat.SetValue(null, Enum.Parse(compressionFormat.PropertyType, "Gzip"));
            decompressionFallback.SetValue(null, true);
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
    }
}

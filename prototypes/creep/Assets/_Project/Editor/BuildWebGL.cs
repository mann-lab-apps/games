using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.Creep.EditorTools
{
    public static class BuildWebGL
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/WebGL/creep";

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
            PlayerSettings.productName = "Please Move to the Back of the Mart";
            PlayerSettings.bundleVersion = "0.1";

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
  background: #080b0f;
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
  background: #080b0f;
}

#unity-footer {
  display: none;
}
");
        }
    }
}

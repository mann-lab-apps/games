using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MannLab.Games.GatherAndShot.EditorTools
{
    public static class BuildWebGL
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/WebGL/gather-and-shot";

        public static void Build()
        {
            CreateGameScene.Create();
            Directory.CreateDirectory(OutputPath);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

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

            PatchWebGLShell();
        }

        private static void PatchWebGLShell()
        {
            var indexPath = Path.Combine(OutputPath, "index.html");
            if (File.Exists(indexPath))
            {
                var html = File.ReadAllText(indexPath)
                    .Replace("Unity Web Player | Gather _ Shot", "Gather & Shot")
                    .Replace("Gather _ Shot", "Gather & Shot")
                    .Replace("<canvas id=\"unity-canvas\" width=960 height=600 tabindex=\"-1\"></canvas>", "<canvas id=\"unity-canvas\" width=540 height=960 tabindex=\"-1\"></canvas>")
                    .Replace("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">", "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n    <meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes, viewport-fit=cover\">")
                    .Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"540px\";")
                    .Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"960px\";");
                File.WriteAllText(indexPath, html);
            }

            var stylePath = Path.Combine(OutputPath, "TemplateData", "style.css");
            Directory.CreateDirectory(Path.GetDirectoryName(stylePath));
            File.WriteAllText(
                stylePath,
                @"html, body {
  width: 100%;
  height: 100%;
  padding: 0;
  margin: 0;
  overflow: hidden;
  background: #101010;
}

#unity-container {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #101010;
}

#unity-container.unity-desktop {
  left: 0;
  top: 0;
  transform: none;
}

#unity-container.unity-mobile {
  position: fixed;
  width: 100%;
  height: 100%;
}

#unity-canvas {
  width: min(100vw, calc(100vh * 9 / 16)) !important;
  height: min(100vh, calc(100vw * 16 / 9)) !important;
  aspect-ratio: 9 / 16;
  max-width: 100vw;
  max-height: 100vh;
  background: #000;
  display: block;
  outline: 0;
  touch-action: none;
}

.unity-mobile #unity-canvas {
  width: 100vw !important;
  height: 100vh !important;
}

#unity-loading-bar {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  display: none;
}

#unity-logo {
  display: none;
}

#unity-progress-bar-empty {
  width: 180px;
  height: 10px;
  overflow: hidden;
  border-radius: 5px;
  background: rgba(255,255,255,0.2);
}

#unity-progress-bar-full {
  width: 0%;
  height: 10px;
  background: #58a6ce;
}

#unity-footer,
#unity-logo-title-footer,
#unity-build-title,
#unity-fullscreen-button {
  display: none !important;
}

#unity-warning {
  position: absolute;
  left: 50%;
  top: 16px;
  transform: translateX(-50%);
  max-width: min(88vw, 420px);
  background: white;
  color: #222;
  padding: 10px;
  display: none;
  font-family: Arial, sans-serif;
  font-size: 14px;
}
");
        }
    }
}

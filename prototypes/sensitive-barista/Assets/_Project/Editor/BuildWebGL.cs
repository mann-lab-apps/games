using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MannLab.Games.SensitiveBarista.EditorTools
{
    public static class BuildWebGL
    {
        private const string ProductName = "Too Picky Coffee";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/WebGL/sensitive-barista";
        private const string IconAssetPath = "Assets/_Project/Art/TooPickyCoffeeIcon.png";
        private const string IconFileName = "too-picky-coffee-icon.png";

        public static void Build()
        {
            Directory.CreateDirectory(OutputPath);
            EnsureBrandingAssets();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            PlayerSettings.stripEngineCode = false;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbols = false;

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = ProductName;
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
            PatchCacheBusting();
            PatchPageBranding();
        }

        private static void EnsureBrandingAssets()
        {
            var iconDirectory = Path.GetDirectoryName(IconAssetPath);
            if (!string.IsNullOrEmpty(iconDirectory))
            {
                Directory.CreateDirectory(iconDirectory);
            }

            var icon = CreateIconTexture(512);
            File.WriteAllBytes(IconAssetPath, icon.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(icon);
            AssetDatabase.ImportAsset(IconAssetPath, ImportAssetOptions.ForceUpdate);
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
                "var loaderUrl = buildUrl + \"/sensitive-barista.loader.js\";",
                $"var buildVersion = \"?v={version}\";\n      var loaderUrl = buildUrl + \"/sensitive-barista.loader.js\" + buildVersion;");
            html = html.Replace(
                "dataUrl: buildUrl + \"/sensitive-barista.data\",",
                "dataUrl: buildUrl + \"/sensitive-barista.data\" + buildVersion,");
            html = html.Replace(
                "frameworkUrl: buildUrl + \"/sensitive-barista.framework.js\",",
                "frameworkUrl: buildUrl + \"/sensitive-barista.framework.js\" + buildVersion,");
            html = html.Replace(
                "codeUrl: buildUrl + \"/sensitive-barista.wasm\",",
                "codeUrl: buildUrl + \"/sensitive-barista.wasm\" + buildVersion,");
            File.WriteAllText(indexPath, html);
        }

        private static void PatchPageBranding()
        {
            var indexPath = Path.Combine(OutputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var templateDataPath = Path.Combine(OutputPath, "TemplateData");
            Directory.CreateDirectory(templateDataPath);
            File.Copy(IconAssetPath, Path.Combine(templateDataPath, IconFileName), true);

            var html = File.ReadAllText(indexPath);
            html = Regex.Replace(html, "<title>.*?</title>", $"<title>{ProductName}</title>", RegexOptions.IgnoreCase);
            if (!html.Contains(IconFileName, StringComparison.Ordinal))
            {
                html = html.Replace(
                    "</head>",
                    $"  <link rel=\"icon\" type=\"image/png\" href=\"TemplateData/{IconFileName}\">\n" +
                    $"  <link rel=\"apple-touch-icon\" href=\"TemplateData/{IconFileName}\">\n" +
                    "</head>");
            }

            File.WriteAllText(indexPath, html);
        }

        private static Texture2D CreateIconTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Too Picky Coffee Icon"
            };

            Fill(texture, new Color32(248, 245, 235, 255));
            DrawRoundedRect(texture, 24, 24, size - 48, size - 48, 78, new Color32(255, 253, 247, 255));
            DrawRoundedRectOutline(texture, 24, 24, size - 48, size - 48, 78, 10, new Color32(44, 40, 35, 255));

            var glass = new[]
            {
                new Vector2(118f, 398f),
                new Vector2(394f, 398f),
                new Vector2(348f, 88f),
                new Vector2(164f, 88f)
            };
            FillPolygon(texture, glass, new Color32(229, 239, 236, 210));

            FillLiquidLayer(texture, 98f, 202f, new Color32(111, 72, 54, 255));
            FillLiquidLayer(texture, 202f, 240f, new Color32(255, 226, 166, 255));
            FillLiquidLayer(texture, 240f, 318f, new Color32(96, 187, 233, 235));
            DrawWavySurface(texture, 318f, new Color32(65, 141, 178, 210));

            DrawIceShard(texture, new Vector2(178f, 324f), 46f, 42f, -14f);
            DrawIceShard(texture, new Vector2(230f, 346f), 52f, 48f, 11f);
            DrawIceShard(texture, new Vector2(286f, 334f), 50f, 44f, -8f);
            DrawIceShard(texture, new Vector2(338f, 344f), 48f, 45f, 16f);

            DrawLine(texture, new Vector2(118f, 398f), new Vector2(394f, 398f), 12f, new Color32(38, 35, 31, 255));
            DrawLine(texture, new Vector2(118f, 398f), new Vector2(164f, 88f), 10f, new Color32(55, 54, 49, 255));
            DrawLine(texture, new Vector2(394f, 398f), new Vector2(348f, 88f), 10f, new Color32(55, 54, 49, 255));
            DrawLine(texture, new Vector2(164f, 88f), new Vector2(348f, 88f), 10f, new Color32(55, 54, 49, 255));
            DrawLine(texture, new Vector2(188f, 112f), new Vector2(324f, 112f), 4f, new Color32(44, 40, 35, 190));
            DrawLine(texture, new Vector2(158f, 350f), new Vector2(354f, 350f), 5f, new Color32(238, 168, 64, 235));
            DrawPickyBubble(texture);

            texture.Apply(false, false);
            return texture;
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            var pixels = new Color32[texture.width * texture.height];
            for (var index = 0; index < pixels.Length; index += 1)
            {
                pixels[index] = color;
            }

            texture.SetPixels32(pixels);
        }

        private static void DrawRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color32 color)
        {
            for (var py = y; py < y + height; py += 1)
            {
                for (var px = x; px < x + width; px += 1)
                {
                    if (InsideRoundedRect(px, py, x, y, width, height, radius))
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static void DrawRoundedRectOutline(Texture2D texture, int x, int y, int width, int height, int radius, int thickness, Color32 color)
        {
            for (var py = y; py < y + height; py += 1)
            {
                for (var px = x; px < x + width; px += 1)
                {
                    if (InsideRoundedRect(px, py, x, y, width, height, radius) &&
                        !InsideRoundedRect(px, py, x + thickness, y + thickness, width - thickness * 2, height - thickness * 2, Math.Max(0, radius - thickness)))
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static bool InsideRoundedRect(int px, int py, int x, int y, int width, int height, int radius)
        {
            var cx = Clamp(px, x + radius, x + width - radius - 1);
            var cy = Clamp(py, y + radius, y + height - radius - 1);
            var dx = px - cx;
            var dy = py - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static void FillLiquidLayer(Texture2D texture, float bottomY, float topY, Color32 color)
        {
            FillPolygon(
                texture,
                new[]
                {
                    new Vector2(CupLeftAtIconY(bottomY), bottomY),
                    new Vector2(CupRightAtIconY(bottomY), bottomY),
                    new Vector2(CupRightAtIconY(topY), topY),
                    new Vector2(CupLeftAtIconY(topY), topY)
                },
                color);
        }

        private static float CupLeftAtIconY(float y)
        {
            return Mathf.Lerp(164f, 118f, Mathf.InverseLerp(88f, 398f, y));
        }

        private static float CupRightAtIconY(float y)
        {
            return Mathf.Lerp(348f, 394f, Mathf.InverseLerp(88f, 398f, y));
        }

        private static void DrawWavySurface(Texture2D texture, float y, Color32 color)
        {
            var left = CupLeftAtIconY(y) + 6f;
            var right = CupRightAtIconY(y) - 6f;
            var previous = new Vector2(left, y);
            for (var step = 1; step <= 32; step += 1)
            {
                var t = step / 32f;
                var next = new Vector2(Mathf.Lerp(left, right, t), y + Mathf.Sin(t * Mathf.PI * 3f) * 3f);
                DrawLine(texture, previous, next, 4f, color);
                previous = next;
            }
        }

        private static void DrawIceShard(Texture2D texture, Vector2 center, float width, float height, float angleDegrees)
        {
            var angle = angleDegrees * Mathf.Deg2Rad;
            var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var up = new Vector2(-right.y, right.x);
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var points = new[]
            {
                center - right * halfWidth - up * halfHeight,
                center + right * halfWidth - up * (halfHeight * 0.82f),
                center + right * (halfWidth * 0.88f) + up * halfHeight,
                center - right * (halfWidth * 0.92f) + up * (halfHeight * 0.9f)
            };

            FillPolygon(texture, points, new Color32(189, 235, 255, 235));
            for (var index = 0; index < points.Length; index += 1)
            {
                DrawLine(texture, points[index], points[(index + 1) % points.Length], 3f, new Color32(72, 145, 170, 170));
            }

            DrawLine(texture, center - right * 8f - up * 12f, center + right * 8f + up * 12f, 3f, new Color32(255, 255, 247, 180));
        }

        private static void DrawPickyBubble(Texture2D texture)
        {
            DrawRoundedRect(texture, 326, 346, 96, 76, 22, new Color32(255, 253, 247, 245));
            DrawRoundedRectOutline(texture, 326, 346, 96, 76, 22, 5, new Color32(44, 40, 35, 245));
            FillPolygon(
                texture,
                new[]
                {
                    new Vector2(350f, 350f),
                    new Vector2(330f, 326f),
                    new Vector2(370f, 346f)
                },
                new Color32(255, 253, 247, 245));
            DrawLine(texture, new Vector2(348f, 350f), new Vector2(331f, 329f), 5f, new Color32(44, 40, 35, 245));
            DrawLine(texture, new Vector2(331f, 329f), new Vector2(369f, 347f), 5f, new Color32(44, 40, 35, 245));
            DrawLine(texture, new Vector2(374f, 398f), new Vector2(374f, 372f), 8f, new Color32(238, 168, 64, 255));
            DrawLine(texture, new Vector2(374f, 360f), new Vector2(374f, 356f), 8f, new Color32(238, 168, 64, 255));
        }

        private static void FillPolygon(Texture2D texture, Vector2[] points, Color32 color)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(Min(points, point => point.x)), 0, texture.width - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Max(points, point => point.x)), 0, texture.width - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Min(points, point => point.y)), 0, texture.height - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Max(points, point => point.y)), 0, texture.height - 1);

            for (var y = minY; y <= maxY; y += 1)
            {
                for (var x = minX; x <= maxX; x += 1)
                {
                    if (PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), points))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (int index = 0, previous = polygon.Length - 1; index < polygon.Length; previous = index, index += 1)
            {
                var a = polygon[index];
                var b = polygon[previous];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, float thickness, Color32 color)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(start.x, end.x) - thickness), 0, texture.width - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(start.x, end.x) + thickness), 0, texture.width - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(start.y, end.y) - thickness), 0, texture.height - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(start.y, end.y) + thickness), 0, texture.height - 1);
            var segment = end - start;
            var lengthSquared = Mathf.Max(0.0001f, segment.sqrMagnitude);
            var radiusSquared = thickness * thickness * 0.25f;

            for (var y = minY; y <= maxY; y += 1)
            {
                for (var x = minX; x <= maxX; x += 1)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
                    var closest = start + segment * t;
                    if ((point - closest).sqrMagnitude <= radiusSquared)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static float Min(Vector2[] points, Func<Vector2, float> selector)
        {
            var value = selector(points[0]);
            for (var index = 1; index < points.Length; index += 1)
            {
                value = Mathf.Min(value, selector(points[index]));
            }

            return value;
        }

        private static float Max(Vector2[] points, Func<Vector2, float> selector)
        {
            var value = selector(points[0]);
            for (var index = 1; index < points.Length; index += 1)
            {
                value = Mathf.Max(value, selector(points[index]));
            }

            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}

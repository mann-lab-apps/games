using System.IO;
using UnityEditor;
using UnityEngine;

namespace MannLab.Games.OnePlusOneMinusOne.EditorTools
{
    public static class GenerateAppIcon
    {
        private const int Size = 1024;
        private const string OutputPath = "Assets/_Project/Art/AppIcon-1024.png";

        public static void Run()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            var pixels = new Color32[Size * Size];
            var paper = new Color32(255, 255, 252, 255);
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = paper;
            }

            DrawIconBackdrop(pixels);
            DrawStickFriend(pixels, new Vector2(256f, 512f), 118f, 424f, 22f, new Color32(255, 253, 244, 255), new Color32(255, 187, 178, 125), 5);
            DrawEqualsFriend(pixels, new Vector2(512f, 512f));
            DrawStickFriend(pixels, new Vector2(768f, 512f), 118f, 424f, 22f, new Color32(255, 253, 244, 255), new Color32(255, 187, 178, 125), 17);

            texture.SetPixels32(pixels);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, bytes);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
        }

        private static void DrawIconBackdrop(Color32[] pixels)
        {
            DrawRoundedRect(pixels, new Rect(86f, 86f, 852f, 852f), 92f, new Color32(255, 250, 228, 255), new Color32(38, 37, 34, 255), 10f, 41);
        }

        private static void DrawStickFriend(Color32[] pixels, Vector2 center, float width, float height, float radius, Color32 fill, Color32 hatch, int seed)
        {
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            DrawRoundedRect(pixels, rect, radius, fill, new Color32(38, 37, 34, 255), 8f, seed);

            for (var i = -8; i < 20; i++)
            {
                var x = rect.xMin + i * 34f + Jitter(seed + i * 23, 9f);
                DrawClippedHatch(pixels, rect, radius - 12f, new Vector2(x, rect.yMin + 22f), new Vector2(x + height * 0.78f, rect.yMax - 22f), hatch, 4f);
            }

            var faceY = center.y + height * 0.23f;
            DrawCircle(pixels, new Vector2(center.x - 26f, faceY + 8f), 10.5f, new Color32(56, 54, 49, 230));
            DrawCircle(pixels, new Vector2(center.x + 26f, faceY + 8f), 10.5f, new Color32(56, 54, 49, 230));
            DrawRoundedRect(pixels, new Rect(center.x - 16f, faceY - 38f, 32f, 25f), 3f, new Color32(255, 255, 252, 255), new Color32(52, 50, 46, 235), 4f, seed + 7);
        }

        private static void DrawEqualsFriend(Color32[] pixels, Vector2 center)
        {
            var fill = new Color32(255, 235, 156, 255);
            var hatch = new Color32(218, 170, 54, 118);
            DrawHorizontalStickFriend(pixels, center + new Vector2(0f, 48f), 264f, 74f, 12f, fill, hatch, 31, false);
            DrawHorizontalStickFriend(pixels, center + new Vector2(0f, -48f), 264f, 74f, 12f, fill, hatch, 47, true);
        }

        private static void DrawHorizontalStickFriend(Color32[] pixels, Vector2 center, float width, float height, float radius, Color32 fill, Color32 hatch, int seed, bool lower)
        {
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            DrawRoundedRect(pixels, rect, radius, fill, new Color32(38, 37, 34, 255), 7f, seed);

            for (var i = -3; i < 10; i++)
            {
                var x = rect.xMin + i * 30f + Jitter(seed + i * 17, 7f);
                DrawClippedHatch(pixels, rect, radius - 5f, new Vector2(x, rect.yMin + 9f), new Vector2(x + width * 0.52f, rect.yMax - 8f), hatch, 3f);
            }

            var faceCenter = center + new Vector2(lower ? 44f : 40f, lower ? -1f : 1f);
            DrawCircle(pixels, faceCenter + new Vector2(-14f, 8f), 5.3f, new Color32(56, 54, 49, 205));
            DrawCircle(pixels, faceCenter + new Vector2(14f, 7f), 5f, new Color32(56, 54, 49, 205));
            DrawEllipse(
                pixels,
                faceCenter + new Vector2(5f, -7f),
                lower ? new Vector2(24f, 12f) : new Vector2(22f, 13f),
                new Color32(255, 255, 252, 255),
                new Color32(52, 50, 46, 230),
                3f,
                seed + 7);
        }

        private static void DrawRoundedRect(Color32[] pixels, Rect rect, float radius, Color32 fill, Color32 outline, float outlineThickness, int seed)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(rect.xMin - outlineThickness - 2f), 0, Size - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(rect.xMax + outlineThickness + 2f), 0, Size - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(rect.yMin - outlineThickness - 2f), 0, Size - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(rect.yMax + outlineThickness + 2f), 0, Size - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var distance = RoundedRectDistance(p, rect, radius);
                    if (distance <= 0f)
                    {
                        SetPixel(pixels, x, y, fill);
                    }
                    else if (distance <= outlineThickness + Jitter(seed + x * 3 + y * 5, 1.6f))
                    {
                        SetPixel(pixels, x, y, outline);
                    }
                }
            }
        }

        private static void DrawClippedHatch(Color32[] pixels, Rect rect, float radius, Vector2 start, Vector2 end, Color32 color, float thickness)
        {
            const int steps = 96;
            var previous = start;
            var hasPrevious = false;
            for (var i = 0; i <= steps; i++)
            {
                var point = Vector2.Lerp(start, end, i / (float)steps);
                var inside = RoundedRectDistance(point, rect, radius) <= -8f;
                if (inside && hasPrevious)
                {
                    DrawLine(pixels, previous, point, color, thickness);
                }

                previous = point;
                hasPrevious = inside;
            }
        }

        private static void DrawLine(Color32[] pixels, Vector2 start, Vector2 end, Color32 color, float thickness)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(start.x, end.x) - thickness), 0, Size - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(start.x, end.x) + thickness), 0, Size - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(start.y, end.y) - thickness), 0, Size - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(start.y, end.y) + thickness), 0, Size - 1);
            var segment = end - start;
            var lengthSquared = Mathf.Max(1f, segment.sqrMagnitude);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
                    var closest = start + segment * t;
                    if ((point - closest).magnitude <= thickness * 0.5f)
                    {
                        SetPixel(pixels, x, y, color);
                    }
                }
            }
        }

        private static void DrawCircle(Color32[] pixels, Vector2 center, float radius, Color32 color)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(center.x - radius), 0, Size - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + radius), 0, Size - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(center.y - radius), 0, Size - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + radius), 0, Size - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if ((new Vector2(x + 0.5f, y + 0.5f) - center).sqrMagnitude <= radius * radius)
                    {
                        SetPixel(pixels, x, y, color);
                    }
                }
            }
        }

        private static void DrawEllipse(Color32[] pixels, Vector2 center, Vector2 size, Color32 fill, Color32 outline, float outlineThickness, int seed)
        {
            var radius = size * 0.5f;
            var minX = Mathf.Clamp(Mathf.FloorToInt(center.x - radius.x - outlineThickness - 2f), 0, Size - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + radius.x + outlineThickness + 2f), 0, Size - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(center.y - radius.y - outlineThickness - 2f), 0, Size - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + radius.y + outlineThickness + 2f), 0, Size - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var normalized = new Vector2((point.x - center.x) / radius.x, (point.y - center.y) / radius.y);
                    var distance = normalized.magnitude;
                    if (distance <= 1f)
                    {
                        SetPixel(pixels, x, y, fill);
                    }
                    else if ((distance - 1f) * Mathf.Min(radius.x, radius.y) <= outlineThickness + Jitter(seed + x * 5 + y * 7, 0.8f))
                    {
                        SetPixel(pixels, x, y, outline);
                    }
                }
            }
        }

        private static float RoundedRectDistance(Vector2 point, Rect rect, float radius)
        {
            var center = rect.center;
            var half = new Vector2(rect.width * 0.5f - radius, rect.height * 0.5f - radius);
            var q = new Vector2(Mathf.Abs(point.x - center.x), Mathf.Abs(point.y - center.y)) - half;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Jitter(int value, float amount)
        {
            var n = Mathf.Sin(value * 12.9898f) * 43758.5453f;
            return (n - Mathf.Floor(n) - 0.5f) * amount;
        }

        private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
        {
            pixels[y * Size + x] = color;
        }
    }
}

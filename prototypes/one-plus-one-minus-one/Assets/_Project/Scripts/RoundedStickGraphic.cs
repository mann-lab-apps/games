using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.OnePlusOneMinusOne
{
    public sealed class RoundedStickGraphic : Graphic
    {
        [SerializeField] private Color backgroundColor = Color.white;
        [SerializeField] private Color hatchColor = Color.white;
        [SerializeField] private Color outlineColor = Color.black;
        [SerializeField] private float inset = 5f;
        [SerializeField] private float spacing = 14f;
        [SerializeField] private float hatchThickness = 2.1f;
        [SerializeField] private float outlineThickness = 3f;
        [SerializeField] private float jitter = 2.4f;
        [SerializeField] private float cornerRadiusRatio = 0.22f;
        [SerializeField] private int seed = 23;

        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;
                SetVerticesDirty();
            }
        }

        public Color HatchColor
        {
            get => hatchColor;
            set
            {
                hatchColor = value;
                SetVerticesDirty();
            }
        }

        public Color OutlineColor
        {
            get => outlineColor;
            set
            {
                outlineColor = value;
                SetVerticesDirty();
            }
        }

        public float Inset
        {
            get => inset;
            set
            {
                inset = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public float Spacing
        {
            get => spacing;
            set
            {
                spacing = Mathf.Max(5f, value);
                SetVerticesDirty();
            }
        }

        public float HatchThickness
        {
            get => hatchThickness;
            set
            {
                hatchThickness = Mathf.Max(0.5f, value);
                SetVerticesDirty();
            }
        }

        public float OutlineThickness
        {
            get => outlineThickness;
            set
            {
                outlineThickness = Mathf.Max(0.5f, value);
                SetVerticesDirty();
            }
        }

        public float Jitter
        {
            get => jitter;
            set
            {
                jitter = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public float CornerRadiusRatio
        {
            get => cornerRadiusRatio;
            set
            {
                cornerRadiusRatio = Mathf.Clamp(value, 0.04f, 0.48f);
                SetVerticesDirty();
            }
        }

        public int Seed
        {
            get => seed;
            set
            {
                seed = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) * cornerRadiusRatio;
            AddRoundedFill(vh, rect, radius, backgroundColor);
            AddHatches(vh, rect, radius);
            AddSketchOutline(vh, rect, radius);
        }

        private void AddRoundedFill(VertexHelper vh, Rect rect, float radius, Color vertexColor)
        {
            var points = BuildRoundedPath(rect, radius, 7, seed, 0f);
            var index = vh.currentVertCount;
            vh.AddVert(rect.center, vertexColor, Vector2.zero);
            for (var i = 0; i < points.Length; i++)
            {
                vh.AddVert(points[i], vertexColor, Vector2.zero);
            }

            for (var i = 0; i < points.Length; i++)
            {
                vh.AddTriangle(index, index + 1 + i, index + 1 + ((i + 1) % points.Length));
            }
        }

        private void AddHatches(VertexHelper vh, Rect rect, float radius)
        {
            var hatchRect = new Rect(
                rect.xMin + inset,
                rect.yMin + inset,
                Mathf.Max(0f, rect.width - inset * 2f),
                Mathf.Max(0f, rect.height - inset * 2f));

            if (hatchRect.width <= 0f || hatchRect.height <= 0f)
            {
                return;
            }

            var hatchRadius = Mathf.Max(0f, radius - inset);
            var direction = new Vector2(1f, 1f).normalized;
            var normal = new Vector2(-direction.y, direction.x);
            var diagonal = Mathf.Sqrt(hatchRect.width * hatchRect.width + hatchRect.height * hatchRect.height);
            var extent = diagonal + spacing * 2f;
            var center = hatchRect.center;
            var lineIndex = 0;

            for (var offset = -diagonal; offset <= diagonal; offset += spacing)
            {
                var lineSeed = seed + lineIndex * 43;
                var shiftedOffset = offset + (Fract(Mathf.Sin(lineSeed * 18.219f) * 31873.17f) - 0.5f) * jitter;
                var shiftedCenter = center + normal * shiftedOffset;
                var start = shiftedCenter - direction * extent + JitterOffset(lineSeed + 1, jitter * 0.55f);
                var end = shiftedCenter + direction * extent + JitterOffset(lineSeed + 2, jitter * 0.55f);
                AddRoundedLineSegments(vh, hatchRect, hatchRadius, start, end, hatchColor, hatchThickness);
                lineIndex++;
            }
        }

        private void AddRoundedLineSegments(VertexHelper vh, Rect rect, float radius, Vector2 start, Vector2 end, Color vertexColor, float thickness)
        {
            const int samples = 72;
            var segmentStart = Vector2.zero;
            var hasSegment = false;
            var previous = start;

            for (var i = 0; i <= samples; i++)
            {
                var point = Vector2.Lerp(start, end, i / (float)samples);
                var inside = IsInsideRoundedRect(point, rect, radius);
                if (inside && !hasSegment)
                {
                    segmentStart = point;
                    hasSegment = true;
                }
                else if (!inside && hasSegment)
                {
                    AddLine(vh, segmentStart, previous, vertexColor, thickness);
                    hasSegment = false;
                }

                previous = point;
            }

            if (hasSegment)
            {
                AddLine(vh, segmentStart, end, vertexColor, thickness);
            }
        }

        private void AddSketchOutline(VertexHelper vh, Rect rect, float radius)
        {
            for (var stroke = 0; stroke < 2; stroke++)
            {
                var points = BuildRoundedPath(rect, radius, 7, seed + stroke * 31, jitter);
                for (var i = 0; i < points.Length; i++)
                {
                    AddLine(vh, points[i], points[(i + 1) % points.Length], outlineColor, outlineThickness);
                }
            }
        }

        private Vector2[] BuildRoundedPath(Rect rect, float radius, int steps, int pathSeed, float pointJitter)
        {
            var points = new Vector2[(steps + 1) * 4];
            var centers = new[]
            {
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius)
            };

            var index = 0;
            for (var corner = 0; corner < 4; corner++)
            {
                var startAngle = corner * 0.5f * Mathf.PI;
                for (var step = 0; step <= steps; step++)
                {
                    var angle = startAngle + step / (float)steps * 0.5f * Mathf.PI;
                    var point = centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (pointJitter > 0f)
                    {
                        point += JitterOffset(pathSeed + index * 13, pointJitter);
                    }

                    points[index] = point;
                    index++;
                }
            }

            return points;
        }

        private static bool IsInsideRoundedRect(Vector2 point, Rect rect, float radius)
        {
            var clamped = new Vector2(
                Mathf.Clamp(point.x, rect.xMin + radius, rect.xMax - radius),
                Mathf.Clamp(point.y, rect.yMin + radius, rect.yMax - radius));
            return (point - clamped).sqrMagnitude <= radius * radius;
        }

        private Vector2 JitterOffset(int value, float amount)
        {
            var x = Mathf.Sin(value * 12.9898f) * 43758.5453f;
            var y = Mathf.Sin((value + 19) * 78.233f) * 24634.6345f;
            return new Vector2((Fract(x) - 0.5f) * amount, (Fract(y) - 0.5f) * amount);
        }

        private static float Fract(float value)
        {
            return value - Mathf.Floor(value);
        }

        private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, Color vertexColor, float thickness)
        {
            var delta = end - start;
            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var direction = delta.normalized;
            var normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);
            var index = vh.currentVertCount;

            vh.AddVert(start - normal, vertexColor, Vector2.zero);
            vh.AddVert(start + normal, vertexColor, Vector2.zero);
            vh.AddVert(end + normal, vertexColor, Vector2.zero);
            vh.AddVert(end - normal, vertexColor, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}

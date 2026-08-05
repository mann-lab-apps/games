using UnityEngine;
using UnityEngine.UI;

namespace MannLab.HyperCasual
{
    public sealed class SketchHatchFillGraphic : Graphic
    {
        [SerializeField] private Color backgroundColor = SketchPalette.HatchPaper;
        [SerializeField] private Color hatchColor = SketchPalette.HatchBlue;
        [SerializeField] private float inset = 8f;
        [SerializeField] private float spacing = 18f;
        [SerializeField] private float thickness = 2.6f;
        [SerializeField] private float jitter = 2.8f;
        [SerializeField] private int strokes = 2;
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
                spacing = Mathf.Max(4f, value);
                SetVerticesDirty();
            }
        }

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(0.5f, value);
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

        public int Strokes
        {
            get => strokes;
            set
            {
                strokes = Mathf.Max(1, value);
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
            AddQuad(vh, rect.min, rect.max, backgroundColor);

            var hatchRect = new Rect(
                rect.xMin + inset,
                rect.yMin + inset,
                Mathf.Max(0f, rect.width - inset * 2f),
                Mathf.Max(0f, rect.height - inset * 2f));

            if (hatchRect.width <= 0f || hatchRect.height <= 0f)
            {
                return;
            }

            var direction = new Vector2(1f, 1f).normalized;
            var normal = new Vector2(-direction.y, direction.x);
            var diagonal = Mathf.Sqrt(hatchRect.width * hatchRect.width + hatchRect.height * hatchRect.height);
            var extent = diagonal + spacing * 2f;
            var center = hatchRect.center;
            var minOffset = -diagonal;
            var maxOffset = diagonal;
            var lineIndex = 0;

            for (var offset = minOffset; offset <= maxOffset; offset += spacing)
            {
                for (var stroke = 0; stroke < strokes; stroke++)
                {
                    var lineSeed = seed + lineIndex * 43 + stroke * 13;
                    var shiftedOffset = offset + (Fract(Mathf.Sin(lineSeed * 18.219f) * 31873.17f) - 0.5f) * jitter;
                    var shiftedCenter = center + normal * shiftedOffset;
                    var start = shiftedCenter - direction * extent + JitterOffset(lineSeed + 1);
                    var end = shiftedCenter + direction * extent + JitterOffset(lineSeed + 2);

                    if (ClipLineToRect(hatchRect, ref start, ref end))
                    {
                        AddLine(vh, start, end, hatchColor, thickness);
                    }
                }

                lineIndex++;
            }
        }

        private Vector2 JitterOffset(int value)
        {
            var x = Mathf.Sin(value * 12.9898f) * 43758.5453f;
            var y = Mathf.Sin((value + 19) * 78.233f) * 24634.6345f;
            return new Vector2((Fract(x) - 0.5f) * jitter, (Fract(y) - 0.5f) * jitter);
        }

        private static float Fract(float value)
        {
            return value - Mathf.Floor(value);
        }

        private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color vertexColor)
        {
            var index = vh.currentVertCount;
            vh.AddVert(new Vector2(min.x, min.y), vertexColor, Vector2.zero);
            vh.AddVert(new Vector2(min.x, max.y), vertexColor, Vector2.zero);
            vh.AddVert(new Vector2(max.x, max.y), vertexColor, Vector2.zero);
            vh.AddVert(new Vector2(max.x, min.y), vertexColor, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, Color vertexColor, float lineThickness)
        {
            var delta = end - start;
            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var direction = delta.normalized;
            var normal = new Vector2(-direction.y, direction.x) * (lineThickness * 0.5f);
            var index = vh.currentVertCount;

            vh.AddVert(start - normal, vertexColor, Vector2.zero);
            vh.AddVert(start + normal, vertexColor, Vector2.zero);
            vh.AddVert(end + normal, vertexColor, Vector2.zero);
            vh.AddVert(end - normal, vertexColor, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static bool ClipLineToRect(Rect rect, ref Vector2 start, ref Vector2 end)
        {
            var delta = end - start;
            var t0 = 0f;
            var t1 = 1f;

            if (!ClipEdge(-delta.x, start.x - rect.xMin, ref t0, ref t1) ||
                !ClipEdge(delta.x, rect.xMax - start.x, ref t0, ref t1) ||
                !ClipEdge(-delta.y, start.y - rect.yMin, ref t0, ref t1) ||
                !ClipEdge(delta.y, rect.yMax - start.y, ref t0, ref t1))
            {
                return false;
            }

            var originalStart = start;
            if (t1 < 1f)
            {
                end = originalStart + delta * t1;
            }

            if (t0 > 0f)
            {
                start = originalStart + delta * t0;
            }

            return true;
        }

        private static bool ClipEdge(float direction, float distance, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return distance >= 0f;
            }

            var ratio = distance / direction;
            if (direction < 0f)
            {
                if (ratio > t1)
                {
                    return false;
                }

                if (ratio > t0)
                {
                    t0 = ratio;
                }
            }
            else
            {
                if (ratio < t0)
                {
                    return false;
                }

                if (ratio < t1)
                {
                    t1 = ratio;
                }
            }

            return true;
        }
    }
}

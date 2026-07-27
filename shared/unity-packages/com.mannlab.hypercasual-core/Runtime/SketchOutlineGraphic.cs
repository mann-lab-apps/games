using UnityEngine;
using UnityEngine.UI;

namespace MannLab.HyperCasual
{
    public sealed class SketchOutlineGraphic : Graphic
    {
        [SerializeField] private float thickness = SketchMetrics.DefaultLine;
        [SerializeField] private float jitter = SketchMetrics.DefaultJitter;
        [SerializeField] private int strokes = SketchMetrics.DefaultStrokes;
        [SerializeField] private int seed = 7;

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(0.1f, value);
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
            for (var i = 0; i < strokes; i++)
            {
                var offsetSeed = seed + i * 31;
                var topLeft = new Vector2(rect.xMin, rect.yMax) + JitterOffset(offsetSeed);
                var topRight = new Vector2(rect.xMax, rect.yMax) + JitterOffset(offsetSeed + 1);
                var bottomRight = new Vector2(rect.xMax, rect.yMin) + JitterOffset(offsetSeed + 2);
                var bottomLeft = new Vector2(rect.xMin, rect.yMin) + JitterOffset(offsetSeed + 3);

                AddLine(vh, topLeft, topRight);
                AddLine(vh, topRight, bottomRight);
                AddLine(vh, bottomRight, bottomLeft);
                AddLine(vh, bottomLeft, topLeft);
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

        private void AddLine(VertexHelper vh, Vector2 start, Vector2 end)
        {
            var direction = (end - start).normalized;
            var normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);
            var index = vh.currentVertCount;

            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.Game10000
{
    public sealed class SketchOutlineGraphic : Graphic
    {
        [SerializeField] private float thickness = 2.5f;
        [SerializeField] private float jitter = 2.5f;
        [SerializeField] private int strokes = 2;
        [SerializeField] private int seed = 7;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            for (var i = 0; i < strokes; i++)
            {
                var offsetSeed = seed + i * 31;
                var topLeft = new Vector2(rect.xMin, rect.yMax) + Jitter(offsetSeed);
                var topRight = new Vector2(rect.xMax, rect.yMax) + Jitter(offsetSeed + 1);
                var bottomRight = new Vector2(rect.xMax, rect.yMin) + Jitter(offsetSeed + 2);
                var bottomLeft = new Vector2(rect.xMin, rect.yMin) + Jitter(offsetSeed + 3);

                AddLine(vh, topLeft, topRight);
                AddLine(vh, topRight, bottomRight);
                AddLine(vh, bottomRight, bottomLeft);
                AddLine(vh, bottomLeft, topLeft);
            }
        }

        private Vector2 Jitter(int value)
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


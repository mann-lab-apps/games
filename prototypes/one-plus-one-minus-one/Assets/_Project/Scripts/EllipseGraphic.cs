using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.OnePlusOneMinusOne
{
    public sealed class EllipseGraphic : Graphic
    {
        [SerializeField]
        private int segments = 24;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            var center = rect.center;
            var radiusX = rect.width * 0.5f;
            var radiusY = rect.height * 0.5f;
            var count = Mathf.Max(8, segments);

            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center;
            vh.AddVert(vertex);

            for (var i = 0; i <= count; i++)
            {
                var angle = i / (float)count * Mathf.PI * 2f;
                vertex.position = new Vector3(
                    center.x + Mathf.Cos(angle) * radiusX,
                    center.y + Mathf.Sin(angle) * radiusY,
                    0f);
                vh.AddVert(vertex);
            }

            for (var i = 1; i <= count; i++)
            {
                vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}

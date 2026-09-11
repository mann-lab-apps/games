using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.OnePlusOneMinusOne
{
    public sealed class TriangleGraphic : Graphic
    {
        [SerializeField]
        private bool pointDown = true;

        public bool PointDown
        {
            get => pointDown;
            set
            {
                pointDown = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            if (pointDown)
            {
                vertex.position = new Vector3(rect.center.x, rect.yMin, 0f);
                vh.AddVert(vertex);
                vertex.position = new Vector3(rect.xMin, rect.yMax, 0f);
                vh.AddVert(vertex);
                vertex.position = new Vector3(rect.xMax, rect.yMax, 0f);
                vh.AddVert(vertex);
            }
            else
            {
                vertex.position = new Vector3(rect.center.x, rect.yMax, 0f);
                vh.AddVert(vertex);
                vertex.position = new Vector3(rect.xMin, rect.yMin, 0f);
                vh.AddVert(vertex);
                vertex.position = new Vector3(rect.xMax, rect.yMin, 0f);
                vh.AddVert(vertex);
            }

            vh.AddTriangle(0, 1, 2);
        }
    }
}

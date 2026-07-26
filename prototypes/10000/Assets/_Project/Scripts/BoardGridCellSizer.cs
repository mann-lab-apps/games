using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.Game10000
{
    [RequireComponent(typeof(GridLayoutGroup))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardGridCellSizer : MonoBehaviour
    {
        private GridLayoutGroup grid;
        private RectTransform rectTransform;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            var totalSpacing = grid.spacing.x * (BoardData.Size - 1);
            var cellSize = (rectTransform.rect.width - totalSpacing) / BoardData.Size;
            grid.cellSize = new Vector2(cellSize, cellSize);
        }
    }
}


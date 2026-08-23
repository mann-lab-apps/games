using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.Game2048Blink
{
    [RequireComponent(typeof(GridLayoutGroup))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BlinkBoardGridCellSizer : MonoBehaviour
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
            var totalHorizontalSpacing = grid.spacing.x * (Blink2048Board.Size - 1);
            var totalVerticalSpacing = grid.spacing.y * (Blink2048Board.Size - 1);
            var availableWidth = rectTransform.rect.width - totalHorizontalSpacing;
            var availableHeight = rectTransform.rect.height - totalVerticalSpacing;
            var cellSize = Mathf.Max(0f, Mathf.Min(availableWidth, availableHeight) / Blink2048Board.Size);
            grid.cellSize = new Vector2(cellSize, cellSize);
        }
    }
}

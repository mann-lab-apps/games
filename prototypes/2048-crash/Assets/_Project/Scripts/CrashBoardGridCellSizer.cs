using UnityEngine;
using UnityEngine.UI;

namespace MannLab.Games.Game2048Crash
{
    [RequireComponent(typeof(GridLayoutGroup))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CrashBoardGridCellSizer : MonoBehaviour
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
            var totalSpacing = grid.spacing.x * (Crash2048Board.Size - 1);
            var cellSize = (rectTransform.rect.width - totalSpacing) / Crash2048Board.Size;
            grid.cellSize = new Vector2(cellSize, cellSize);
        }
    }
}

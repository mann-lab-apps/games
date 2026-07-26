using UnityEngine;

namespace MannLab.Games.Game10000
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardSizeFitter : MonoBehaviour
    {
        [SerializeField] private float horizontalPadding = 32f;
        [SerializeField] private float reservedVerticalSpace = 230f;

        private RectTransform rectTransform;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        private void LateUpdate()
        {
            var scaleFactor = canvas == null ? 1f : canvas.scaleFactor;
            var availableWidth = Screen.width / scaleFactor - horizontalPadding;
            var availableHeight = Screen.height / scaleFactor - reservedVerticalSpace;
            var size = Mathf.Max(220f, Mathf.Min(availableWidth, availableHeight));

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        }
    }
}


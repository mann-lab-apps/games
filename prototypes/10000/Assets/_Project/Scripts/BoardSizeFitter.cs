using UnityEngine;

namespace MannLab.Games.Game10000
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardSizeFitter : MonoBehaviour
    {
        [SerializeField] private float horizontalPadding = 64f;
        [SerializeField] private float topMargin = 190f;
        [SerializeField] private float bottomPadding = 48f;
        [SerializeField] private float maxScreenCoverage = 1.45f;
        [SerializeField] private float embeddedHeightMultiplier = 1.85f;

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
            var availableHeight = (Screen.height / scaleFactor - topMargin - bottomPadding) * embeddedHeightMultiplier;
            var screenLimit = Mathf.Min(Screen.width, Screen.height) / scaleFactor * maxScreenCoverage;
            var size = Mathf.Max(220f, Mathf.Min(availableWidth, availableHeight, screenLimit));

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        }
    }
}

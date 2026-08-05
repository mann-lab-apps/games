using UnityEngine;

namespace MannLab.Games.Game2048Crash
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CrashBoardSizeFitter : MonoBehaviour
    {
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            var parentRect = rectTransform.parent as RectTransform;
            var availableWidth = parentRect == null ? rectTransform.rect.width : parentRect.rect.width;
            var availableHeight = parentRect == null ? rectTransform.rect.height : parentRect.rect.height;
            var size = Mathf.Max(0f, Mathf.Min(availableWidth, availableHeight));

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        }
    }
}

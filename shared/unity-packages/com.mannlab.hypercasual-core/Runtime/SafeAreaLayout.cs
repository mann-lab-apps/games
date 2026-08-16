using UnityEngine;

namespace MannLab.HyperCasual
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            ApplySafeArea();
        }

        private void Update()
        {
            ApplySafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (rectTransform == null)
            {
                return;
            }

            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(screenWidth, screenHeight);
            if (safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x = Mathf.Clamp01(anchorMin.x / screenWidth);
            anchorMin.y = Mathf.Clamp01(anchorMin.y / screenHeight);
            anchorMax.x = Mathf.Clamp01(anchorMax.x / screenWidth);
            anchorMax.y = Mathf.Clamp01(anchorMax.y / screenHeight);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}

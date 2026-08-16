using UnityEngine;
using UnityEngine.UI;

namespace MannLab.HyperCasual
{
    public static class SketchUiFactory
    {
        public static RectTransform CreateSafeAreaRoot(Transform parent, string name = "Safe Area Root")
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(SafeAreaLayout));
            root.transform.SetParent(parent, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static ColorBlock ButtonColors()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = SketchPalette.TilePaper;
            colors.highlightedColor = SketchPalette.WarmHighlight;
            colors.pressedColor = SketchPalette.WarmPressed;
            colors.selectedColor = SketchPalette.WarmHighlight;
            colors.disabledColor = SketchPalette.WarmShadow;
            return colors;
        }
    }
}

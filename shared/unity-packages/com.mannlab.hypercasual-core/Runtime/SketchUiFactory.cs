using UnityEngine.UI;

namespace MannLab.HyperCasual
{
    public static class SketchUiFactory
    {
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

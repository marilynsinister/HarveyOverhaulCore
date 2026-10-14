using StardewUI.Layout;
using StardewValley;

namespace HarveyOverhaul.Core.UI;

/// <summary>
/// Размер окна «План Харви» под текущий экран. Раньше окно было 1040×760 всегда и на 1280×720
/// (или с крупным масштабом интерфейса) вылезало за край. Меню и SML берут размер отсюда.
/// </summary>
public static class HarveyPanelLayout
{
    public const int PreferredWidth = 1040;
    public const int PreferredHeight = 760;
    public const int MinWidth = 640;
    public const int MinHeight = 440;

    /// <summary>Рамка + отступы frame в HarveyPanel.sml (border 36/40 + padding 32 с каждой стороны).</summary>
    private const int HorizontalChrome = 36 + 40 + 32 + 32;
    private const int TabMargin = 8;
    private const int MaxTabWidth = 160;
    private const int MinTabWidth = 96;

    public readonly record struct Size(int Width, int Height, LayoutParameters PanelLayout, LayoutParameters TabLayout, bool ShowTabIcons);

    public static Size Compute(int tabCount)
    {
        var viewport = Game1.uiViewport;
        int width = Fit(PreferredWidth, MinWidth, viewport.Width, 0.92f);
        int height = Fit(PreferredHeight, MinHeight, viewport.Height, 0.88f);

        int count = Math.Max(1, tabCount);
        int tabWidth = Math.Clamp((width - HorizontalChrome) / count - TabMargin, MinTabWidth, MaxTabWidth);

        // На узких вкладках иконка съедает место у подписи («Справочник» не влезет).
        bool icons = tabWidth >= 140;
        return new Size(width, height, LayoutParameters.FixedSize(width, height), LayoutParameters.FixedSize(tabWidth, 56), icons);
    }

    private static int Fit(int preferred, int min, int screen, float share)
    {
        if (screen <= 0)
            return preferred;
        return Math.Max(Math.Min(min, screen), Math.Min(preferred, (int)(screen * share)));
    }
}

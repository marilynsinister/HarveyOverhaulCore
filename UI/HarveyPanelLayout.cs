using StardewUI.Layout;
using StardewValley;
using HarveyOverhaul.Core.Models;

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

    /// <summary>Рамка + отступы frame в HarveyPanel.sml (border 36/40 + padding 28 с каждой стороны).</summary>
    private const int HorizontalChrome = 36 + 40 + 28 + 28;
    /// <summary>margin="4,0,4,0" у каждой вкладки.</summary>
    private const int TabMargin = 8;
    private const int MaxTabWidth = 168;
    /// <summary>Поля кнопки ButtonLight вокруг подписи и место под иконку 24px + отступ.</summary>
    private const int TabLabelPadding = 28;
    private const int TabIconWidth = 30;
    private const int TabHeight = 52;
    private const int TabHeightCompact = 46;

    public readonly record struct Size(int Width, int Height, LayoutParameters PanelLayout);

    public readonly record struct TabsLayout(int PerRow, LayoutParameters ButtonLayout, bool ShowIcons);

    public static Size Compute()
    {
        var viewport = Game1.uiViewport;
        return Compute(viewport.Width, viewport.Height);
    }

    public static Size Compute(int viewportWidth, int viewportHeight)
    {
        int width = Fit(PreferredWidth, MinWidth, viewportWidth, 0.92f);
        int height = Fit(PreferredHeight, MinHeight, viewportHeight, 0.88f);
        return new Size(width, height, LayoutParameters.FixedSize(width, height));
    }

    /// <summary>Size each tab to its own label, then wrap whole buttons. A long handbook label no longer widens every tab.</summary>
    public static IReadOnlyList<HarveyPanelTabRowViewModel> ArrangeTabs(IReadOnlyList<HarveyPanelTabButtonViewModel> tabs)
    {
        var size = Compute();
        int available = ContentWidth(size.Width, size.Height);
        int[] labels = tabs.Select(t => MeasureSmall(t.Label) + TabLabelPadding).ToArray();
        bool icons = RowCount(labels.Select(w => w + TabIconWidth), available) <= RowCount(labels, available);
        var rows = new List<List<HarveyPanelTabButtonViewModel>>();
        int used = 0;
        foreach (var (tab, index) in tabs.Select((tab, index) => (tab, index)))
        {
            int width = Math.Min(available - TabMargin, labels[index] + (icons ? TabIconWidth : 0));
            if (rows.Count == 0 || used + width + TabMargin > available)
            {
                rows.Add(new());
                used = 0;
            }
            tab.ButtonLayout = LayoutParameters.FixedSize(Math.Max(1, width), TabHeightCompact);
            tab.ShowIcon = icons;
            rows[^1].Add(tab);
            used += width + TabMargin;
        }
        return rows.Select(row => new HarveyPanelTabRowViewModel(row)).ToList();
    }

    public static int ContentWidth(int width, int height) =>
        Math.Max(1, width - (width < 850 || height < 650 ? 80 : HorizontalChrome));

    private static int RowCount(IEnumerable<int> widths, int available)
    {
        int rows = 1, used = 0;
        foreach (int width in widths)
        {
            int occupied = Math.Min(available, width + TabMargin);
            if (used > 0 && used + occupied > available) { rows++; used = 0; }
            used += occupied;
        }
        return rows;
    }

    /// <summary>
    /// Вкладки по ширине самой длинной подписи. Если в один ряд не помещаются — переносим во второй
    /// (раньше 7 вкладок по 96px+ уходили за правый край окна на узком экране).
    /// Иконки показываем, только если с ними не понадобится лишний ряд.
    /// </summary>
    public static TabsLayout ComputeTabs(IReadOnlyList<string> labels)
    {
        int available = Compute().Width - HorizontalChrome;
        int count = Math.Max(1, labels.Count);
        int labelWidth = labels.Count == 0 ? 80 : labels.Max(MeasureSmall);

        int withIcons = labelWidth + TabLabelPadding + TabIconWidth;
        int withoutIcons = labelWidth + TabLabelPadding;

        int rowsWithIcons = Rows(count, withIcons, available);
        int rowsWithout = Rows(count, withoutIcons, available);
        bool icons = rowsWithIcons <= rowsWithout;
        int minWidth = icons ? withIcons : withoutIcons;
        int rows = icons ? rowsWithIcons : rowsWithout;

        int perRow = (int)Math.Ceiling(count / (double)rows);
        int tabWidth = Math.Max(minWidth, Math.Min(MaxTabWidth, available / perRow - TabMargin));
        int tabHeight = rows > 1 ? TabHeightCompact : TabHeight;

        return new TabsLayout(perRow, LayoutParameters.FixedSize(tabWidth, tabHeight), icons);
    }

    private static int Rows(int count, int tabWidth, int available)
    {
        int perRow = Math.Max(1, available / (tabWidth + TabMargin));
        return (int)Math.Ceiling(count / (double)perRow);
    }

    private static int MeasureSmall(string text)
    {
        try
        {
            return (int)Math.Ceiling(Game1.smallFont.MeasureString(text ?? "").X);
        }
        catch
        {
            return (text?.Length ?? 0) * 12;
        }
    }

    private static int Fit(int preferred, int min, int screen, float share)
    {
        if (screen <= 0)
            return preferred;
        return Math.Max(Math.Min(min, screen), Math.Min(preferred, (int)(screen * share)));
    }
}

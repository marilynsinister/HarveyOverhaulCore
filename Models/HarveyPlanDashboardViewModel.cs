using HarveyOverhaul.Core.UI;
using StardewUI.Graphics;
using StardewUI.Layout;

namespace HarveyOverhaul.Core.Models;

/// <summary>A daily summary. One-off tasks and rules maintained until sleep have separate counts.</summary>
public sealed class HarveyPlanDashboardViewModel
{
    public string StatusText { get; init; } = "";
    public string StatusColor { get; init; } = HarveyPlanColors.Good;
    public string MainActionTitle { get; init; } = "";
    public string MainActionBody { get; init; } = "";
    public string NoticeText { get; init; } = "";
    public string ProgressText { get; init; } = "";
    public float ProgressFraction { get; init; }
    public IReadOnlyList<HarveyPlanMetricViewModel> Metrics { get; init; } = Array.Empty<HarveyPlanMetricViewModel>();

    public Sprite? Portrait => HarveyPanelIcons.HarveyPortrait();
    public bool HasPortrait => Portrait != null;
    public bool HasMainActionBody => !string.IsNullOrWhiteSpace(MainActionBody);
    public bool HasNotice => !string.IsNullOrWhiteSpace(NoticeText);
    public bool HasMetrics => Metrics.Count > 0;
    public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressText);
    public bool HasProgressFill => HasProgress && ProgressFraction > 0f;
    public Sprite BarSprite => UiSprites.White;
    public LayoutParameters ProgressFillLayout => HarveyPanelSectionViewModel.PercentFill(ProgressFraction, 10);
}

public sealed class HarveyPlanMetricViewModel
{
    public string Value { get; init; } = "";
    public string Label { get; init; } = "";
    public string Tooltip { get; init; } = "";
    public string Color { get; init; } = HarveyPlanColors.Text;
    public string Illustration { get; init; } = "";
    public Sprite? Icon => HarveyPanelIcons.Illustration(Illustration);
    public bool HasIcon => Icon != null;
}

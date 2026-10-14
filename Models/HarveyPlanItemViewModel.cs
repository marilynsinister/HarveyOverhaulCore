using System.ComponentModel;
using System.Runtime.CompilerServices;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewValley;

namespace HarveyOverhaul.Core.Models;

/// <summary>
/// Один пункт плана в окне: иконка состояния, заголовок, полоска прогресса и раскрываемые подробности
/// («Почему / Риск / Дальше»). Раньше всё это было одним текстовым блоком с ASCII-метками и легендой.
/// </summary>
public sealed class HarveyPlanItemViewModel : INotifyPropertyChanged
{
    /// <summary>Ширина дорожки прогресса в пикселях (должна совпадать с layout в HarveyPanel.sml).</summary>
    public const int ProgressTrackWidth = 220;
    private const int ProgressBarHeight = 10;

    private bool _expanded;

    public string Title { get; init; } = "";
    public string TitleColor { get; init; } = HarveyPlanColors.Text;

    /// <summary>Короткое пояснение под заголовком (Text директивы).</summary>
    public string Subtitle { get; init; } = "";

    public Sprite? StateIcon { get; init; }
    public string StateTint { get; init; } = "#ffffff";
    public string StateTooltip { get; init; } = "";

    public string ProgressText { get; init; } = "";
    public float ProgressFraction { get; init; }
    public string ProgressColor { get; init; } = HarveyPlanColors.Good;

    /// <summary>«Почему» и «Дальше» — обычным цветом.</summary>
    public string DetailText { get; init; } = "";

    /// <summary>Чем грозит нарушение — выделено цветом.</summary>
    public string RiskText { get; init; } = "";

    /// <summary>Белый пиксель для полосок прогресса (цвет задаётся tint).</summary>
    public Sprite BarSprite => UiSprites.White;

    public bool HasStateIcon => StateIcon != null;
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressText);
    public bool HasProgressFill => HasProgress && ProgressFraction > 0f;
    public bool HasDetails => !string.IsNullOrWhiteSpace(DetailText) || !string.IsNullOrWhiteSpace(RiskText);

    public LayoutParameters ProgressFillLayout => LayoutParameters.FixedSize(
        Math.Max(2, (int)MathF.Round(ProgressTrackWidth * Math.Clamp(ProgressFraction, 0f, 1f))),
        ProgressBarHeight);

    public bool Expanded
    {
        get => _expanded;
        init => _expanded = value;
    }

    public bool ShowDetails => Expanded && HasDetails;
    public bool ShowDetailText => ShowDetails && !string.IsNullOrWhiteSpace(DetailText);
    public bool ShowRiskText => ShowDetails && !string.IsNullOrWhiteSpace(RiskText);
    public string ToggleLabel => Expanded ? "Скрыть подробности" : "Почему и что дальше?";

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool ToggleDetails()
    {
        if (!HasDetails)
            return false;

        _expanded = !_expanded;
        Game1.playSound(_expanded ? "shwip" : "smallSelect");
        OnPropertyChanged(nameof(Expanded));
        OnPropertyChanged(nameof(ShowDetails));
        OnPropertyChanged(nameof(ShowDetailText));
        OnPropertyChanged(nameof(ShowRiskText));
        OnPropertyChanged(nameof(ToggleLabel));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Палитра окна «План Харви» (тёплые тона пергамента Stardew).</summary>
public static class HarveyPlanColors
{
    public const string Text = "#3b2a1a";
    public const string Muted = "#7f6139";
    public const string Faded = "#8a7a66";
    public const string Danger = "#8b4513";
    public const string Good = "#3f7a2a";
    public const string Warn = "#c07a1a";
    public const string Track = "#c9a77a";
    public const string Link = "#5b6fa8";
}

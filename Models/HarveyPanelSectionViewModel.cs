using StardewUI.Graphics;
using StardewUI.Layout;

namespace HarveyOverhaul.Core.Models;

public sealed class HarveyPanelSectionViewModel
{
    public string Headline { get; set; } = "";
    public string StatusLine { get; set; } = "";
    /// <summary>Текст секции. Если есть <see cref="Items"/>, в окне не показывается — остаётся для текстовой версии и логов.</summary>
    public string BodyText { get; set; } = "";
    public string AccentColor { get; set; } = "#3b2a1a";
    public string StatusColor { get; set; } = "#7f6139";

    /// <summary>Пункты плана с иконками состояния и прогрессом.</summary>
    public IReadOnlyList<HarveyPlanItemViewModel> Items { get; set; } = Array.Empty<HarveyPlanItemViewModel>();

    /// <summary>Мелкая приписка под пунктами («Подробнее — во вкладке «План»»).</summary>
    public string Footnote { get; set; } = "";

    /// <summary>Общий прогресс секции (шапка плана): «выполнено 2 из 5».</summary>
    public string ProgressText { get; set; } = "";
    public float ProgressFraction { get; set; }
    public string ProgressColor { get; set; } = HarveyPlanColors.Good;

    public bool HasHeadline => !string.IsNullOrWhiteSpace(Headline);
    public bool HasStatusLine => !string.IsNullOrWhiteSpace(StatusLine);
    public bool HasItems => Items.Count > 0;
    public bool HasBodyText => !HasItems && !string.IsNullOrWhiteSpace(BodyText);
    public bool HasFootnote => !string.IsNullOrWhiteSpace(Footnote);
    public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressText);
    public bool HasProgressFill => HasProgress && ProgressFraction > 0f;
    public Sprite BarSprite => UiSprites.White;

    public LayoutParameters ProgressFillLayout => LayoutParameters.FixedSize(
        Math.Max(2, (int)MathF.Round(SectionProgressTrackWidth * Math.Clamp(ProgressFraction, 0f, 1f))),
        14);

    /// <summary>Ширина общей полоски прогресса (должна совпадать с layout в HarveyPanel.sml).</summary>
    public const int SectionProgressTrackWidth = 360;

    /// <summary>Иконка у заголовка секции (подбирается по смыслу заголовка при показе вкладки).</summary>
    public Sprite? Icon { get; set; }
    public bool HasIcon => Icon != null && HasHeadline;

    /// <summary>Кнопка под секцией («Пересмотреть договорённость»). Пусто — без кнопки.</summary>
    public string ActionLabel { get; set; } = "";

    /// <summary>Что делать по нажатию. Ставит Core (HarveyPanelService) — маршрут к провайдеру секции.</summary>
    public Action? ActionCallback { get; set; }

    public bool HasAction => ActionCallback != null && !string.IsNullOrWhiteSpace(ActionLabel);

    public bool RunAction()
    {
        if (ActionCallback == null)
            return false;

        StardewValley.Game1.playSound("smallSelect");
        ActionCallback();
        return true;
    }
}

using StardewUI.Graphics;
using StardewUI.Layout;

namespace HarveyOverhaul.Core.Models;

public enum HarveyPanelSectionStyle
{
    /// <summary>Обычная карточка в рамке — пункты, которые нужно выполнить.</summary>
    Card,
    /// <summary>Шапка вкладки: крупный заголовок, общий прогресс.</summary>
    Hero,
    /// <summary>Справка без рамки — чтобы не спорила с карточками действий.</summary>
    Plain,
}

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

    public HarveyPlanDashboardViewModel? Dashboard { get; set; }
    public bool HasDashboard => Dashboard != null;
    public bool Collapsible { get; set; }
    public bool IsCollapsible => Collapsible || IsPlain;
    public bool Expanded { get; set; }
    public string ItemCountText => Items.Count > 0 ? Items.Count.ToString() : "";

    public bool HasHeadline => !string.IsNullOrWhiteSpace(Headline);
    public bool HasStatusLine => !string.IsNullOrWhiteSpace(StatusLine);
    public bool HasItems => Items.Count > 0;
    public bool HasBodyText => !HasDashboard && !HasItems && !string.IsNullOrWhiteSpace(BodyText);
    public bool HasFootnote => !string.IsNullOrWhiteSpace(Footnote);
    public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressText);
    public bool HasProgressFill => HasProgress && ProgressFraction > 0f;
    public Sprite BarSprite => UiSprites.White;

    /// <summary>
    /// Заливка общей полоски прогресса — в процентах от дорожки. Раньше дорожка была 360px и вместе
    /// с подписью вылезала за край карточки в узком окне.
    /// </summary>
    public LayoutParameters ProgressFillLayout => PercentFill(ProgressFraction, 14);

    internal static LayoutParameters PercentFill(float fraction, int height) => new()
    {
        Width = Length.Percent(Math.Max(1.5f, 100f * Math.Clamp(fraction, 0f, 1f))),
        Height = Length.Px(height),
    };

    /// <summary>Как выглядит секция: шапка, обычная карточка или справочный блок без рамки.</summary>
    public HarveyPanelSectionStyle Style { get; set; } = HarveyPanelSectionStyle.Card;

    /// <summary>Крупный заголовок (шрифт dialogue) — только у шапки; у остальных компактный жирный.</summary>
    public bool IsHero => Style == HarveyPanelSectionStyle.Hero;
    public bool IsPlain => Style == HarveyPanelSectionStyle.Plain;

    public Sprite? FrameBackground => IsPlain ? null : UiSprites.MenuBackground;
    public Sprite? FrameBorder => IsPlain ? null : UiSprites.MenuBorder;
    public Edges FrameBorderThickness => IsPlain ? Edges.NONE : new Edges(12);
    public Edges FramePadding => IsHero ? new Edges(22, 20, 22, 18)
        : IsPlain ? new Edges(8, 4, 8, 4)
        : new Edges(18, 14, 18, 14);

    public LayoutParameters IconLayout => IsHero ? LayoutParameters.FixedSize(36, 36) : LayoutParameters.FixedSize(24, 24);

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

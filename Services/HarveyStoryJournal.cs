using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewUI.Graphics;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Вкладка «История»: какие сюжетные сцены с Харви уже были и где искать следующие.
/// Сцен больше семидесяти, у каждой свои условия — без подсказки игрок легко застревает.
/// Подсказки мягкие («по вечерам на пирсе»), без пересказа сюжета; заголовки будущих сцен скрыты.
/// </summary>
public sealed class HarveyStoryJournal
{
    public const string AssetName = "Mods/marilynsinister.HarveyOverhaul/StoryJournal";

    /// <summary>Повторяемые сцены снимаются из eventsSeen; факт первого просмотра Core запоминает флагом.</summary>
    private const string SeenFlagPrefix = "HarveyJournal_Seen_";

    /// <summary>Сколько «следующих» сцен подсказывать в одной арке.</summary>
    private const int MaxHintsPerArc = 2;

    private readonly IMonitor _monitor;
    private string? _lastLoadError;

    public HarveyStoryJournal(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public bool HasEntries => Context.IsWorldReady && Load().Count > 0;

    /// <summary>Запомнить первый просмотр повторяемых сцен (их ID снимаются из eventsSeen).</summary>
    public void OnDayStarted()
    {
        if (!Context.IsWorldReady)
            return;

        foreach (var entry in Load().Values.Where(e => e.Repeatable))
        {
            foreach (string id in SplitIds(entry.EventIds))
            {
                if (Game1.player.eventsSeen.Contains(id))
                    Game1.player.mailReceived.Add(SeenFlagPrefix + id);
            }
        }
    }

    public IReadOnlyList<HarveyPanelSectionViewModel> BuildSections()
    {
        var entries = Load();
        if (entries.Count == 0)
        {
            return
            [
                new HarveyPanelSectionViewModel
                {
                    Headline = "История",
                    BodyText = "Журнал сцен недоступен: нужен Harvey Overhaul [CP].",
                },
            ];
        }

        var sections = new List<HarveyPanelSectionViewModel>();
        int totalSeen = 0, total = 0;

        foreach (var arc in entries.Values
                     .GroupBy(e => e.Arc)
                     .OrderBy(g => g.Min(e => e.ArcOrder))
                     .ThenBy(g => g.Key, StringComparer.Ordinal))
        {
            var ordered = arc.OrderBy(e => e.Order).ToList();
            var seen = ordered.Where(IsSeen).ToList();
            var next = ordered
                .Where(e => !IsSeen(e) && RequirementsMet(e) && ConditionMet(e))
                .Take(MaxHintsPerArc)
                .ToList();
            int hidden = ordered.Count - seen.Count - next.Count;

            totalSeen += seen.Count;
            total += ordered.Count;

            // Арка, которую ещё не начали и не на пороге, — одной строкой внизу, без спойлеров.
            if (seen.Count == 0 && next.Count == 0)
                continue;

            var items = new List<HarveyPlanItemViewModel>();
            items.AddRange(seen.Select(e => new HarveyPlanItemViewModel
            {
                Title = e.Title,
                TitleColor = HarveyPlanColors.Faded,
                Subtitle = e.Summary,
                StateIcon = UiSprites.CheckboxChecked,
                StateTooltip = "Сцена была",
            }));
            items.AddRange(next.Select(e => new HarveyPlanItemViewModel
            {
                Title = "Что-то впереди…",
                Subtitle = e.Hint,
                StateIcon = UiSprites.SmallRightArrow,
                StateTint = "#c07a1a",
                StateTooltip = "Подсказка: где может случиться следующая сцена",
            }));

            sections.Add(new HarveyPanelSectionViewModel
            {
                Headline = arc.Key,
                ProgressText = $"Сцен: {seen.Count} из {ordered.Count}",
                ProgressFraction = ordered.Count > 0 ? (float)seen.Count / ordered.Count : 0f,
                Items = items,
                Footnote = hidden > 0 ? $"Ещё сцен впереди: {hidden}. Они откроются позже." : "",
            });
        }

        int untouched = entries.Values
            .GroupBy(e => e.Arc)
            .Count(g => !g.Any(IsSeen) && !g.Any(e => RequirementsMet(e) && ConditionMet(e)));

        sections.Insert(0, new HarveyPanelSectionViewModel
        {
            Headline = "Ваша история с Харви",
            BodyText = "Здесь отмечены сцены, которые уже были, и подсказки, где искать следующие. "
                + "Сцены идут не каждый день: между ними Харви нужно несколько дней, а многие ждут погоды, сезона или часа.",
            ProgressText = $"Всего сцен: {totalSeen} из {total}",
            ProgressFraction = total > 0 ? (float)totalSeen / total : 0f,
            Footnote = untouched > 0 ? $"Ещё не открытых сюжетных линий: {untouched}." : "",
        });

        return sections;
    }

    private Dictionary<string, HarveyStoryJournalEntry> Load()
    {
        try
        {
            var data = Game1.content.Load<Dictionary<string, HarveyStoryJournalEntry>>(AssetName);
            _lastLoadError = null;
            return data;
        }
        catch (Exception ex)
        {
            if (_lastLoadError != ex.Message)
            {
                _lastLoadError = ex.Message;
                _monitor.Log($"[StoryJournal] {AssetName} не загружен: {ex.Message}", LogLevel.Trace);
            }

            return new Dictionary<string, HarveyStoryJournalEntry>();
        }
    }

    private static bool IsSeen(HarveyStoryJournalEntry entry)
        => SplitIds(entry.EventIds).Any(id =>
            Game1.player.eventsSeen.Contains(id)
            || (entry.Repeatable && Game1.player.mailReceived.Contains(SeenFlagPrefix + id)));

    private static bool RequirementsMet(HarveyStoryJournalEntry entry)
        => SplitIds(entry.Requires).All(id => Game1.player.eventsSeen.Contains(id));

    private static bool ConditionMet(HarveyStoryJournalEntry entry)
        => string.IsNullOrWhiteSpace(entry.Condition) || GameStateQuery.CheckConditions(entry.Condition);

    private static IEnumerable<string> SplitIds(string ids)
        => (ids ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

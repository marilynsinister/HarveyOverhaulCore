using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>Снимок диагностики клика по Харви (заполняют Injury/Stress, печатает Core).</summary>
public sealed class HarveyMedicalClickDiagnostics
{
    private readonly IMonitor _monitor;
    private readonly object _lock = new();

    public string? InjuryDebuffSummary { get; private set; }
    public string? StressStateSummary { get; private set; }
    public string? ActiveTopicsSummary { get; private set; }
    public string? ClickGateSummary { get; private set; }
    public string? TopicShownSummary { get; private set; }
    public string? ActionExecutedSummary { get; private set; }

    public HarveyMedicalClickDiagnostics(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void SetInjuryDebuffSummary(string? summary)
    {
        lock (_lock)
        {
            InjuryDebuffSummary = summary;
        }
    }

    public void SetStressStateSummary(string? summary)
    {
        lock (_lock)
        {
            StressStateSummary = summary;
        }
    }

    public void SetActiveTopicsSummary(string? summary)
    {
        lock (_lock)
        {
            ActiveTopicsSummary = summary;
        }
    }

    public void SetClickGate(bool suppressed, string reason)
    {
        lock (_lock)
        {
            ClickGateSummary = suppressed
                ? $"SUPPRESSED ({reason})"
                : $"NOT_SUPPRESSED ({reason})";
        }
    }

    public void SetTopicShown(string? topicKey)
    {
        lock (_lock)
        {
            TopicShownSummary = topicKey ?? "(none)";
        }
    }

    public void SetActionExecuted(string? actionSummary)
    {
        lock (_lock)
        {
            ActionExecutedSummary = actionSummary ?? "(none)";
        }
    }

    public void LogClickSnapshot(HarveyMedicalIntentResolution? resolution, string phase)
    {
        lock (_lock)
        {
            string selected = resolution?.Selected != null
                ? $"{resolution.Selected.ProviderId}/{resolution.Selected.StateId} " +
                  $"prio={resolution.Selected.BasePriority} topic={resolution.Selected.TopicKey} " +
                  $"action={resolution.Selected.ActionKey}"
                : "(none)";

            string festival = resolution?.FestivalBlockedLongTreatment == true
                ? $" festivalDefer={resolution.ActiveFestivalDeferTopicKey ?? "-"}"
                : "";

            _monitor.Log(
                $"[HarveyClick] {phase} | selected={selected}{festival} | " +
                $"injury={InjuryDebuffSummary ?? "-"} | stress={StressStateSummary ?? "-"} | " +
                $"topics={ActiveTopicsSummary ?? "-"} | gate={ClickGateSummary ?? "-"} | " +
                $"shown={TopicShownSummary ?? "-"} | action={ActionExecutedSummary ?? "-"}",
                LogLevel.Info);
        }
    }

    public static string BuildActiveTopicsSnapshot()
    {
        if (!Context.IsWorldReady || Game1.player?.activeDialogueEvents == null)
            return "(no player)";

        var relevant = Game1.player.activeDialogueEvents.Keys
            .Where(k => k.Contains("Injury", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Treatment", StringComparison.OrdinalIgnoreCase)
                || k.Contains("DeepCuts", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Stress", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Hunger", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith("topic", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith("HarveyMod_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return relevant.Count == 0 ? "(none)" : string.Join(", ", relevant);
    }
}

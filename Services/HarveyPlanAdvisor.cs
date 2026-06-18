using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Advisor/Presenter-слой «Плана Харви»: объединяет факты Injury и Stress в понятный снимок.
/// </summary>
public sealed class HarveyPlanAdvisor
{
    private readonly HarveyCareDirectiveRegistry _registry;

    public HarveyPlanAdvisor(HarveyCareDirectiveRegistry registry)
    {
        _registry = registry;
    }

    public HarveyCareDirectiveRegistry DirectiveRegistry => _registry;

    public HarveyPlanSnapshot BuildSnapshot()
        => BuildSnapshotCore();

    public string BuildDebugReport()
    {
        var snapshot = BuildSnapshotCore();
        var sb = new System.Text.StringBuilder(HarveyPlanUiBuilder.BuildDebugReport(snapshot));
        sb.AppendLine($"Directive providers: [{string.Join(", ", _registry.GetRegisteredProviderIds())}]");
        return sb.ToString();
    }

    private HarveyPlanSnapshot BuildSnapshotCore()
    {
        var (injuryRawFacts, stressRawFacts) = _registry.CollectRawFacts();
        var (injuryDirectives, stressDirectives) = _registry.CollectDirectives();
        var merged = MergeAndDedupe(injuryDirectives, stressDirectives);

        var snapshot = new HarveyPlanSnapshot
        {
            InjuryProviderRegistered = _registry.IsRegistered(HarveyProviderRegistry.InjuryProviderId),
            StressProviderRegistered = _registry.IsRegistered(HarveyProviderRegistry.StressProviderId),
            InjuryRawFacts = injuryRawFacts,
            StressRawFacts = stressRawFacts,
            InjuryDirectiveCount = injuryDirectives.Count,
            StressDirectiveCount = stressDirectives.Count,
            AllDirectives = merged,
            Tone = CalculateTone(merged),
            ToneText = BuildToneText(CalculateTone(merged)),
            HarveyAdvice = SelectHarveyAdvice(merged),
            PrimaryAction = GetPrimaryDirective(merged),
            ImmediateActions = FilterByType(merged, HarveyCareDirectiveType.ImmediateAction),
            TodayRules = GetTodayRules(merged),
            AvoidRules = GetAvoidRules(merged),
            Warnings = GetWarnings(merged),
            FailureReasons = GetFailureReasons(merged),
            MedicalNotes = GetMedicalNotes(merged),
            StressNotes = GetStressNotes(merged),
        };

        if (!snapshot.HasAnyContent && merged.Count > 0)
            ApplyCatchAllClassification(snapshot, merged);

        if (snapshot.RawFactsCount > 0 && !snapshot.HasAnyContent)
            snapshot.FallbackReason = HarveyPlanFallbackReason.MappingFailed;

        return snapshot;
    }

    private static void ApplyCatchAllClassification(HarveyPlanSnapshot snapshot, IReadOnlyList<HarveyCareDirective> merged)
    {
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Track(IEnumerable<HarveyCareDirective> items)
        {
            foreach (var item in items)
                assigned.Add(item.Id);
        }

        if (snapshot.PrimaryAction != null)
            assigned.Add(snapshot.PrimaryAction.Id);

        Track(snapshot.ImmediateActions);
        Track(snapshot.TodayRules);
        Track(snapshot.AvoidRules);
        Track(snapshot.Warnings);
        Track(snapshot.FailureReasons);
        Track(snapshot.MedicalNotes);
        Track(snapshot.StressNotes);

        foreach (var directive in merged.Where(d => !assigned.Contains(d.Id)))
        {
            if (directive.Source == HarveyCareDirectiveSource.Stress)
                snapshot.StressNotes.Add(directive);
            else
                snapshot.MedicalNotes.Add(directive);
        }

        snapshot.PrimaryAction ??= GetPrimaryDirective(merged);
    }

    public HarveyCareDirective? GetPrimaryDirective()
        => GetPrimaryDirective(BuildSnapshot().AllDirectives.ToList());

    public string CalculateTone()
        => CalculateTone(BuildSnapshot().AllDirectives.ToList());

    public List<HarveyCareDirective> GetTodayRules()
        => GetTodayRules(BuildSnapshot().AllDirectives.ToList());

    public List<HarveyCareDirective> GetAvoidRules()
        => GetAvoidRules(BuildSnapshot().AllDirectives.ToList());

    public List<HarveyCareDirective> GetWarnings()
        => GetWarnings(BuildSnapshot().AllDirectives.ToList());

    public List<HarveyCareDirective> GetFailureReasons()
        => GetFailureReasons(BuildSnapshot().AllDirectives.ToList());

    private static List<HarveyCareDirective> MergeAndDedupe(
        IReadOnlyList<HarveyCareDirective> injury,
        IReadOnlyList<HarveyCareDirective> stress)
    {
        var byId = new Dictionary<string, HarveyCareDirective>(StringComparer.OrdinalIgnoreCase);

        foreach (var directive in injury.Concat(stress))
        {
            if (string.IsNullOrWhiteSpace(directive.Id))
                continue;

            if (!byId.TryGetValue(directive.Id, out var existing)
                || HarveyCareDirectivePriority.Rank(directive.Priority)
                   < HarveyCareDirectivePriority.Rank(existing.Priority))
            {
                byId[directive.Id] = directive;
            }
        }

        return byId.Values
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ThenBy(d => TypeOrder(d.Type))
            .ThenBy(d => d.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int TypeOrder(string type) => type switch
    {
        HarveyCareDirectiveType.ImmediateAction => 0,
        HarveyCareDirectiveType.Appointment => 1,
        HarveyCareDirectiveType.Avoid => 2,
        HarveyCareDirectiveType.TodayRule => 3,
        HarveyCareDirectiveType.Warning => 4,
        HarveyCareDirectiveType.FailureReason => 5,
        _ => 6,
    };

    private static HarveyCareDirective? GetPrimaryDirective(IReadOnlyList<HarveyCareDirective> merged)
    {
        var candidate = merged
            .Where(d => d.Type == HarveyCareDirectiveType.ImmediateAction)
            .Where(d => d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning)
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .FirstOrDefault();

        if (candidate != null)
            return candidate;

        candidate = merged
            .Where(d => d.Type == HarveyCareDirectiveType.Appointment)
            .Where(d => d.State == HarveyCareDirectiveState.Active)
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .FirstOrDefault();

        if (candidate != null)
            return candidate;

        return merged
            .Where(d => d.State == HarveyCareDirectiveState.Active)
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .FirstOrDefault();
    }

    private static string CalculateTone(IReadOnlyList<HarveyCareDirective> merged)
    {
        if (merged.Count == 0)
            return HarveyCareDirectiveTone.Calm;

        if (merged.Any(d => d.State == HarveyCareDirectiveState.Failed)
            || merged.Any(d => d.Priority == HarveyCareDirectivePriority.Critical && d.State == HarveyCareDirectiveState.Active))
        {
            return HarveyCareDirectiveTone.Strict;
        }

        if (merged.Any(d => d.State == HarveyCareDirectiveState.Warning)
            || merged.Any(d => d.Type == HarveyCareDirectiveType.Warning && d.State == HarveyCareDirectiveState.Active)
            || merged.Any(d => d.Priority == HarveyCareDirectivePriority.High && d.State == HarveyCareDirectiveState.Active))
        {
            return HarveyCareDirectiveTone.Worried;
        }

        bool hasActive = merged.Any(d =>
            d.State == HarveyCareDirectiveState.Active
            && d.Type is HarveyCareDirectiveType.ImmediateAction
                or HarveyCareDirectiveType.TodayRule
                or HarveyCareDirectiveType.Avoid
                or HarveyCareDirectiveType.Appointment);

        if (!hasActive)
        {
            return merged.Any(d => d.HarveyTone == HarveyCareDirectiveTone.Tender)
                ? HarveyCareDirectiveTone.Tender
                : HarveyCareDirectiveTone.Soft;
        }

        return HarveyCareDirectiveTone.Calm;
    }

    private static string BuildToneText(string tone) => tone switch
    {
        HarveyCareDirectiveTone.Soft =>
            "Харви спокоен. Сегодня ты бережёшь себя, и это заметно.",
        HarveyCareDirectiveTone.Calm =>
            "Харви наблюдает за режимом. Пока всё под контролем.",
        HarveyCareDirectiveTone.Worried =>
            "Харви тревожится. Есть риск сорвать лечение, но день ещё можно спасти.",
        HarveyCareDirectiveTone.Strict =>
            "Харви строг. Ты рисковала здоровьем, и он обязательно поговорит с тобой.",
        HarveyCareDirectiveTone.Tender =>
            "Харви рядом. Сейчас главное — не геройствовать и дать себе восстановиться.",
        _ => "Харви наблюдает за режимом. Пока всё под контролем.",
    };

    private static string SelectHarveyAdvice(IReadOnlyList<HarveyCareDirective> merged)
    {
        var advice = merged
            .Select(d => d.HarveyAdvice)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (!string.IsNullOrWhiteSpace(advice))
            return advice!;

        var primary = GetPrimaryDirective(merged);
        if (primary != null && !string.IsNullOrWhiteSpace(primary.Text))
        {
            var appointment = merged.FirstOrDefault(d =>
                d.Type == HarveyCareDirectiveType.Appointment && d.State == HarveyCareDirectiveState.Active);
            if (appointment != null && !string.Equals(appointment.Id, primary.Id, StringComparison.OrdinalIgnoreCase))
                return $"Сначала {primary.Title.ToLowerInvariant()}. Потом {appointment.Title.ToLowerInvariant()}.";
        }

        return "";
    }

    private static List<HarveyCareDirective> GetTodayRules(IReadOnlyList<HarveyCareDirective> merged)
        => merged
            .Where(d => d.Type == HarveyCareDirectiveType.TodayRule)
            .Where(d => d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Done or HarveyCareDirectiveState.Warning)
            .OrderBy(d => d.State == HarveyCareDirectiveState.Done ? 1 : 0)
            .ThenBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();

    private static List<HarveyCareDirective> GetAvoidRules(IReadOnlyList<HarveyCareDirective> merged)
        => FilterByType(merged, HarveyCareDirectiveType.Avoid);

    private static List<HarveyCareDirective> GetWarnings(IReadOnlyList<HarveyCareDirective> merged)
        => merged
            .Where(d => d.Type == HarveyCareDirectiveType.Warning
                || (d.State == HarveyCareDirectiveState.Warning && d.Type != HarveyCareDirectiveType.FailureReason))
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();

    private static List<HarveyCareDirective> GetFailureReasons(IReadOnlyList<HarveyCareDirective> merged)
        => merged
            .Where(d => d.Type == HarveyCareDirectiveType.FailureReason
                || (d.CanFailDay && d.State == HarveyCareDirectiveState.Active && !string.IsNullOrWhiteSpace(d.FailureText)))
            .Concat(merged.Where(d => d.State == HarveyCareDirectiveState.Failed))
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();

    private static List<HarveyCareDirective> GetMedicalNotes(IReadOnlyList<HarveyCareDirective> merged)
        => merged
            .Where(d => d.Source == HarveyCareDirectiveSource.Injury)
            .Where(d => d.Type == HarveyCareDirectiveType.Advice
                || d.Type == HarveyCareDirectiveType.Appointment
                || d.Id.StartsWith("injury.", StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Type != HarveyCareDirectiveType.TodayRule
                && d.Type != HarveyCareDirectiveType.Avoid
                && d.Type != HarveyCareDirectiveType.ImmediateAction
                && d.Type != HarveyCareDirectiveType.Warning
                && d.Type != HarveyCareDirectiveType.FailureReason)
            .Where(d => !ReferenceEquals(d, GetPrimaryDirective(merged)))
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();

    private static List<HarveyCareDirective> GetStressNotes(IReadOnlyList<HarveyCareDirective> merged)
        => merged
            .Where(d => d.Source == HarveyCareDirectiveSource.Stress)
            .Where(d => d.Type != HarveyCareDirectiveType.TodayRule
                && d.Type != HarveyCareDirectiveType.Avoid
                && d.Type != HarveyCareDirectiveType.ImmediateAction
                && d.Type != HarveyCareDirectiveType.Warning
                && d.Type != HarveyCareDirectiveType.FailureReason
                && d.Type != HarveyCareDirectiveType.Appointment)
            .Concat(merged.Where(d =>
                d.Source == HarveyCareDirectiveSource.Stress
                && d.Type == HarveyCareDirectiveType.TodayRule))
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();

    private static List<HarveyCareDirective> FilterByType(
        IReadOnlyList<HarveyCareDirective> merged,
        string type)
        => merged
            .Where(d => d.Type == type && d.State != HarveyCareDirectiveState.Info)
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();
}

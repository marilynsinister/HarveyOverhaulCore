using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.UI;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Собирает view model окна «План Харви» из contributions зарегистрированных providers.
/// </summary>
public sealed class HarveyPanelService
{
    private readonly HarveyProviderRegistry _registry;

    public HarveyPanelService(HarveyProviderRegistry registry)
    {
        _registry = registry;
    }

    public HarveyPanelTab ResolveDefaultTab()
    {
        var contributions = _registry.CollectContributions();

        if (contributions.Any(c => c.HasActiveRecoveryPlan))
            return HarveyPanelTab.Plan;

        if (contributions.Any(c => c.HasPendingHarveyReview))
            return HarveyPanelTab.Overview;

        return HarveyPanelTab.Overview;
    }

    public HarveyPanelViewModel BuildViewModel(HarveyPanelTab selectedTab = HarveyPanelTab.Overview)
    {
        var contributions = _registry.CollectContributions();
        var overview = MergeOverview(contributions);
        var stress = contributions.Select(c => c.StressFields).FirstOrDefault(f => f != null);
        var trust = MergeTrust(contributions);
        var plan = ResolvePlanFields(contributions);
        var injuriesBody = ResolveInjuriesBody(contributions);

        ApplyPlaceholders(ref overview, ref stress, ref trust, ref plan, ref injuriesBody, contributions);

        var vm = new HarveyPanelViewModel
        {
            OverviewStateLine = overview.StateLine,
            OverviewAssignmentLine = overview.AssignmentLine,
            OverviewProgressLine = overview.ProgressLine,
            OverviewAfterLine = overview.AfterLine,
            OverviewStressLine = overview.StressLine,
            OverviewInjuriesLine = overview.InjuriesLine,
            OverviewAdviceLine = overview.AdviceLine,
            StressAssignmentTitle = stress?.AssignmentTitle ?? "",
            StressAssignmentProgress = stress?.AssignmentProgress ?? "",
            StressAssignmentObjective = stress?.AssignmentObjective ?? "",
            StressAssignmentAfter = stress?.AssignmentAfter ?? "",
            StressNoAssignmentLine = stress?.NoAssignmentLine ?? "",
            Handbook = stress?.Handbook ?? new HandbookViewModel(),
            InjuriesBody = injuriesBody,
            PlanTitle = plan?.Title ?? "",
            PlanBody = plan?.Body ?? "",
            TrustLevelLine = trust.LevelLine,
            TrustDescriptionLine = trust.DescriptionLine,
            TrustPermissionsLine = trust.PermissionsLine,
            TrustPlaceholder = trust.Placeholder,
        };

        InitializeTabs(vm, selectedTab);
        return vm;
    }

    private void ApplyPlaceholders(
        ref HarveyPanelOverviewFields overview,
        ref HarveyPanelStressFields? stress,
        ref HarveyPanelTrustFields trust,
        ref HarveyPanelPlanFields? plan,
        ref string injuriesBody,
        IReadOnlyList<HarveyPanelContribution> contributions)
    {
        bool stressRegistered = _registry.IsRegistered(HarveyProviderRegistry.StressProviderId);
        bool injuryRegistered = _registry.IsRegistered(HarveyProviderRegistry.InjuryProviderId);
        bool stressContributed = contributions.Any(c =>
            string.Equals(c.ProviderId, HarveyProviderRegistry.StressProviderId, StringComparison.Ordinal));
        bool injuryContributed = contributions.Any(c =>
            string.Equals(c.ProviderId, HarveyProviderRegistry.InjuryProviderId, StringComparison.Ordinal));

        if (_registry.DidProviderFail(HarveyProviderRegistry.StressProviderId))
        {
            stress ??= new HarveyPanelStressFields();
            stress.NoAssignmentLine = HarveyPanelPlaceholders.ProviderUnavailable;
        }
        else if (!stressRegistered || !stressContributed)
        {
            stress ??= new HarveyPanelStressFields();
            stress.NoAssignmentLine = HarveyPanelPlaceholders.NoStressData;
        }

        if (_registry.DidProviderFail(HarveyProviderRegistry.InjuryProviderId))
        {
            injuriesBody = HarveyPanelPlaceholders.ProviderUnavailable;
        }
        else if (!injuryRegistered || !injuryContributed)
        {
            if (string.IsNullOrWhiteSpace(injuriesBody))
                injuriesBody = HarveyPanelPlaceholders.NoInjuryData;
        }

        if (plan == null || !HasPlanContent(plan))
        {
            bool anyRecoveryFlag = contributions.Any(c => c.HasActiveRecoveryPlan);
            if (!anyRecoveryFlag)
            {
                plan = new HarveyPanelPlanFields
                {
                    Title = HarveyPanelPlaceholders.NoRecoveryPlan,
                    Body = "",
                };
            }
        }

        if (string.IsNullOrWhiteSpace(overview.StateLine)
            && string.IsNullOrWhiteSpace(overview.AssignmentLine)
            && string.IsNullOrWhiteSpace(overview.AdviceLine))
        {
            overview.StateLine = HarveyPanelTexts.Overview.CalmHeadline;
            overview.AssignmentLine = HarveyPanelTexts.Overview.CalmBody;
        }
    }

    private static HarveyPanelOverviewFields MergeOverview(IReadOnlyList<HarveyPanelContribution> contributions)
    {
        var merged = new HarveyPanelOverviewFields();

        foreach (var contribution in contributions)
        {
            if (contribution.OverviewFields is { } fields)
            {
                AssignIfEmpty(merged, fields);
            }
        }

        var sectionText = MergeSectionsText(contributions.SelectMany(c => c.OverviewSections));
        if (!string.IsNullOrWhiteSpace(sectionText))
        {
            if (string.IsNullOrWhiteSpace(merged.StateLine))
                merged.StateLine = sectionText;
            else if (string.IsNullOrWhiteSpace(merged.AssignmentLine))
                merged.AssignmentLine = sectionText;
            else if (string.IsNullOrWhiteSpace(merged.AdviceLine))
                merged.AdviceLine = sectionText;
        }

        return merged;
    }

    private static void AssignIfEmpty(HarveyPanelOverviewFields merged, HarveyPanelOverviewFields fields)
    {
        if (string.IsNullOrWhiteSpace(merged.StateLine) && !string.IsNullOrWhiteSpace(fields.StateLine))
            merged.StateLine = fields.StateLine;
        if (string.IsNullOrWhiteSpace(merged.AssignmentLine) && !string.IsNullOrWhiteSpace(fields.AssignmentLine))
            merged.AssignmentLine = fields.AssignmentLine;
        if (string.IsNullOrWhiteSpace(merged.ProgressLine) && !string.IsNullOrWhiteSpace(fields.ProgressLine))
            merged.ProgressLine = fields.ProgressLine;
        if (string.IsNullOrWhiteSpace(merged.AfterLine) && !string.IsNullOrWhiteSpace(fields.AfterLine))
            merged.AfterLine = fields.AfterLine;
        if (string.IsNullOrWhiteSpace(merged.StressLine) && !string.IsNullOrWhiteSpace(fields.StressLine))
            merged.StressLine = fields.StressLine;
        if (string.IsNullOrWhiteSpace(merged.AdviceLine) && !string.IsNullOrWhiteSpace(fields.AdviceLine))
            merged.AdviceLine = fields.AdviceLine;

        if (!string.IsNullOrWhiteSpace(fields.InjuriesLine))
            merged.InjuriesLine = fields.InjuriesLine;
    }

    private static HarveyPanelTrustFields MergeTrust(IReadOnlyList<HarveyPanelContribution> contributions)
    {
        var merged = new HarveyPanelTrustFields();

        foreach (var contribution in contributions)
        {
            if (contribution.TrustFields is { } fields)
            {
                if (string.IsNullOrWhiteSpace(merged.LevelLine) && !string.IsNullOrWhiteSpace(fields.LevelLine))
                    merged.LevelLine = fields.LevelLine;
                if (string.IsNullOrWhiteSpace(merged.DescriptionLine) && !string.IsNullOrWhiteSpace(fields.DescriptionLine))
                    merged.DescriptionLine = fields.DescriptionLine;
                if (string.IsNullOrWhiteSpace(merged.PermissionsLine) && !string.IsNullOrWhiteSpace(fields.PermissionsLine))
                    merged.PermissionsLine = fields.PermissionsLine;
                if (string.IsNullOrWhiteSpace(merged.Placeholder) && !string.IsNullOrWhiteSpace(fields.Placeholder))
                    merged.Placeholder = fields.Placeholder;
            }
        }

        var sectionText = MergeSectionsText(contributions.SelectMany(c => c.TrustSections));
        if (string.IsNullOrWhiteSpace(merged.DescriptionLine) && !string.IsNullOrWhiteSpace(sectionText))
            merged.DescriptionLine = sectionText;

        return merged;
    }

    private static HarveyPanelPlanFields? ResolvePlanFields(IReadOnlyList<HarveyPanelContribution> contributions)
    {
        var recoveryPlan = contributions
            .FirstOrDefault(c => c.HasActiveRecoveryPlan && c.PlanFields != null)
            ?.PlanFields;

        if (recoveryPlan != null)
            return recoveryPlan;

        var fromFields = contributions.Select(c => c.PlanFields).FirstOrDefault(f => f != null && HasPlanContent(f));
        if (fromFields != null)
            return fromFields;

        var sectionText = MergeSectionsText(contributions.SelectMany(c => c.PlanSections));
        if (string.IsNullOrWhiteSpace(sectionText))
            return null;

        return new HarveyPanelPlanFields
        {
            Title = HarveyPanelTexts.Plan.ActiveTitle,
            Body = sectionText,
        };
    }

    private static string ResolveInjuriesBody(IReadOnlyList<HarveyPanelContribution> contributions)
    {
        var directBody = contributions
            .Select(c => c.InjuriesBody)
            .FirstOrDefault(body => !string.IsNullOrWhiteSpace(body));

        if (!string.IsNullOrWhiteSpace(directBody))
            return directBody;

        return MergeSectionsText(contributions.SelectMany(c => c.InjurySections));
    }

    private static bool HasPlanContent(HarveyPanelPlanFields fields)
        => !string.IsNullOrWhiteSpace(fields.Title) || !string.IsNullOrWhiteSpace(fields.Body);

    private static string MergeSectionsText(IEnumerable<HarveyPanelSectionDto> sections)
    {
        var lines = sections
            .OrderBy(s => s.Priority)
            .SelectMany(FormatSection)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return string.Join("\n\n", lines);
    }

    private static IEnumerable<string> FormatSection(HarveyPanelSectionDto section)
    {
        if (!string.IsNullOrWhiteSpace(section.Status))
            yield return section.Status;

        if (!string.IsNullOrWhiteSpace(section.Title) && !string.IsNullOrWhiteSpace(section.Body))
            yield return $"{section.Title}\n{section.Body}";
        else if (!string.IsNullOrWhiteSpace(section.Title))
            yield return section.Title;
        else if (!string.IsNullOrWhiteSpace(section.Body))
            yield return section.Body;
    }

    private static void InitializeTabs(HarveyPanelViewModel vm, HarveyPanelTab selectedTab)
    {
        var selectedKey = selectedTab.ToString();

        foreach (var (tab, label) in new[]
        {
            (HarveyPanelTab.Overview, HarveyPanelTexts.Tabs.Overview),
            (HarveyPanelTab.Stress, HarveyPanelTexts.Tabs.Stress),
            (HarveyPanelTab.Injuries, HarveyPanelTexts.Tabs.Injuries),
            (HarveyPanelTab.Plan, HarveyPanelTexts.Tabs.Plan),
            (HarveyPanelTab.Trust, HarveyPanelTexts.Tabs.Trust),
        })
        {
            var key = tab.ToString();
            vm.Tabs.Add(new HarveyPanelTabButtonViewModel
            {
                Key = key,
                Label = label,
                Active = string.Equals(key, selectedKey, StringComparison.Ordinal),
            });
        }

        vm.SelectTab(selectedKey);
    }
}

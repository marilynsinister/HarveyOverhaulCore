using HarveyOverhaul.Core.Models;
using StardewModdingAPI;

namespace HarveyOverhaul.Core.Services;

/// <summary>Логирование открытия «Плана Харви» и причин fallback.</summary>
public static class HarveyPlanDiagnostics
{
    public static string ResolveFallbackReason(
        HarveyProviderRegistry panelRegistry,
        HarveyCareDirectiveRegistry directiveRegistry,
        HarveyPlanSnapshot snapshot,
        IReadOnlyList<HarveyPanelContribution> contributions)
    {
        if (snapshot.HasAnyContent)
            return HarveyPlanFallbackReason.None;

        if (snapshot.RawFactsCount > 0 || snapshot.AllDirectives.Count > 0)
            return HarveyPlanFallbackReason.MappingFailed;

        bool injuryDirectiveRegistered = directiveRegistry.IsRegistered(HarveyProviderRegistry.InjuryProviderId);
        bool stressDirectiveRegistered = directiveRegistry.IsRegistered(HarveyProviderRegistry.StressProviderId);

        if (!injuryDirectiveRegistered && !stressDirectiveRegistered)
            return HarveyPlanFallbackReason.NoProviders;

        bool contributionsActive = contributions.Any(c => c.HasActiveRecoveryPlan || c.HasPendingHarveyReview);
        if (contributionsActive && snapshot.RawFactsCount == 0 && snapshot.AllDirectives.Count == 0)
            return HarveyPlanFallbackReason.StateMismatch;

        return HarveyPlanFallbackReason.ProvidersReturnedZeroFacts;
    }

    public static void LogBuildSnapshot(IMonitor monitor, HarveyPlanSnapshot snapshot)
    {
        monitor.Log("[HarveyPlan/Core] === BuildSnapshot START ===", LogLevel.Info);
        monitor.Log(
            $"[HarveyPlan/Core] Injury provider found: {snapshot.InjuryProviderRegistered}",
            LogLevel.Info);
        monitor.Log(
            $"[HarveyPlan/Core] Stress provider found: {snapshot.StressProviderRegistered}",
            LogLevel.Info);

        monitor.Log($"[HarveyPlan/Core] Raw Injury facts count: {snapshot.InjuryRawFacts.Count}", LogLevel.Info);
        foreach (var fact in snapshot.InjuryRawFacts)
        {
            monitor.Log(
                $"[HarveyPlan/Core] Raw Injury fact: Id={fact.Id}, Kind={fact.Kind}, State={fact.State}, " +
                $"Current={fact.Current}, Goal={fact.Goal}, Label={fact.Label}, Details={TrimForLog(fact.Details)}",
                LogLevel.Info);
        }

        monitor.Log($"[HarveyPlan/Core] Raw Stress facts count: {snapshot.StressRawFacts.Count}", LogLevel.Info);
        foreach (var fact in snapshot.StressRawFacts)
        {
            monitor.Log(
                $"[HarveyPlan/Core] Raw Stress fact: Id={fact.Id}, Kind={fact.Kind}, State={fact.State}, " +
                $"Current={fact.Current}, Goal={fact.Goal}, Label={fact.Label}, Details={TrimForLog(fact.Details)}",
                LogLevel.Info);
        }

        monitor.Log($"[HarveyPlan/Core] Directives count after mapping: {snapshot.AllDirectives.Count}", LogLevel.Info);
        foreach (var directive in snapshot.AllDirectives)
        {
            monitor.Log(
                $"[HarveyPlan/Core] Directive: Id={directive.Id}, Type={directive.Type}, Source={directive.Source}, " +
                $"Title={directive.Title}, State={directive.State}, Current={directive.Current}, Goal={directive.Goal}",
                LogLevel.Info);
        }

        monitor.Log("[HarveyPlan/Core] Sections:", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] ImmediateActions={snapshot.ImmediateActions.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] TodayRules={snapshot.TodayRules.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] AvoidRules={snapshot.AvoidRules.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] Warnings={snapshot.Warnings.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] FailureReasons={snapshot.FailureReasons.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] MedicalNotes={snapshot.MedicalNotes.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] StressNotes={snapshot.StressNotes.Count}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] HasAnyContent={snapshot.HasAnyContent}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] FallbackReason={snapshot.FallbackReason}", LogLevel.Info);

        if (snapshot.RawFactsCount > 0 && !snapshot.HasAnyContent)
        {
            monitor.Log(
                "[HarveyPlan/Core] ERROR: Raw facts exist but no plan sections were built.",
                LogLevel.Error);
        }

        monitor.Log("[HarveyPlan/Core] === BuildSnapshot END ===", LogLevel.Info);
    }

    public static void LogOpenPlan(
        IMonitor monitor,
        HarveyProviderRegistry panelRegistry,
        HarveyCareDirectiveRegistry directiveRegistry,
        HarveyPlanSnapshot snapshot,
        string? fallbackReason,
        HarveyPanelTab selectedTab,
        int planSectionCount)
    {
        LogBuildSnapshot(monitor, snapshot);

        bool injuryPanel = panelRegistry.IsRegistered(HarveyProviderRegistry.InjuryProviderId);
        bool stressPanel = panelRegistry.IsRegistered(HarveyProviderRegistry.StressProviderId);

        monitor.Log("[HarveyPlan/Core] OpenPlan called", LogLevel.Info);
        monitor.Log(
            $"[HarveyPlan/Core] Injury panel provider: {(injuryPanel ? "found" : "not found")}",
            LogLevel.Info);
        monitor.Log(
            $"[HarveyPlan/Core] Stress panel provider: {(stressPanel ? "found" : "not found")}",
            LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] Rendering menu: HarveyPanelMenu from Core", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] Selected tab: {selectedTab}", LogLevel.Info);
        monitor.Log($"[HarveyPlan/Core] Plan UI section count: {planSectionCount}", LogLevel.Info);

        if (!string.IsNullOrWhiteSpace(fallbackReason))
        {
            monitor.Log(
                $"[HarveyPlan/Core] Fallback shown because: {fallbackReason}",
                fallbackReason is HarveyPlanFallbackReason.StateMismatch or HarveyPlanFallbackReason.MappingFailed
                    ? LogLevel.Warn
                    : LogLevel.Info);
        }
    }

    public static string BuildDebugFooter(HarveyPlanSnapshot snapshot, bool verbose)
        => HarveyPlanUiBuilder.BuildDebugFooter(snapshot, verbose);

    private static string TrimForLog(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string trimmed = value.Replace('\n', ' ').Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..117] + "...";
    }
}

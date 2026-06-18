using System.Text;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.UI;

namespace HarveyOverhaul.Core.Services;

/// <summary>Строит секции StardewUI и текст плана из HarveyPlanSnapshot.</summary>
internal static class HarveyPlanUiBuilder
{
    public static List<HarveyPanelSectionViewModel> BuildPlanTabSections(HarveyPlanSnapshot snapshot)
    {
        if (snapshot.RawFactsCount == 0 && snapshot.AllDirectives.Count == 0)
            return [BuildEmptyPlanSection()];

        if (snapshot.RawFactsCount > 0 && !snapshot.HasAnyContent)
            return BuildMappingDiagnosticSections(snapshot);

        return BuildFullPlanSections(snapshot);
    }

    public static string BuildPlanDetailBody(HarveyPlanSnapshot snapshot)
    {
        if (snapshot.RawFactsCount == 0 && snapshot.AllDirectives.Count == 0)
            return HarveyPanelTexts.Overview.CalmAdvice;

        if (snapshot.RawFactsCount > 0 && !snapshot.HasAnyContent)
            return BuildMappingDiagnosticBody(snapshot);

        return BuildFullPlanBody(snapshot);
    }

    public static List<HarveyPanelSectionDto> BuildSections(HarveyPlanSnapshot snapshot)
        => BuildFullPlanSections(snapshot)
            .Select(section => new HarveyPanelSectionDto
            {
                Title = section.Headline,
                Status = section.StatusLine,
                Body = section.BodyText,
                Priority = 0,
            })
            .ToList();

    public static HarveyPanelPlanFields? BuildPlanFields(HarveyPlanSnapshot snapshot)
    {
        string body = BuildPlanDetailBody(snapshot);
        if (string.IsNullOrWhiteSpace(body))
            return null;

        return new HarveyPanelPlanFields
        {
            Title = snapshot.Title,
            Body = body,
        };
    }

    public static HarveyPanelSectionViewModel? BuildOverviewSummary(HarveyPlanSnapshot snapshot)
    {
        if (!snapshot.HasAnyContent && snapshot.RawFactsCount == 0)
            return null;

        var urgent = snapshot.Warnings.FirstOrDefault(w =>
                w.Priority == HarveyCareDirectivePriority.Critical || w.Priority == HarveyCareDirectivePriority.High)
            ?? snapshot.FailureReasons.FirstOrDefault(r => r.State == HarveyCareDirectiveState.Warning)
            ?? snapshot.PrimaryAction;

        int assignmentCount = snapshot.ImmediateActions.Count
            + snapshot.TodayRules.Count
            + snapshot.AvoidRules.Count
            + snapshot.StressNotes.Count;
        if (assignmentCount == 0 && snapshot.AllDirectives.Count > 0)
            assignmentCount = snapshot.AllDirectives.Count;

        var bodyParts = new List<string>();
        if (snapshot.PrimaryAction != null)
            bodyParts.Add(FormatDirective(snapshot.PrimaryAction));

        foreach (var rule in snapshot.TodayRules.Take(3))
            bodyParts.Add(FormatDirective(rule));

        foreach (var rule in snapshot.StressNotes.Take(2))
            bodyParts.Add(FormatDirective(rule));

        if (bodyParts.Count == 0)
        {
            foreach (var warning in snapshot.Warnings.Take(2))
                bodyParts.Add(FormatDirective(warning));
        }

        return new HarveyPanelSectionViewModel
        {
            Headline = $"Активных назначений: {assignmentCount}",
            StatusLine = urgent != null ? $"Срочно: {urgent.Title.ToLowerInvariant()}" : "",
            BodyText = string.Join("\n\n", bodyParts.Where(part => !string.IsNullOrWhiteSpace(part))),
            AccentColor = urgent != null ? "#8b4513" : "#3b2a1a",
            StatusColor = urgent != null ? "#8b4513" : "#7f6139",
        };
    }

    public static string BuildDebugReport(HarveyPlanSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[HarveyPlan] === Snapshot dump ===");
        sb.AppendLine($"Providers: Injury={(snapshot.InjuryProviderRegistered ? "yes" : "no")}, Stress={(snapshot.StressProviderRegistered ? "yes" : "no")}");
        sb.AppendLine($"Raw facts: Injury={snapshot.InjuryRawFacts.Count}, Stress={snapshot.StressRawFacts.Count}, total={snapshot.RawFactsCount}");
        sb.AppendLine($"Directives (mapped): total={snapshot.AllDirectives.Count} (Injury={snapshot.InjuryDirectiveCount}, Stress={snapshot.StressDirectiveCount})");
        sb.AppendLine("Sections:");
        sb.AppendLine($"  Immediate={snapshot.ImmediateActions.Count}");
        sb.AppendLine($"  Today={snapshot.TodayRules.Count}");
        sb.AppendLine($"  Avoid={snapshot.AvoidRules.Count}");
        sb.AppendLine($"  Warnings={snapshot.Warnings.Count}");
        sb.AppendLine($"  Failures={snapshot.FailureReasons.Count}");
        sb.AppendLine($"  Medical={snapshot.MedicalNotes.Count}");
        sb.AppendLine($"  Stress={snapshot.StressNotes.Count}");
        sb.AppendLine($"  Plan UI sections (est.): {BuildPlanTabSections(snapshot).Count}");
        sb.AppendLine($"HasAnyContent={snapshot.HasAnyContent}");
        sb.AppendLine($"Tone: {snapshot.Tone}");
        sb.AppendLine($"PrimaryAction: {snapshot.PrimaryAction?.Id ?? "-"} ({snapshot.PrimaryAction?.Title ?? "-"})");
        sb.AppendLine($"HarveyAdvice: {(string.IsNullOrWhiteSpace(snapshot.HarveyAdvice) ? "-" : snapshot.HarveyAdvice)}");
        sb.AppendLine($"FallbackReason: {snapshot.FallbackReason}");

        foreach (var fact in snapshot.InjuryRawFacts)
        {
            sb.AppendLine(
                $"  Raw[Injury] id={fact.Id} kind={fact.Kind} state={fact.State} " +
                $"progress={FormatRawProgress(fact)} label={fact.Label}");
        }

        foreach (var fact in snapshot.StressRawFacts)
        {
            sb.AppendLine(
                $"  Raw[Stress] id={fact.Id} kind={fact.Kind} state={fact.State} " +
                $"progress={FormatRawProgress(fact)} label={fact.Label}");
        }

        foreach (var d in snapshot.AllDirectives)
        {
            sb.AppendLine(
                $"  Directive id={d.Id} source={d.Source} type={d.Type} state={d.State} pri={d.Priority} " +
                $"progress={FormatProgress(d)} title={d.Title}");
        }

        return sb.ToString();
    }

    public static string BuildDebugFooter(HarveyPlanSnapshot snapshot, bool verbose)
    {
        if (!verbose)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine("Debug · HarveyOverhaulCore");
        sb.AppendLine($"Providers: Injury={(snapshot.InjuryProviderRegistered ? "yes" : "no")}, Stress={(snapshot.StressProviderRegistered ? "yes" : "no")}");
        sb.AppendLine($"Raw facts: Injury={snapshot.InjuryRawFacts.Count}, Stress={snapshot.StressRawFacts.Count}");
        sb.AppendLine($"Directives: {snapshot.AllDirectives.Count}");
        sb.AppendLine(
            $"Sections: Immediate={snapshot.ImmediateActions.Count}, Today={snapshot.TodayRules.Count}, " +
            $"Avoid={snapshot.AvoidRules.Count}, Medical={snapshot.MedicalNotes.Count}, " +
            $"Stress={snapshot.StressNotes.Count}, Warnings={snapshot.Warnings.Count}, Failures={snapshot.FailureReasons.Count}");
        sb.AppendLine($"HasAnyContent={snapshot.HasAnyContent}, Fallback={snapshot.FallbackReason}");
        return sb.ToString().TrimEnd();
    }

    private static List<HarveyPanelSectionViewModel> BuildFullPlanSections(HarveyPlanSnapshot snapshot)
    {
        var sections = new List<HarveyPanelSectionViewModel>();

        sections.Add(new HarveyPanelSectionViewModel
        {
            Headline = snapshot.Title,
            StatusLine = ToneHeadline(snapshot.Tone),
            BodyText = snapshot.ToneText,
            AccentColor = MapToneAccent(snapshot.Tone),
            StatusColor = MapToneStatus(snapshot.Tone),
        });

        var immediate = snapshot.ImmediateActions
            .Where(d => d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning)
            .ToList();
        if (immediate.Count > 0)
            AddDirectiveSection(sections, "Что сделать сейчас", immediate);
        else if (snapshot.PrimaryAction != null)
            AddDirectiveSection(sections, "Что сделать сейчас", [snapshot.PrimaryAction]);

        AddDirectiveSectionIfAny(sections, "Назначения на сегодня", snapshot.TodayRules);
        AddDirectiveSectionIfAny(sections, "Чего избегать", snapshot.AvoidRules);
        AddDirectiveSectionIfAny(sections, "Предупреждения", snapshot.Warnings);
        AddDirectiveSectionIfAny(sections, "Почему день может сорваться", snapshot.FailureReasons, useFailureText: true);
        AddDirectiveSectionIfAny(sections, "Травмы и лечение", snapshot.MedicalNotes);
        AddDirectiveSectionIfAny(sections, "Стресс и безопасность", snapshot.StressNotes);

        var infoDirectives = snapshot.AllDirectives
            .Where(d => d.Type == HarveyCareDirectiveType.Advice || d.State == HarveyCareDirectiveState.Info)
            .Where(d => !snapshot.MedicalNotes.Any(m => string.Equals(m.Id, d.Id, StringComparison.OrdinalIgnoreCase)))
            .Where(d => !snapshot.StressNotes.Any(m => string.Equals(m.Id, d.Id, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        AddDirectiveSectionIfAny(sections, "Дополнительно", infoDirectives);

        if (!string.IsNullOrWhiteSpace(snapshot.HarveyAdvice))
        {
            sections.Add(new HarveyPanelSectionViewModel
            {
                Headline = "Совет Харви",
                BodyText = $"«{snapshot.HarveyAdvice}»",
                AccentColor = "#3b2a1a",
                StatusColor = "#7f6139",
            });
        }

        return sections;
    }

    private static List<HarveyPanelSectionViewModel> BuildMappingDiagnosticSections(HarveyPlanSnapshot snapshot)
    {
        return
        [
            new HarveyPanelSectionViewModel
            {
                Headline = "Диагностика плана",
                StatusLine = "Core получил состояния, но не смог превратить их в указания",
                BodyText = BuildMappingDiagnosticBody(snapshot),
                AccentColor = "#8b4513",
                StatusColor = "#8b4513",
            },
        ];
    }

    private static string BuildMappingDiagnosticBody(HarveyPlanSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Core получил состояния, но не смог превратить их в указания.");
        sb.AppendLine();
        sb.AppendLine("Сырые состояния:");

        foreach (var fact in snapshot.AllRawFacts)
        {
            sb.Append("• [");
            sb.Append(fact.Source);
            sb.Append("] ");
            sb.Append(fact.Label);
            sb.Append(" — ");
            sb.Append(fact.State);

            string progress = FormatRawProgress(fact);
            if (!string.IsNullOrWhiteSpace(progress))
            {
                sb.Append(' ');
                sb.Append(progress);
            }

            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(fact.Details))
                sb.AppendLine($"  {fact.Details.Trim()}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildFullPlanBody(HarveyPlanSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToneHeadline(snapshot.Tone));
        sb.AppendLine(snapshot.ToneText);

        AppendBodyBlock(sb, "Что сделать сейчас", snapshot.PrimaryAction != null
            ? [snapshot.PrimaryAction]
            : snapshot.ImmediateActions);
        AppendBodyBlock(sb, "Назначения на сегодня", snapshot.TodayRules);
        AppendBodyBlock(sb, "Чего избегать", snapshot.AvoidRules);
        AppendBodyBlock(sb, "Предупреждения", snapshot.Warnings);
        AppendBodyBlock(sb, "Почему день может сорваться", snapshot.FailureReasons, useFailureText: true);
        AppendBodyBlock(sb, "Травмы и лечение", snapshot.MedicalNotes);
        AppendBodyBlock(sb, "Стресс и безопасность", snapshot.StressNotes);

        if (!string.IsNullOrWhiteSpace(snapshot.HarveyAdvice))
        {
            sb.AppendLine();
            sb.AppendLine("Совет Харви");
            sb.AppendLine($"«{snapshot.HarveyAdvice}»");
        }

        return sb.ToString().TrimEnd();
    }

    private static HarveyPanelSectionViewModel BuildEmptyPlanSection()
        => new()
        {
            Headline = HarveyPanelTexts.Overview.CalmHeadline,
            BodyText = HarveyPanelTexts.Overview.CalmAdvice,
        };

    private static void AddDirectiveSectionIfAny(
        List<HarveyPanelSectionViewModel> sections,
        string headline,
        IReadOnlyList<HarveyCareDirective> directives,
        bool useFailureText = false)
    {
        if (directives.Count == 0)
            return;

        AddDirectiveSection(sections, headline, directives, useFailureText);
    }

    private static void AddDirectiveSection(
        List<HarveyPanelSectionViewModel> sections,
        string headline,
        IReadOnlyList<HarveyCareDirective> directives,
        bool useFailureText = false)
    {
        sections.Add(new HarveyPanelSectionViewModel
        {
            Headline = headline,
            BodyText = string.Join("\n\n", directives.Select(d => FormatDirective(d, useFailureText))),
            AccentColor = directives.Any(d => d.Priority == HarveyCareDirectivePriority.Critical)
                ? "#8b4513"
                : "#3b2a1a",
            StatusColor = directives.Any(d => d.Priority == HarveyCareDirectivePriority.High)
                ? "#7f6139"
                : "#7f6139",
        });
    }

    private static void AppendBodyBlock(
        StringBuilder sb,
        string headline,
        IReadOnlyList<HarveyCareDirective> directives,
        bool useFailureText = false)
    {
        if (directives.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine(headline);
        foreach (var directive in directives)
            sb.AppendLine(FormatDirective(directive, useFailureText));
    }

    private static string FormatDirective(HarveyCareDirective d, bool useFailureText = false)
    {
        string mark = d.State switch
        {
            HarveyCareDirectiveState.Done => "✓ ",
            HarveyCareDirectiveState.Failed => "✗ ",
            HarveyCareDirectiveState.Warning => "⚠ ",
            _ => "• ",
        };

        var line = new StringBuilder();
        line.Append(mark);
        line.Append(d.Title);

        string progress = FormatProgress(d);
        if (!string.IsNullOrWhiteSpace(progress))
            line.Append($" — {progress}");

        if (d.State == HarveyCareDirectiveState.Done)
            line.Append(" — выполнено");

        line.Append('.');

        string detail = useFailureText && !string.IsNullOrWhiteSpace(d.FailureText)
            ? d.FailureText
            : d.Text;

        if (!string.IsNullOrWhiteSpace(detail))
        {
            line.AppendLine();
            line.Append("  ");
            line.Append(detail.Trim());
        }

        return line.ToString().TrimEnd();
    }

    private static string FormatProgress(HarveyCareDirective d)
    {
        if (d.Goal <= 0)
            return "";

        int current = Math.Min(d.Current, d.Goal);
        return string.IsNullOrWhiteSpace(d.Unit)
            ? $"{current}/{d.Goal}"
            : $"{current}/{d.Goal} {d.Unit}";
    }

    private static string FormatRawProgress(HarveyCareRawFact fact)
    {
        if (fact.Goal <= 0)
            return "";

        int current = Math.Min(fact.Current, fact.Goal);
        return string.IsNullOrWhiteSpace(fact.Unit)
            ? $"{current}/{fact.Goal}"
            : $"{current}/{fact.Goal} {fact.Unit}";
    }

    private static string ToneHeadline(string tone) => tone switch
    {
        HarveyCareDirectiveTone.Soft => "Харви спокоен",
        HarveyCareDirectiveTone.Calm => "Харви наблюдает",
        HarveyCareDirectiveTone.Worried => "Харви тревожится",
        HarveyCareDirectiveTone.Strict => "Харви строг",
        HarveyCareDirectiveTone.Tender => "Харви рядом",
        _ => "Харви наблюдает",
    };

    private static string MapToneAccent(string tone) => tone switch
    {
        HarveyCareDirectiveTone.Strict => "#8b4513",
        HarveyCareDirectiveTone.Worried => "#7f6139",
        _ => "#3b2a1a",
    };

    private static string MapToneStatus(string tone) => tone switch
    {
        HarveyCareDirectiveTone.Strict => "#8b4513",
        HarveyCareDirectiveTone.Worried => "#7f6139",
        _ => "#7f6139",
    };
}

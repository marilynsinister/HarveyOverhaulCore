using System.Text;
using HarveyOverhaul.Core.Core;
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
            return BuildMappingDiagnosticSections(snapshot)[0].BodyText;

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

        // Считаем только невыполненные назначения, каждое один раз (включая визиты к Харви).
        int assignmentCount = snapshot.AllDirectives
            .Where(d => d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning or HarveyCareDirectiveState.Failed)
            .Where(d => d.Type is not (HarveyCareDirectiveType.Advice or HarveyCareDirectiveType.FailureReason or HarveyCareDirectiveType.Warning))
            .Select(d => d.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bodyParts = new List<string>();
        void AddPart(HarveyCareDirective d)
        {
            if (shown.Add(d.Id))
                bodyParts.Add(FormatDirective(d));
        }

        if (snapshot.PrimaryAction != null)
            AddPart(snapshot.PrimaryAction);

        foreach (var rule in snapshot.TodayRules.Where(r => r.State != HarveyCareDirectiveState.Done).Take(3))
            AddPart(rule);

        foreach (var rule in snapshot.StressNotes.Take(2))
            AddPart(rule);

        if (bodyParts.Count == 0)
        {
            foreach (var warning in snapshot.Warnings.Take(2))
                AddPart(warning);
        }

        if (bodyParts.Count > 0)
            bodyParts.Add("Почему это нужно и что делать дальше — во вкладке «План».");

        return new HarveyPanelSectionViewModel
        {
            Headline = $"Активных назначений: {assignmentCount}",
            StatusLine = urgent != null ? $"Срочно: {PlayerGrammar.LowerFirst(urgent.Title)}" : "",
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

    /// <summary>
    /// Вкладка «План» читается сверху вниз как инструкция:
    /// почему есть план → что сделать сейчас → что потом → режим дня → запреты → что пошло не так → что будет дальше.
    /// Каждый пункт показывается один раз; последствия нарушения — прямо в пункте, а не отдельным дублирующим блоком.
    /// </summary>
    private static List<HarveyPanelSectionViewModel> BuildFullPlanSections(HarveyPlanSnapshot snapshot)
    {
        var sections = new List<HarveyPanelSectionViewModel> { BuildHeaderSection(snapshot) };
        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Провалы — первыми после шапки: игрок должен сразу понять, что случилось и как это исправить.
        var failed = Unshown(snapshot.AllDirectives.Where(d => d.State == HarveyCareDirectiveState.Failed), shown);
        AddDirectiveSectionIfAny(
            sections,
            "Что пошло не так",
            failed,
            status: "Сегодняшний день лечения не засчитан",
            accent: "#8b4513");

        var now = Unshown(BuildNowList(snapshot), shown);
        if (now.Count > 0)
        {
            AddDirectiveSectionIfAny(
                sections,
                "Шаг 1 — сделай сейчас",
                [now[0]],
                status: StepStatus(now[0]),
                accent: now[0].Priority == HarveyCareDirectivePriority.Critical ? "#8b4513" : "#3b2a1a");
        }

        var later = now.Skip(1).Concat(Unshown(GetAppointments(snapshot), shown)).ToList();
        AddDirectiveSectionIfAny(
            sections,
            now.Count > 0 ? "Шаг 2 — затем" : "Шаг 1 — визит к Харви",
            later,
            status: later.Count > 1 ? "По порядку, сверху вниз" : "",
            numbered: later.Count > 1);

        var rules = Unshown(snapshot.TodayRules, shown);
        AddDirectiveSectionIfAny(sections, "Режим на сегодня", rules, status: ProgressStatus(rules));

        var avoid = Unshown(snapshot.AvoidRules, shown);
        AddDirectiveSectionIfAny(sections, "Сегодня нельзя", avoid, status: avoid.Count > 0 ? "Нарушение сорвёт день лечения" : "");

        // Невыполненные задания плана, которые сорвут день, если не успеть до сна.
        var pending = Unshown(snapshot.FailureReasons, shown);
        AddDirectiveSectionIfAny(sections, "Успей до конца дня", pending, status: "Иначе день не засчитается");

        AddDirectiveSectionIfAny(sections, "Предупреждения", Unshown(snapshot.Warnings, shown));
        AddDirectiveSectionIfAny(sections, "Травмы и лечение", Unshown(snapshot.MedicalNotes, shown));
        AddDirectiveSectionIfAny(sections, "Стресс и безопасность", Unshown(snapshot.StressNotes, shown));

        var infoDirectives = snapshot.AllDirectives
            .Where(d => d.Type == HarveyCareDirectiveType.Advice || d.State == HarveyCareDirectiveState.Info);
        AddDirectiveSectionIfAny(sections, "Дополнительно", Unshown(infoDirectives, shown));

        sections.Add(new HarveyPanelSectionViewModel
        {
            Headline = "Что будет дальше",
            BodyText = BuildWhatNext(snapshot),
            AccentColor = "#2e6b2e",
        });

        // «Совет Харви» вкладки «План» показывается в подвале окна (adviceByTab), отдельной секцией не дублируем.
        return sections;
    }

    private const string Legend = "✓ сделано   • нужно сделать   ⚠ под угрозой   ✗ нарушено";

    private static HarveyPanelSectionViewModel BuildHeaderSection(HarveyPlanSnapshot snapshot)
    {
        var actionable = GetActionable(snapshot);
        int done = actionable.Count(d => d.State == HarveyCareDirectiveState.Done);

        string status = ToneHeadline(snapshot.Tone);
        if (actionable.Count > 0)
            status += $" · выполнено {done} из {actionable.Count}";

        var body = new StringBuilder();
        body.AppendLine(snapshot.ToneText);
        body.AppendLine();
        body.AppendLine(BuildWhySummary(snapshot));
        body.AppendLine();
        body.Append(Legend);

        return new HarveyPanelSectionViewModel
        {
            Headline = snapshot.Title,
            StatusLine = status,
            BodyText = body.ToString(),
            AccentColor = MapToneAccent(snapshot.Tone),
            StatusColor = MapToneStatus(snapshot.Tone),
        };
    }

    /// <summary>Пункты, у которых есть результат «сделано/не сделано» (без советов и справочных строк).</summary>
    private static List<HarveyCareDirective> GetActionable(HarveyPlanSnapshot snapshot)
        => snapshot.AllDirectives
            .Where(d => d.Type is HarveyCareDirectiveType.ImmediateAction
                or HarveyCareDirectiveType.Appointment
                or HarveyCareDirectiveType.TodayRule
                or HarveyCareDirectiveType.Avoid)
            .Where(d => d.State != HarveyCareDirectiveState.Info)
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

    /// <summary>«Почему Харви составил план»: причины от модов, иначе — общая формулировка по источникам.</summary>
    private static string BuildWhySummary(HarveyPlanSnapshot snapshot)
    {
        var reasons = snapshot.AllDirectives
            .Where(d => d.State != HarveyCareDirectiveState.Done)
            .Select(d => d.Reason?.Trim() ?? "")
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

        if (reasons.Count > 0)
            return "Почему Харви составил план:\n" + string.Join("\n", reasons.Select(r => $"• {r}"));

        bool injury = snapshot.AllDirectives.Any(d =>
            d.Source is HarveyCareDirectiveSource.Injury or HarveyCareDirectiveSource.Mixed
            && d.State != HarveyCareDirectiveState.Done);
        bool stress = snapshot.AllDirectives.Any(d =>
            d.Source is HarveyCareDirectiveSource.Stress or HarveyCareDirectiveSource.Mixed
            && d.State != HarveyCareDirectiveState.Done);

        return (injury, stress) switch
        {
            (true, true) => "Почему Харви составил план: травма ещё заживает, а стресс мешает восстановлению.",
            (true, false) => "Почему Харви составил план: травма ещё заживает, ей нужен режим.",
            (false, true) => "Почему Харви составил план: накопился стресс, ему нужно время и забота.",
            _ => "Почему Харви составил план: он хочет убедиться, что ты восстанавливаешься.",
        };
    }

    /// <summary>Объяснение, чем закончится день и что делать после выполнения пунктов.</summary>
    private static string BuildWhatNext(HarveyPlanSnapshot snapshot)
    {
        if (snapshot.AllDirectives.Any(d => d.State == HarveyCareDirectiveState.Failed))
        {
            return "Сегодня план нарушен, и этот день лечения не засчитается — восстановление может затянуться.\n" +
                   "Поговори с Харви: он объяснит, чем это опасно, и скорректирует план. Завтра можно продолжить.";
        }

        bool hasAppointment = snapshot.AllDirectives.Any(d =>
            d.Type == HarveyCareDirectiveType.Appointment && d.State == HarveyCareDirectiveState.Active);
        bool hasOpenSteps = snapshot.AllDirectives.Any(d =>
            d.Type == HarveyCareDirectiveType.ImmediateAction
            && d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning);
        bool hasRules = snapshot.AllDirectives.Any(d =>
            d.Type is HarveyCareDirectiveType.TodayRule or HarveyCareDirectiveType.Avoid
            && d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning);

        var lines = new List<string>();
        if (hasOpenSteps)
            lines.Add("Выполни шаги по порядку — прогресс обновляется сам, план можно открыть снова в любой момент.");
        if (hasAppointment)
            lines.Add("Затем найди Харви и поговори с ним: днём он обычно в клинике. Он проверит состояние и обновит план.");
        if (hasRules)
            lines.Add("Соблюдай режим до сна: день восстановления засчитывается в конце дня, когда ты ложишься спать.");

        if (lines.Count == 0)
            lines.Add("На сегодня всё выполнено. Отдыхай — Харви сам скажет, если понадобится осмотр.");

        return string.Join("\n", lines);
    }

    private static string StepStatus(HarveyCareDirective d) => d.Type switch
    {
        HarveyCareDirectiveType.Appointment => d.Priority == HarveyCareDirectivePriority.Critical
            ? "Срочно к Харви"
            : "Нужен разговор с Харви",
        _ => d.Priority == HarveyCareDirectivePriority.Critical ? "Срочно" : "Самое важное сейчас",
    };

    private static string ProgressStatus(IReadOnlyList<HarveyCareDirective> items)
    {
        if (items.Count == 0)
            return "";

        int done = items.Count(d => d.State == HarveyCareDirectiveState.Done);
        return done == items.Count
            ? "Всё соблюдено"
            : $"Соблюдено {done} из {items.Count} — засчитается к концу дня";
    }

    private static List<HarveyPanelSectionViewModel> BuildMappingDiagnosticSections(HarveyPlanSnapshot snapshot)
    {
        // Игроку — обычный список состояний без технических полей; подробности — в консольном дампе плана.
        var lines = snapshot.AllRawFacts
            .Select(f => string.IsNullOrWhiteSpace(f.Details) ? $"• {f.Label}" : $"• {f.Label}\n   {f.Details.Trim()}")
            .Distinct()
            .ToList();

        return
        [
            new HarveyPanelSectionViewModel
            {
                Headline = snapshot.Title,
                StatusLine = "Харви присматривает за твоим состоянием",
                BodyText = lines.Count > 0
                    ? string.Join("\n", lines) + "\n\nЕсли что-то понадобится, Харви скажет сам — загляни к нему в клинику."
                    : HarveyPanelTexts.Overview.CalmAdvice,
            },
        ];
    }


    /// <summary>Текстовая версия плана — те же секции, что и в окне (один источник правды).</summary>
    private static string BuildFullPlanBody(HarveyPlanSnapshot snapshot)
    {
        var sb = new StringBuilder();
        foreach (var section in BuildFullPlanSections(snapshot))
        {
            if (sb.Length > 0)
                sb.AppendLine();
            if (section.HasHeadline)
                sb.AppendLine(section.Headline);
            if (section.HasStatusLine)
                sb.AppendLine(section.StatusLine);
            if (section.HasBodyText)
                sb.AppendLine(section.BodyText);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.HarveyAdvice))
        {
            sb.AppendLine();
            sb.AppendLine("Совет Харви");
            sb.AppendLine($"«{snapshot.HarveyAdvice}»");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Главное действие первым, затем остальные активные срочные действия.</summary>
    private static List<HarveyCareDirective> BuildNowList(HarveyPlanSnapshot snapshot)
    {
        var list = new List<HarveyCareDirective>();
        if (snapshot.PrimaryAction != null)
            list.Add(snapshot.PrimaryAction);

        list.AddRange(snapshot.ImmediateActions
            .Where(d => d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning));
        return list;
    }

    /// <summary>Все активные визиты к Харви — и травмы, и стресс (раньше стресс-визиты терялись, если не были главными).</summary>
    private static List<HarveyCareDirective> GetAppointments(HarveyPlanSnapshot snapshot)
        => snapshot.AllDirectives
            .Where(d => d.Type == HarveyCareDirectiveType.Appointment && d.State == HarveyCareDirectiveState.Active)
            .OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority))
            .ToList();

    private static List<HarveyCareDirective> Unshown(IEnumerable<HarveyCareDirective> directives, HashSet<string> shown)
        => directives.Where(d => shown.Add(d.Id)).ToList();

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
        string status = "",
        string? accent = null,
        bool numbered = false)
    {
        if (directives.Count == 0)
            return;

        var items = directives
            .Select((d, i) => FormatDirective(d, detailed: true, number: numbered ? i + 1 : 0))
            .ToList();

        sections.Add(new HarveyPanelSectionViewModel
        {
            Headline = headline,
            StatusLine = status,
            BodyText = string.Join("\n\n", items),
            AccentColor = accent
                ?? (directives.Any(d => d.Priority == HarveyCareDirectivePriority.Critical) ? "#8b4513" : "#3b2a1a"),
            StatusColor = directives.Any(d => d.State is HarveyCareDirectiveState.Failed or HarveyCareDirectiveState.Warning)
                ? "#8b4513"
                : "#7f6139",
        });
    }

    /// <summary>
    /// Пункт плана. Подробный вид отвечает на три вопроса игрока:
    /// что сделать (заголовок + прогресс), почему (Reason) и что дальше / чем грозит нарушение.
    /// </summary>
    private static string FormatDirective(HarveyCareDirective d, bool detailed = false, int number = 0)
    {
        string mark = d.State switch
        {
            HarveyCareDirectiveState.Done => "✓",
            HarveyCareDirectiveState.Failed => "✗",
            HarveyCareDirectiveState.Warning => "⚠",
            _ => number > 0 ? $"{number}." : "•",
        };

        var line = new StringBuilder();
        line.Append(mark);
        line.Append(' ');
        line.Append(d.Title.TrimEnd('.'));

        string progress = FormatProgress(d);
        if (!string.IsNullOrWhiteSpace(progress))
            line.Append($" — {progress}");

        if (d.State == HarveyCareDirectiveState.Done)
            line.Append(" — выполнено");

        AppendDetail(line, null, d.Text);

        if (!detailed || d.State == HarveyCareDirectiveState.Done)
            return line.ToString().TrimEnd();

        AppendDetail(line, "Почему:", d.Reason);

        string remaining = FormatRemaining(d);
        if (!string.IsNullOrWhiteSpace(remaining))
            AppendDetail(line, "Осталось:", remaining);

        if (!string.IsNullOrWhiteSpace(d.FailureText) && d.State != HarveyCareDirectiveState.Failed)
            AppendDetail(line, "Риск:", FormatFailureRisk(d.FailureText));

        AppendDetail(line, "Дальше:", ResolveNextStep(d));

        return line.ToString().TrimEnd();
    }

    private static void AppendDetail(StringBuilder line, string? label, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        line.AppendLine();
        line.Append("   ");
        if (label != null)
        {
            line.Append(label);
            line.Append(' ');
        }

        line.Append(PlayerGrammar.UpperFirst(text.Trim()));
    }

    /// <summary>FailureText пишется провайдерами как «если ляжешь после полуночи» или «повязка промокла».</summary>
    private static string FormatFailureRisk(string failureText)
    {
        string text = failureText.Trim().TrimEnd('.');
        return text.StartsWith("если ", StringComparison.OrdinalIgnoreCase)
            ? $"день лечения сорвётся, {text}."
            : $"день лечения сорвётся — {PlayerGrammar.LowerFirst(text)}.";
    }

    private static string FormatRemaining(HarveyCareDirective d)
    {
        if (d.Goal <= 0 || d.Current >= d.Goal)
            return "";

        int left = d.Goal - Math.Max(0, d.Current);
        return string.IsNullOrWhiteSpace(d.Unit) ? left.ToString() : $"{left} {d.Unit}";
    }

    /// <summary>Следующий шаг от провайдера или общий по типу пункта.</summary>
    private static string ResolveNextStep(HarveyCareDirective d)
    {
        if (!string.IsNullOrWhiteSpace(d.NextStep))
            return d.NextStep;

        if (d.State == HarveyCareDirectiveState.Failed)
            return "поговори с Харви — он скорректирует план.";

        return d.Type switch
        {
            HarveyCareDirectiveType.Appointment =>
                "найди Харви и поговори с ним (днём он обычно в клинике).",
            HarveyCareDirectiveType.ImmediateAction when d.Goal > 0 =>
                "когда шкала заполнится, пункт отметится сам.",
            _ => "",
        };
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

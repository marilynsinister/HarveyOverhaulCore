using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Services;

/// <summary>Presentation only: all completion and failure states come from the providers.</summary>
public static class HarveyPlanDashboardBuilder
{
    public static HarveyPlanDashboardViewModel Build(HarveyPlanSnapshot snapshot)
    {
        var directives = snapshot.AllDirectives
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()).ToList();
        var actions = directives.Where(d => IsAction(d) && d.State != HarveyCareDirectiveState.Info).ToList();
        int done = actions.Count(d => d.State == HarveyCareDirectiveState.Done);
        int remaining = actions.Count - done;
        int rules = directives.Count(d => IsRule(d) && d.State is HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning);
        bool failed = directives.Any(d => d.State == HarveyCareDirectiveState.Failed);
        bool failedDay = directives.Any(IsTreatmentDayFailure);
        bool urgent = directives.Any(d => IsOpen(d) && d.Priority == HarveyCareDirectivePriority.Critical);

        var primary = snapshot.PrimaryAction is { } p && IsOpen(p) && IsAction(p) ? p
            : actions.Where(IsOpen).OrderBy(d => HarveyCareDirectivePriority.Rank(d.Priority)).FirstOrDefault();
        var main = primary ?? directives.FirstOrDefault(d => IsOpen(d) && d.Priority == HarveyCareDirectivePriority.Critical);

        string status = failed ? "Есть нарушение" : urgent ? "Нужно внимание сейчас"
            : primary?.Type == HarveyCareDirectiveType.Appointment ? "Харви ждёт разговора"
            : remaining > 0 ? "Есть задачи на сегодня"
            : rules > 0 ? "Продолжай беречь себя"
            : snapshot.HasAnyContent || snapshot.RawFactsCount > 0 ? "Харви наблюдает" : "Сегодня всё спокойно";
        string color = failed || urgent ? HarveyPlanColors.Danger
            : remaining > 0 ? HarveyPlanColors.Muted : HarveyPlanColors.Good;

        var metrics = new List<HarveyPlanMetricViewModel>();
        if (actions.Count > 0 || rules > 0)
        {
            metrics.Add(new()
            {
                Value = actions.Count > 0 ? $"{done} / {actions.Count}" : "0",
                Label = "Сделано",
                Illustration = "kit",
                Color = HarveyPlanColors.Good,
                Tooltip = "Только разовые действия и визиты к Харви. Правила режима считаются отдельно.",
            });
            metrics.Add(new()
            {
                Value = remaining.ToString(),
                Label = "Ещё сделать",
                Illustration = "bag",
                Color = remaining > 0 ? HarveyPlanColors.Text : HarveyPlanColors.Good,
                Tooltip = "Незавершённые действия. Игра отмечает результат автоматически — нажимать галочки не нужно.",
            });
            metrics.Add(new()
            {
                Value = rules.ToString(),
                Label = "Правил до сна",
                Illustration = "lantern",
                Tooltip = "Это режим и ограничения, а не разовые задания. Соблюдай их до конца дня.",
            });
        }

        return new()
        {
            StatusText = status,
            StatusColor = color,
            MainActionTitle = main?.Title.Trim().TrimEnd('.')
                ?? (rules > 0 ? "Соблюдай режим до сна" : "Срочных действий нет"),
            MainActionBody = main?.Text?.Trim() ?? (rules > 0
                ? "Правила и ограничения — ниже. Они действуют весь день."
                : "Если состояние изменится, здесь появится новое назначение."),
            NoticeText = failedDay
                ? "Есть нарушение, из-за которого день лечения не засчитается. Ниже — причина и следующий шаг."
                : failed ? "Один из пунктов нарушен. Посмотри причину и следующий шаг ниже."
                : "",
            ProgressText = actions.Count > 0 ? $"Разовые задачи: {done} из {actions.Count}" : "",
            ProgressFraction = actions.Count > 0 ? (float)done / actions.Count : 0,
            Metrics = metrics,
        };
    }

    public static bool IsTreatmentDayFailure(HarveyCareDirective directive) =>
        directive.State == HarveyCareDirectiveState.Failed
        && (directive.CanFailDay || directive.Type == HarveyCareDirectiveType.FailureReason);

    private static bool IsAction(HarveyCareDirective d) => d.Type is
        HarveyCareDirectiveType.ImmediateAction or HarveyCareDirectiveType.Appointment;
    private static bool IsRule(HarveyCareDirective d) => d.Type is
        HarveyCareDirectiveType.TodayRule or HarveyCareDirectiveType.Avoid;
    private static bool IsOpen(HarveyCareDirective d) => d.State is
        HarveyCareDirectiveState.Active or HarveyCareDirectiveState.Warning;
}

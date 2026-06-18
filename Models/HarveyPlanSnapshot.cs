namespace HarveyOverhaul.Core.Models;

/// <summary>Снимок «Плана Харви» для UI и debug.</summary>
public sealed class HarveyPlanSnapshot
{
    public string Title { get; set; } = "План Харви";
    public string Tone { get; set; } = HarveyCareDirectiveTone.Calm;
    public string ToneText { get; set; } = "";
    public string HarveyAdvice { get; set; } = "";

    public HarveyCareDirective? PrimaryAction { get; set; }

    public List<HarveyCareDirective> ImmediateActions { get; set; } = new();
    public List<HarveyCareDirective> TodayRules { get; set; } = new();
    public List<HarveyCareDirective> AvoidRules { get; set; } = new();
    public List<HarveyCareDirective> Warnings { get; set; } = new();
    public List<HarveyCareDirective> FailureReasons { get; set; } = new();
    public List<HarveyCareDirective> MedicalNotes { get; set; } = new();
    public List<HarveyCareDirective> StressNotes { get; set; } = new();

    public List<HarveyCareRawFact> InjuryRawFacts { get; set; } = new();
    public List<HarveyCareRawFact> StressRawFacts { get; set; } = new();

    public int InjuryDirectiveCount { get; set; }
    public int StressDirectiveCount { get; set; }
    public bool InjuryProviderRegistered { get; set; }
    public bool StressProviderRegistered { get; set; }
    public IReadOnlyList<HarveyCareDirective> AllDirectives { get; set; } = Array.Empty<HarveyCareDirective>();
    public string FallbackReason { get; set; } = HarveyPlanFallbackReason.None;

    public int RawFactsCount => InjuryRawFacts.Count + StressRawFacts.Count;

    public IEnumerable<HarveyCareRawFact> AllRawFacts
        => InjuryRawFacts.Concat(StressRawFacts);

    public bool HasAnyContent =>
        PrimaryAction != null
        || ImmediateActions.Count > 0
        || TodayRules.Count > 0
        || AvoidRules.Count > 0
        || Warnings.Count > 0
        || FailureReasons.Count > 0
        || MedicalNotes.Count > 0
        || StressNotes.Count > 0
        || AllDirectives.Any(d =>
            d.Type is HarveyCareDirectiveType.ImmediateAction
                or HarveyCareDirectiveType.TodayRule
                or HarveyCareDirectiveType.Avoid
                or HarveyCareDirectiveType.Warning
                or HarveyCareDirectiveType.FailureReason
                or HarveyCareDirectiveType.Appointment);
}

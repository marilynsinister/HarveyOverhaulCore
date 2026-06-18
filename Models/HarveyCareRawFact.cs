namespace HarveyOverhaul.Core.Models;

/// <summary>Сырой факт от Injury/Stress до advisor-слоя Core.</summary>
public sealed class HarveyCareRawFact
{
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    public string Kind { get; set; } = "";
    public string State { get; set; } = HarveyCareDirectiveState.Active;
    public string Label { get; set; } = "";
    public string Details { get; set; } = "";
    public int Current { get; set; }
    public int Goal { get; set; }
    public string Unit { get; set; } = "";
    public string Priority { get; set; } = HarveyCareDirectivePriority.Normal;

    public static HarveyCareRawFact FromDirective(HarveyCareDirective directive)
        => new()
        {
            Id = directive.Id,
            Source = directive.Source,
            Kind = directive.Type,
            State = directive.State,
            Label = string.IsNullOrWhiteSpace(directive.Title) ? directive.Id : directive.Title,
            Details = string.IsNullOrWhiteSpace(directive.Text) ? directive.FailureText : directive.Text,
            Current = directive.Current,
            Goal = directive.Goal,
            Unit = directive.Unit,
            Priority = directive.Priority,
        };
}

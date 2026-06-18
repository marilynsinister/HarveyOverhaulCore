namespace HarveyOverhaul.Core.Models;

/// <summary>Одно указание Харви для игрока (факт → человеческий текст).</summary>
public sealed class HarveyCareDirective
{
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";

    public int Current { get; set; }
    public int Goal { get; set; }
    public string Unit { get; set; } = "";

    public string Priority { get; set; } = HarveyCareDirectivePriority.Normal;
    public string State { get; set; } = HarveyCareDirectiveState.Active;
    public bool CanFailDay { get; set; }
    public string FailureText { get; set; } = "";

    public string HarveyTone { get; set; } = HarveyCareDirectiveTone.Calm;
    public string HarveyAdvice { get; set; } = "";
}

public static class HarveyCareDirectiveSource
{
    public const string Injury = "Injury";
    public const string Stress = "Stress";
    public const string Mixed = "Mixed";
}

public static class HarveyCareDirectiveType
{
    public const string ImmediateAction = "ImmediateAction";
    public const string TodayRule = "TodayRule";
    public const string Avoid = "Avoid";
    public const string Warning = "Warning";
    public const string Appointment = "Appointment";
    public const string Advice = "Advice";
    public const string FailureReason = "FailureReason";
}

public static class HarveyCareDirectivePriority
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Critical = "Critical";

    public static int Rank(string priority) => priority switch
    {
        Critical => 0,
        High => 1,
        Normal => 2,
        Low => 3,
        _ => 2,
    };
}

public static class HarveyCareDirectiveState
{
    public const string Active = "Active";
    public const string Done = "Done";
    public const string Warning = "Warning";
    public const string Failed = "Failed";
    public const string Info = "Info";
}

public static class HarveyCareDirectiveTone
{
    public const string Soft = "Soft";
    public const string Calm = "Calm";
    public const string Worried = "Worried";
    public const string Strict = "Strict";
    public const string Tender = "Tender";
}

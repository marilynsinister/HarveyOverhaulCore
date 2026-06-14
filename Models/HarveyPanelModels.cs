namespace HarveyOverhaul.Core.Models;

public enum HarveyPanelTab
{
    Overview,
    Stress,
    Injuries,
    Plan,
    Trust
}

public sealed class HarveyPanelOverviewFields
{
    public string StateLine { get; set; } = "";
    public string AssignmentLine { get; set; } = "";
    public string ProgressLine { get; set; } = "";
    public string AfterLine { get; set; } = "";
    public string StressLine { get; set; } = "";
    public string InjuriesLine { get; set; } = "";
    public string AdviceLine { get; set; } = "";
}

public sealed class HarveyPanelStressFields
{
    public string AssignmentTitle { get; set; } = "";
    public string AssignmentProgress { get; set; } = "";
    public string AssignmentObjective { get; set; } = "";
    public string AssignmentAfter { get; set; } = "";
    public string NoAssignmentLine { get; set; } = "";
    public HandbookViewModel Handbook { get; set; } = new();
}

public sealed class HarveyPanelPlanFields
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class HarveyPanelTrustFields
{
    public string LevelLine { get; set; } = "";
    public string DescriptionLine { get; set; } = "";
    public string PermissionsLine { get; set; } = "";
    public string Placeholder { get; set; } = "";
}

public sealed class HarveyPanelContribution
{
    public string ProviderId { get; set; } = "";
    public List<HarveyPanelSectionDto> OverviewSections { get; set; } = new();
    public List<HarveyPanelSectionDto> StressSections { get; set; } = new();
    public List<HarveyPanelSectionDto> InjurySections { get; set; } = new();
    public List<HarveyPanelSectionDto> PlanSections { get; set; } = new();
    public List<HarveyPanelSectionDto> TrustSections { get; set; } = new();

    public HarveyPanelOverviewFields? OverviewFields { get; set; }
    public HarveyPanelStressFields? StressFields { get; set; }
    public string InjuriesBody { get; set; } = "";
    public HarveyPanelPlanFields? PlanFields { get; set; }
    public HarveyPanelTrustFields? TrustFields { get; set; }

    public bool HasPriorityAppointment { get; set; }
    public bool HasPendingHarveyReview { get; set; }
    public bool HasActiveRecoveryPlan { get; set; }

    public string SummaryText { get; set; } = "";
    public string HarveyAdviceText { get; set; } = "";
}

public sealed class HarveyPanelSectionDto
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "";
    public int Priority { get; set; }
    public HarveyPanelSeverity Severity { get; set; } = HarveyPanelSeverity.Normal;
}

public enum HarveyPanelSeverity
{
    Info,
    Normal,
    Warning,
    Urgent,
    Success
}

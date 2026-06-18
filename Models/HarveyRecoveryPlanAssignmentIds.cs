namespace HarveyOverhaul.Core.Models;

/// <summary>Идентификаторы назначений единого «Плана Харви».</summary>
public static class HarveyRecoveryPlanAssignmentIds
{
    public const string FindSafePlace = "FindSafePlace";
    public const string DontStayAlone = "DontStayAlone";

    public static string TalkHarveyNextPhase(string injuryId)
        => $"TalkHarvey_NextPhase_{injuryId}";

    public static string TalkHarveyRecovery(string injuryId)
        => $"TalkHarvey_Recovery_{injuryId}";

    public static bool IsHarveyTalkAssignment(string assignmentId)
        => assignmentId.StartsWith("TalkHarvey_", System.StringComparison.Ordinal);
}

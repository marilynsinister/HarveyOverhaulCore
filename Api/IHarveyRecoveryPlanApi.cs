namespace HarveyOverhaul.Core.Api;

/// <summary>
/// API единого «Плана Харви» (save-state в Injury mod).
/// Stress/Injury вызывают через ModRegistry.GetApi.
/// </summary>
public interface IHarveyRecoveryPlanApi
{
    bool IsPlanActive();

    void StartPlan(string source, IReadOnlyList<string> assignmentIds, string? planId = null);

    void AddAssignment(string assignmentId, int goal = 0);

    void AddProgress(string assignmentId, int amount);

    void SetProgress(string assignmentId, int current, int goal);

    bool CompleteAssignment(string assignmentId);

    void FailAssignment(string assignmentId, string reason);

    void RegisterWarning(string type, string text);

    void RegisterViolation(string type, string text);

    void RemoveAssignment(string assignmentId);
}

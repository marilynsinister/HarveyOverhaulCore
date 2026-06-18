using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Api;

public interface IHarveyCoreApi
{
    void RegisterPanelProvider(IHarveyPanelProvider provider);
    void UnregisterPanelProvider(string uniqueId);

    void RegisterCareDirectiveProvider(IHarveyCareDirectiveProvider provider);
    void UnregisterCareDirectiveProvider(string providerId);

    void OpenPanel(HarveyPanelTab tab = HarveyPanelTab.Overview);
    void ClosePanel();
    bool IsPanelOpen { get; }

    bool HasPendingHarveyReview();
    bool HasPriorityHarveyInteraction();

    bool ShouldCountTreatmentTime();

    void RegisterMedicalIntents(string providerId, IReadOnlyList<HarveyMedicalIntentRegistration> intents);
    void ClearMedicalIntents(string providerId);

    void RegisterMedicalIntentPublisher(string providerId, Action publishIntents);
    void RegisterMedicalTopicApplier(string providerId, Action<HarveyMedicalIntentResolution> applyTopics);
    void UnregisterMedicalIntentContributor(string providerId);

    HarveyMedicalIntentResolution PrepareHarveyMedicalClick(bool logDetails = false);
    HarveyMedicalIntentResolution ResolveHarveyMedicalIntent(bool logDetails = false);
    HarveyMedicalIntentResolution? GetLastHarveyMedicalIntentResolution();
    bool IsMedicalIntentActive(string providerId, string topicKey, string stateId);
    bool IsMedicalIntentSelectedForProvider(string providerId, string? stateId = null);

    void SetHarveyClickInjurySummary(string summary);
    void SetHarveyClickStressSummary(string summary);
    void SetHarveyClickGate(bool suppressed, string reason);
    void SetHarveyClickTopicShown(string? topicKey);
    void SetHarveyClickActionExecuted(string summary);
    void LogHarveyClickDiagnostics(string phase);

    bool IsFestivalContext();
    HarveyMedicalConversationSettings MedicalConversationSettings { get; }

    HarveyPlanSnapshot BuildHarveyPlanSnapshot();
}

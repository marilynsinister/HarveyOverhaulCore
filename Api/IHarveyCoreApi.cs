using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Api;

public interface IHarveyCoreApi
{
    void RegisterPanelProvider(IHarveyPanelProvider provider);
    void UnregisterPanelProvider(string uniqueId);

    void RegisterCareDirectiveProvider(IHarveyCareDirectiveProvider provider);
    void UnregisterCareDirectiveProvider(string providerId);

    /// <summary>Сырые факты конкретного провайдера (Injury/Stress читают состояние друг друга только через Core).</summary>
    IReadOnlyList<HarveyCareRawFact> GetCareFacts(string providerId);

    /// <summary>Есть ли у провайдера активные (не Done/Info) факты ухода.</summary>
    bool HasActiveCareState(string providerId);

    /// <summary>Единый RecoveryPlan (save-state владеет Injury). Регистрирует владелец, остальные получают через Core.</summary>
    void RegisterRecoveryPlanApi(IHarveyRecoveryPlanApi api);
    IHarveyRecoveryPlanApi? GetRecoveryPlanApi();

    /// <summary>Состояния стресса (владеет Stress). Регистрирует Stress, остальные получают через Core.</summary>
    void RegisterStressStateApi(IHarveyStressStateApi api);
    IHarveyStressStateApi? GetStressStateApi();

    /// <summary>Единая шкала доверия (владеет Stress). Без Stress — null, Injury работает на своей шкале.</summary>
    void RegisterCareTrustApi(IHarveyCareTrustApi api);
    IHarveyCareTrustApi? GetCareTrustApi();

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

    /// <summary>
    /// Общий режим сложности (<see cref="HarveyDifficulty"/>): Cozy / Standard / Strict.
    /// null — «PerMod»: каждая часть мода живёт по своим настройкам.
    /// </summary>
    string? DifficultyOverride => null;

    /// <summary>Ступень 4 лестницы последствий: Харви сам приходит на ферму, если лечение запущено.</summary>
    bool HouseCallsEnabled => true;

    /// <summary>Ступень 5: обморок/срыв после повторного отказа. На «Уютно» всегда false.</summary>
    bool NeglectCollapseEnabled => true;

    /// <summary>
    /// Занять единственный за день слот визита Харви на ферму (Injury и Stress делят его).
    /// false — сегодня уже приходит другой провайдер.
    /// </summary>
    bool TryClaimHouseCall(string providerId) => true;

    /// <summary>Визит не состоялся — освободить слот, чтобы его мог занять другой провайдер.</summary>
    void ReleaseHouseCall(string providerId) { }
}

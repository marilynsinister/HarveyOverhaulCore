using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using HarveyOverhaul.Core.UI;

namespace HarveyOverhaul.Core.Api;

public sealed class HarveyCoreApi : IHarveyCoreApi
{
    private readonly HarveyProviderRegistry _registry;
    private readonly HarveyCareDirectiveRegistry _directiveRegistry;
    private readonly GameStateGuard _gameStateGuard;
    private readonly HarveyPanelMenu _panelMenu;
    private readonly HarveyPanelService _panelService;
    private readonly HarveyMedicalIntentArbitrator _medicalIntentArbitrator;
    private readonly HarveyMedicalClickDiagnostics _clickDiagnostics;

    public HarveyCoreApi(
        HarveyProviderRegistry registry,
        HarveyCareDirectiveRegistry directiveRegistry,
        GameStateGuard gameStateGuard,
        HarveyPanelMenu panelMenu,
        HarveyPanelService panelService,
        HarveyMedicalIntentArbitrator medicalIntentArbitrator,
        HarveyMedicalClickDiagnostics clickDiagnostics)
    {
        _registry = registry;
        _directiveRegistry = directiveRegistry;
        _gameStateGuard = gameStateGuard;
        _panelMenu = panelMenu;
        _panelService = panelService;
        _medicalIntentArbitrator = medicalIntentArbitrator;
        _clickDiagnostics = clickDiagnostics;
    }

    public void RegisterPanelProvider(IHarveyPanelProvider provider)
        => _registry.Register(provider);

    public void UnregisterPanelProvider(string uniqueId)
        => _registry.Unregister(uniqueId);

    public void RegisterCareDirectiveProvider(IHarveyCareDirectiveProvider provider)
        => _directiveRegistry.Register(provider);

    public void UnregisterCareDirectiveProvider(string providerId)
        => _directiveRegistry.Unregister(providerId);

    public void OpenPanel(HarveyPanelTab tab = HarveyPanelTab.Overview)
        => _panelMenu.OpenToTab(_panelService, tab);

    public void ClosePanel()
        => _panelMenu.Close();

    public bool IsPanelOpen
        => _panelMenu.IsOpen;

    public bool HasPendingHarveyReview()
        => _panelService.HasPendingHarveyReview();

    public bool HasPriorityHarveyInteraction()
        => _panelService.HasPriorityHarveyInteraction();

    public bool ShouldCountTreatmentTime()
        => _gameStateGuard.ShouldCountTreatmentTime();

    public void RegisterMedicalIntents(string providerId, IReadOnlyList<HarveyMedicalIntentRegistration> intents)
        => _medicalIntentArbitrator.RegisterMedicalIntents(providerId, intents);

    public void ClearMedicalIntents(string providerId)
        => _medicalIntentArbitrator.ClearMedicalIntents(providerId);

    public void RegisterMedicalIntentPublisher(string providerId, Action publishIntents)
        => _medicalIntentArbitrator.RegisterIntentPublisher(providerId, publishIntents);

    public void RegisterMedicalTopicApplier(string providerId, Action<HarveyMedicalIntentResolution> applyTopics)
        => _medicalIntentArbitrator.RegisterTopicApplier(providerId, applyTopics);

    public void UnregisterMedicalIntentContributor(string providerId)
        => _medicalIntentArbitrator.UnregisterIntentContributor(providerId);

    public HarveyMedicalIntentResolution PrepareHarveyMedicalClick(bool logDetails = false)
    {
        _clickDiagnostics.SetActiveTopicsSummary(HarveyMedicalClickDiagnostics.BuildActiveTopicsSnapshot());
        var resolution = _medicalIntentArbitrator.PrepareHarveyClick(logDetails);
        _clickDiagnostics.SetTopicShown(resolution.Selected?.TopicKey);
        _clickDiagnostics.LogClickSnapshot(resolution, "prepare");
        return resolution;
    }

    public HarveyMedicalIntentResolution ResolveHarveyMedicalIntent(bool logDetails = false)
        => _medicalIntentArbitrator.Resolve(logDetails: logDetails);

    public HarveyMedicalIntentResolution? GetLastHarveyMedicalIntentResolution()
        => _medicalIntentArbitrator.GetLastResolution();

    public bool IsMedicalIntentActive(string providerId, string topicKey, string stateId)
        => _medicalIntentArbitrator.IsMedicalIntentActive(providerId, topicKey, stateId);

    public bool IsMedicalIntentSelectedForProvider(string providerId, string? stateId = null)
        => _medicalIntentArbitrator.IsIntentSelectedForProvider(providerId, stateId);

    public bool IsFestivalContext()
        => HarveyMedicalContextHelper.IsFestivalContext();

    public HarveyMedicalConversationSettings MedicalConversationSettings
        => _medicalIntentArbitrator.Settings;

    public void SetHarveyClickInjurySummary(string summary)
        => _clickDiagnostics.SetInjuryDebuffSummary(summary);

    public void SetHarveyClickStressSummary(string summary)
        => _clickDiagnostics.SetStressStateSummary(summary);

    public void SetHarveyClickGate(bool suppressed, string reason)
        => _clickDiagnostics.SetClickGate(suppressed, reason);

    public void SetHarveyClickTopicShown(string? topicKey)
        => _clickDiagnostics.SetTopicShown(topicKey);

    public void SetHarveyClickActionExecuted(string summary)
        => _clickDiagnostics.SetActionExecuted(summary);

    public void LogHarveyClickDiagnostics(string phase)
        => _clickDiagnostics.LogClickSnapshot(
            _medicalIntentArbitrator.GetLastResolution(),
            phase);

    public HarveyPlanSnapshot BuildHarveyPlanSnapshot()
        => _panelService.BuildHarveyPlanSnapshot();
}

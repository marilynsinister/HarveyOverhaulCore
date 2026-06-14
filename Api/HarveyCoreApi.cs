using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using HarveyOverhaul.Core.UI;

namespace HarveyOverhaul.Core.Api;

public sealed class HarveyCoreApi : IHarveyCoreApi
{
    private readonly HarveyProviderRegistry _registry;
    private readonly GameStateGuard _gameStateGuard;
    private readonly HarveyPanelMenu _panelMenu;
    private readonly HarveyPanelService _panelService;

    public HarveyCoreApi(
        HarveyProviderRegistry registry,
        GameStateGuard gameStateGuard,
        HarveyPanelMenu panelMenu,
        HarveyPanelService panelService)
    {
        _registry = registry;
        _gameStateGuard = gameStateGuard;
        _panelMenu = panelMenu;
        _panelService = panelService;
    }

    public void RegisterPanelProvider(IHarveyPanelProvider provider)
        => _registry.Register(provider);

    public void UnregisterPanelProvider(string uniqueId)
        => _registry.Unregister(uniqueId);

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
}

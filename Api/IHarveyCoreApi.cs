using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Api;

public interface IHarveyCoreApi
{
    void RegisterPanelProvider(IHarveyPanelProvider provider);
    void UnregisterPanelProvider(string uniqueId);

    void OpenPanel(HarveyPanelTab tab = HarveyPanelTab.Overview);
    void ClosePanel();
    bool IsPanelOpen { get; }

    bool HasPendingHarveyReview();
    bool HasPriorityHarveyInteraction();

    bool ShouldCountTreatmentTime();
}

using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Api;

public interface IHarveyPanelProvider
{
    string UniqueId { get; }
    string DisplayName { get; }
    int Priority { get; }

    HarveyPanelContribution GetPanelContribution();
}

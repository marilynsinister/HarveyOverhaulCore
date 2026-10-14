using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Api;

public interface IHarveyPanelProvider
{
    string UniqueId { get; }
    string DisplayName { get; }
    int Priority { get; }

    HarveyPanelContribution GetPanelContribution();
}

/// <summary>
/// Провайдер, у секций которого есть кнопки (<see cref="HarveyPanelSectionDto.ActionId"/>).
/// После нажатия Core пересобирает окно на той же вкладке.
/// </summary>
public interface IHarveyPanelActionProvider : IHarveyPanelProvider
{
    /// <summary>Обработать нажатие. true — состояние изменилось, окно нужно обновить.</summary>
    bool HandlePanelAction(string actionId);
}

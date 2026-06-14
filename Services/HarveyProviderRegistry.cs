using HarveyOverhaul.Core.Api;
using HarveyOverhaul.Core.Models;
using StardewModdingAPI;

namespace HarveyOverhaul.Core.Services;

public sealed class HarveyProviderRegistry
{
    public const string StressProviderId = "marilynsinister.HarveyStressMeter";
    public const string InjuryProviderId = "marilynsinister.HarveyOverhaul.Injury";

    private readonly IMonitor _monitor;
    private readonly List<IHarveyPanelProvider> _providers = new();
    private readonly HashSet<string> _failedProviderIds = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public HarveyProviderRegistry(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void Register(IHarveyPanelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_lock)
        {
            _providers.RemoveAll(p => string.Equals(p.UniqueId, provider.UniqueId, StringComparison.Ordinal));
            _providers.Add(provider);
        }
    }

    public void Unregister(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            return;

        lock (_lock)
        {
            _providers.RemoveAll(p => string.Equals(p.UniqueId, uniqueId, StringComparison.Ordinal));
        }
    }

    public IReadOnlyList<IHarveyPanelProvider> GetProviders()
    {
        lock (_lock)
        {
            return _providers
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.UniqueId, StringComparer.Ordinal)
                .ToList();
        }
    }

    public bool IsRegistered(string uniqueId)
    {
        lock (_lock)
        {
            return _providers.Any(p => string.Equals(p.UniqueId, uniqueId, StringComparison.Ordinal));
        }
    }

    public bool DidProviderFail(string uniqueId)
    {
        lock (_lock)
        {
            return _failedProviderIds.Contains(uniqueId);
        }
    }

    public IReadOnlyList<HarveyPanelContribution> CollectContributions()
    {
        var contributions = new List<HarveyPanelContribution>();

        lock (_lock)
        {
            _failedProviderIds.Clear();
        }

        foreach (var provider in GetProviders())
        {
            try
            {
                var contribution = provider.GetPanelContribution();
                if (contribution != null)
                    contributions.Add(contribution);
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _failedProviderIds.Add(provider.UniqueId);
                }

                _monitor.Log(
                    $"Panel provider '{provider.UniqueId}' ({provider.DisplayName}) threw an exception and was skipped: {ex}",
                    LogLevel.Error);
            }
        }

        return contributions;
    }
}

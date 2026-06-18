using HarveyOverhaul.Core.Api;
using HarveyOverhaul.Core.Models;
using StardewModdingAPI;

namespace HarveyOverhaul.Core.Services;

public sealed class HarveyCareDirectiveRegistry
{
    private readonly IMonitor _monitor;
    private readonly List<IHarveyCareDirectiveProvider> _providers = new();
    private readonly object _lock = new();

    public HarveyCareDirectiveRegistry(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void Register(IHarveyCareDirectiveProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_lock)
        {
            _providers.RemoveAll(p => string.Equals(p.ProviderId, provider.ProviderId, StringComparison.Ordinal));
            _providers.Add(provider);
        }
    }

    public void Unregister(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        lock (_lock)
        {
            _providers.RemoveAll(p => string.Equals(p.ProviderId, providerId, StringComparison.Ordinal));
        }
    }

    public IReadOnlyList<IHarveyCareDirectiveProvider> GetProviders()
    {
        lock (_lock)
        {
            return _providers.ToList();
        }
    }

    public bool IsRegistered(string providerId)
    {
        lock (_lock)
        {
            return _providers.Any(p => string.Equals(p.ProviderId, providerId, StringComparison.Ordinal));
        }
    }

    public IReadOnlyList<string> GetRegisteredProviderIds()
        => GetProviders().Select(p => p.ProviderId).ToList();

    public (List<HarveyCareDirective> Injury, List<HarveyCareDirective> Stress) CollectDirectives()
    {
        var injury = new List<HarveyCareDirective>();
        var stress = new List<HarveyCareDirective>();

        foreach (var provider in GetProviders())
        {
            try
            {
                var batch = provider.GetCareDirectives();
                if (batch == null || batch.Count == 0)
                    continue;

                if (string.Equals(provider.ProviderId, HarveyProviderRegistry.InjuryProviderId, StringComparison.Ordinal))
                    injury.AddRange(batch);
                else if (string.Equals(provider.ProviderId, HarveyProviderRegistry.StressProviderId, StringComparison.Ordinal))
                    stress.AddRange(batch);
                else
                {
                    foreach (var d in batch)
                    {
                        if (d.Source == HarveyCareDirectiveSource.Stress)
                            stress.Add(d);
                        else
                            injury.Add(d);
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    $"Care directive provider '{provider.ProviderId}' threw: {ex}",
                    LogLevel.Error);
            }
        }

        return (injury, stress);
    }

    public (List<HarveyCareRawFact> Injury, List<HarveyCareRawFact> Stress) CollectRawFacts()
    {
        var injury = new List<HarveyCareRawFact>();
        var stress = new List<HarveyCareRawFact>();

        foreach (var provider in GetProviders())
        {
            try
            {
                var batch = provider.GetRawCareFacts();
                if (batch == null || batch.Count == 0)
                    continue;

                if (string.Equals(provider.ProviderId, HarveyProviderRegistry.InjuryProviderId, StringComparison.Ordinal))
                    injury.AddRange(batch);
                else if (string.Equals(provider.ProviderId, HarveyProviderRegistry.StressProviderId, StringComparison.Ordinal))
                    stress.AddRange(batch);
                else
                {
                    foreach (var fact in batch)
                    {
                        if (fact.Source == HarveyCareDirectiveSource.Stress)
                            stress.Add(fact);
                        else
                            injury.Add(fact);
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    $"Raw fact provider '{provider.ProviderId}' threw: {ex}",
                    LogLevel.Error);
            }
        }

        return (injury, stress);
    }
}

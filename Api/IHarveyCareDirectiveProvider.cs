using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.Api;

/// <summary>
/// Отдаёт факты/состояния для «Плана Харви» (контракт IHarveyPlanStateProvider).
/// Injury и Stress регистрируются в Core через RegisterCareDirectiveProvider.
/// Core превращает HarveyCareDirective в human-readable секции плана.
/// </summary>
public interface IHarveyCareDirectiveProvider
{
    string ProviderId { get; }
    IReadOnlyList<HarveyCareDirective> GetCareDirectives();

    /// <summary>Сырые факты до advisor-слоя. По умолчанию — projection из directives.</summary>
    IReadOnlyList<HarveyCareRawFact> GetRawCareFacts()
        => GetCareDirectives().Select(HarveyCareRawFact.FromDirective).ToList();
}

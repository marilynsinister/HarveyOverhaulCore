namespace HarveyOverhaul.Core.Models;

/// <summary>Причина показа мягкого fallback-совета вместо активного плана.</summary>
public static class HarveyPlanFallbackReason
{
    public const string None = "";
    public const string NoProviders = "NoProviders";
    public const string ProvidersReturnedZeroFacts = "ProvidersReturnedZeroFacts";
    public const string StateMismatch = "StateMismatch";
    public const string MappingFailed = "MappingFailed";
    public const string LegacyQuestMode = "LegacyQuestMode";
}

namespace HarveyOverhaul.Core.Models;

/// <summary>
/// «Границы» — что фермерша разрешила Харви (владелец ответов — Stress). Ключи совпадают с HarveyCareConsents в Stress.
/// </summary>
public static class HarveyCareConsentIds
{
    /// <summary>Заходить или спрашивать вечером, как она (в том числе короткий ночной обход в Injury).</summary>
    public const string EveningChecks = "EveningChecks";

    /// <summary>Брать за руку, когда ей плохо в грозу.</summary>
    public const string HoldHand = "HoldHand";

    /// <summary>Искать её, если она уйдёт в грозу.</summary>
    public const string SearchInStorm = "SearchInStorm";
}

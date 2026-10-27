using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// «Харви приходит сам» (ступень 4 лестницы последствий): не больше одного визита в день на весь мод.
/// Injury и Stress ведут свои лестницы, но слот визита берут здесь: кто первый занял — тот и приходит,
/// второй ждёт завтрашнего утра. Injury проверяет свою лестницу раньше, поэтому травма идёт первой.
/// Слот живёт в памяти: после перезагрузки день начинается заново, и визит тоже.
/// </summary>
public sealed class HarveyHouseCallCoordinator
{
    private int _claimedDay = -1;
    private string? _claimedBy;

    /// <summary>Занять слот визита на сегодня. true — слот свободен или уже принадлежит этому провайдеру.</summary>
    public bool TryClaim(string providerId)
    {
        int today = (int)Game1.stats.DaysPlayed;
        if (_claimedDay == today)
            return string.Equals(_claimedBy, providerId, StringComparison.OrdinalIgnoreCase);

        _claimedDay = today;
        _claimedBy = providerId;
        return true;
    }

    /// <summary>Визит не состоялся (событие не стартовало) — отдать слот другому провайдеру.</summary>
    public void Release(string providerId)
    {
        if (string.Equals(_claimedBy, providerId, StringComparison.OrdinalIgnoreCase))
        {
            _claimedDay = -1;
            _claimedBy = null;
        }
    }

    public bool IsClaimedToday() => _claimedDay == (int)Game1.stats.DaysPlayed;

    public string? ClaimedByToday() => IsClaimedToday() ? _claimedBy : null;
}

using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Годовщина свадьбы с Харви для CP-событий: в ванили нет GameStateQuery на «дней в браке».
/// В день годовщины ставит флаг <see cref="AnniversaryFlag"/> и снимает событие годовщины
/// из просмотренных, чтобы оно повторялось каждый год. В остальные дни флаг снимается.
/// </summary>
public sealed class HarveyMarriageMilestones
{
    public const string AnniversaryFlag = "HarveyMarriage_AnniversaryToday";
    public const string AnniversaryEventId = "HarveyOverhaulMarriage.M3_Anniversary";
    private const int DaysPerYear = 112;

    private readonly IMonitor _monitor;

    public HarveyMarriageMilestones(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void OnDayStarted()
    {
        var player = Game1.player;
        bool anniversary = IsAnniversaryToday(player, out int daysMarried);

        if (!anniversary)
        {
            player.mailReceived.Remove(AnniversaryFlag);
            return;
        }

        player.mailReceived.Add(AnniversaryFlag);
        player.eventsSeen.Remove(AnniversaryEventId);
        _monitor.Log($"[Marriage] Годовщина с Харви: {daysMarried / DaysPerYear} г.", LogLevel.Debug);
    }

    private static bool IsAnniversaryToday(Farmer player, out int daysMarried)
    {
        daysMarried = 0;
        if (!player.friendshipData.TryGetValue("Harvey", out var friendship) || !friendship.IsMarried())
            return false;

        daysMarried = friendship.DaysMarried;
        return daysMarried > 0 && daysMarried % DaysPerYear == 0;
    }
}

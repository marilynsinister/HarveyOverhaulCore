using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Годовщина свадьбы с Харви для CP-событий: в ванили нет GameStateQuery на «дней в браке».
/// В день годовщины ставит флаг <see cref="AnniversaryFlag"/> и выбирает сцену по году брака.
/// Событие выбранного этапа возвращается один раз за день; остальные дни снимают временные флаги.
/// Там же — флаги помолвки, кануна свадьбы, первого утра и двух лет брака.
/// </summary>
public sealed class HarveyMarriageMilestones
{
    public const string AnniversaryFlag = "HarveyMarriage_AnniversaryToday";
    public const string AnniversaryEventId = "HarveyOverhaulMarriage.M3_Anniversary";
    public const string SecondFlag = "HarveyMarriage_AnniversarySecond";
    public const string LaterFlag = "HarveyMarriage_AnniversaryLater";
    public const string SecondEventId = "HarveyOverhaulAnniversary.A2_Kitchen";
    public const string LaterEventId = "HarveyOverhaulAnniversary.A3_Together";
    public const string EngagedFlag = "HarveyRomance_Engaged";
    public const string WeddingEveFlag = "HarveyRomance_WeddingEve";
    public const string FirstMorningFlag = "HarveyRomance_FirstMorning";
    public const string TwoYearsFlag = "HarveyRomance_TwoYears";
    private const string ResetDayKey = "marilynsinister.HarveyOverhaul.Core/AnniversaryResetDay";
    private const int DaysPerYear = 112;

    private readonly IMonitor _monitor;

    public HarveyMarriageMilestones(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void OnDayStarted()
    {
        var player = Game1.player;
        UpdateRomanceFlags(player);
        bool anniversary = IsAnniversaryToday(player, out int daysMarried);
        player.mailReceived.Remove(SecondFlag);
        player.mailReceived.Remove(LaterFlag);

        if (!anniversary)
        {
            player.mailReceived.Remove(AnniversaryFlag);
            return;
        }

        player.mailReceived.Add(AnniversaryFlag);
        int stage = AnniversaryStage(daysMarried);
        if (stage == 2)
            player.mailReceived.Add(SecondFlag);
        else if (stage == 3)
            player.mailReceived.Add(LaterFlag);

        // SaveLoaded тоже обновляет флаги. Повторная загрузка того же утра не должна возвращать уже сыгранную сцену.
        string today = Game1.stats.DaysPlayed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (player.modData.TryGetValue(ResetDayKey, out string? resetDay) && resetDay == today)
            return;
        player.modData[ResetDayKey] = today;
        player.eventsSeen.Remove(stage == 1 ? AnniversaryEventId : stage == 2 ? SecondEventId : LaterEventId);
        _monitor.Log($"[Marriage] Годовщина с Харви: {daysMarried / DaysPerYear} г.", LogLevel.Debug);
    }

    /// <summary>
    /// Флаги помолвки и брака для сцен P1–P4 и W4 (docs/romance-proposal.md): в ванили нет GameStateQuery
    /// на «завтра свадьба» и «дней в браке». Состояние пересчитывается каждое утро, лишние флаги снимаются.
    /// </summary>
    private static void UpdateRomanceFlags(Farmer player)
    {
        player.friendshipData.TryGetValue("Harvey", out var friendship);
        bool engaged = friendship?.IsEngaged() == true;
        bool married = friendship?.IsMarried() == true && !friendship.IsRoommate();
        int daysToWedding = engaged && friendship!.WeddingDate != null
            ? friendship.WeddingDate.TotalDays - Game1.Date.TotalDays
            : -1;
        int daysMarried = married ? friendship!.DaysMarried : -1;

        SetFlag(player, EngagedFlag, engaged);
        SetFlag(player, WeddingEveFlag, IsWeddingEve(daysToWedding));
        SetFlag(player, FirstMorningFlag, IsFirstMorning(daysMarried));
        SetFlag(player, TwoYearsFlag, daysMarried >= 2 * DaysPerYear);
    }

    /// <summary>Свадьба завтра.</summary>
    public static bool IsWeddingEve(int daysToWedding) => daysToWedding == 1;

    /// <summary>
    /// Первое утро в браке. В день свадьбы DaysMarried = 0, но утро занято церемонией, поэтому
    /// флаг ставится на следующий день, когда DaysMarried становится 1.
    /// </summary>
    public static bool IsFirstMorning(int daysMarried) => daysMarried == 1;

    private static void SetFlag(Farmer player, string flag, bool on)
    {
        if (on)
            player.mailReceived.Add(flag);
        else
            player.mailReceived.Remove(flag);
    }

    /// <summary>Первый год, второй год, третий и следующие; 0 — сегодня не годовщина.</summary>
    public static int AnniversaryStage(int daysMarried)
        => daysMarried <= 0 || daysMarried % DaysPerYear != 0 ? 0 : Math.Min(daysMarried / DaysPerYear, 3);

    private static bool IsAnniversaryToday(Farmer player, out int daysMarried)
    {
        daysMarried = 0;
        if (!player.friendshipData.TryGetValue("Harvey", out var friendship) || !friendship.IsMarried())
            return false;

        daysMarried = friendship.DaysMarried;
        return daysMarried > 0 && daysMarried % DaysPerYear == 0;
    }
}

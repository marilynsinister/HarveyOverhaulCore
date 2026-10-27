using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Рация по вечерам (линия «Позывной», docs/romance-proposal.md §5.1). После C1 у фермерши рация Харви.
/// Если в 22:00 она не дома и не рядом с ним — короткий вызов в эфире с её позывным (C2).
/// • не чаще раза за вечер; если в C1 она сказала «мне не нужна охрана» — раз в три дня;
/// • на «Уютно» — через раз;
/// • в браке ждёт, пока не сыграна сцена M7 «Ждал ночью», чтобы не перебить её.
/// </summary>
public sealed class HarveyRadioService(IModHelper helper, IMonitor monitor, ModConfig config)
{
    public const string RadioFlag = "HarveyRomance_Radio";
    public const string DeclinedFlag = "HarveyRomance_RadioDeclined";
    public const string LateReturnEvent = "HarveyOverhaulMarriage.M7_LateReturn";
    public const int CallTime = 2200;
    private const string LastCallKey = "marilynsinister.HarveyOverhaul.Core/RadioLastCallDay";

    private static readonly string[] Callsigns = { "Field", "Daisy", "Sun" };

    private int _checkedDay = -1;

    public void Initialize()
    {
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => _checkedDay = -1;
    }

    /// <summary>Минимум дней между вызовами.</summary>
    public static int MinGapDays(bool declined) => declined ? 3 : 1;

    public static bool ShouldCall(bool hasRadio, bool datingOrMarried, bool married, bool sawLateReturn, bool awayFromHome,
        bool withHarvey, int daysSinceLastCall, bool declined, bool cozy, double roll)
    {
        if (!hasRadio || !datingOrMarried || !awayFromHome || withHarvey)
            return false;
        if (married && !sawLateReturn)
            return false;
        if (daysSinceLastCall < MinGapDays(declined))
            return false;
        return !cozy || roll < 0.5;
    }

    /// <summary>Текст вызова. callsign — Field/Daisy/Sun или null, variant — любой индекс (берётся по модулю).</summary>
    public static string Message(string? callsign, bool married, int variant)
    {
        string name = callsign switch
        {
            "Field" => "Поле-один",
            "Daisy" => "Ромашка",
            "Sun" => "Солнце",
            _ => "Приём",
        };
        string[] lines = married
            ? new[]
            {
                $"Рация: «{name}, это Клиника. Двадцать два ноль-ноль. Чайник горячий. Ты где?»",
                $"Рация: «{name}, приём. Дома тихо и слишком много места. Возвращайся осторожно».",
                $"Рация: «{name}, Клиника на связи. Фонарь на крыльце горит. Это для тебя».",
            }
            : new[]
            {
                $"Рация: «{name}, это Клиника. Двадцать два ноль-ноль. Ты где?»",
                $"Рация: «{name}, приём. Ответишь — или мне идти?»",
                $"Рация: «{name}, Клиника на связи. Просто проверяю эфир. …И тебя».",
            };
        return lines[((variant % lines.Length) + lines.Length) % lines.Length];
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady || e.NewTime < CallTime || Game1.eventUp || Game1.isFestival())
            return;
        int today = (int)Game1.stats.DaysPlayed;
        if (_checkedDay == today)
            return;
        _checkedDay = today;

        try
        {
            var player = Game1.player;
            var location = player.currentLocation;
            if (location == null)
                return;

            player.friendshipData.TryGetValue("Harvey", out var friendship);
            bool married = friendship?.IsMarried() == true;
            bool dating = married || friendship?.IsDating() == true;
            var harvey = Game1.getCharacterFromName("Harvey", mustBeVillager: true);
            int lastCall = player.modData.TryGetValue(LastCallKey, out string? raw) && int.TryParse(raw, out int parsed) ? parsed : -999;

            bool call = ShouldCall(
                hasRadio: player.mailReceived.Contains(RadioFlag),
                datingOrMarried: dating,
                married: married,
                sawLateReturn: player.eventsSeen.Contains(LateReturnEvent),
                awayFromHome: IsAwayFromHome(location),
                withHarvey: harvey?.currentLocation == location,
                daysSinceLastCall: today - lastCall,
                declined: player.mailReceived.Contains(DeclinedFlag),
                cozy: HarveyDifficulty.Normalize(config.Difficulty) == HarveyDifficulty.Cozy,
                roll: Game1.random.NextDouble());
            if (!call)
                return;

            string? callsign = Callsigns.FirstOrDefault(id => player.mailReceived.Contains("HarveyRomance_Callsign_" + id));
            Game1.playSound("phone");
            Game1.addHUDMessage(new HUDMessage(Message(callsign, married, Game1.random.Next(3))));
            player.modData[LastCallKey] = today.ToString();
            monitor.Log($"[Radio] Вызов в эфире: {location.Name}, позывной {callsign ?? "не выбран"}.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            monitor.Log($"[Radio] Вызов не отправлен: {ex.Message}", LogLevel.Warn);
        }
    }

    /// <summary>Не дома: не на ферме, не в её постройках, не там, где она гуляет вместе с Харви (Зузу, праздник у клиники).</summary>
    private static bool IsAwayFromHome(GameLocation location)
    {
        if (location is Farm or FarmHouse || location.IsFarm || location.GetParentLocation() is Farm)
            return false;
        string name = location.NameOrUniqueName ?? "";
        return !name.StartsWith("HarveyZuzu", StringComparison.Ordinal)
               && !name.Equals("HarveyCelebrationRoom", StringComparison.Ordinal);
    }
}

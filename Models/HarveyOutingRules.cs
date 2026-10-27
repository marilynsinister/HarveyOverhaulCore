namespace HarveyOverhaul.Core.Models;

/// <summary>Правила календаря и сохранённой поездки, не зависящие от игрового интерфейса.</summary>
public static class HarveyOutingRules
{
    public static bool IsBirthdayToday(bool happyBirthdayInstalled, string? happyBirthdayToday,
        string fallbackSeason, int fallbackDay, string season, int day)
        => happyBirthdayInstalled
            ? bool.TryParse(happyBirthdayToday, out bool birthday) && birthday
            : fallbackDay is >= 1 and <= 28 && string.Equals(fallbackSeason, season, StringComparison.OrdinalIgnoreCase)
                && day == fallbackDay;

    public static bool ShouldQueueBirthday(bool birthday, bool together, int year, int lastQueuedYear)
        => birthday && together && year != lastQueuedYear;

    public static string NormalizeStyle(string? style)
        => style is "Family" or "Surprise" ? style : "Quiet";

    public static bool CanDepart(bool enabled, bool together, bool saturday, bool festivalWeekend, int time)
        => enabled && together && saturday && !festivalWeekend && time is >= 800 and <= 1300;

    /// <summary>День рождения Харви — 14 зимы, воскресенье: выезд в субботу 13-го увозит его на праздник в Зузу.</summary>
    public static bool IsHarveyBirthdayTrip(string season, int day)
        => string.Equals(season, "winter", StringComparison.OrdinalIgnoreCase) && day == 13;

    public static bool CanResume(int startDay, int today, string phase)
        => today == startDay && phase is "Touring" or "Overnight"
            || today == startDay + 1 && phase is "Overnight" or "Sunday";
}

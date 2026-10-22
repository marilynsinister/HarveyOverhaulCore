using HarveyOverhaul.Core.Models;

internal static class HarveyOutingsChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(HarveyOutingRules.IsBirthdayToday(true, "True", "NotSet", 1, "fall", 12),
            "Happy Birthday's public token must work without a second configured date.");
        check(!HarveyOutingRules.IsBirthdayToday(true, "False", "fall", 12, "fall", 12),
            "An installed Happy Birthday owns the date: the fallback must not create a second birthday.");
        check(!HarveyOutingRules.IsBirthdayToday(true, null, "fall", 12, "fall", 12),
            "Before Happy Birthday is ready or a date is chosen, no birthday may be invented.");
        check(HarveyOutingRules.IsBirthdayToday(false, null, "winter", 28, "winter", 28),
            "A configured fallback works without Happy Birthday, including the last day of the year.");
        check(!HarveyOutingRules.IsBirthdayToday(false, "true", "NotSet", 1, "spring", 1),
            "New saves without a chosen date must not default to spring 1.");
        check(!HarveyOutingRules.IsBirthdayToday(false, null, "spring", 29, "spring", 29),
            "Invalid configured dates must not create a celebration.");
        check(HarveyOutingRules.ShouldQueueBirthday(true, true, 2, 1), "A new year can queue a new invitation.");
        check(!HarveyOutingRules.ShouldQueueBirthday(true, true, 2, 2),
            "Reloading or changing the configured date within one year must not duplicate the celebration.");
        check(!HarveyOutingRules.ShouldQueueBirthday(false, true, 2, 1), "An ordinary day must not queue a birthday.");
        check(!HarveyOutingRules.ShouldQueueBirthday(true, false, 2, 1), "Romantic scenes must require a relationship.");
        foreach (int time in new[] { 800, 900, 1300 })
            check(HarveyOutingRules.CanDepart(true, true, true, false, time), "Departure window includes its endpoints.");
        foreach (int time in new[] { 600, 750, 1310, 2500 })
            check(!HarveyOutingRules.CanDepart(true, true, true, false, time), "Departure outside the window must be rejected.");
        check(!HarveyOutingRules.CanDepart(true, true, true, true, 1000), "A festival on either day reserves the weekend.");
        check(!HarveyOutingRules.CanDepart(true, true, false, false, 1000), "Departure is on Saturday.");
        check(!HarveyOutingRules.CanDepart(false, true, true, false, 1000), "Disabling new trips must work.");
        check(HarveyOutingRules.CanResume(100, 101, "Overnight"), "A saved Saturday night resumes on Sunday.");
        check(HarveyOutingRules.CanResume(100, 101, "Sunday"), "Reloading Sunday preserves the visit.");
        check(!HarveyOutingRules.CanResume(100, 101, "Touring"), "Passing out without booking sleep must not drag the player back to the city.");
        check(!HarveyOutingRules.CanResume(100, 102, "Sunday"), "An expired visit must not keep the player in Zuzu on Monday.");
        check(HarveyOutingRules.NormalizeStyle("Surprise") == "Surprise", "Surprise is a supported preference.");
        check(HarveyOutingRules.NormalizeStyle(null) == "Quiet", "Old configuration files use the quiet default.");
    }
}

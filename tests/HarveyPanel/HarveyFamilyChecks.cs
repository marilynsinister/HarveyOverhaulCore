using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;

internal static class HarveyFamilyChecks
{
    public static void Run(Action<bool, string> check)
    {
        foreach (int day in new[] { -1, 0, 1, 111, 113, 223, 225, 335, 337 })
            check(HarveyMarriageMilestones.AnniversaryStage(day) == 0,
                $"Day {day} must not start an anniversary.");
        check(HarveyMarriageMilestones.AnniversaryStage(112) == 1, "First anniversary uses the beach scene.");
        check(HarveyMarriageMilestones.AnniversaryStage(224) == 2, "Second anniversary uses the kitchen scene.");
        check(HarveyMarriageMilestones.AnniversaryStage(336) == 3, "Third anniversary unlocks the later variants.");
        check(HarveyMarriageMilestones.AnniversaryStage(1120) == 3, "Later years must remain reachable.");

        check(HarveyMarriageMilestones.IsWeddingEve(1), "The day before the wedding is the eve.");
        foreach (int days in new[] { -1, 0, 2, 3 })
            check(!HarveyMarriageMilestones.IsWeddingEve(days), $"{days} days to the wedding is not the eve.");
        check(HarveyMarriageMilestones.IsFirstMorning(1), "The morning after the wedding is the first morning.");
        foreach (int days in new[] { -1, 0, 2 })
            check(!HarveyMarriageMilestones.IsFirstMorning(days), $"Day {days} of marriage is not the first morning.");

        var memory = new HarveyKeepsakeData
        {
            Description = "drawing without a chosen detail",
            Choices = new()
            {
                new() { Flag = "birds", Description = "drawing with birds" },
                new() { Flag = "home", Description = "drawing with a home" },
            },
        };
        check(memory.Describe(_ => false) == memory.Description,
            "Skipping a scene or updating an old save must use an honest fallback description.");
        check(memory.Describe(flag => flag == "home") == "drawing with a home",
            "The keepsake must remember the actual response, not the first response in the menu.");
        check(new HarveyKeepsakeData { Description = "plane" }.Describe(_ => true) == "plane",
            "Keepsakes without response variants must remain readable.");

        RadioChecks(check);
        GrowthAndBalanceChecks(check);
    }

    private static void GrowthAndBalanceChecks(Action<bool, string> check)
    {
        check(HarveyFamilyLifeService.ChildYears(0) == 0 && HarveyFamilyLifeService.ChildYears(111) == 0,
            "A child younger than a year has no year flags.");
        check(HarveyFamilyLifeService.ChildYears(112) == 1 && HarveyFamilyLifeService.ChildYears(224) == 2,
            "Year flags follow 112-day years.");
        check(HarveyFamilyLifeService.ChildYears(5000) == HarveyFamilyLifeService.MaxChildYears, "Year flags stop at the last stage.");

        check(HarveyOutingRules.IsHarveyBirthdayTrip("winter", 13), "Departing on winter 13 is Harvey's birthday trip (14 is Sunday).");
        foreach (var (season, day) in new[] { ("winter", 6), ("winter", 14), ("winter", 20), ("fall", 13) })
            check(!HarveyOutingRules.IsHarveyBirthdayTrip(season, day), $"{season} {day} is an ordinary trip.");

        check(HarveyBalanceStats.Category("buffStressThunder") == "Stress" && HarveyBalanceStats.Category("buffDarknessLevel2") == "Stress",
            "Stress and darkness debuffs are counted as stress.");
        check(HarveyBalanceStats.Category("buffFracturedBone") == "Injury" && HarveyBalanceStats.Category("HarveyMod_WetBandage") == "Injury",
            "Injuries and complications are counted as injuries.");
        var summary = HarveyBalanceStats.Summarize(new[]
        {
            new HarveyBalanceStats.DayRecord { Difficulty = "Cozy", InjuryOnsets = 1 },
            new HarveyBalanceStats.DayRecord { Difficulty = "Cozy", StressOnsets = 2, StressAtDayEnd = 1 },
            new HarveyBalanceStats.DayRecord { Difficulty = "Strict", InjuryOnsets = 2, InjuryAtDayEnd = 1 },
        });
        var cozy = summary.Single(s => s.Difficulty == "Cozy");
        check(cozy.Days == 2 && Math.Abs(cozy.InjuriesPerWeek - 3.5) < 1e-9 && Math.Abs(cozy.StressPerWeek - 7) < 1e-9,
            "Balance summary converts per-day onsets to per-week rates for each difficulty.");
        check(Math.Abs(cozy.ShareDaysEndingDebuffed - 0.5) < 1e-9 && summary.Single(s => s.Difficulty == "Strict").Days == 1,
            "Balance summary keeps difficulties apart and counts days that end debuffed.");
    }

    private static void RadioChecks(Action<bool, string> check)
    {
        bool Call(bool hasRadio = true, bool dating = true, bool married = false, bool sawLate = false, bool away = true,
            bool withHarvey = false, int gap = 5, bool declined = false, bool cozy = false, double roll = 0.9)
            => HarveyRadioService.ShouldCall(hasRadio, dating, married, sawLate, away, withHarvey, gap, declined, cozy, roll);

        check(Call(), "Dating, radio given, away from home: Harvey calls.");
        check(!Call(hasRadio: false), "No radio before C1.");
        check(!Call(dating: false), "No radio calls outside a relationship.");
        check(!Call(away: false), "At home there is nobody to call.");
        check(!Call(withHarvey: true), "No radio call when Harvey is standing next to her.");
        check(!Call(married: true), "In marriage the radio waits for M7 «Ждал ночью».");
        check(Call(married: true, sawLate: true), "After M7 the married radio works.");
        check(!Call(gap: 0), "At most one call per evening.");
        check(!Call(declined: true, gap: 2) && Call(declined: true, gap: 3), "«Мне не нужна охрана» — once in three days.");
        check(!Call(cozy: true, roll: 0.9) && Call(cozy: true, roll: 0.1), "Cozy difficulty calls every other time.");

        foreach (string? callsign in new[] { "Field", "Daisy", "Sun", null })
            foreach (bool married in new[] { false, true })
                for (int variant = -1; variant < 4; variant++)
                    check(!string.IsNullOrWhiteSpace(HarveyRadioService.Message(callsign, married, variant)),
                        $"Radio text exists for {callsign ?? "no callsign"}, married={married}, variant={variant}.");
        check(HarveyRadioService.Message("Daisy", false, 0).Contains("Ромашка"), "The chosen callsign is used on air.");
        foreach (int variant in Enumerable.Range(0, 3))
        {
            string dating = HarveyRadioService.Message("Sun", false, variant);
            check(!dating.Contains("Дома") && !dating.Contains("крыльце") && !dating.Contains("Чайник"),
                $"Dating radio must not talk as if they live together: {dating}");
        }
    }
}

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
    }
}

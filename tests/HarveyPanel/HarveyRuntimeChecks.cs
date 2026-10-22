using HarveyOverhaul.Core.Services;

internal static class HarveyRuntimeChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(!HarveyArendArrivalService.ShouldArrive(false, false, false, false), "Arend must not arrive before the letter.");
        check(!HarveyArendArrivalService.ShouldArrive(false, false, true, true), "Honor the four-day wait after inviting Arend.");
        check(HarveyArendArrivalService.ShouldArrive(false, false, true, false), "A missed bus-stop meeting must not prevent arrival.");
        check(HarveyArendArrivalService.ShouldArrive(false, true, false, true), "An existing arrival note must repair a save even without the scene flag.");
        check(HarveyArendArrivalService.ShouldArrive(true, false, false, true), "The normal arrival scene must unlock the permanent NPC immediately.");

    }
}

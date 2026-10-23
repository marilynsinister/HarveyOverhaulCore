using HarveyOverhaul.Core.Services;
using Microsoft.Xna.Framework;
using System.Reflection;

internal static class HarveyRuntimeChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(!HarveyArendArrivalService.ShouldArrive(false, false, false, false), "Arend must not arrive before the letter.");
        check(!HarveyArendArrivalService.ShouldArrive(false, false, true, true), "Honor the four-day wait after inviting Arend.");
        check(HarveyArendArrivalService.ShouldArrive(false, false, true, false), "A missed bus-stop meeting must not prevent arrival.");
        check(HarveyArendArrivalService.ShouldArrive(false, true, false, true), "An existing arrival note must repair a save even without the scene flag.");
        check(HarveyArendArrivalService.ShouldArrive(true, false, false, true), "The normal arrival scene must unlock the permanent NPC immediately.");

        var compose = typeof(HarveyEventStaging).GetMethod("Compose", BindingFlags.NonPublic | BindingFlags.Static)!;
        const string scene = "continue/2 7/farmer 2 6 1 Harvey 3 6 3/skippable/viewport 2 7 true/speak Harvey \"A/B\"/faceDirection Harvey 3/viewport -1000 -1000/end";
        string staged = (string)compose.Invoke(null, new object[] { scene, new Point(27, 31), new Point(27, 32), "ambientLight 70 75 110/" })!;
        var commands = HarveyEventStaging.SplitCommands(staged);
        check(commands[0] == "continue" && commands[1] == "27 31", "Keep music and use the live player camera position.");
        check(commands[2] == "farmer -1 -1 2 Harvey 27 32 0", "Keep the player's bed position and face the visitor toward them.");
        check(commands[3] == "ignoreEventTileOffset", "Absolute runtime tiles must disable FarmHouse offsets in the fourth command.");
        check(commands[5] == "ambientLight 70 75 110", "Insert night lighting after the header, not before it.");
        check(commands.Contains("speak Harvey \"A/B\""), "A slash inside dialogue must stay inside one command.");
        check(commands.Contains("faceDirection Harvey 0"), "A visitor below the player must face up even if the CP scene faced left.");
        check(!commands.Contains("viewport 2 7 true") && commands.Contains("viewport -1000 -1000"), "Retarget the CP camera while preserving the closing fade viewport.");
        check(commands.Count(c => c == "continue") == 1 && commands[^1] == "end", "Don't leave a second music/header sequence in the body.");

    }
}

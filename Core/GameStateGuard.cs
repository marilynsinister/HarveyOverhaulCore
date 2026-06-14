using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Core;

/// <summary>
/// Проверки состояния игры, общие для Stress/Injury (таймеры лечения и т.д.).
/// </summary>
public sealed class GameStateGuard
{
    public bool ShouldCountTreatmentTime()
    {
        if (!Context.IsWorldReady)
            return false;

        if (Game1.activeClickableMenu != null)
            return false;

        if (IsEventActive())
            return false;

        if (Game1.dialogueUp)
            return false;

        if (Game1.paused)
            return false;

        return true;
    }

    public static bool IsEventActive()
        => Game1.CurrentEvent != null || Game1.eventUp;
}

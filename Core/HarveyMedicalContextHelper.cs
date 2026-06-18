using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Core;

/// <summary>Проверки фестиваля, клиники и близости Харви.</summary>
public static class HarveyMedicalContextHelper
{
    public static bool IsFestivalContext()
    {
        if (!Context.IsWorldReady)
            return false;

        return Game1.isFestival();
    }

    public static bool IsAtClinic()
    {
        if (!Context.IsWorldReady)
            return false;

        var loc = Game1.currentLocation;
        if (loc == null)
            return false;

        return loc.NameOrUniqueName.Equals("Hospital", StringComparison.OrdinalIgnoreCase)
            || loc.NameOrUniqueName.Contains("Clinic", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHarveyNearby(int tileRadius = 4)
    {
        if (!Context.IsWorldReady)
            return false;

        var harvey = Game1.getCharacterFromName("Harvey");
        if (harvey?.currentLocation == null || Game1.player?.currentLocation == null)
            return false;

        if (harvey.currentLocation != Game1.player.currentLocation)
            return false;

        int dx = Math.Abs(harvey.TilePoint.X - Game1.player.TilePoint.X);
        int dy = Math.Abs(harvey.TilePoint.Y - Game1.player.TilePoint.Y);
        return dx <= tileRadius && dy <= tileRadius;
    }

    public static bool IsMarriedToHarvey()
    {
        if (!Context.IsWorldReady)
            return false;

        return string.Equals(Game1.player.spouse, "Harvey", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSpouseMedicalContext()
    {
        if (!IsMarriedToHarvey())
            return false;

        var harvey = Game1.getCharacterFromName("Harvey");
        return harvey?.currentLocation != null
            && Game1.player?.currentLocation != null
            && ReferenceEquals(harvey.currentLocation, Game1.player.currentLocation);
    }

    public static HarveyMedicalContext BuildCurrent()
        => new()
        {
            IsFestival = IsFestivalContext(),
            IsAtClinic = IsAtClinic(),
            HarveyIsNearby = IsHarveyNearby(),
            IsSpouseMedicalContext = IsSpouseMedicalContext(),
        };
}

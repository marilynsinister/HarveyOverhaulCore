using StardewValley;

namespace HarveyOverhaul.Core.UI;

internal static class HarveyToneHelper
{
    public static bool IsInformal()
    {
        if (string.Equals(Game1.player?.spouse, "Harvey", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Game1.player?.friendshipData.TryGetValue("Harvey", out var data) != true)
            return false;

        return data.Status == FriendshipStatus.Dating;
    }
}

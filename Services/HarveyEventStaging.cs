using System.Text;
using Microsoft.Xna.Framework;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>Place a visiting Harvey on the loaded map without assuming a house layout or NPC schedule.</summary>
public static class HarveyEventStaging
{
    public static string AtPlayer(GameLocation location, string script, string openingCommands = "")
    {
        Point player = Game1.player.TilePoint;
        return Compose(script, player, FindVisitorTile(location, player), openingCommands);
    }

    internal static string Compose(string script, Point player, Point visitor, string openingCommands = "")
    {
        var commands = SplitCommands(script);
        // CP definitions are complete events. Rebuild only their header for the current save.
        if (commands.Count < 3)
            throw new ArgumentException("An event needs a music/view/actors header.", nameof(script));

        int direction = Math.Abs(visitor.X - player.X) >= Math.Abs(visitor.Y - player.Y)
            ? visitor.X > player.X ? 3 : 1
            : visitor.Y > player.Y ? 0 : 2;

        return $"{commands[0]}/{player.X} {player.Y}/farmer -1 -1 2 Harvey {visitor.X} {visitor.Y} {direction}/"
            + "ignoreEventTileOffset/"
            + $"viewport {player.X} {player.Y} true/"
            + openingCommands.Trim().TrimStart('/')
            + (string.IsNullOrWhiteSpace(openingCommands) || openingCommands.TrimEnd().EndsWith('/') ? "" : "/")
            + string.Join('/', commands.Skip(3).Select(c => c.StartsWith("faceDirection Harvey ", StringComparison.Ordinal)
                ? $"faceDirection Harvey {direction}"
                : c.StartsWith("viewport ", StringComparison.Ordinal) && c != "viewport -1000 -1000"
                    ? $"viewport {player.X} {player.Y} true" : c));
    }

    private static Point FindVisitorTile(GameLocation location, Point player)
    {
        // Prefer a little space beside the player, and search only the visible vicinity.
        for (int distance = 1; distance <= 4; distance++)
        {
            for (int dy = -distance; dy <= distance; dy++)
            for (int dx = -distance; dx <= distance; dx++)
            {
                if (Math.Abs(dx) + Math.Abs(dy) != distance)
                    continue;
                var tile = new Point(player.X + dx, player.Y + dy);
                if (!location.isTileOnMap(tile.X, tile.Y)
                    || location.Map.GetLayer("Back")?.Tiles[tile.X, tile.Y] == null
                    || !location.isTilePassable(new xTile.Dimensions.Location(tile.X, tile.Y), Game1.viewport)
                    || location.objects.ContainsKey(new Vector2(tile.X, tile.Y))
                    || location.terrainFeatures.TryGetValue(new Vector2(tile.X, tile.Y), out var terrain) && !terrain.isPassable(Game1.player))
                    continue;

                var feet = new Rectangle(tile.X * 64 + 8, tile.Y * 64 + 16, 48, 40);
                if (location.furniture.Any(f => !f.isPassable() && f.boundingBox.Value.Intersects(feet))
                    || location.characters.Any(n => n.Name != "Harvey" && n.GetBoundingBox().Intersects(feet)))
                    continue;
                return tile;
            }
        }
        // Let callers defer the event instead of consuming it with Harvey hidden or inside furniture.
        throw new InvalidOperationException("No free visitor tile near the player.");
    }

    public static List<string> SplitCommands(string script)
    {
        var result = new List<string>();
        var command = new StringBuilder();
        bool quoted = false;
        foreach (char ch in script)
        {
            if (ch == '"') quoted = !quoted;
            if (ch == '/' && !quoted)
            {
                result.Add(command.ToString().Trim());
                command.Clear();
            }
            else command.Append(ch);
        }
        if (command.Length > 0) result.Add(command.ToString().Trim());
        return result;
    }
}

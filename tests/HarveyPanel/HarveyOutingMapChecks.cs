using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;

internal static class HarveyOutingMapChecks
{
    public static void Run(string workspace, Action<bool, string> check)
    {
        string game = Environment.GetEnvironmentVariable("STARDEW_GAME_PATH")
            ?? @"D:\Games\Steam\steamapps\common\Stardew Valley";
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(game, "smapi-internal", "TMXTile.dll"));
        // Как у загрузчика игры: исходный тайл 16 px, масштаб 4, итоговый тайл xTile 64 px.
        var format = Activator.CreateInstance(assembly.GetType("TMXTile.TMXFormat")!, 16, 16, 4, 4)!;
        var load = format.GetType().GetMethod("Load", new[] { typeof(string) })!;
        string maps = Path.Combine(workspace, "HarveyOverhaul [CP]", "assets", "Maps");
        string[] files = Directory.GetFiles(maps, "Harvey*.tmx");
        check(files.Length == 6, "All six outing maps must be included.");
        int checkedProperties = 0;
        foreach (string path in files)
        {
            var map = load.Invoke(format, new object[] { path })!;
            check(map != null, "The installed TMX parser must load " + Path.GetFileName(path));
            var layers = (IEnumerable)map!.GetType().GetProperty("Layers")!.GetValue(map)!;
            object layer = layers.Cast<object>().Single(candidate =>
                (string)candidate.GetType().GetProperty("Id")!.GetValue(candidate)! == "Buildings");
            var tiles = layer.GetType().GetProperty("Tiles")!.GetValue(layer)!;
            var indexer = tiles.GetType().GetProperty("Item", new[] { typeof(int), typeof(int) })!;
            var root = XDocument.Load(path).Root!;
            foreach (var obj in root.Elements("objectgroup").Where(group => (string?)group.Attribute("name") == "Buildings")
                         .Elements("object"))
            {
                int x = (int)obj.Attribute("x")! / 16, y = (int)obj.Attribute("y")! / 16;
                foreach (var expected in obj.Element("properties")!.Elements("property"))
                {
                    var tile = indexer.GetValue(tiles, new object[] { x, y });
                    check(tile != null, $"TMX must retain the tile at {Path.GetFileName(path)} {x},{y}.");
                    var properties = (IEnumerable)tile!.GetType().GetProperty("Properties")!.GetValue(tile)!;
                    string name = (string)expected.Attribute("name")!;
                    var actual = properties.Cast<object>().FirstOrDefault(property =>
                        (string)property.GetType().GetProperty("Key")!.GetValue(property)! == name);
                    check(actual != null && actual.GetType().GetProperty("Value")!.GetValue(actual)!.ToString()
                            == (string?)expected.Attribute("value"),
                        $"TMX must retain {name} at {Path.GetFileName(path)} {x},{y}; ignored properties break doors and interactions.");
                    checkedProperties++;
                }
            }
        }
        check(checkedProperties == 19, "The real parser must retain 15 actions and four passable doors.");
        Console.WriteLine("Parsed outing maps with installed TMXTile: 6 maps, 15 actions, 4 passable doors.");
    }
}

using HarmonyLib;
using HarveyOverhaul.Core.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;

namespace HarveyOverhaul.Core.Services;

/// <summary>Ребёнок в семейных сценах и осмотр памятной мебели, в том числе стоящей на столе.</summary>
public sealed class HarveyFamilyLifeService
{
    public const string ToddlerFlag = "HarveyFamily_ToddlerReady";
    /// <summary>HarveyFamily_ChildYear1…3: прошёл год, два, три с рождения (спрайт в игре дальше «ходит» не растёт, растёт история).</summary>
    public const string ChildYearPrefix = "HarveyFamily_ChildYear";
    public const int MaxChildYears = 3;
    public const string ChildAsset = "Characters/HarveyFamilyChild";
    public const string KeepsakesAsset = "Mods/marilynsinister.HarveyOverhaul/Keepsakes";
    private static HarveyFamilyLifeService? _instance;

    private readonly IModHelper _helper;
    private readonly HashSet<string> _commentedToday = new(StringComparer.Ordinal);

    public HarveyFamilyLifeService(IModHelper helper)
    {
        _helper = helper;
    }

    public void Initialize(Harmony harmony)
    {
        _instance = this;
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.getDescription)),
            postfix: new HarmonyMethod(typeof(HarveyFamilyLifeService), nameof(DescribeFurniture)));
        _helper.Events.Content.AssetRequested += OnAssetRequested;
        _helper.Events.GameLoop.DayStarted += (_, _) => RefreshFamily();
        _helper.Events.GameLoop.SaveLoaded += (_, _) => RefreshFamily();
        _helper.Events.GameLoop.ReturnedToTitle += (_, _) => _commentedToday.Clear();
        _helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private static Child? SceneChild()
        => Game1.player?.getChildren().FirstOrDefault(child => child.Age >= 3);

    private void RefreshFamily()
    {
        _commentedToday.Clear();
        var child = Game1.player.spouse == "Harvey" ? SceneChild() : null;
        if (child != null)
            Game1.player.mailReceived.Add(ToddlerFlag);
        else
            Game1.player.mailReceived.Remove(ToddlerFlag);
        int years = child == null ? 0 : ChildYears(child.daysOld.Value);
        for (int year = 1; year <= MaxChildYears; year++)
        {
            if (year <= years)
                Game1.player.mailReceived.Add(ChildYearPrefix + year);
            else
                Game1.player.mailReceived.Remove(ChildYearPrefix + year);
        }
        // Другой ребёнок, возраст, внешность или другое сохранение — всегда актуальный спрайт.
        _helper.GameContent.InvalidateCache(ChildAsset);
    }

    /// <summary>Полных игровых лет с рождения (112 дней), не больше <see cref="MaxChildYears"/>.</summary>
    public static int ChildYears(int daysOld) => Math.Clamp(daysOld / 112, 0, MaxChildYears);

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(KeepsakesAsset))
            e.LoadFrom(() => new Dictionary<string, HarveyKeepsakeData>(), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(ChildAsset))
            e.LoadFrom(() => _helper.GameContent.Load<Texture2D>(
                SceneChild()?.Sprite.textureName.Value ?? "Characters/Toddler"), AssetLoadPriority.Exclusive);
    }

    private HarveyKeepsakeData? GetKeepsake(string id)
        => _helper.GameContent.Load<Dictionary<string, HarveyKeepsakeData>>(KeepsakesAsset).GetValueOrDefault(id);

    private static void DescribeFurniture(Furniture __instance, ref string __result)
    {
        if (_instance == null || !Context.IsWorldReady)
            return;
        var data = _instance.GetKeepsake(__instance.ItemId);
        if (data != null)
            __result = data.Describe(flag => Game1.player.mailReceived.Contains(flag));
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree || !e.Button.IsActionButton()
            || _helper.Input.IsSuppressed(e.Button) || Game1.player.ActiveObject is Furniture
            || _helper.Input.IsDown(SButton.LeftShift) || _helper.Input.IsDown(SButton.RightShift))
            return;

        var tile = e.Cursor.GrabTile;
        if (Vector2.Distance(Game1.player.Tile, tile) > 2)
            return;
        var point = new Point((int)tile.X * 64 + 32, (int)tile.Y * 64 + 32);
        foreach (var placed in Game1.currentLocation.furniture.Reverse())
        {
            if (!placed.boundingBox.Value.Contains(point))
                continue;
            // Маленькие сувениры можно поставить на стол; обрабатываем сначала их.
            var furniture = placed.heldObject.Value is Furniture held ? held : placed;
            var data = GetKeepsake(furniture.ItemId);
            if (data == null)
                continue;

            _helper.Input.Suppress(e.Button);
            string text = data.Describe(flag => Game1.player.mailReceived.Contains(flag));
            bool together = Game1.player.friendshipData.TryGetValue("Harvey", out var friendship)
                && (friendship.IsMarried() || friendship.IsDating() || friendship.IsEngaged());
            foreach (var npc in Game1.currentLocation.characters)
            {
                string line = npc.Name == "Harvey" && together ? data.HarveyLine
                    : npc.Name == "Arend" ? data.ArendLine : "";
                if (!string.IsNullOrWhiteSpace(line) && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 5
                    && _commentedToday.Add(npc.Name + "/" + furniture.ItemId))
                    text += "#" + (npc.Name == "Harvey" ? "Харви" : "Аренд") + ": «" + line + "»";
            }
            Game1.drawObjectDialogue(text);
            return;
        }
    }
}

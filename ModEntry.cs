using HarveyOverhaul.Core.Api;
using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using HarveyOverhaul.Core.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace HarveyOverhaul.Core;

public sealed class ModEntry : Mod
{
    private ModConfig _config = null!;
    private HarveyProviderRegistry _providerRegistry = null!;
    private HarveyCoreApi _coreApi = null!;
    private HarveyPanelMenu _panelMenu = null!;
    private HarveyPanelService _panelService = null!;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _providerRegistry = new HarveyProviderRegistry(Monitor);
        var gameStateGuard = new GameStateGuard();
        _panelMenu = new HarveyPanelMenu(Monitor);
        _panelService = new HarveyPanelService(_providerRegistry);
        _coreApi = new HarveyCoreApi(_providerRegistry, gameStateGuard, _panelMenu, _panelService);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add(
            "harvey_panel_debug",
            "Debug info for Harvey Overhaul Core panel (StardewUI).",
            (_, __) => Monitor.Log(_panelMenu.BuildDebugReport(_panelService, _providerRegistry), LogLevel.Info));

        Monitor.Log("Harvey Overhaul Core loaded — shared contracts and provider registry ready.", LogLevel.Info);
    }

    public override object GetApi()
        => _coreApi;

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _panelMenu.TryInitialize(Helper);

        if (_panelMenu.IsAvailable)
            Monitor.Log("[HarveyOverhaul.Core] Окно «План Харви» готово (клавиша H).", LogLevel.Info);
        else
            Monitor.Log("[HarveyOverhaul.Core] StardewUI недоступен — окно «План Харви» не откроется.", LogLevel.Warn);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (!_config.OpenHarveyPanel.JustPressed())
            return;

        Monitor.Log("[HarveyOverhaul.Core] H pressed.", LogLevel.Debug);

        if (GameStateGuard.IsEventActive())
        {
            Monitor.Log("[HarveyOverhaul.Core] Open blocked: event active.", LogLevel.Debug);
            return;
        }

        if (Game1.dialogueUp)
        {
            Monitor.Log("[HarveyOverhaul.Core] Open blocked: dialogue open.", LogLevel.Debug);
            return;
        }

        if (_panelMenu.IsOpen)
        {
            Monitor.Log("[HarveyOverhaul.Core] Closing Harvey panel.", LogLevel.Debug);
            _panelMenu.Close();
            Helper.Input.SuppressActiveKeybinds(_config.OpenHarveyPanel);
            return;
        }

        if (Game1.activeClickableMenu != null)
        {
            Monitor.Log(
                $"[HarveyOverhaul.Core] Open blocked: active menu {Game1.activeClickableMenu.GetType().Name}.",
                LogLevel.Debug);
            return;
        }

        var tab = _panelService.ResolveDefaultTab();
        Monitor.Log($"[HarveyOverhaul.Core] Opening Harvey panel: {tab}.", LogLevel.Debug);
        _panelMenu.TryOpen(_panelService, tab);
        Helper.Input.SuppressActiveKeybinds(_config.OpenHarveyPanel);
    }
}

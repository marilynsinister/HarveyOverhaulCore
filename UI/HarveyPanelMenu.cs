using System.IO;
using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley;

namespace HarveyOverhaul.Core.UI;

/// <summary>StardewUI-окно «План Харви» с вкладками.</summary>
public sealed class HarveyPanelMenu
{
    public const string ModUniqueId = "marilynsinister.HarveyOverhaul.Core";
    public const string ViewAssetName = "Mods/marilynsinister.HarveyOverhaul.Core/Views/HarveyPanel";

    private readonly IMonitor _monitor;
    private IViewEngine? _viewEngine;
    private IMenuController? _menuController;
    private HarveyPanelViewModel? _activeViewModel;
    private HarveyPanelTab _lastTab = HarveyPanelTab.Overview;
    private bool _assetsRegistered;
    private bool _delayedOpenScheduled;

    public HarveyPanelMenu(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public bool IsAvailable => _viewEngine != null;

    public bool IsOpen =>
        _menuController?.Menu != null
        && Game1.activeClickableMenu == _menuController.Menu;

    public void TryInitialize(IModHelper helper)
    {
        if (_viewEngine != null)
            return;

        if (!helper.ModRegistry.IsLoaded("focustense.StardewUI"))
        {
            _monitor.Log("[HarveyOverhaul.Core] StardewUI не установлен — окно «План Харви» недоступно.", LogLevel.Warn);
            return;
        }

        _viewEngine = helper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        if (_viewEngine == null)
        {
            _monitor.Log("[HarveyOverhaul.Core] StardewUI API недоступен.", LogLevel.Warn);
            return;
        }

        string viewsDirectory = Path.Combine(helper.DirectoryPath, "assets", "views");
        _viewEngine.RegisterViews($"Mods/{ModUniqueId}/Views", viewsDirectory);

        string spritesDirectory = Path.Combine(helper.DirectoryPath, "assets", "sprites");
        if (Directory.Exists(spritesDirectory))
            _viewEngine.RegisterSprites($"Mods/{ModUniqueId}/Sprites", spritesDirectory);

        _viewEngine.PreloadModels(
            typeof(HarveyPanelViewModel),
            typeof(HarveyPanelTabButtonViewModel),
            typeof(HandbookViewModel),
            typeof(HandbookRow));
        _viewEngine.PreloadAssets();
        _assetsRegistered = true;

        _monitor.Log("[HarveyOverhaul.Core] StardewUI views зарегистрированы.", LogLevel.Debug);
    }

    public void Toggle(HarveyPanelService panelService)
    {
        if (IsOpen)
            Close();
        else
            TryOpen(panelService);
    }

    public bool TryOpen(HarveyPanelService panelService, HarveyPanelTab? tab = null)
    {
        if (!Context.IsWorldReady)
        {
            _monitor.Log("[HarveyOverhaul.Core] Open blocked: world is not ready.", LogLevel.Debug);
            return false;
        }

        if (_viewEngine == null)
        {
            _monitor.Log("[HarveyOverhaul.Core] Open blocked: StardewUI is not available.", LogLevel.Warn);
            Game1.addHUDMessage(new HUDMessage("Окно «План Харви» недоступно: нужен StardewUI.", HUDMessage.error_type));
            return false;
        }

        if (!_assetsRegistered)
        {
            _monitor.Log("[HarveyOverhaul.Core] Open blocked: view assets are not registered.", LogLevel.Warn);
            Game1.addHUDMessage(new HUDMessage("Окно «План Харви» недоступно: нужен StardewUI.", HUDMessage.error_type));
            return false;
        }

        if (Game1.activeClickableMenu != null && !IsOpen)
        {
            _monitor.Log(
                $"[HarveyOverhaul.Core] Open blocked: active menu {Game1.activeClickableMenu.GetType().Name}.",
                LogLevel.Debug);
            return false;
        }

        if (!Context.IsPlayerFree && !IsOpen)
        {
            if (Game1.activeClickableMenu == null && !GameStateGuard.IsEventActive())
            {
                ScheduleDelayedOpen(panelService, tab ?? _lastTab);
                return false;
            }

            _monitor.Log("[HarveyOverhaul.Core] Open blocked: player is not free.", LogLevel.Debug);
            return false;
        }

        var selectedTab = tab ?? _lastTab;

        try
        {
            HarveyPanelViewModel viewModel = panelService.BuildViewModel(selectedTab);
            _activeViewModel = viewModel;

            _menuController?.Dispose();
            _menuController = _viewEngine.CreateMenuControllerFromAsset(ViewAssetName, viewModel);
            if (_menuController?.Menu == null)
            {
                _monitor.Log("[HarveyOverhaul.Core] Open failed: menu controller returned null.", LogLevel.Warn);
                _menuController?.Dispose();
                _menuController = null;
                _activeViewModel = null;
                return false;
            }

            _menuController.Closed += OnMenuClosed;
            Game1.activeClickableMenu = _menuController.Menu;
            return true;
        }
        catch (Exception ex)
        {
            _monitor.Log($"[HarveyOverhaul.Core] Open failed: {ex}", LogLevel.Error);
            _menuController?.Dispose();
            _menuController = null;
            _activeViewModel = null;
            return false;
        }
    }

    public void Close()
    {
        if (!IsOpen || _menuController == null)
            return;

        _menuController.Menu.exitThisMenu();
    }

    public void OpenToTab(HarveyPanelService panelService, HarveyPanelTab tab)
    {
        if (IsOpen)
            Close();

        TryOpen(panelService, tab);
    }

    private void ScheduleDelayedOpen(HarveyPanelService panelService, HarveyPanelTab tab)
    {
        if (_delayedOpenScheduled)
            return;

        _delayedOpenScheduled = true;

        Game1.delayedActions.Add(new DelayedAction(75, () =>
        {
            _delayedOpenScheduled = false;

            if (!Context.IsWorldReady || IsOpen)
                return;

            if (Game1.activeClickableMenu != null || GameStateGuard.IsEventActive())
                return;

            TryOpen(panelService, tab);
        }));
    }

    private void OnMenuClosed()
    {
        if (_activeViewModel != null
            && Enum.TryParse<HarveyPanelTab>(_activeViewModel.SelectedTabKey, out var tab))
        {
            _lastTab = tab;
        }

        _activeViewModel = null;
        _menuController = null;
    }
}

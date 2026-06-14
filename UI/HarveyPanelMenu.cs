using System.IO;
using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley;
using StardewValley.Menus;

namespace HarveyOverhaul.Core.UI;

/// <summary>StardewUI-окно «План Харви» с вкладками.</summary>
public sealed class HarveyPanelMenu
{
    public const string ModUniqueId = "marilynsinister.HarveyOverhaul.Core";
    public const string ViewAssetName = "Mods/marilynsinister.HarveyOverhaul.Core/Views/HarveyPanel";

    private const int PreferredWidth = 900;
    private const int PreferredHeight = 620;
    private const int MinWidth = 700;
    private const int MinHeight = 450;

    private readonly IMonitor _monitor;
    private IViewEngine? _viewEngine;
    private IMenuController? _menuController;
    private HarveyPanelViewModel? _activeViewModel;
    private HarveyPanelTab _lastTab = HarveyPanelTab.Overview;
    private bool _assetsRegistered;
    private bool _delayedOpenScheduled;
    private string? _lastOpenError;

    public HarveyPanelMenu(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public bool IsAvailable => _viewEngine != null;

    public bool IsOpen =>
        _menuController?.Menu != null
        && Game1.activeClickableMenu == _menuController.Menu;

    public string? LastOpenError => _lastOpenError;

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
            typeof(HarveyPanelSectionViewModel),
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
        _lastOpenError = null;

        if (!Context.IsWorldReady)
        {
            _lastOpenError = "world is not ready";
            _monitor.Log("[HarveyPanel] Open blocked: world is not ready.", LogLevel.Debug);
            return false;
        }

        if (_viewEngine == null)
        {
            _lastOpenError = "StardewUI API unavailable";
            _monitor.Log("[HarveyPanel] Open failed: StardewUI is not available.", LogLevel.Warn);
            Game1.addHUDMessage(new HUDMessage("Окно «План Харви» недоступно: нужен StardewUI.", HUDMessage.error_type));
            return false;
        }

        if (!_assetsRegistered)
        {
            _lastOpenError = "StardewUI view asset not found";
            _monitor.Log("[HarveyPanel] Open failed: StardewUI view asset not found.", LogLevel.Warn);
            Game1.addHUDMessage(new HUDMessage("Окно «План Харви» недоступно: нужен StardewUI.", HUDMessage.error_type));
            return false;
        }

        if (Game1.activeClickableMenu != null && !IsOpen)
        {
            _lastOpenError = $"active menu {Game1.activeClickableMenu.GetType().Name}";
            _monitor.Log(
                $"[HarveyPanel] Open blocked: active menu {Game1.activeClickableMenu.GetType().Name}.",
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

            _lastOpenError = "player is not free";
            _monitor.Log("[HarveyPanel] Open blocked: player is not free.", LogLevel.Debug);
            return false;
        }

        var selectedTab = tab ?? _lastTab;

        try
        {
            _monitor.Log("[HarveyPanel] Opening panel.", LogLevel.Debug);
            _monitor.Log($"[HarveyPanel] View asset: {ViewAssetName}", LogLevel.Debug);

            HarveyPanelViewModel viewModel = panelService.BuildViewModel(selectedTab);
            if (viewModel.Tabs.Count == 0)
            {
                _lastOpenError = "view model has no tabs";
                _monitor.Log("[HarveyPanel] Open failed: view model has no tabs.", LogLevel.Warn);
                return false;
            }

            viewModel.SetCloseHandler(Close);
            _activeViewModel = viewModel;

            _monitor.Log(
                $"[HarveyPanel] ViewModel created. Tabs={viewModel.Tabs.Count}, Sections={viewModel.ActiveSections.Count}",
                LogLevel.Debug);

            _menuController?.Dispose();
            _menuController = _viewEngine.CreateMenuControllerFromAsset(ViewAssetName, viewModel);
            if (_menuController == null)
            {
                _lastOpenError = "exception while creating menu controller";
                _monitor.Log("[HarveyPanel] Open failed: exception while creating menu controller.", LogLevel.Warn);
                _activeViewModel = null;
                return false;
            }

            _menuController.EnableCloseButton(null, null, 1f);
            ApplyMenuLayout(_menuController);

            if (_menuController.Menu == null)
            {
                _lastOpenError = "menu controller returned null menu";
                _monitor.Log("[HarveyPanel] Open failed: menu controller returned null.", LogLevel.Warn);
                _menuController.Dispose();
                _menuController = null;
                _activeViewModel = null;
                return false;
            }

            _menuController.Closed += OnMenuClosed;
            Game1.activeClickableMenu = _menuController.Menu;

            LogMenuBounds("[HarveyPanel] Menu bounds");
            _monitor.Log("[HarveyPanel] Open succeeded.", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            _lastOpenError = ex.Message;
            _monitor.Log($"[HarveyPanel] Open failed: exception while creating menu controller: {ex}", LogLevel.Error);
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
        if (_activeViewModel != null && _menuController?.Menu != null)
        {
            _activeViewModel.SelectTab(tab.ToString());
            _lastTab = tab;
            return;
        }

        TryOpen(panelService, tab);
    }

    public string BuildDebugReport(HarveyPanelService panelService, HarveyProviderRegistry registry)
    {
        var preview = panelService.BuildViewModel(_lastTab);
        var lines = new List<string>
        {
            $"Core loaded: yes",
            $"StardewUI API available: {IsAvailable}",
            $"View asset registered: {_assetsRegistered}",
            $"View asset name: {ViewAssetName}",
            $"Providers registered: {registry.GetProviders().Count}",
        };

        foreach (var provider in registry.GetProviders())
            lines.Add($"  - {provider.UniqueId} ({provider.DisplayName})");

        lines.Add($"Current tabs count: {preview.Tabs.Count}");
        lines.Add($"Current sections count: {preview.ActiveSections.Count}");
        lines.Add($"Last open error: {_lastOpenError ?? "(none)"}");
        lines.Add($"Panel open: {IsOpen}");

        if (IsOpen && _menuController?.Menu is IClickableMenu menu)
            lines.Add($"Current panel bounds: X={menu.xPositionOnScreen}, Y={menu.yPositionOnScreen}, W={menu.width}, H={menu.height}");
        else
            lines.Add("Current panel bounds: (not open)");

        return string.Join("\n", lines);
    }

    private void ApplyMenuLayout(IMenuController controller)
    {
        controller.PositionSelector = () =>
        {
            var viewport = Game1.uiViewport;
            int width = controller.Menu?.width > 0 ? controller.Menu.width : PreferredWidth;
            int height = controller.Menu?.height > 0 ? controller.Menu.height : PreferredHeight;

            width = Math.Clamp(width, MinWidth, Math.Max(MinWidth, (int)(viewport.Width * 0.9f)));
            height = Math.Clamp(height, MinHeight, Math.Max(MinHeight, (int)(viewport.Height * 0.85f)));

            if (controller.Menu != null)
            {
                controller.Menu.width = width;
                controller.Menu.height = height;
            }

            return new Point(
                Math.Max(0, (viewport.Width - width) / 2),
                Math.Max(0, (viewport.Height - height) / 2));
        };
    }

    private void LogMenuBounds(string prefix)
    {
        if (_menuController?.Menu is not IClickableMenu menu)
            return;

        _monitor.Log(
            $"{prefix}: X={menu.xPositionOnScreen}, Y={menu.yPositionOnScreen}, W={menu.width}, H={menu.height}",
            LogLevel.Debug);
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

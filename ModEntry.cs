using HarveyOverhaul.Core.Api;
using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using HarveyOverhaul.Core.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace HarveyOverhaul.Core;

public sealed class ModEntry : Mod
{
    private ModConfig _config = null!;
    private HarveyProviderRegistry _providerRegistry = null!;
    private HarveyCareDirectiveRegistry _directiveRegistry = null!;
    private HarveyCoreApi _coreApi = null!;
    private HarveyPanelMenu _panelMenu = null!;
    private HarveyPanelService _panelService = null!;
    private HarveyMedicalIntentArbitrator _medicalIntentArbitrator = null!;
    private IModHelper _helper = null!;

    public override void Entry(IModHelper helper)
    {
        _helper = helper;
        _config = helper.ReadConfig<ModConfig>();
        _providerRegistry = new HarveyProviderRegistry(Monitor);
        _directiveRegistry = new HarveyCareDirectiveRegistry(Monitor);
        var gameStateGuard = new GameStateGuard();
        _panelMenu = new HarveyPanelMenu(Monitor);
        var planAdvisor = new HarveyPlanAdvisor(_directiveRegistry);
        _panelService = new HarveyPanelService(_providerRegistry, planAdvisor);

        var conversationSettings = new HarveyMedicalConversationSettings
        {
            AllowBasicTreatmentOutsideClinic = _config.AllowBasicTreatmentOutsideClinic,
            AllowPhaseTransitionOutsideClinic = _config.AllowPhaseTransitionOutsideClinic,
            AllowRecoveryOutsideClinic = _config.AllowRecoveryOutsideClinic,
            RequireClinicForSevereInjuries = _config.RequireClinicForSevereInjuries,
            BlockLongTreatmentDuringFestivals = _config.BlockLongTreatmentDuringFestivals,
        };
        _medicalIntentArbitrator = new HarveyMedicalIntentArbitrator(Monitor, conversationSettings);
        var clickDiagnostics = new HarveyMedicalClickDiagnostics(Monitor);
        _coreApi = new HarveyCoreApi(
            _providerRegistry,
            _directiveRegistry,
            gameStateGuard,
            _panelMenu,
            _panelService,
            _medicalIntentArbitrator,
            clickDiagnostics);

        helper.ConsoleCommands.Add(
            "harvey_plan_debug",
            "Debug Harvey Plan advisor snapshot (directives, tone, primary action).",
            (_, __) => LogHarveyPlanDump(includeUiPreview: true));

        helper.ConsoleCommands.Add(
            "harvey_plan_dump",
            "Full dump: providers, raw facts, directives, sections, tone, fallback.",
            (_, __) => LogHarveyPlanDump(includeUiPreview: false));

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add(
            "harvey_panel_debug",
            "Debug info for Harvey Overhaul Core panel (StardewUI).",
            (_, __) => Monitor.Log(_panelMenu.BuildDebugReport(_panelService, _providerRegistry), LogLevel.Info));

        helper.ConsoleCommands.Add(
            "harvey_intent_debug",
            "Print current Harvey medical intent arbitration (Core).",
            (_, __) => LogHarveyIntentDebug());

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

        TryLogHarveyClickIntent(e);

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
        var (snapshot, fallbackReason) = _panelService.BuildPlanOpenContext();
        var preview = _panelService.BuildViewModel(tab, _config.DebugMode);
        int planSectionCount = preview.ActiveSections.Count;
        HarveyPlanDiagnostics.LogOpenPlan(
            Monitor,
            _providerRegistry,
            _directiveRegistry,
            snapshot,
            fallbackReason,
            tab,
            planSectionCount);

        Monitor.Log($"[HarveyOverhaul.Core] Opening Harvey panel: {tab}.", LogLevel.Debug);
        _panelMenu.TryOpen(_panelService, tab, _config.DebugMode);
        Helper.Input.SuppressActiveKeybinds(_config.OpenHarveyPanel);
    }

    private void LogHarveyIntentDebug()
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("[HarveyIntent] world not ready", LogLevel.Info);
            return;
        }

        var resolution = _coreApi.ResolveHarveyMedicalIntent(logDetails: true);
        if (resolution.Selected == null)
        {
            Monitor.Log("[HarveyIntent] no selected intent", LogLevel.Info);
            return;
        }

        var s = resolution.Selected;
        Monitor.Log(
            $"[HarveyIntent] winner={s.ProviderId} kind={s.Kind} state={s.StateId} priority={s.BasePriority} " +
            $"topic={s.TopicKey} action={s.ActionKey} festivalDefer={resolution.ActiveFestivalDeferTopicKey ?? "-"}",
            LogLevel.Info);
    }

    private void TryLogHarveyClickIntent(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (Game1.eventUp || Game1.CurrentEvent != null)
            return;

        if (!e.Button.IsActionButton())
            return;

        if (Game1.activeClickableMenu is DialogueBox)
            return;

        var loc = Game1.currentLocation;
        if (loc == null)
            return;

        var tile = _helper.Input.GetCursorPosition().GrabTile;
        if (!TryIsHarveyAtTile(loc, tile))
            return;

        var resolution = _coreApi.PrepareHarveyMedicalClick(logDetails: true);
    }

    private static bool TryIsHarveyAtTile(GameLocation loc, Microsoft.Xna.Framework.Vector2 tile)
    {
        foreach (var npc in loc.characters)
        {
            if (npc.Name.Equals("Harvey", StringComparison.OrdinalIgnoreCase)
                && npc.TilePoint.X == (int)tile.X
                && npc.TilePoint.Y == (int)tile.Y)
            {
                return true;
            }
        }

        return false;
    }

    private void LogHarveyPlanDump(bool includeUiPreview)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("[HarveyPlan] world not ready", LogLevel.Warn);
            return;
        }

        var snapshot = _panelService.BuildHarveyPlanSnapshot();
        HarveyPlanDiagnostics.LogBuildSnapshot(Monitor, snapshot);
        Monitor.Log(_panelService.BuildPlanDebugReport(), LogLevel.Info);

        if (includeUiPreview)
        {
            var preview = _panelService.BuildViewModel(HarveyPanelTab.Plan, _config.DebugMode);
            Monitor.Log(
                $"[HarveyPlan] UI preview: tabs={preview.Tabs.Count}, " +
                $"planSections={preview.ActiveSections.Count}, tab={preview.SelectedTabKey}, " +
                $"planDetailLen={preview.PlanDetailBody.Length}, adviceLen={preview.HarveyAdviceText.Length}",
                LogLevel.Info);

            foreach (var section in preview.ActiveSections)
            {
                Monitor.Log(
                    $"[HarveyPlan] UI section: headline='{section.Headline}', bodyLen={section.BodyText.Length}",
                    LogLevel.Info);
            }
        }
    }
}

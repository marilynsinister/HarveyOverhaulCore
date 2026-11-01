using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Сцены с Харви (утренние визиты в доме фермы и др.): если другой мод открыл своё окно поверх реплики
/// или вопроса сцены, реплика пропадала, а сцена шла дальше без неё — выбор не задавался, визит «сбивался».
/// Запоминаем подменённое окно сцены и возвращаем его, когда чужое окно закроют.
/// </summary>
public sealed class HarveyEventDialogueGuard(IModHelper helper, IMonitor monitor)
{
    private DialogueBox? _stashed;
    private IClickableMenu? _intruder;
    private Event? _event;
    private int _command = -1;
    private DialogueBox? _observedDialogue;
    private Event? _observedEvent;
    private int _observedCommand = -1;

    public void Initialize()
    {
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            if (_stashed != null && (!ReferenceEquals(Game1.CurrentEvent, _event)
                || _event?.CurrentCommand != _command))
                ClearStash();
        };
        helper.Events.GameLoop.DayStarted += (_, _) => Clear();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        bool hadStash = _stashed != null;
        var restore = ProcessMenuChange(Game1.CurrentEvent, e.OldMenu, e.NewMenu);
        if (restore != null)
        {
            Game1.activeClickableMenu = restore;
            Game1.dialogueUp = true;
            monitor.Log("[HarveyEventGuard] Незавершённая реплика текущей команды возвращена.", LogLevel.Debug);
        }
        else if (!hadStash && _stashed != null)
        {
            monitor.Log(
                $"[HarveyEventGuard] Окно {e.NewMenu!.GetType().Name} перекрыло незавершённую реплику {_event!.id}, команда {_command}.",
                LogLevel.Debug);
        }
    }

    // MenuChanged reports the menus observed at frame boundaries. The game can
    // close a DialogueBox, consume its text and open the next menu in one frame.
    // That is normal for hospitaldeath -> showItemsLost, and for consecutive
    // message/speak commands. Only an unfinished box owned by the SAME event
    // command can be suspended and resumed.
    internal DialogueBox? ProcessMenuChange(Event? evt, IClickableMenu? oldMenu, IClickableMenu? newMenu)
    {
        if (_stashed != null)
        {
            if (!ReferenceEquals(evt, _event) || evt?.CurrentCommand != _command || !CanResume(_stashed))
                ClearStash();
            else if (newMenu == null && ReferenceEquals(oldMenu, _intruder))
            {
                var restore = _stashed;
                ClearStash();
                Observe(evt, restore);
                return restore;
            }
            else if (ReferenceEquals(oldMenu, _intruder) && newMenu != null)
                _intruder = newMenu;
        }

        if (_stashed == null && oldMenu is DialogueBox box && newMenu != null
            && !ReferenceEquals(oldMenu, newMenu) && ReferenceEquals(box, _observedDialogue)
            && evt != null && ReferenceEquals(evt, _observedEvent) && evt.CurrentCommand == _observedCommand
            && IsHarveyEvent(evt) && CanResume(box) && !IsSameSpeaker(box, newMenu))
        {
            _stashed = box;
            _intruder = newMenu;
            _event = evt;
            _command = evt.CurrentCommand;
        }

        Observe(evt, newMenu);
        return null;
    }

    private static bool CanResume(DialogueBox box)
        => !box.dialogueFinished && !(box.transitioning && !box.transitioningBigger)
           && !string.IsNullOrWhiteSpace(box.getCurrentString());

    private void Observe(Event? evt, IClickableMenu? menu)
    {
        _observedDialogue = menu as DialogueBox;
        _observedEvent = evt;
        _observedCommand = evt?.CurrentCommand ?? -1;
    }

    private static bool IsHarveyEvent(Event evt)
        => (evt.id?.Contains("Harvey", StringComparison.OrdinalIgnoreCase) ?? false)
           || evt.actors.Any(actor => actor.Name == "Harvey");

    private static bool IsSameSpeaker(DialogueBox oldBox, IClickableMenu newMenu)
        => newMenu is DialogueBox newBox
           && oldBox.characterDialogue?.speaker != null
           && ReferenceEquals(oldBox.characterDialogue.speaker, newBox.characterDialogue?.speaker);

    private void ClearStash()
    {
        _stashed = null;
        _intruder = null;
        _event = null;
        _command = -1;
    }

    private void Clear()
    {
        ClearStash();
        Observe(null, null);
    }
}

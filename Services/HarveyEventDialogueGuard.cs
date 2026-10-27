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
    private IClickableMenu? _stashed;
    private IClickableMenu? _intruder;
    private Event? _event;

    public void Initialize()
    {
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            if (_stashed != null && !ReferenceEquals(Game1.CurrentEvent, _event))
                Clear();
        };
        helper.Events.GameLoop.DayStarted += (_, _) => Clear();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        var evt = Game1.CurrentEvent;

        if (_stashed != null)
        {
            if (e.NewMenu == null && ReferenceEquals(e.OldMenu, _intruder) && ReferenceEquals(evt, _event))
            {
                var restore = _stashed;
                Clear();
                Game1.activeClickableMenu = restore;
                monitor.Log("[HarveyEventGuard] Чужое окно закрыто — реплика сцены с Харви возвращена.", LogLevel.Debug);
            }
            return;
        }

        // Окно сцены не закрыли, а сразу подменили другим — так делает только чужой код:
        // свои реплики сцена открывает после закрытия предыдущих.
        if (e.OldMenu is not DialogueBox box || e.NewMenu == null || ReferenceEquals(e.OldMenu, e.NewMenu))
            return;
        if (evt == null || !IsHarveyEvent(evt) || IsSameSpeaker(box, e.NewMenu))
            return;

        _stashed = box;
        _intruder = e.NewMenu;
        _event = evt;
        monitor.Log(
            $"[HarveyEventGuard] Окно {e.NewMenu.GetType().Name} открылось поверх сцены {evt.id} — реплика будет показана после него.",
            LogLevel.Info);
    }

    private static bool IsHarveyEvent(Event evt)
        => (evt.id?.Contains("Harvey", StringComparison.OrdinalIgnoreCase) ?? false)
           || evt.actors.Any(actor => actor.Name == "Harvey");

    private static bool IsSameSpeaker(DialogueBox oldBox, IClickableMenu newMenu)
        => newMenu is DialogueBox newBox
           && oldBox.characterDialogue?.speaker != null
           && ReferenceEquals(oldBox.characterDialogue.speaker, newBox.characterDialogue?.speaker);

    private void Clear()
    {
        _stashed = null;
        _intruder = null;
        _event = null;
    }
}

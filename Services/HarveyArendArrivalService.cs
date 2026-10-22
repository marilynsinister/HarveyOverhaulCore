using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>Создать постоянного Аренда сразу после приезда и восстановить старые сейвы с письмом без NPC.</summary>
public sealed class HarveyArendArrivalService(IModHelper helper, IMonitor monitor)
{
    public const string ArrivalEvent = "HarveyOverhaulArend.A1_Arrival";
    public const string ArrivalNote = "HarveyArend_ArrivedNote";
    public const string Letter = "HarveyArend_Letter";
    public const string WaitingTopic = "HarveyArend_Waiting";
    private string? _lastError;

    public void Initialize()
    {
        helper.Events.GameLoop.SaveLoaded += (_, _) => EnsureArrived();
        helper.Events.GameLoop.DayStarted += (_, _) => EnsureArrived();
        // DayStarted CP и завершение сцены могут поставить флаг позже обработчика Core.
        helper.Events.GameLoop.OneSecondUpdateTicked += (_, _) => EnsureArrived();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => _lastError = null;
    }

    public static bool ShouldArrive(bool sawArrival, bool hasArrivalNote, bool readLetter, bool waiting)
        => sawArrival || hasArrivalNote || (readLetter && !waiting);

    private void EnsureArrived()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.eventUp)
            return;
        try
        {
            // Уважает ArendEnabled: выключенный CP-персонаж не существует в Data/Characters.
            if (!NPC.TryGetData("Arend", out _))
                return;
            var owner = Game1.getAllFarmers().FirstOrDefault(farmer => ShouldArrive(
                farmer.eventsSeen.Contains(ArrivalEvent), farmer.hasOrWillReceiveMail(ArrivalNote),
                farmer.mailReceived.Contains(Letter), farmer.activeDialogueEvents.ContainsKey(WaitingTopic)));
            if (owner == null)
                return;

            bool offscreenArrival = !owner.eventsSeen.Contains(ArrivalEvent);
            bool addedEvent = owner.eventsSeen.Add(ArrivalEvent);
            var arend = Game1.getCharacterFromName("Arend", mustBeVillager: true);
            if (arend == null)
            {
                // Используем штатный конструктор игры, домашнюю карту и настройки персонажа из CP.
                Game1.AddCharacterIfNecessary("Arend");
                arend = Game1.getCharacterFromName("Arend", mustBeVillager: true);
                if (arend == null)
                {
                    if (addedEvent)
                        owner.eventsSeen.Remove(ArrivalEvent);
                    WarnOnce("Не удалось создать Аренда; повторю после загрузки игровых данных.");
                    return;
                }
                // В день приезда (или восстановления) отдыхает наверху, как обещано в письме.
                // Со следующего утра игра загружает его обычное расписание.
                arend.ClearSchedule();
                monitor.Log("[Arend] Создан постоянный NPC в комнате Харви; обычное расписание — со следующего утра.", LogLevel.Info);
            }

            if (offscreenArrival && !owner.hasOrWillReceiveMail(ArrivalNote))
            {
                owner.mailbox.Add(ArrivalNote);
                owner.activeDialogueEvents["HarveyArend_Arrived"] = 7;
            }
            _lastError = null;
        }
        catch (Exception ex)
        {
            WarnOnce($"Не удалось восстановить приезжающего Аренда: {ex.Message}");
        }
    }

    private void WarnOnce(string message)
    {
        if (_lastError == message)
            return;
        _lastError = message;
        monitor.Log("[Arend] " + message, LogLevel.Warn);
    }
}

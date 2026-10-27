using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Triggers;

namespace HarveyOverhaul.Core.Services;

/// <summary>День рождения из Happy Birthday и поездки в отдельный район Зузу без координат фермы.</summary>
public sealed class HarveyOutingsService(IModHelper helper, IMonitor monitor, ModConfig config)
{
    public const string ContextAsset = "Mods/marilynsinister.HarveyOverhaul/Outings";
    public const string BirthdayEvent = "HarveyOverhaulBirthday.B1_Celebration";
    public const string BirthdayPending = "HarveyBirthday_Pending";
    public const string PartyRoom = "HarveyCelebrationRoom";
    public const string Square = "HarveyZuzuSquare";
    public const string Inn = "HarveyZuzuInn";
    public const string HappyBirthdayId = "Omegasis.HappyBirthday";
    private const string Prefix = "marilynsinister.HarveyOverhaul.Core/Outings/";
    private const string CompanionName = "HarveyZuzuCompanion";
    private const string Invite = "marilynsinister.HarveyOverhaul_mailBirthdayInvitation";
    /// <summary>Уже возвращались из Зузу хотя бы раз — открывает сцены второй поездки.</summary>
    public const string VisitedFlag = "HarveyZuzu_Visited";
    /// <summary>Текущая поездка — на день рождения Харви (выезд 13 зимы).</summary>
    public const string BirthdayTripFlag = "HarveyZuzu_HisBirthday";
    private bool _resumeHotel;
    private bool _returnWhenFree;
    private string? _lastError;
    private readonly Dictionary<string, Point> _anchors = new()
    {
        [Square] = new(18, 14), ["HarveyZuzuCafe"] = new(13, 10),
        ["HarveyZuzuBooks"] = new(13, 10), ["HarveyZuzuCampus"] = new(13, 10), [Inn] = new(13, 10),
    };

    public void Initialize()
    {
        helper.Events.Content.AssetRequested += (_, e) =>
        {
            if (e.NameWithoutLocale.IsEquivalentTo(ContextAsset))
                e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Exclusive);
        };
        helper.Events.GameLoop.SaveLoaded += (_, _) => StartDay();
        helper.Events.GameLoop.DayStarted += (_, _) => StartDay();
        helper.Events.GameLoop.OneSecondUpdateTicked += (_, _) => Update();
        helper.Events.GameLoop.DayEnding += (_, _) => RemoveCompanions();
        helper.Events.GameLoop.Saving += (_, _) => RemoveCompanions();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { _resumeHotel = false; _returnWhenFree = false; _lastError = null; };
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        GameLocation.RegisterTileAction("HarveyOuting", (_, args, who, _) =>
        {
            if (who != Game1.player || args.Length < 2 || !Context.IsWorldReady || Game1.eventUp)
                return false;
            HandlePlace(args[1]);
            return true;
        });
        TriggerActionManager.RegisterAction("marilynsinister.HarveyOverhaul.Core_BirthdayComplete",
            (string[] _, StardewValley.Delegates.TriggerActionContext _, out string? error) =>
            {
                error = null;
                if (Context.IsWorldReady)
                {
                    Game1.player.mailReceived.Remove(BirthdayPending);
                    Set("BirthdayCompletedYear", Game1.year.ToString());
                }
                return true;
            });
    }

    public void RefreshSettings()
    {
        if (Context.IsWorldReady)
            UpdateBirthday();
    }

    private int Today => (int)Game1.stats.DaysPlayed;
    private string? Get(string key) => Game1.player.modData.TryGetValue(Prefix + key, out string? value) ? value : null;
    private void Set(string key, string value) => Game1.player.modData[Prefix + key] = value;
    private int Number(string key, int fallback = -1) => int.TryParse(Get(key), out int value) ? value : fallback;
    private bool Together => Game1.player.friendshipData.TryGetValue("Harvey", out var friendship)
        && (friendship.IsDating() || friendship.IsEngaged() || friendship.IsMarried());
    private bool Active => Get("TripPhase") is "Touring" or "Overnight" or "Sunday";
    private static bool IsCity(string name) => name.StartsWith("HarveyZuzu", StringComparison.Ordinal);

    private void StartDay()
    {
        if (!Context.IsWorldReady)
            return;
        _lastError = null;
        if (Active)
        {
            if (!HarveyOutingRules.CanResume(Number("TripStart"), Today, Get("TripPhase")!))
            {
                _returnWhenFree = IsCity(Game1.currentLocation.Name);
                EndTrip();
            }
            else if (Today > Number("TripStart"))
            {
                Set("TripPhase", "Sunday");
                _resumeHotel = true;
            }
        }
        UpdateTripFlags();
        UpdateBirthday();
    }

    private void UpdateBirthday()
    {
        var player = Game1.player;
        var data = helper.GameContent.Load<Dictionary<string, string>>(ContextAsset);
        if (data.Count == 0)
            return; // CP-пак отсутствует: не создаём письма и ссылки на отсутствующие карты.
        bool birthday = config.EnablePlayerBirthday && HarveyOutingRules.IsBirthdayToday(
            helper.ModRegistry.IsLoaded(HappyBirthdayId), data.GetValueOrDefault("HappyBirthdayToday"),
            config.BirthdaySeason, config.BirthdayDay, Game1.currentSeason, Game1.dayOfMonth);
        if (HarveyOutingRules.ShouldQueueBirthday(birthday, Together, Game1.year, Number("BirthdayYear")))
        {
            Set("BirthdayYear", Game1.year.ToString());
            Set("BirthdayDeadline", (Today + 7).ToString());
            player.eventsSeen.Remove(BirthdayEvent);
            player.mailReceived.Add(BirthdayPending);
            player.mailReceived.Remove(Invite);
            if (!player.mailbox.Contains(Invite))
                player.mailbox.Add(Invite);
        }
        if (!config.EnablePlayerBirthday || !Together || Today > Number("BirthdayDeadline", int.MaxValue))
            player.mailReceived.Remove(BirthdayPending);
        string style = HarveyOutingRules.NormalizeStyle(Get("BirthdayStyle") ?? config.BirthdayStyle);
        foreach (string value in new[] { "Quiet", "Family", "Surprise" })
        {
            string flag = "HarveyBirthday_" + value;
            if (value == style)
                player.mailReceived.Add(flag);
            else
                player.mailReceived.Remove(flag);
        }
    }

    private void Update()
    {
        if (!Context.IsWorldReady)
            return;
        try
        {
            // Happy Birthday может закончить загрузку и выбор даты уже после SaveLoaded/DayStarted.
            UpdateBirthday();
            if (!Context.IsPlayerFree || Game1.eventUp)
                return;
            if (_returnWhenFree)
            {
                _returnWhenFree = false;
                ReturnToValley();
                return;
            }
            if (Active && Get("TripPhase") == "Overnight" && Today == Number("TripStart")
                && !Game1.player.isInBed.Value)
                Set("TripPhase", "Touring"); // отменённое ожидание сна в мультиплеере
            if (_resumeHotel)
            {
                _resumeHotel = false;
                // Госпитализация и обморок имеют приоритет над продолжением прогулки.
                if (Game1.currentLocation is FarmHouse || Game1.currentLocation.Name == Inn)
                    Game1.warpFarmer(Inn, 12, 13, 0);
                else if (!IsCity(Game1.currentLocation.Name))
                    EndTrip();
            }
            if (Active && !Together)
            {
                ReturnToValley();
                return;
            }
            if (Active && Get("TripPhase") == "Sunday" && Game1.timeOfDay >= 2200 && IsCity(Game1.currentLocation.Name))
            {
                ReturnToValley();
                Game1.drawObjectDialogue("Вечерний автобус привозит вас в долину. Харви проверяет, что вы забрали все покупки.");
                return;
            }
            EnsureCompanion();
            _lastError = null;
        }
        catch (Exception ex)
        {
            if (_lastError != ex.Message)
                monitor.Log("[HarveyOutings] " + ex.Message, LogLevel.Warn);
            _lastError = ex.Message;
        }
    }

    private void UpdateTripFlags()
    {
        foreach (string flag in new[] { "HarveyZuzu_Active", "HarveyZuzu_Sunday", BirthdayTripFlag })
            Game1.player.mailReceived.Remove(flag);
        if (Active)
            Game1.player.mailReceived.Add("HarveyZuzu_Active");
        if (Get("TripPhase") == "Sunday")
            Game1.player.mailReceived.Add("HarveyZuzu_Sunday");
        if (Active && Get("TripBirthday") == "true")
            Game1.player.mailReceived.Add(BirthdayTripFlag);
        // Сохранения, где поездка была до появления флага: дата возвращения уже записана.
        if (Number("TripLastReturn") > 0)
            Game1.player.mailReceived.Add(VisitedFlag);
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;
        RemoveCompanions();
        if (IsCity(e.OldLocation.Name) && !IsCity(e.NewLocation.Name) && Active)
            EndTrip();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree || Game1.eventUp)
            return;
        if (config.OpenHarveyOutings.JustPressed())
        {
            helper.Input.SuppressActiveKeybinds(config.OpenHarveyOutings);
            ShowMenu();
            return;
        }
        if (e.Button.IsActionButton() && !helper.Input.IsSuppressed(e.Button) && IsCity(Game1.currentLocation.Name))
        {
            var companion = Game1.currentLocation.characters.FirstOrDefault(npc => npc.Name == CompanionName
                && Vector2.Distance(npc.Tile, e.Cursor.GrabTile) <= 0.6f && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 2);
            if (companion == null)
                return;
            helper.Input.Suppress(e.Button);
            var data = helper.GameContent.Load<Dictionary<string, string>>(ContextAsset);
            string text = data.GetValueOrDefault("Talk_" + Game1.currentLocation.Name,
                "Я рад, что мы выбрались. Покажешь, куда теперь пойдём?$h");
            companion.CurrentDialogue.Push(new Dialogue(companion, null, text));
            Game1.drawDialogue(companion);
        }
    }

    private void ShowMenu()
    {
        if (helper.GameContent.Load<Dictionary<string, string>>(ContextAsset).Count == 0)
        {
            Game1.drawObjectDialogue("Для встреч нужен Harvey Overhaul [CP].");
            return;
        }
        var options = new List<Response>();
        if (IsCity(Game1.currentLocation.Name))
        {
            options.Add(new("Return", "Вернуться в долину"));
            if (Game1.currentLocation.Name == Inn && Active && Get("TripPhase") == "Touring")
                options.Add(new("Sleep", "Переночевать в гостинице"));
        }
        else if (Game1.player.mailReceived.Contains(BirthdayPending) && !Utility.isFestivalDay()
                 && Game1.timeOfDay is >= 1700 and <= 2400)
            options.Add(new("Birthday", "Пойти на свой праздник"));
        if (!IsCity(Game1.currentLocation.Name) && Game1.currentLocation.Name != PartyRoom)
            options.Add(new("Trip", "Выходные в Зузу"));
        options.Add(new("Style", "Как я хочу отпраздновать день рождения"));
        options.Add(new("Close", "Закрыть"));
        Ask("Встречи с Харви", options, answer =>
        {
            if (answer == "Return") ReturnToValley();
            else if (answer == "Sleep") HandlePlace("Sleep");
            else if (answer == "Birthday") Game1.warpFarmer(PartyRoom, 12, 14, 0);
            else if (answer == "Trip") OfferTrip();
            else if (answer == "Style") ChooseStyle();
        });
    }

    private static void Ask(string question, IEnumerable<Response> responses, Action<string> answer)
        => Game1.currentLocation.createQuestionDialogue(question, responses.ToArray(), (_, choice) => answer(choice));

    private void ChooseStyle()
        => Ask("Как тебе будет приятнее?", new[] { new Response("Quiet", "Тихий вечер с Харви"),
            new Response("Family", "Небольшой праздник с близкими"), new Response("Surprise", "Пусть приготовят сюрприз"),
            new Response("Cancel", "Решу позже") }, answer =>
        {
            if (answer == "Cancel") return;
            Set("BirthdayStyle", HarveyOutingRules.NormalizeStyle(answer));
            UpdateBirthday();
            Game1.drawObjectDialogue("Договорились. Пожелание сохранено для этой фермерши.");
        });

    private void OfferTrip()
    {
        bool festival = Utility.isFestivalDay() || Utility.isFestivalDay(Game1.dayOfMonth + 1,
            Enum.Parse<Season>(Game1.currentSeason, true));
        bool canGo = HarveyOutingRules.CanDepart(config.EnableZuzuTrips, Together,
            Game1.dayOfMonth % 7 == 6, festival, Game1.timeOfDay);
        if (!canGo || Game1.currentLocation.Name != "BusStop")
        {
            Game1.drawObjectDialogue("Харви предлагает выехать с автобусной остановки в субботу, с 8:00 до 13:00, "
                + "когда оба выходных свободны от праздников. Поездка доступна, когда вы встречаетесь или женаты.");
            return;
        }
        int fare = Math.Max(0, config.ZuzuTripFare);
        bool hisBirthday = HarveyOutingRules.IsHarveyBirthdayTrip(Game1.currentSeason, Game1.dayOfMonth);
        string question = hisBirthday
            ? $"Завтра у Харви день рождения. Увезти его в Зузу на выходные? Два билета и номер: {fare} з."
            : $"Два билета и номер на одну ночь: {fare} з. Вернуться можно раньше. Поехать?";
        Ask(question,
            new[] { new Response("Yes", "Поехать"), new Response("No", "В другой раз") }, answer =>
        {
            if (answer != "Yes") return;
            if (Game1.player.Money < fare)
            {
                Game1.drawObjectDialogue("На билеты и номер пока не хватает денег. Поездку можно отложить.");
                return;
            }
            Game1.player.Money -= fare;
            Set("TripStart", Today.ToString());
            Set("TripPhase", "Touring");
            Set("TripBirthday", hisBirthday ? "true" : "false");
            UpdateTripFlags();
            Game1.warpFarmer(Square, 18, 23, 0);
        });
    }

    private void HandlePlace(string place)
    {
        if (place == "Return") { ReturnToValley(); return; }
        if (place == "BirthdayReturn") { Game1.warpFarmer("Hospital", 10, 19, 0); return; }
        if (!Active && IsCity(Game1.currentLocation.Name))
        {
            Game1.drawObjectDialogue("Выход к автобусу — на юге площади. Вернуться можно и через меню встреч.");
            return;
        }
        if (place == "Sleep")
        {
            if (!Active || Get("TripPhase") != "Touring")
            {
                Game1.drawObjectDialogue("Ваш номер был забронирован на субботнюю ночь. Обратный автобус — у площади.");
                return;
            }
            Ask("Остаться в гостинице до утра? День завершится обычным сохранением.",
                new[] { new Response("Yes", "Лечь спать"), new Response("No", "Погулять ещё") }, answer =>
            {
                if (answer != "Yes") return;
                Set("TripPhase", "Overnight");
                Game1.player.isInBed.Value = true;
                Game1.player.sleptInTemporaryBed.Value = true;
                Game1.currentLocation.answerDialogueAction("Sleep_Yes", Array.Empty<string>());
            });
            return;
        }
        var data = helper.GameContent.Load<Dictionary<string, string>>(ContextAsset);
        Game1.drawObjectDialogue(data.GetValueOrDefault("Place_" + place, "Тихое место в большом городе."));
    }

    private void ReturnToValley()
    {
        EndTrip();
        Game1.warpFarmer("BusStop", 23, 23, 2);
    }

    private void EndTrip()
    {
        if (Active)
        {
            Game1.player.activeDialogueEvents["HarveyZuzu_Returned"] = 7;
            Set("TripLastReturn", Today.ToString());
            Game1.player.mailReceived.Add(VisitedFlag);
        }
        Set("TripBirthday", "false");
        Set("TripPhase", "None");
        _resumeHotel = false;
        UpdateTripFlags();
        RemoveCompanions();
    }

    private void RemoveCompanions()
    {
        foreach (var location in Game1.locations.Where(location => IsCity(location.Name)))
            for (int i = location.characters.Count - 1; i >= 0; i--)
                if (location.characters[i].Name == CompanionName)
                    location.characters.RemoveAt(i);
    }

    private void EnsureCompanion()
    {
        if (!Active || !_anchors.TryGetValue(Game1.currentLocation.Name, out var anchor)
            || Game1.currentLocation.characters.Any(npc => npc.Name == CompanionName))
            return;
        var realHarvey = Game1.getCharacterFromName("Harvey");
        var companion = new NPC(new AnimatedSprite("Characters/Harvey", 0, 16, 32), anchor.ToVector2() * 64,
            3, CompanionName) { displayName = "Харви", Portrait = realHarvey?.Portrait, EventActor = true };
        companion.currentLocation = Game1.currentLocation;
        Game1.currentLocation.characters.Add(companion);
    }
}

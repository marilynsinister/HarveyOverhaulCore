using System.Diagnostics;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Общий лимит HUD-сообщений модов Harvey Overhaul (травмы, стресс, темнота, ремиссия), чтобы в плохой день
/// они не шли потоком. Ванильные и чужие сообщения не трогаем.
/// • Повтор того же текста в течение 60 игровых минут — отбрасывается.
/// • Предупреждения и достижения (error/achievement) — всегда, кроме повторов.
/// • Остальные — не больше <see cref="InfoPerHour"/> за игровой час; лишние ждут в короткой очереди.
/// </summary>
public static class HudMessageLimiter
{
    private const int InfoPerHour = 2;
    private const int WindowMinutes = 60;
    private const int MaxQueued = 3;

    private static readonly HashSet<string> OwnAssemblies = new(StringComparer.Ordinal)
    {
        "HarveyOverhaulInjury",
        "HarveyStressMeter",
        "HarveyOverhaulCore",
    };

    private static readonly Dictionary<string, int> RecentTexts = new(StringComparer.Ordinal);
    private static readonly List<int> RecentInfoTimes = new();
    private static readonly LinkedList<HUDMessage> Queue = new();
    private static IMonitor? _monitor;
    private static bool _releasing;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        var method = AccessTools.Method(typeof(Game1), nameof(Game1.addHUDMessage), new[] { typeof(HUDMessage) });
        if (method == null)
        {
            monitor.Log("[HudLimiter] Game1.addHUDMessage не найден — лимит выключен", LogLevel.Warn);
            return;
        }

        harmony.Patch(method, prefix: new HarmonyMethod(typeof(HudMessageLimiter), nameof(Prefix)));
    }

    /// <summary>Раз в секунду: выпустить сообщение из очереди, если бюджет освободился.</summary>
    public static void OnOneSecond()
    {
        if (!Context.IsWorldReady || Queue.Count == 0)
            return;

        int now = GameMinutesNow();
        Prune(now);
        if (RecentInfoTimes.Count >= InfoPerHour)
            return;

        var next = Queue.First!.Value;
        Queue.RemoveFirst();
        RecentInfoTimes.Add(now);
        RecentTexts[next.message ?? ""] = now;

        _releasing = true;
        try
        {
            Game1.addHUDMessage(next);
        }
        finally
        {
            _releasing = false;
        }
    }

    public static void OnDayStarted()
    {
        RecentTexts.Clear();
        RecentInfoTimes.Clear();
        Queue.Clear();
    }

    private static bool Prefix(HUDMessage message)
    {
        if (_releasing || !Context.IsWorldReady || message == null || message.messageSubject != null || !IsFromOwnMod())
            return true;

        int now = GameMinutesNow();
        Prune(now);

        string text = message.message ?? "";
        if (RecentTexts.ContainsKey(text) || Queue.Any(q => q.message == text))
            return false;

        bool priority = message.whatType is HUDMessage.error_type or HUDMessage.achievement_type;
        if (priority || RecentInfoTimes.Count < InfoPerHour)
        {
            RecentTexts[text] = now;
            if (!priority)
                RecentInfoTimes.Add(now);
            return true;
        }

        if (Queue.Count >= MaxQueued)
            Queue.RemoveFirst();
        Queue.AddLast(message);
        _monitor?.Log($"[HudLimiter] отложено: {text}", LogLevel.Trace);
        return false;
    }

    private static void Prune(int now)
    {
        RecentInfoTimes.RemoveAll(t => now - t >= WindowMinutes);
        foreach (var key in RecentTexts.Where(p => now - p.Value >= WindowMinutes).Select(p => p.Key).ToList())
            RecentTexts.Remove(key);
    }

    private static int GameMinutesNow()
        => Game1.Date.TotalDays * 1440 + Utility.ConvertTimeToMinutes(Game1.timeOfDay);

    /// <summary>Первый не-игровой кадр стека принадлежит нашему моду (не StardewValley/SMAPI/Harmony).</summary>
    private static bool IsFromOwnMod()
    {
        var frames = new StackTrace(2, false).GetFrames();
        foreach (var frame in frames)
        {
            var type = frame.GetMethod()?.DeclaringType;
            if (type == null || type == typeof(HudMessageLimiter))
                continue;

            string? asm = type.Assembly.GetName().Name;
            if (asm is "Stardew Valley" or "StardewValley" or "StardewValley.GameData" or "0Harmony" or "MonoGame.Framework")
                continue;

            return asm != null && OwnAssemblies.Contains(asm);
        }

        return false;
    }
}

using System.Reflection;
using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Buffs;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Статистика для баланса: сколько раз за день появляются дебаффы Harvey Overhaul (травмы и стресс отдельно)
/// и с чем фермерша заканчивает день — по каждому режиму сложности. Только наблюдает, ничего не меняет.
/// Строки по дням — в data/balance/&lt;сохранение&gt;.json в папке Core; отчёт — консольная команда harvey_balance_report.
/// Свои дебаффы узнаём, как HarveyDifficultyBuffScaler, по иконке из Mods/marilynsinister.HarveyOverhaul….
/// </summary>
public sealed class HarveyBalanceStats(IModHelper helper, IMonitor monitor, ModConfig config)
{
    private const string OwnIconPrefix = "Mods/marilynsinister.HarveyOverhaul";

    private static readonly FieldInfo[] FloatFields = typeof(BuffAttributesData)
        .GetFields(BindingFlags.Public | BindingFlags.Instance)
        .Where(f => f.FieldType == typeof(float))
        .ToArray();

    private Dictionary<string, string>? _ownDebuffs; // id → Injury | Stress
    private readonly HashSet<string> _active = new(StringComparer.Ordinal);
    private int _injuryOnsets;
    private int _stressOnsets;

    public sealed class DayRecord
    {
        public int Day { get; set; }
        public string Date { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public int InjuryOnsets { get; set; }
        public int StressOnsets { get; set; }
        public int InjuryAtDayEnd { get; set; }
        public int StressAtDayEnd { get; set; }
    }

    public sealed class Summary
    {
        public string Difficulty { get; set; } = "";
        public int Days { get; set; }
        public double InjuriesPerWeek { get; set; }
        public double StressPerWeek { get; set; }
        public double ShareDaysEndingDebuffed { get; set; }
    }

    public void Initialize()
    {
        helper.Events.Content.AssetsInvalidated += (_, e) =>
        {
            if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo("Data/Buffs")))
                _ownDebuffs = null;
        };
        helper.Events.GameLoop.DayStarted += (_, _) => ResetDay();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => ResetDay();
        helper.Events.GameLoop.OneSecondUpdateTicked += (_, _) => Track();
        helper.Events.GameLoop.DayEnding += (_, _) => WriteDay();
        helper.ConsoleCommands.Add("harvey_balance_report",
            "Статистика дебаффов Harvey Overhaul по режимам сложности для текущего сохранения.", (_, _) => Report());
        helper.ConsoleCommands.Add("harvey_balance_reset",
            "Удалить собранную статистику баланса для текущего сохранения.", (_, _) => ResetFile());
    }

    /// <summary>Сводка по режимам: дебаффов в неделю и доля дней, которые заканчиваются с дебаффом.</summary>
    public static List<Summary> Summarize(IEnumerable<DayRecord> records)
        => records.GroupBy(r => r.Difficulty).OrderBy(g => g.Key).Select(g =>
        {
            int days = g.Count();
            return new Summary
            {
                Difficulty = g.Key,
                Days = days,
                InjuriesPerWeek = days == 0 ? 0 : g.Sum(r => r.InjuryOnsets) * 7.0 / days,
                StressPerWeek = days == 0 ? 0 : g.Sum(r => r.StressOnsets) * 7.0 / days,
                ShareDaysEndingDebuffed = days == 0 ? 0 : g.Count(r => r.InjuryAtDayEnd + r.StressAtDayEnd > 0) / (double)days,
            };
        }).ToList();

    /// <summary>Стресс — то, что ставит HarveyStressMeter (стресс, темнота, тусклый свет); остальное — травмы и неприятности.</summary>
    public static string Category(string buffId)
        => buffId.Contains("Stress", StringComparison.OrdinalIgnoreCase)
           || buffId.Contains("Darkness", StringComparison.OrdinalIgnoreCase)
           || buffId.Contains("DimLight", StringComparison.OrdinalIgnoreCase)
            ? "Stress" : "Injury";

    private string FilePath => $"data/balance/{Constants.SaveFolderName}.json";

    private void ResetDay()
    {
        _active.Clear();
        _injuryOnsets = 0;
        _stressOnsets = 0;
        if (Context.IsWorldReady)
            foreach (string id in OwnActiveDebuffs())
                _active.Add(id); // перешедшие с прошлого дня — не новые
    }

    private void Track()
    {
        if (!Context.IsWorldReady)
            return;
        try
        {
            var now = OwnActiveDebuffs().ToHashSet(StringComparer.Ordinal);
            foreach (string id in now.Where(id => !_active.Contains(id)))
            {
                if (OwnDebuffs()[id] == "Stress") _stressOnsets++;
                else _injuryOnsets++;
            }
            _active.Clear();
            _active.UnionWith(now);
        }
        catch (Exception ex)
        {
            monitor.LogOnce($"[Balance] Статистика не собирается: {ex.Message}", LogLevel.Trace);
        }
    }

    private void WriteDay()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Constants.SaveFolderName == null)
            return;
        try
        {
            var active = OwnActiveDebuffs().ToList();
            var records = helper.Data.ReadJsonFile<List<DayRecord>>(FilePath) ?? new();
            int day = (int)Game1.stats.DaysPlayed;
            records.RemoveAll(r => r.Day == day); // повторный конец дня после загрузки
            records.Add(new DayRecord
            {
                Day = day,
                Date = $"{Game1.currentSeason} {Game1.dayOfMonth}, год {Game1.year}",
                Difficulty = HarveyDifficulty.Normalize(config.Difficulty),
                InjuryOnsets = _injuryOnsets,
                StressOnsets = _stressOnsets,
                InjuryAtDayEnd = active.Count(id => OwnDebuffs()[id] == "Injury"),
                StressAtDayEnd = active.Count(id => OwnDebuffs()[id] == "Stress"),
            });
            helper.Data.WriteJsonFile(FilePath, records);
        }
        catch (Exception ex)
        {
            monitor.Log($"[Balance] Не удалось записать день: {ex.Message}", LogLevel.Trace);
        }
    }

    private void Report()
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Загрузите сохранение.", LogLevel.Info);
            return;
        }
        var records = helper.Data.ReadJsonFile<List<DayRecord>>(FilePath) ?? new();
        if (records.Count == 0)
        {
            monitor.Log("Статистики пока нет: она пишется в конце каждого игрового дня.", LogLevel.Info);
            return;
        }
        monitor.Log($"Баланс Harvey Overhaul, {Constants.SaveFolderName}: {records.Count} дн. (файл {FilePath})", LogLevel.Info);
        foreach (var s in Summarize(records))
            monitor.Log($"  {HarveyDifficulty.DisplayName(s.Difficulty)}: {s.Days} дн. · травм в неделю {s.InjuriesPerWeek:0.0} · "
                        + $"стресса в неделю {s.StressPerWeek:0.0} · дней, закончившихся с дебаффом, {s.ShareDaysEndingDebuffed:P0}",
                LogLevel.Info);
    }

    private void ResetFile()
    {
        if (!Context.IsWorldReady)
            return;
        helper.Data.WriteJsonFile<List<DayRecord>>(FilePath, new());
        monitor.Log("Статистика баланса для этого сохранения очищена.", LogLevel.Info);
    }

    private IEnumerable<string> OwnActiveDebuffs()
    {
        var own = OwnDebuffs();
        return Game1.player.buffs.AppliedBuffs.Keys.Where(own.ContainsKey);
    }

    private Dictionary<string, string> OwnDebuffs()
    {
        if (_ownDebuffs != null)
            return _ownDebuffs;
        _ownDebuffs = new(StringComparer.Ordinal);
        foreach (var (id, data) in DataLoader.Buffs(Game1.content))
        {
            if (data?.IconTexture?.StartsWith(OwnIconPrefix, StringComparison.OrdinalIgnoreCase) != true)
                continue;
            bool negative = data.Effects != null && FloatFields.Any(f => f.GetValue(data.Effects) is float v && v < 0);
            if (data.IsDebuff || negative)
                _ownDebuffs[id] = Category(id);
        }
        return _ownDebuffs;
    }
}

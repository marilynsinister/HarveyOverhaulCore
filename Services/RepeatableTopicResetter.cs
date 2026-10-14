using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// В 1.6 реплику по разговорному топику NPC произносит один раз за всю игру: после показа ставится
/// mailReceived «&lt;NPC&gt;_&lt;topic&gt;», и при повторном появлении топика NPC молчит.
/// Медицинские топики повторяются (вторая заноза, новая травма, новая фаза), поэтому при каждом
/// новом появлении topic*/HarveyMod_*-ключа флаг снимается. Не чаще раза в игровой день на ключ — защита от
/// зацикливания, если код переставляет топик сразу после реплики.
/// Ловит топики из любого источника: C#, CP $action, команды событий.
/// </summary>
public sealed class RepeatableTopicResetter
{
    private const int PollIntervalTicks = 30;

    private readonly IMonitor _monitor;
    private readonly HashSet<string> _knownTopics = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _lastResetDay = new(StringComparer.Ordinal);

    public RepeatableTopicResetter(IMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <summary>После загрузки все активные топики считаются новыми — так чинятся уже «сгоревшие» флаги.</summary>
    public void OnSaveLoaded()
    {
        _knownTopics.Clear();
        _lastResetDay.Clear();
        Poll();
    }

    public void OnUpdateTicked(uint ticks)
    {
        if (ticks % PollIntervalTicks == 0)
            Poll();
    }

    private void Poll()
    {
        if (!Context.IsWorldReady || Game1.player?.activeDialogueEvents is not { } active)
            return;

        int today = Game1.Date.TotalDays;
        var current = new HashSet<string>(StringComparer.Ordinal);

        foreach (string key in active.Keys)
        {
            if (!IsRepeatableTopic(key))
                continue;

            current.Add(key);
            if (_knownTopics.Contains(key))
                continue;

            if (_lastResetDay.TryGetValue(key, out int lastDay) && lastDay == today)
                continue;

            _lastResetDay[key] = today;
            int removed = Game1.player.mailReceived.RemoveWhere(flag => IsNpcTopicFlag(flag, key));
            if (removed > 0)
                _monitor.Log($"[TopicReset] {key}: снято {removed} флаг(ов) «уже сказано» — NPC снова отреагирует", LogLevel.Debug);
        }

        _knownTopics.Clear();
        _knownTopics.UnionWith(current);
    }

    /// <summary>Медицинские/сюжетные топики модов Харви: topic* и HarveyMod_* (например HarveyMod_RecoveryPlan_NeedVisit).</summary>
    private static bool IsRepeatableTopic(string key)
        => (key.StartsWith("topic", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("HarveyMod_", StringComparison.OrdinalIgnoreCase))
           && !key.Contains("_memory_", StringComparison.Ordinal);

    /// <summary>Флаг вида «&lt;NPC&gt;_&lt;topic&gt;», где префикс — реальный NPC (не questComplete_ и т.п.).</summary>
    private static bool IsNpcTopicFlag(string flag, string key)
    {
        if (flag.Length <= key.Length + 1 || !flag.EndsWith("_" + key, StringComparison.Ordinal))
            return false;

        string npcName = flag[..(flag.Length - key.Length - 1)];
        return Game1.characterData?.ContainsKey(npcName) == true;
    }
}

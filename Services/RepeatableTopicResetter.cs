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
/// «Новый» — значит, его не было при прошлой проверке. Список уже виденных топиков хранится в modData
/// игрока, поэтому загрузка сейва не делает все активные топики новыми: иначе Харви заново пересказывал бы
/// сюжетные топики (рассказ о городе фермера и т.п.) и уже сказанные медицинские реплики после каждой загрузки.
/// </summary>
public sealed class RepeatableTopicResetter
{
    private const int PollIntervalTicks = 30;
    private const string KnownTopicsKey = "marilynsinister.HarveyOverhaul.Core/KnownRepeatableTopics";

    private readonly IMonitor _monitor;
    private readonly HashSet<string> _knownTopics = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _lastResetDay = new(StringComparer.Ordinal);
    private bool _loaded;

    public RepeatableTopicResetter(IMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <summary>
    /// Восстановить список уже виденных топиков из сейва. В старых сейвах списка нет — тогда все активные
    /// топики считаются уже виденными (флаги не снимаются).
    /// </summary>
    public void OnSaveLoaded()
    {
        _knownTopics.Clear();
        _lastResetDay.Clear();
        _loaded = true;

        if (Game1.player?.modData.TryGetValue(KnownTopicsKey, out string? saved) == true)
        {
            _knownTopics.UnionWith(saved.Split('|', StringSplitOptions.RemoveEmptyEntries));
            Poll();
            return;
        }

        if (Game1.player?.activeDialogueEvents is { } active)
            _knownTopics.UnionWith(active.Keys.Where(IsRepeatableTopic));
        SaveKnownTopics();
    }

    public void OnReturnedToTitle()
    {
        _loaded = false;
        _knownTopics.Clear();
        _lastResetDay.Clear();
    }

    public void OnUpdateTicked(uint ticks)
    {
        if (ticks % PollIntervalTicks == 0)
            Poll();
    }

    private void Poll()
    {
        if (!_loaded || !Context.IsWorldReady || Game1.player?.activeDialogueEvents is not { } active)
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

        if (current.SetEquals(_knownTopics))
            return;

        _knownTopics.Clear();
        _knownTopics.UnionWith(current);
        SaveKnownTopics();
    }

    private void SaveKnownTopics()
    {
        if (Game1.player?.modData is { } modData)
            modData[KnownTopicsKey] = string.Join('|', _knownTopics.OrderBy(t => t, StringComparer.Ordinal));
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

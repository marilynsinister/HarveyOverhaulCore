namespace HarveyOverhaul.Core.Models;

/// <summary>
/// Одна сцена во вкладке «История». Данные приходят из CP-ассета
/// <c>Mods/marilynsinister.HarveyOverhaul/StoryJournal</c> (assets/Code/storyJournal.json),
/// поэтому новые сцены добавляются без пересборки Core.
/// </summary>
public sealed class HarveyStoryJournalEntry
{
    /// <summary>Арка (заголовок группы во вкладке).</summary>
    public string Arc { get; set; } = "";

    /// <summary>Порядок арки во вкладке (меньше — выше).</summary>
    public int ArcOrder { get; set; }

    /// <summary>Порядок сцены внутри арки.</summary>
    public int Order { get; set; }

    /// <summary>ID события; несколько через пробел — варианты одной сцены (Dating / Married и т.п.).</summary>
    public string EventIds { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>Одна строка «что было» — показывается после просмотра.</summary>
    public string Summary { get; set; } = "";

    /// <summary>Мягкая подсказка, где и когда искать сцену (без спойлеров).</summary>
    public string Hint { get; set; } = "";

    /// <summary>ID событий, которые должны быть просмотрены, чтобы сцена считалась «следующей» (через пробел).</summary>
    public string Requires { get; set; } = "";

    /// <summary>Необязательный GameStateQuery: пока ложен, подсказка не показывается (например, нужен брак).</summary>
    public string Condition { get; set; } = "";

    /// <summary>Повторяющаяся сцена (годовщина, ротация брака) — считается пройденной после первого раза.</summary>
    public bool Repeatable { get; set; }
}

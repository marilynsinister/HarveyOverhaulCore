using StardewValley;

namespace HarveyOverhaul.Core.Core;

/// <summary>Грамматический род игрока для русских текстов Харви (общий для Injury/Stress/Core).</summary>
public static class PlayerGrammar
{
    public static bool IsMale => Game1.player?.IsMale ?? false;

    /// <summary>Выбор формы по полу игрока: Gendered("один", "одна").</summary>
    public static string Gendered(string male, string female)
        => IsMale ? male : female;

    /// <summary>Первая буква строчная, имена внутри не трогаем: «Иди к Харви» → «иди к Харви».</summary>
    public static string LowerFirst(string? text)
        => string.IsNullOrEmpty(text) ? "" : char.ToLowerInvariant(text[0]) + text[1..];

    /// <summary>Первая буква заглавная: «найди Харви» → «Найди Харви».</summary>
    public static string UpperFirst(string? text)
        => string.IsNullOrEmpty(text) ? "" : char.ToUpperInvariant(text[0]) + text[1..];
}

namespace HarveyOverhaul.Core.Models;

/// <summary>
/// Общий режим сложности Harvey Overhaul (настройка Core). Один переключатель для всех частей мода:
/// сила штрафов дебаффов из CP (Data/Buffs), частота и длительность травм (Injury), рост стресса и его штрафы (Stress).
/// <see cref="PerMod"/> — каждая часть живёт по своим настройкам, как раньше.
/// </summary>
public static class HarveyDifficulty
{
    public const string Cozy = "Cozy";
    public const string Standard = "Standard";
    public const string Strict = "Strict";
    public const string PerMod = "PerMod";

    public static readonly string[] All = { Cozy, Standard, Strict, PerMod };

    public static string Normalize(string? value)
        => All.FirstOrDefault(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)) ?? Standard;

    /// <summary>Множитель отрицательных эффектов дебаффов (MaxStamina, удача, навыки…). Плюсы не меняются.</summary>
    public static float DebuffStrength(string difficulty) => difficulty switch
    {
        Cozy => 0.5f,
        Strict => 1.25f,
        _ => 1f,
    };

    /// <summary>Множитель роста стресса (Stress).</summary>
    public static float StressGain(string difficulty) => difficulty switch
    {
        Cozy => 0.75f,
        Strict => 1.15f,
        _ => 1f,
    };

    public static string DisplayName(string difficulty) => difficulty switch
    {
        Cozy => "Уютно",
        Standard => "Обычно",
        Strict => "Строго",
        PerMod => "Настройки каждой части",
        _ => difficulty,
    };
}

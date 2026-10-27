using StardewModdingAPI.Utilities;

namespace HarveyOverhaul.Core.Core;

public sealed class ModConfig
{
    public KeybindList OpenHarveyPanel { get; set; } = KeybindList.Parse("H");
    public KeybindList OpenHarveyOutings { get; set; } = KeybindList.Parse("J");
    public bool EnablePlayerBirthday { get; set; } = true;
    public string BirthdaySeason { get; set; } = "NotSet";
    public int BirthdayDay { get; set; } = 1;
    public string BirthdayStyle { get; set; } = "Quiet";
    public bool EnableZuzuTrips { get; set; } = true;
    public int ZuzuTripFare { get; set; } = 500;

    /// <summary>Показывать debug-строку внизу окна «План Харви».</summary>
    public bool DebugMode { get; set; }

    public bool AllowBasicTreatmentOutsideClinic { get; set; } = true;
    public bool AllowPhaseTransitionOutsideClinic { get; set; } = true;
    public bool AllowRecoveryOutsideClinic { get; set; } = true;
    public bool RequireClinicForSevereInjuries { get; set; } = true;
    public bool BlockLongTreatmentDuringFestivals { get; set; } = true;

    /// <summary>
    /// Общий режим сложности (HarveyDifficulty): Cozy, Standard, Strict или PerMod.
    /// Не PerMod — Injury и Stress берут пресет отсюда, а минусы дебаффов из CP масштабируются.
    /// </summary>
    public string Difficulty { get; set; } = HarveyOverhaul.Core.Models.HarveyDifficulty.Standard;

    /// <summary>Лестница последствий, ступень 4: если лечение запущено, Харви сам приходит на ферму.</summary>
    public bool EnableHouseCalls { get; set; } = true;

    /// <summary>Ступень 5: после повторного отказа — обморок от травмы или срыв от стресса. На «Уютно» не бывает.</summary>
    public bool EnableNeglectCollapse { get; set; } = true;
}

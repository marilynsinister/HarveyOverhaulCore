using StardewModdingAPI.Utilities;

namespace HarveyOverhaul.Core.Core;

public sealed class ModConfig
{
    public KeybindList OpenHarveyPanel { get; set; } = KeybindList.Parse("H");

    /// <summary>Показывать debug-строку внизу окна «План Харви».</summary>
    public bool DebugMode { get; set; }

    public bool AllowBasicTreatmentOutsideClinic { get; set; } = true;
    public bool AllowPhaseTransitionOutsideClinic { get; set; } = true;
    public bool AllowRecoveryOutsideClinic { get; set; } = true;
    public bool RequireClinicForSevereInjuries { get; set; } = true;
    public bool BlockLongTreatmentDuringFestivals { get; set; } = true;
}

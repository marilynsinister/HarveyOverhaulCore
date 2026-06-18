namespace HarveyOverhaul.Core.Models;

/// <summary>Категория медицинского интента Харви (приоритеты задаются в арбитре).</summary>
public enum HarveyMedicalIntentKind
{
    Injury,
    Stress,
    Recovery,
    PhaseTransition,
    Complication,
    Romantic,
}

/// <summary>Настройки клиники и фестивалей для медицинских разговоров.</summary>
public sealed class HarveyMedicalConversationSettings
{
    public bool AllowBasicTreatmentOutsideClinic { get; set; } = true;
    public bool AllowPhaseTransitionOutsideClinic { get; set; } = true;
    public bool AllowRecoveryOutsideClinic { get; set; } = true;
    public bool RequireClinicForSevereInjuries { get; set; } = true;
    public bool BlockLongTreatmentDuringFestivals { get; set; } = true;
}

/// <summary>Интент, зарегистрированный модом Injury или Stress (без прямых ссылок между модами).</summary>
public sealed class HarveyMedicalIntentRegistration
{
    public string ProviderId { get; init; } = "";
    public HarveyMedicalIntentKind Kind { get; init; }
    public string StateId { get; init; } = "";
    public int BasePriority { get; init; }
    public int DangerRank { get; init; }
    public int StateAgeTicks { get; init; }
    public bool IsPhaseReady { get; init; }
    public string ActionKey { get; init; } = "";
    public string TopicKey { get; init; } = "";
    /// <summary>Legacy/alias CP topics с тем же $action и stateId (напр. HarveyMod_TreatmentNeeded_*).</summary>
    public IReadOnlyList<string> AlternativeTopicKeys { get; init; } = Array.Empty<string>();
    public bool AllowDuringFestival { get; init; }
    public bool AllowOutsideClinic { get; init; }
    public bool RequiresClinic { get; init; }
    public bool IsEmergency { get; init; }
    public string FallbackLine { get; init; } = "";
    public string? FestivalDeferTopicKey { get; init; }
    public string? FestivalDeferLine { get; init; }
}

/// <summary>Отклонённый интент с причиной (для логов).</summary>
public sealed class HarveyMedicalIntentRejection
{
    public HarveyMedicalIntentRegistration Intent { get; init; } = null!;
    public string Reason { get; init; } = "";
}

/// <summary>Результат арбитража при клике / опросе.</summary>
public sealed class HarveyMedicalIntentResolution
{
    public HarveyMedicalIntentRegistration? Selected { get; init; }
    public IReadOnlyList<HarveyMedicalIntentRejection> Rejected { get; init; } = Array.Empty<HarveyMedicalIntentRejection>();
    public bool FestivalBlockedLongTreatment { get; init; }
    public string? ActiveFestivalDeferTopicKey { get; init; }
    public int ResolvedAtTick { get; init; }
}

public static class HarveyMedicalIntentPriorities
{
    public const int EmergencyInjury = 1000;
    public const int ReadyForRecovery = 900;
    /// <summary>Осложнения выше фазового перехода и выписки — сначала перевязка.</summary>
    public const int Complication = 920;
    public const int ReadyForNextPhase = 850;
    public const int UntreatedInjury = 800;
    public const int SevereStress = 500;
    public const int MinorStress = 300;
    public const int Romantic = 50;
}

/// <summary>Контекст для проверки клиники / фестиваля.</summary>
public sealed class HarveyMedicalContext
{
    public bool IsFestival { get; init; }
    public bool IsAtClinic { get; init; }
    public bool HarveyIsNearby { get; init; }
    /// <summary>Супруг Харви в той же локации — медицинский контекст как в клинике.</summary>
    public bool IsSpouseMedicalContext { get; init; }
}

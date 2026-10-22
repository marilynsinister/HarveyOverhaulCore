namespace HarveyOverhaul.Core.Api;

/// <summary>
/// Единое доверие Харви к тому, как фермерша соблюдает лечение (save-state владеет Stress).
/// Stress регистрирует реализацию в Core; Injury начисляет сюда очки за режим травм,
/// чтобы во вкладке «Доверие» была одна шкала, а не две.
/// </summary>
public interface IHarveyCareTrustApi
{
    /// <summary>Очки доверия.</summary>
    int Points { get; }

    /// <summary>Действующий уровень 0–4 (с потолком по отношениям): 0 — нет бонусов, 4 — опора.</summary>
    int Level { get; }

    /// <summary>Название уровня для игрока («Знакомый врач», «Опора»…).</summary>
    string LevelTitle { get; }

    /// <summary>Начислить очки. sourceProviderId — кто начисляет (для лога), reason — за что.</summary>
    void Award(string sourceProviderId, string reason, int points);

    /// <summary>Снять очки (с кулдауном и потолком штрафа владельца шкалы).</summary>
    void Penalize(string sourceProviderId, string reason, int points);

    /// <summary>
    /// Харви это не запрещено (HarveyCareConsentIds): «да» или ещё не спрашивал. «Пока нет» и «нет» он соблюдает.
    /// </summary>
    bool IsConsentAllowed(string consentId) => true;
}

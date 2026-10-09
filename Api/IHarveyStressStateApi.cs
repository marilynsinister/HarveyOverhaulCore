namespace HarveyOverhaul.Core.Api;

/// <summary>
/// Состояния стресса для других модов (save-state и баффы принадлежат Stress mod).
/// Stress регистрирует реализацию в Core, Injury получает её через IHarveyCoreApi.
/// Ключи условий — <see cref="HarveyOverhaul.Core.Models.HarveyStressConditions"/>.
/// </summary>
public interface IHarveyStressStateApi
{
    /// <summary>Есть ли у игрока сейчас это стресс-состояние (бафф, причина или активное лечение).</summary>
    bool HasCondition(string conditionId);

    /// <summary>
    /// Попросить Stress запустить реакцию (например, страх грозы). Решение — за Stress:
    /// учитываются иммунитет и уже идущее лечение. true — состояние активно после вызова.
    /// </summary>
    bool RequestReaction(string conditionId, string sourceProviderId);
}

using StardewModdingAPI;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Предупреждает в SMAPI-логе о модах, которые спорят с характером Харви или патчат те же события.
/// Ничего не отключает — только объясняет игроку, откуда может взяться «чужой» Харви.
/// </summary>
public static class HarveyModCompatibilityCheck
{
    /// <param name="RequireAll">true — предупреждать, только если загружены все моды (конфликт пары).</param>
    private sealed record KnownConflict(string[] UniqueIds, string Message, bool RequireAll = false);

    private static readonly KnownConflict[] Conflicts =
    {
        new(
            new[] { "Wink_Wonk_Wank_Wenk.ShyHarvey" },
            "Shy Harvey Dialogue переписывает ванильные реплики Харви и брака: там он застенчивый. " +
            "Harvey Overhaul заменяет большую часть этих ключей, но оставшиеся реплики будут звучать в другом характере."),
        new(
            new[] { "novust.harveyofftheclock" },
            "Harvey: Off The Clock — совместим по картам, но по характеру спорит с Harvey Overhaul " +
            "(там Харви неловкий и забывает о себе). Его сцены будут идти вперемешку с нашими."),
        new(
            new[] { "tar.HarveyMarriage", "Azurysu.HarveyMarriageRevamp" },
            "Harvey Marriage Expansion и его Revamp одновременно патчат событие 111111111 в HarveyRoom. " +
            "Оставьте один из двух.",
            RequireAll: true),
        new(
            new[] { "CSS.HarveyCares", "SweetAppletun.DirtyishHarvey" },
            "Harvey Cares / Too Sweet Harvey тоже дописывают брачные реплики Харви. " +
            "На совпадающих ключах побеждает Harvey Overhaul, остальные реплики звучат в тоне этих модов."),
    };

    public static void Run(IModRegistry registry, IMonitor monitor)
    {
        if (!registry.IsLoaded("marilynsinister.HarveyOverhaul"))
            return;

        foreach (var conflict in Conflicts)
        {
            bool hit = conflict.RequireAll
                ? conflict.UniqueIds.All(registry.IsLoaded)
                : conflict.UniqueIds.Any(registry.IsLoaded);

            if (hit)
                monitor.Log($"[Совместимость] {conflict.Message}", LogLevel.Warn);
        }
    }
}

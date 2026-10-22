using System.Reflection;
using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Buffs;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Масштабирует минусы дебаффов Harvey Overhaul по режиму сложности. Правка Data/Buffs после CP, поэтому
/// одинаково действует на баффы из событий, триггеров, Injury и Stress: все они читают Data/Buffs.
/// Свои баффы узнаём по иконке из Mods/marilynsinister.HarveyOverhaul…
/// </summary>
public sealed class HarveyDifficultyBuffScaler
{
    private const string OwnIconPrefix = "Mods/marilynsinister.HarveyOverhaul";

    /// <summary>Целые характеристики (уровни, выносливость, скорость…) — округляем; множители — нет.</summary>
    private static readonly HashSet<string> FractionalStats = new(StringComparer.Ordinal)
    {
        "AttackMultiplier", "KnockbackMultiplier", "WeaponSpeedMultiplier",
        "CriticalChanceMultiplier", "CriticalPowerMultiplier", "WeaponPrecisionMultiplier",
    };

    private static readonly FieldInfo[] FloatFields = typeof(BuffAttributesData)
        .GetFields(BindingFlags.Public | BindingFlags.Instance)
        .Where(f => f.FieldType == typeof(float))
        .ToArray();

    private readonly IMonitor _monitor;
    private readonly Func<string> _getDifficulty;

    public HarveyDifficultyBuffScaler(IMonitor monitor, Func<string> getDifficulty)
    {
        _monitor = monitor;
        _getDifficulty = getDifficulty;
    }

    public void OnAssetRequested(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Buffs"))
            return;

        float strength = HarveyDifficulty.DebuffStrength(_getDifficulty());
        if (Math.Abs(strength - 1f) < 0.001f)
            return;

        e.Edit(asset => Scale(asset.AsDictionary<string, BuffData>().Data, strength), AssetEditPriority.Late + 1000);
    }

    private void Scale(IDictionary<string, BuffData> buffs, float strength)
    {
        int changed = 0;
        foreach (var data in buffs.Values)
        {
            if (data?.Effects == null || data.IconTexture?.StartsWith(OwnIconPrefix, StringComparison.OrdinalIgnoreCase) != true)
                continue;

            bool any = false;
            foreach (var field in FloatFields)
            {
                float value = (float)field.GetValue(data.Effects)!;
                if (value >= 0)
                    continue;

                field.SetValue(data.Effects, ScaleValue(field.Name, value, strength));
                any = true;
            }

            if (any)
                changed++;
        }

        _monitor.Log($"[Difficulty] Data/Buffs: минусы {changed} дебаффов ×{strength:0.##}", LogLevel.Trace);
    }

    /// <summary>
    /// Мягче — к нулю (−1 к удаче при «Уютно» пропадает), строже — до ближайшего целого (−40 → −50).
    /// </summary>
    private static float ScaleValue(string stat, float value, float strength)
    {
        float scaled = value * strength;
        if (FractionalStats.Contains(stat))
            return scaled;

        return strength < 1f
            ? MathF.Truncate(scaled)
            : MathF.Round(scaled, MidpointRounding.AwayFromZero);
    }
}

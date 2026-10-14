using HarveyOverhaul.Core.Models;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// «Что можно сделать сегодня» во вкладке «Обзор»: 2–4 конкретных шага из текущих баффов
/// (травмы, неприятности, стресс), даже когда активного плана лечения нет.
/// Читает только ID баффов — не зависит от API модов травм и стресса.
/// </summary>
internal static class HarveyTodayChecklist
{
    private const int MaxItems = 4;
    private const string FirstAidKitRecipe = "marilynsinister.HarveyOverhaul_FirstAidKit";
    private const string FirstAidKitItem = "(O)marilynsinister.HarveyOverhaul_FirstAidKit";

    /// <summary>Порядок = приоритет: сначала то, что требует Харви или лечится сразу.</summary>
    private static readonly (string BuffId, string Step)[] BuffSteps =
    {
        ("HarveyMod_DoctorVisitNeeded", "Зайти к Харви — он ждёт на приём."),
        ("HarveyMod_DirtyWound", "Обработать рану антисептиком, пока не воспалилась."),
        ("HarveyMod_WetBandage", "Сменить намокшую повязку — чистый бинт."),
        ("HarveyMod_PainFlare", "Боль усилилась: обезболивающее или к Харви."),
        ("HarveyMod_Splinter", "Вытащить занозу: бинт или антисептик — или покажи Харви."),
        ("HarveyMod_BeeSting", "Укус пчелы: обезболивающее, отёк спадёт."),
        ("HarveyMod_Sunstroke", "Перегрелась: выпей воды или напиток и уйди в тень."),
        ("HarveyMod_Frostbite", "Замёрзли руки: горячий напиток или травяной сбор."),
        ("buffCold", "Простуда: тепло, травяной сбор и не мокнуть под дождём."),
        ("buffStressHunger", "Поесть — лучше что-то горячее."),
        ("buffStressLonely", "Поговорить с кем-нибудь из жителей."),
        ("buffStressTired", "Сделать паузу: посидеть, отдохнуть дома."),
        ("buffStressNoSleep", "Лечь пораньше — недосып копится."),
    };

    public static IReadOnlyList<HarveyPanelSectionViewModel> AppendTo(List<HarveyPanelSectionViewModel> sections)
    {
        var steps = BuildSteps();
        if (steps.Count > 0)
        {
            sections.Add(new HarveyPanelSectionViewModel
            {
                Headline = "Что можно сделать сегодня",
                BodyText = string.Join("\n", steps.Select(s => "• " + s)),
            });
        }

        return sections;
    }

    private static List<string> BuildSteps()
    {
        var player = Game1.player;
        if (player == null)
            return new List<string>();

        var buffs = player.buffs.AppliedBuffs;
        var steps = BuffSteps
            .Where(p => buffs.ContainsKey(p.BuffId))
            .Select(p => p.Step)
            .ToList();

        if (buffs.ContainsKey("buffStressLoadTier"))
        {
            if (Game1.timeOfDay < 2300)
                steps.Add("Лечь до 23:00 — утром стресс станет ниже.");
            steps.Add("Любимое дело снимет напряжение: рыбалка, закат, животные, вечер в салуне.");
        }

        if (player.craftingRecipes.ContainsKey(FirstAidKitRecipe) && !player.Items.ContainsId(FirstAidKitItem))
            steps.Add("Собираешься в шахту? Возьми аптечку — она смягчит травму.");

        if (steps.Count == 0 && buffs.ContainsKey("buffStressClearMind"))
            steps.Add("Стресса почти нет — «Ясная голова» даёт +1 к удаче. Так держать.");

        return steps.Distinct().Take(MaxItems).ToList();
    }
}

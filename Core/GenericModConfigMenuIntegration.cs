using HarveyOverhaul.Core.Models;
using StardewModdingAPI;

namespace HarveyOverhaul.Core.Core;

/// <summary>Подмножество API Generic Mod Config Menu (spacechase0.GenericModConfigMenu).</summary>
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

    void AddParagraph(IManifest mod, Func<string> text);

    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name,
        Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null,
        Func<int, string>? formatValue = null, string? fieldId = null);
    void AddKeybindList(IManifest mod, Func<StardewModdingAPI.Utilities.KeybindList> getValue,
        Action<StardewModdingAPI.Utilities.KeybindList> setValue, Func<string> name,
        Func<string>? tooltip = null, string? fieldId = null);
}

/// <summary>Меню настроек Core в GMCM (необязательная зависимость): общий режим сложности и клавиша окна.</summary>
internal static class GenericModConfigMenuIntegration
{
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action onSaved)
    {
        var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm == null)
            return;

        gmcm.Register(
            manifest,
            reset: () =>
            {
                config.Difficulty = new ModConfig().Difficulty;
                config.EnableHouseCalls = true;
                config.EnableNeglectCollapse = true;
                config.OpenHarveyOutings = new ModConfig().OpenHarveyOutings;
                config.EnablePlayerBirthday = true;
                config.BirthdaySeason = "NotSet";
                config.BirthdayDay = 1;
                config.BirthdayStyle = "Quiet";
                config.EnableZuzuTrips = true;
                config.ZuzuTripFare = 500;
            },
            save: () =>
            {
                config.Difficulty = HarveyDifficulty.Normalize(config.Difficulty);
                helper.WriteConfig(config);
                onSaved();
            });

        gmcm.AddSectionTitle(manifest, () => "Сложность Harvey Overhaul");
        gmcm.AddParagraph(manifest, () =>
            "Один переключатель для всего мода: травмы, стресс и сила дебаффов. "
            + "Сюжет, сцены и забота Харви от сложности не зависят.");
        gmcm.AddTextOption(
            manifest,
            () => HarveyDifficulty.Normalize(config.Difficulty),
            value => config.Difficulty = value,
            () => "Режим",
            () => "Уютно: травмы вдвое реже и заживают вдвое быстрее, стресс растёт медленнее, минусы дебаффов вдвое слабее.\n"
                  + "Обычно: как задумано.\n"
                  + "Строго: травмы в 1.5 раза чаще, лечение дольше, стресс растёт быстрее, минусы дебаффов на 25% сильнее.\n"
                  + "Настройки каждой части: сложность берётся из настроек Injury и Stress по отдельности.",
            HarveyDifficulty.All,
            HarveyDifficulty.DisplayName);
        gmcm.AddBoolOption(manifest, () => config.EnableHouseCalls, value => config.EnableHouseCalls = value,
            () => "Харви приходит сам",
            () => "Если травму или стресс долго не лечить, Харви утром сам придёт на ферму. "
                  + "Можно согласиться на осмотр или попросить пару дней.");
        gmcm.AddBoolOption(manifest, () => config.EnableNeglectCollapse, value => config.EnableNeglectCollapse = value,
            () => "Обморок, если тянуть дальше",
            () => "После второго отказа тело может не выдержать: обморок и день в клинике (травма) "
                  + "или срыв (стресс). Один раз на случай, без потери денег. На «Уютно» выключено всегда.");

        gmcm.AddSectionTitle(manifest, () => "День рождения и выходные");
        gmcm.AddParagraph(manifest, () => "Дата автоматически берётся из Happy Birthday. "
            + "Резервную дату ниже используют только сохранения без Happy Birthday. "
            + "Формат праздника для конкретной фермерши можно выбрать в меню встреч.");
        gmcm.AddKeybindList(manifest, () => config.OpenHarveyOutings, value => config.OpenHarveyOutings = value,
            () => "Меню встреч с Харви");
        gmcm.AddBoolOption(manifest, () => config.EnablePlayerBirthday, value => config.EnablePlayerBirthday = value,
            () => "Праздновать день рождения");
        gmcm.AddTextOption(manifest, () => config.BirthdaySeason, value => config.BirthdaySeason = value,
            () => "Резервный сезон рождения", allowedValues: new[] { "NotSet", "spring", "summer", "fall", "winter" },
            formatAllowedValue: value => value switch { "spring" => "Весна", "summer" => "Лето", "fall" => "Осень", "winter" => "Зима", _ => "Не выбран" });
        gmcm.AddNumberOption(manifest, () => config.BirthdayDay, value => config.BirthdayDay = value,
            () => "Резервное число рождения", min: 1, max: 28);
        gmcm.AddTextOption(manifest, () => config.BirthdayStyle, value => config.BirthdayStyle = value,
            () => "Формат по умолчанию", allowedValues: new[] { "Quiet", "Family", "Surprise" },
            formatAllowedValue: value => value switch { "Family" => "С близкими", "Surprise" => "Сюрприз", _ => "Тихий вечер" });
        gmcm.AddBoolOption(manifest, () => config.EnableZuzuTrips, value => config.EnableZuzuTrips = value,
            () => "Поездки в Зузу");
        gmcm.AddNumberOption(manifest, () => config.ZuzuTripFare, value => config.ZuzuTripFare = value,
            () => "Билеты и гостиница на двоих", min: 0, max: 5000, interval: 100);

    }
}

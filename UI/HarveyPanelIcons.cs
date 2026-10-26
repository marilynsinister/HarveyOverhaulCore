using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewUI.Graphics;
using StardewValley;
using StardewModdingAPI;
using HarveyOverhaul.Core.Models;

namespace HarveyOverhaul.Core.UI;

/// <summary>
/// Медицинские иллюстрации поставляются с Core. Портрет Харви учитывает Portraiture;
/// ванильные предметы остаются запасным вариантом. Текстуры получает SMAPI, без отдельного кеша.
/// </summary>
public static class HarveyPanelIcons
{
    private static IModContentHelper? _content;

    public static void Initialize(IModContentHelper content) => _content = content;

    /// <summary>Bundled medical illustrations; no dependency on the CP pack's item registry.</summary>
    public static Sprite? Illustration(string key)
    {
        if (key == "portrait")
            return HarveyPortrait();
        if (_content == null)
            return null;
        try
        {
            if (key == "book")
            {
                var book = _content.Load<Texture2D>("assets/sprites/planBook.png");
                return new Sprite(book, new Rectangle(0, 0, 16, 16));
            }
            int index = key switch
            {
                "bandage" => 0, "antiseptic" => 1, "painkiller" => 2,
                "tea" => 3, "kit" => 4, "ointment" => 5,
                "lantern" => 6, "bag" => 7, "broth" => 8,
                _ => -1,
            };
            if (index < 0)
                return null;
            var sheet = _content.Load<Texture2D>("assets/sprites/planItems.png");
            return new Sprite(sheet, new Rectangle(index * 16, 0, 16, 16));
        }
        catch
        {
            return null; // Missing illustration must never prevent opening the plan.
        }
    }

    public static string ForDirective(HarveyCareDirective directive)
    {
        string text = $"{directive.Id} {directive.Title} {directive.Text}";
        if (Has(text, "бинт") || Has(text, "повяз") || Has(text, "bandage")) return "bandage";
        if (Has(text, "антисеп") || Has(text, "antiseptic")) return "antiseptic";
        if (Has(text, "обезбол") || Has(text, "painkiller")) return "painkiller";
        if (Has(text, "мазь") || Has(text, "рубц") || Has(text, "ointment")) return "ointment";
        if (Has(text, "сон") || Has(text, "сна") || Has(text, "лечь") || Has(text, "sleep")
            || Has(text, "темнот") || Has(text, "darkness") || Has(text, "фонар")) return "lantern";
        if (Has(text, "бульон") || Has(text, "еда") || Has(text, "поешь") || Has(text, "meal")) return "broth";
        if (directive.Type == HarveyCareDirectiveType.Appointment) return "bag";
        if (Has(text, "чай") || Has(text, "отдых") || directive.Source == HarveyCareDirectiveSource.Stress) return "tea";
        return directive.Source == HarveyCareDirectiveSource.Injury ? "kit" : "book";
    }

    /// <summary>Иконка вкладки по её ключу (HarveyPanelTab).</summary>
    public static Sprite? ForTab(string tabKey) => tabKey switch
    {
        "Overview" => HarveyPortrait(),
        "Stress" => Illustration("tea") ?? Item("(O)614"),
        "Injuries" => Illustration("bandage") ?? Item("(O)351"),
        "Plan" => Illustration("book") ?? Item("(O)102"),
        "Trust" => Item("(O)458"),       // букет
        "Handbook" => Illustration("book") ?? Item("(O)326"),
        "Story" => Item("(O)402"),       // душистый горошек — память о сценах
        _ => null,
    };

    /// <summary>Иконка секции: по смыслу заголовка, иначе — иконка вкладки.</summary>
    public static Sprite? ForSection(string headline, string tabKey)
    {
        string h = headline ?? "";
        string? illustration =
            Has(h, "Сначала") || Has(h, "сделай сейчас") ? "kit" :
            Has(h, "Следующие шаги") || Has(h, "визит к Харви") ? "bag" :
            Has(h, "Режим") || Has(h, "Стресс") ? "tea" :
            Has(h, "Ограничения") || Has(h, "нельзя") || Has(h, "Успей") ? "lantern" :
            Has(h, "Травм") ? "bandage" :
            Has(h, "Предупрежд") || Has(h, "пошло не так") ? "kit" :
            Has(h, "дальше") || Has(h, "Выполнено") ? "book" : null;

        return illustration != null ? Illustration(illustration) ?? ForTab(tabKey) : ForTab(tabKey);
    }

    public static Sprite? HarveyPortrait()
    {
        Sprite? sprite = null;
        try
        {
            var texture = PortraitureTexture() ?? Game1.content.Load<Texture2D>("Portraits/Harvey");
            // Портреты — сетка 2 колонки; HD-моды увеличивают размер, поэтому берём половину ширины.
            int size = texture.Width / 2;
            sprite = new Sprite(texture, new Rectangle(0, 0, size, size));
        }
        catch
        {
            // нет портрета — без иконки
        }

        return sprite;
    }

    private static System.Reflection.MethodInfo? _portraitureGetPortrait;
    private static bool _portraitureResolved;

    /// <summary>
    /// HD-портрет из активного пака Portraiture (с учётом локации/сезона: Harvey_Hospital, Harvey_Winter и вариантов PortraiturePlus).
    /// Portraiture рисует поверх ванили в диалогах, но <c>Portraits/Harvey</c> не подменяет, поэтому спрашиваем его напрямую.
    /// null — Portraiture нет, выбран ванильный пак или для Харви нет портрета.
    /// </summary>
    private static Texture2D? PortraitureTexture()
    {
        if (!_portraitureResolved)
        {
            _portraitureResolved = true;
            var loader = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Portraiture")
                ?.GetType("Portraiture.TextureLoader");
            _portraitureGetPortrait = loader?.GetMethod(
                "getPortrait",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(NPC), typeof(Texture2D) },
                null);
        }

        if (_portraitureGetPortrait == null || Game1.currentLocation == null)
            return null;

        try
        {
            var harvey = Game1.getCharacterFromName("Harvey");
            if (harvey == null)
                return null;
            return _portraitureGetPortrait.Invoke(null, new object?[] { harvey, harvey.Portrait }) as Texture2D;
        }
        catch
        {
            return null; // другая версия Portraiture — остаёмся на ванильном портрете
        }
    }

    private static Sprite? Item(string qualifiedId)
    {
        Sprite? sprite = null;
        try
        {
            var data = ItemRegistry.GetData(qualifiedId);
            if (data != null)
                sprite = new Sprite(data.GetTexture(), data.GetSourceRect());
        }
        catch
        {
            // предмет недоступен — без иконки
        }

        return sprite;
    }

    private static bool Has(string text, string part) =>
        text.Contains(part, StringComparison.OrdinalIgnoreCase);
}

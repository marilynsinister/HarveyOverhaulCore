using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewUI.Graphics;
using StardewValley;

namespace HarveyOverhaul.Core.UI;

/// <summary>
/// Иконки окна «План Харви». Берём спрайты ванильных предметов через реестр — они есть в любой игре
/// и не зависят от координат на Cursors. Без кеша: другой мод может подменить текстуру. Портрет Харви — для «Совета Харви» и вкладки «Обзор».
/// </summary>
public static class HarveyPanelIcons
{
    /// <summary>Иконка вкладки по её ключу (HarveyPanelTab).</summary>
    public static Sprite? ForTab(string tabKey) => tabKey switch
    {
        "Overview" => HarveyPortrait(),
        "Stress" => Item("(O)614"),      // зелёный чай
        "Injuries" => Item("(O)351"),    // мышечный бальзам
        "Plan" => Item("(O)102"),        // потерянная книга — журнал
        "Trust" => Item("(O)458"),       // букет
        _ => null,
    };

    /// <summary>Иконка секции: по смыслу заголовка, иначе — иконка вкладки.</summary>
    public static Sprite? ForSection(string headline, string tabKey)
    {
        string h = headline ?? "";
        string? itemId =
            Has(h, "сделай сейчас") ? "(O)773" :                       // эликсир — срочно
            Has(h, "затем") || Has(h, "визит к Харви") ? "(O)349" :     // тоник — следующий шаг
            Has(h, "Режим") ? "(O)395" :                                 // кофе — распорядок дня
            Has(h, "нельзя") || Has(h, "не засчит") ? "(O)769" :         // эссенция пустоты — запрет / срыв
            Has(h, "Успей") ? "(O)787" :                                 // батарея — до конца дня
            Has(h, "Предупрежд") ? "(O)768" :                            // солнечная эссенция — внимание
            Has(h, "Травм") ? "(O)351" :
            Has(h, "Стресс") ? "(O)614" :
            Has(h, "дальше") ? "(O)434" :                                // звёздный плод — что впереди
            Has(h, "спокойно") ? "(O)591" :                              // тюльпан
            null;

        return itemId != null ? Item(itemId) : ForTab(tabKey);
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

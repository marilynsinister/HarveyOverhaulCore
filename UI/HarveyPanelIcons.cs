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
            var texture = Game1.content.Load<Texture2D>("Portraits/Harvey");
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

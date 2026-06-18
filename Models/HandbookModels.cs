using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewUI.Graphics;

namespace HarveyOverhaul.Core.Models;

public sealed class SpriteView
{
    public Texture2D Texture { get; init; } = null!;
    public Rectangle? SourceRect { get; init; }

    public SpriteView(Texture2D texture, Rectangle sourceRect)
    {
        Texture = texture;
        SourceRect = sourceRect;
    }

    public Sprite ToSprite()
    {
        Rectangle rect = SourceRect ?? new Rectangle(0, 0, Texture.Width, Texture.Height);
        return new Sprite(Texture, rect);
    }
}

public sealed class HandbookRow
{
    public SpriteView IconSprite { get; init; } = null!;
    public Sprite HandbookIcon => IconSprite.ToSprite();
    public string IconResource { get; init; } = "";
    public string Title { get; init; } = "";
    public string Effects { get; init; } = "";
    public string Causes { get; init; } = "";
    public string CureSummary { get; init; } = "";
    public string StatusText { get; set; } = "";
    public string BuffId { get; init; } = "";
    public string TreatmentStageText { get; set; } = "";
    public string StatusColor { get; set; } = "#7f6139";
}

public sealed class HandbookViewModel
{
    public List<HandbookRow> ActiveStates { get; } = new();
    public List<HandbookRow> AllStates { get; } = new();
}

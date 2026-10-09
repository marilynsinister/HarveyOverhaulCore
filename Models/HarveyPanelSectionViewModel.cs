namespace HarveyOverhaul.Core.Models;

public sealed class HarveyPanelSectionViewModel
{
    public string Headline { get; set; } = "";
    public string StatusLine { get; set; } = "";
    public string BodyText { get; set; } = "";
    public string AccentColor { get; set; } = "#3b2a1a";
    public string StatusColor { get; set; } = "#7f6139";

    public bool HasHeadline => !string.IsNullOrWhiteSpace(Headline);
    public bool HasStatusLine => !string.IsNullOrWhiteSpace(StatusLine);
    public bool HasBodyText => !string.IsNullOrWhiteSpace(BodyText);
}

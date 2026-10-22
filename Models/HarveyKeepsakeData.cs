namespace HarveyOverhaul.Core.Models;

/// <summary>Описание памятной вещи и варианты, выбранные в её сцене.</summary>
public sealed class HarveyKeepsakeData
{
    public string Description { get; set; } = "";
    public string HarveyLine { get; set; } = "";
    public string ArendLine { get; set; } = "";
    public List<HarveyKeepsakeChoice> Choices { get; set; } = new();

    public string Describe(Func<string, bool> hasFlag)
        => Choices.FirstOrDefault(choice => hasFlag(choice.Flag))?.Description ?? Description;
}

public sealed class HarveyKeepsakeChoice
{
    public string Flag { get; set; } = "";
    public string Description { get; set; } = "";
}

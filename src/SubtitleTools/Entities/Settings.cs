namespace SubtitleTools.Entities;

[ExcludeFromCodeCoverage]
public class Settings
{
    public bool AutoCreateBackup { get; init; }
    public int OptimalCharactersPerSecond { get; set; }
}
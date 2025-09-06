namespace SubtitleTools.Entities;

[ExcludeFromCodeCoverage]
public class Paragraph
{
    public int Number { get; init; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public List<string> Lines { get; init; } = [];
}
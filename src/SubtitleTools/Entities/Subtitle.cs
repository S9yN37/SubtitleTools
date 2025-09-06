namespace SubtitleTools.Entities;

[ExcludeFromCodeCoverage]
public class Subtitle
{
    public required string Type { get; init; }
    public required string OriginalFile { get; init; }
    public string Extension => Path.GetExtension(OriginalFile);
    public List<Paragraph> Paragraphs { get; set; } = [];
}
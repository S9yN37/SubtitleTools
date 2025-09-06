namespace SubtitleTools.SubtitleFormats;

public interface ISubtitleFormat
{
    public string Name { get; }
    Subtitle Read(string file, string[] lines);
    IReadOnlyCollection<string> Content(Subtitle subtitle);
}
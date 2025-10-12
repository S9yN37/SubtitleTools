namespace SubtitleTools.SubtitleFormats;

public interface ISubtitleFormat
{
    public string Name { get; }
    Subtitle Read(string file, IReadOnlyList<string> lines);
    IReadOnlyCollection<string> Content(Subtitle subtitle);
}
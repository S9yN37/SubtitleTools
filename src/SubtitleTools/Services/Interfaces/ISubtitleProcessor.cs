namespace SubtitleTools.Services.Interfaces;

public interface ISubtitleProcessor
{
    Task<Subtitle> Load(string fileName);
    Task Save(Subtitle subtitle, string? fileName = null);
    bool Validate(Subtitle subtitle);
    void Sync(Subtitle subtitle, double seconds);
    void Sync(Subtitle subtitle, IReadOnlyList<Segment> segments);
    void Sync(Subtitle subtitle, VisualPoint first, VisualPoint last);
}
namespace SubtitleTools.Services;

public class SubtitleProcessor(
    IFileSystem fileSystem,
    ISubtitleFormatFactory readerFactory,
    IOptions<Settings> options) : ISubtitleProcessor
{
    private readonly Settings _settings = options.Value;

    public async Task<Subtitle> Load(string fileName)
    {
        var parser = readerFactory.GetParser(fileName);
        var lines = await fileSystem.ReadLines(fileName);
        var subtitle = parser.Read(fileName, lines);
        return subtitle;
    }

    public async Task Save(Subtitle subtitle, string? fileName = null)
    {
        if (_settings.AutoCreateBackup)
            fileSystem.Backup(subtitle.OriginalFile);

        var parser = readerFactory.GetParser(subtitle.Extension);
        var file = string.IsNullOrEmpty(fileName) ? subtitle.OriginalFile : fileName;
        var lines = parser.Content(subtitle);
        await fileSystem.WriteLines(file, lines);
    }

    public bool Validate(Subtitle subtitle)
    {
        var valid = true;
        foreach (var p in subtitle.Paragraphs.Where(paragraph => !paragraph.Valid()))
        {
            Console.WriteLine($@"Paragraph {p.Number} is not valid. Range: {p.Start:hh\:mm\:ss},{p.Start:fff} --> {p.End:hh\:mm\:ss},{p.End:fff}, LinesCount: {p.Lines.Count}");
            valid = false;
        }

        return valid;
    }

    public void Sync(Subtitle subtitle, double seconds)
    {
        if (Math.Abs(seconds) < 0.001) return;
        foreach (var paragraph in subtitle.Paragraphs)
            paragraph.Sync(seconds);
    }

    public void Sync(Subtitle subtitle, IReadOnlyList<Segment> segments)
    {
        if (segments.Count == 0) return;
        var end = subtitle.Paragraphs[^1].End;
        var ranges = CreateRanges(segments, end);
        foreach (var range in ranges)
        {
            if (range is { Offset: 0 }) continue;
            foreach (var paragraph in subtitle.Paragraphs.Where(p => p.Start >= range.From && p.End <= range.To))
                paragraph.Sync(range.Offset);
        }
    }

    public void Sync(Subtitle subtitle, VisualPoint first, VisualPoint last)
    {
        var firstParagraph = subtitle.Paragraphs.First(p => p.Number == first.Paragraph);
        var lastParagraph = subtitle.Paragraphs.First(p => p.Number == last.Paragraph);

        // Get original timestamps for the reference points
        var originalFirst = firstParagraph.Start;
        var originalSecond = lastParagraph.Start;

        // Linear Interpolation
        var originalDelta = (originalSecond - originalFirst).TotalMilliseconds;
        var newDelta = (last.NewStart - first.NewStart).TotalMilliseconds;

        if (Math.Abs(originalDelta) < 0.001) // Avoid division by zero
            throw new ArgumentException("Reference points cannot have the same original timestamp");

        var scale = newDelta / originalDelta;
        var offset = first.NewStart.TotalMilliseconds - scale * originalFirst.TotalMilliseconds;

        foreach (var paragraph in subtitle.Paragraphs)
            paragraph.Sync(scale, offset);
    }

    private static List<SegmentRange> CreateRanges(IReadOnlyList<Segment> segments, TimeSpan end)
    {
        var ranges = new List<SegmentRange>();

        // if (segments.Count == 0) return ranges;

        // Create the first range from first segment to either the next segment or the end
        var current = new SegmentRange
        {
            From = segments[0].From,
            To = segments.Count > 1 ? segments[1].From : end,
            Offset = segments[0].Offset
        };

        for (var i = 1; i < segments.Count; i++)
        {
            // Add the current range
            ranges.Add(current);

            // Create the next range
            current = new SegmentRange
            {
                From = segments[i].From,
                To = i + 1 < segments.Count ? segments[i + 1].From : end,
                Offset = segments[i].Offset
            };
        }

        // Add the final range
        ranges.Add(current);
        return ranges;
    }
}
namespace SubtitleTools.SubtitleFormats;

public class SubRip : ISubtitleFormat
{
    private static readonly Regex TimestampPattern = SubRipParser.TimestampRegex();
    private static readonly Regex TimeComponentPattern = SubRipParser.TimeComponentRegex();
    public string Name => "SubRip";

    public Subtitle Read(string file, IReadOnlyList<string> lines)
    {
        var subtitle = new Subtitle { Type = Name, OriginalFile = file };

        var paragraph = new Paragraph();
        var expectingNewParagraph = true;

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                expectingNewParagraph = true;
                continue;
            }

            if (int.TryParse(line, out var number) && expectingNewParagraph)
            {
                paragraph = new Paragraph { Number = number };
                subtitle.Paragraphs.Add(paragraph);
                expectingNewParagraph = false;
                continue;
            }

            var timeStampParse = ParseTimestampRange(line);
            if (timeStampParse.Success)
            {
                paragraph.Start = timeStampParse.Start;
                paragraph.End = timeStampParse.End;
                expectingNewParagraph = false;
                continue;
            }

            paragraph.Lines.Add(line);
        }

        return subtitle;
    }

    public IReadOnlyCollection<string> Content(Subtitle subtitle)
    {
        var lines = new List<string>();
        foreach (var paragraph in subtitle.Paragraphs)
        {
            lines.Add($"{paragraph.Number}");
            lines.Add(TimeStamp(paragraph));
            lines.AddRange(paragraph.Lines);
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static (bool Success, TimeSpan Start, TimeSpan End) ParseTimestampRange(string input)
    {
        var match = TimestampPattern.Match(input);

        if (!match.Success)
            return (false, TimeSpan.Zero, TimeSpan.Zero);

        var startTime = ParseSrtTimeToTimeSpan(match.Groups["start"].Value);
        var endTime = ParseSrtTimeToTimeSpan(match.Groups["end"].Value);

        return (true, startTime, endTime);
    }

    private static TimeSpan ParseSrtTimeToTimeSpan(string srtTime)
    {
        var match = TimeComponentPattern.Match(srtTime);

        // if (!match.Success)
        //     throw new ArgumentException($"{Name}: Invalid timestamp format");

        var hours = int.Parse(match.Groups["hours"].Value);
        var minutes = int.Parse(match.Groups["minutes"].Value);
        var seconds = int.Parse(match.Groups["seconds"].Value);
        var milliseconds = int.Parse(match.Groups["milliseconds"].Value);

        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    private static string TimeStamp(Paragraph p)
    {
        return $@"{p.Start:hh\:mm\:ss},{p.Start:fff} --> {p.End:hh\:mm\:ss},{p.End:fff}";
    }
}

[ExcludeFromCodeCoverage]
public static partial class SubRipParser
{
    [GeneratedRegex(@"(?<start>\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(?<end>\d{2}:\d{2}:\d{2},\d{3})", RegexOptions.Compiled)]
    public static partial Regex TimestampRegex();

    [GeneratedRegex(@"(?<hours>\d{2}):(?<minutes>\d{2}):(?<seconds>\d{2}),(?<milliseconds>\d{3})", RegexOptions.Compiled)]
    public static partial Regex TimeComponentRegex();
}
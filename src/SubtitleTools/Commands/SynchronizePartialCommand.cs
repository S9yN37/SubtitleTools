namespace SubtitleTools.Commands;

[Command("sp", Description = "Synchronize Partial")]
public class SynchronizePartialCommand(ISubtitleProcessor processor, IFileSystem fileSystem) : ICommand
{
    private static readonly Regex SegmentPattern = AdjustPartialCommandParser.SegmentRegex();

    [CommandOption("segments", 's', Description = "TimeSpan/Offset pair in format 'h:m:s(+/-)o'", IsRequired = true)]
    public required IReadOnlyList<string> Adjustments { get; init; }

    [CommandOption("file-name", 'f', Description = "FileName", IsRequired = true)]
    public required string FileName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!fileSystem.FileExists(FileName))
        {
            await console.Output.WriteLineAsync("File not found");
            return;
        }

        var (segments, errors) = GetSegments();
        if (errors.Count > 0)
        {
            await console.Output.WriteLineAsync(string.Join(Environment.NewLine, errors));
            return;
        }

        var subtitle = await processor.Load(FileName);
        var valid = processor.Validate(subtitle);
        if (valid)
        {
            processor.Sync(subtitle, segments);
            await processor.Save(subtitle);
            await console.Output.WriteLineAsync("Subtitle synchronized successfully");
        }
    }

    private (IReadOnlyList<Segment> segments, IReadOnlyList<string> errors) GetSegments()
    {
        var segments = new List<Segment>();
        var errors = new List<string>();

        foreach (var adjustment in Adjustments)
        {
            var input = adjustment.Trim();
            var match = SegmentPattern.Match(input);

            if (match.Success)
            {
                // Extract time components using named groups
                var hours = int.Parse(match.Groups["hours"].Value);
                var minutes = int.Parse(match.Groups["minutes"].Value);
                var seconds = int.Parse(match.Groups["seconds"].Value);

                // Parse offset value
                if (double.TryParse(match.Groups["offset"].Value, out var offset))
                {
                    // Apply sign to offset
                    if (match.Groups["sign"].Value == "-") offset = -offset;

                    // Create segment
                    var segment = new Segment { From = new TimeSpan(hours, minutes, seconds), Offset = offset };

                    segments.Add(segment);
                }
                else
                {
                    errors.Add($"Could not parse offset value '{match.Groups["offset"].Value}' in '{input}'");
                }
            }
            else
            {
                errors.Add($"Could not parse segment string '{input}'");
            }
        }

        return (segments, errors);
    }
}

[ExcludeFromCodeCoverage]
public static partial class AdjustPartialCommandParser
{
    // Match (hh:mm:ss) followed by +/- and offset
    [GeneratedRegex(@"^(?<hours>\d{1,2}):(?<minutes>\d{1,2}):(?<seconds>\d{1,2})(?<sign>[\+\-])(?<offset>.+)$")]
    public static partial Regex SegmentRegex();
}
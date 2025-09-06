namespace SubtitleTools.Commands;

[Command("vs", Description = "Visual Sync")]
public class VisualSyncCommand(ISubtitleProcessor processor, IFileSystem fileSystem) : ICommand
{
    [CommandOption("fp", Description = "First paragraph number", IsRequired = true)]
    public required int FirstParagraph { get; init; }

    [CommandOption("ft", Description = "First visual point time (format: HH:mm:ss.fff or ss.fff)", IsRequired = true)]
    public required string FirstTime { get; init; }

    [CommandOption("lp", Description = "Last paragraph number", IsRequired = true)]
    public required int LastParagraph { get; init; }

    [CommandOption("lt", Description = "Last visual point time (format: HH:mm:ss.fff or ss.fff)", IsRequired = true)]
    public required string LastTime { get; init; }

    [CommandOption("file-name", 'f', Description = "FileName", IsRequired = true)]
    public required string FileName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!fileSystem.FileExists(FileName))
        {
            await console.Output.WriteLineAsync("File not found");
            return;
        }

        // Validate paragraph numbers are positive
        if (FirstParagraph <= 0 || LastParagraph <= 0 || FirstParagraph == LastParagraph)
        {
            await console.Output.WriteLineAsync($"Invalid paragraphs {FirstParagraph} and {LastParagraph}");
            return;
        }

        // Parse time strings to TimeSpan
        if (!TryParseTimeSpan(FirstTime, out var firstTimeSpan))
        {
            await console.Output.WriteLineAsync($"Invalid time format for first visual point: {FirstTime}");
            return;
        }

        if (!TryParseTimeSpan(LastTime, out var lastTimeSpan))
        {
            await console.Output.WriteLineAsync($"Invalid time format for last visual point: {LastTime}");
            return;
        }

        // Create visual points
        var first = new VisualPoint { Paragraph = FirstParagraph, NewStart = firstTimeSpan };
        var last = new VisualPoint { Paragraph = LastParagraph, NewStart = lastTimeSpan };

        var subtitle = await processor.Load(FileName);

        if (subtitle.Paragraphs.All(p => p.Number != first.Paragraph))
        {
            await console.Output.WriteLineAsync($"First paragraph {first.Paragraph} does not exist in subtitle");
            return;
        }

        if (subtitle.Paragraphs.All(p => p.Number != last.Paragraph))
        {
            await console.Output.WriteLineAsync($"Last paragraph {last.Paragraph} does not exist in subtitle");
            return;
        }

        var valid = processor.Validate(subtitle);
        if (valid)
        {
            processor.Sync(subtitle, first, last);
            await processor.Save(subtitle);
            await console.Output.WriteLineAsync("Subtitle synchronized successfully");
        }
    }

    private static bool TryParseTimeSpan(string timeString, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;

        // Try parsing as full time format (HH:mm:ss.fff)
        if (TimeSpan.TryParse(timeString, out timeSpan)) return true;

        // Try parsing as seconds with milliseconds (e.g., "123.456")
        if (double.TryParse(timeString, out var totalSeconds))
        {
            timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return true;
        }

        return false;
    }
}
namespace SubtitleTools.Commands;

[Command("s", Description = "Synchronize")]
public class SynchronizeCommand(ISubtitleProcessor processor, IFileSystem fileSystem) : ICommand
{
    [CommandOption("offset", 'o', Description = "Offset Seconds", IsRequired = true)]
    public required double OffsetSeconds { get; init; }

    [CommandOption("file-name", 'f', Description = "FileName", IsRequired = true)]
    public required string FileName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!fileSystem.FileExists(FileName))
        {
            await console.Output.WriteLineAsync("File not found");
            return;
        }

        var subtitle = await processor.Load(FileName);
        var valid = processor.Validate(subtitle);
        if (valid)
        {
            processor.Sync(subtitle, OffsetSeconds);
            await processor.Save(subtitle);
            await console.Output.WriteLineAsync("Subtitle synchronized successfully");
        }
    }
}
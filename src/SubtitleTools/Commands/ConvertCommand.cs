namespace SubtitleTools.Commands;

[Command("c", Description = "Convert to UTF-8")]
public class ConvertCommand(IFileSystem fileSystem, IOptions<Settings> options) : ICommand
{
    private readonly Settings _settings = options.Value;

    [CommandOption("file-name", 'f', Description = "FileName", IsRequired = true)]
    public required string FileName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!fileSystem.FileExists(FileName))
        {
            await console.Output.WriteLineAsync("File not found");
            return;
        }

        if (_settings.AutoCreateBackup)
            fileSystem.Backup(FileName);

        var subtitle = await fileSystem.ReadContent(FileName);

        await fileSystem.WriteContent(FileName, subtitle);
        await console.Output.WriteLineAsync("Successfully converted to UTF-8");
    }
}
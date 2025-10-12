namespace SubtitleTools.Commands;

[Command("de", Description = "Detect Encoding")]
public class DetectEncodingCommand(IFileSystem fileSystem) : ICommand
{
    [CommandOption("file-name", 'f', Description = "FileName", IsRequired = true)]
    public required string FileName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!fileSystem.FileExists(FileName))
        {
            await console.Output.WriteLineAsync("File not found");
            return;
        }

        var encoding = await fileSystem.GetFileEncoding(FileName);
        await console.Output.WriteLineAsync($"Encoding: {encoding.WebName}");
    }
}
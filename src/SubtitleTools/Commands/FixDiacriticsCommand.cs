namespace SubtitleTools.Commands;

[Command("fd", Description = "Fix Diacritics")]
public class FixDiacriticsCommand(IFileSystem fileSystem, IOptions<Settings> options) : ICommand
{
    private static readonly Dictionary<string, string> DiacriticsMap = new()
    {
        { "Ã", "Ă" },
        { "ã", "ă" },
        { "Ä", "Ă" },
        { "ä", "ă" },
        //{ "", "Â" },
        //{ "â", "â" },
        //{ "Î", "Î" },
        //{ "î", "î" }
        { "ª", "Ș" },
        { "Ş", "Ș" },
        { "º", "ș" },
        { "ş", "ș" },
        { "Þ", "Ț" },
        { "Ţ", "Ț" },
        { "þ", "ț" },
        { "ţ", "ț" }
    };

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

        var sb = new StringBuilder(subtitle);
        foreach (var k in DiacriticsMap.Keys)
            sb.Replace(k, DiacriticsMap[k]);

        await fileSystem.WriteContent(FileName, sb.ToString());
        await console.Output.WriteLineAsync("Diacritics fixed successfully");
    }
}
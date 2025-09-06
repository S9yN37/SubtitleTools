namespace SubtitleTools.Services;

[ExcludeFromCodeCoverage]
public class FileSystem : IFileSystem
{
    public async Task<string> ReadContent(string fileName)
    {
        return await File.ReadAllTextAsync(fileName);
    }

    public async Task<string[]> ReadLines(string file)
    {
        return await File.ReadAllLinesAsync(file);
    }

    public async Task WriteContent(string fileName, string content)
    {
        await File.WriteAllTextAsync(fileName, content);
    }

    public async Task WriteLines(string file, IReadOnlyCollection<string> lines)
    {
        await File.WriteAllLinesAsync(file, lines);
    }

    public bool FileExists(string fileName)
    {
        return File.Exists(fileName);
    }

    public void Copy(string sourceFile, string destinationFile)
    {
        File.Copy(sourceFile, destinationFile);
    }
}
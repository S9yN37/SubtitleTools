namespace SubtitleTools.Services.Interfaces;

public interface IFileSystem
{
    Task<string> ReadContent(string fileName);
    Task<string[]> ReadLines(string fileName);
    Task WriteContent(string fileName, string content);
    Task WriteLines(string fileName, IReadOnlyCollection<string> lines);
    bool FileExists(string fileName);
    void Copy(string sourceFile, string destinationFile);
}
namespace SubtitleTools.Services.Interfaces;

public interface IFileSystem
{
    bool FileExists(string file);
    Task<Encoding> GetFileEncoding(string file);
    Task<string> ReadContent(string file);
    Task<IReadOnlyList<string>> ReadLines(string file);
    Task WriteContent(string file, string content);
    Task WriteLines(string file, IReadOnlyCollection<string> lines);
    void Backup(string file);
}
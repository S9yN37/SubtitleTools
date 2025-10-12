using UtfUnknown;

namespace SubtitleTools.Services
{
    public class FileSystem : IFileSystem
    {
        public FileSystem()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public bool FileExists(string file)
        {
            return File.Exists(file);
        }

        public async Task<Encoding> GetFileEncoding(string file)
        {
            await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var charset = await CharsetDetector.DetectFromStreamAsync(fs);
            var encoding = GetSupportedEncoding(charset);
            return encoding;
        }

        public async Task<string> ReadContent(string file)
        {
            await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var charset = await CharsetDetector.DetectFromStreamAsync(fs);
            var encoding = GetSupportedEncoding(charset);
            fs.Position = 0;
            using var reader = new StreamReader(fs, encoding);
            return await reader.ReadToEndAsync();
        }

        public async Task<IReadOnlyList<string>> ReadLines(string file)
        {
            await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var charset = await CharsetDetector.DetectFromStreamAsync(fs);
            var encoding = GetSupportedEncoding(charset);
            fs.Position = 0;
            using var reader = new StreamReader(fs, encoding);
            var lines = new List<string>();
            while (await reader.ReadLineAsync() is { } line)
                lines.Add(line);
            return lines;
        }

        [ExcludeFromCodeCoverage]
        public async Task WriteContent(string file, string content)
        {
            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            await using var writer = new StreamWriter(file, false, utf8WithBom);
            await writer.WriteAsync(content);
        }

        [ExcludeFromCodeCoverage]
        public async Task WriteLines(string file, IReadOnlyCollection<string> lines)
        {
            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            await using var writer = new StreamWriter(file, false, utf8WithBom);
            foreach (var line in lines)
                await writer.WriteLineAsync(line);
        }

        [ExcludeFromCodeCoverage]
        public void Backup(string file)
        {
            var directory = Path.GetDirectoryName(file) ?? "";
            var name = Path.GetFileName(file);
            var backup = Path.Combine(directory, $"{DateTime.Now:yyMMddHHmmss}_{name}");
            File.Copy(file, backup, overwrite: true);
        }

        private static Encoding GetSupportedEncoding(DetectionResult charset)
        {
            var encodingName = charset?.Detected?.EncodingName;

            if (string.IsNullOrEmpty(encodingName))
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (ArgumentException)
            {
                return EncodingFallbackMap.TryGetValue(encodingName, out var fallbackEncodingName)
                    ? Encoding.GetEncoding(fallbackEncodingName)
                    : Encoding.UTF8;
            }
        }

        private static readonly Dictionary<string, string> EncodingFallbackMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // UTF-7 is deprecated and removed from .NET 5+, use UTF-8 as fallback
            { "utf-7", "utf-8" },

            // UCS-4 variants - use UTF-32 with appropriate byte order
            { "X-ISO-10646-UCS-4-21431", "utf-32BE" }, // Big-endian
            { "X-ISO-10646-UCS-4-34121", "utf-32" }, // Little-endian

            // IBM PC codepage 850 (Western European)
            { "CP 850/IBM 00850", "ibm850" },

            // Korean encodings
            { "cp949", "ks_c_5601-1987" }, // Closest Korean encoding
            { "euc-kr/uhc", "euc-kr" },

            // Traditional Chinese
            { "euc-tw", "big5" }, // Big5 is most common Traditional Chinese encoding

            // Chinese simplified (ISO-2022-CN not widely supported)
            { "iso-2022-cn", "gb2312" },

            // ISO-8859-10 (Nordic languages)
            { "iso-8859-10", "iso-8859-1" }, // Western European fallback

            // ISO-8859-16 (Southeast European)
            { "iso-8859-16", "iso-8859-2" }, // Central European fallback

            // Vietnamese
            { "viscii", "windows-1258" } // Vietnamese Windows codepage
        };
    }
}
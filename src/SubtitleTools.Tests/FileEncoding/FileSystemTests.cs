using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UtfUnknown;
using Assert = NUnit.Framework.Assert;

namespace SubtitleTools.Tests.FileEncoding;

public class FileSystemTests
{
    private IFileSystem _fileSystem;
    private PrivateType _fileSystemPrivateType;

    [SetUp]
    public void Setup()
    {
        _fileSystem = new FileSystem();
        _fileSystemPrivateType = new PrivateType(typeof(FileSystem));
    }

    [Test]
    public void FileExists_Successfully()
    {
        var result = _fileSystem.FileExists(Path.Combine("Subtitles", $"Subtitle.ro.txt"));
        Assert.That(result, Is.True);
    }

    [TestCase("ar", "utf-8")]
    [TestCase("bg", "utf-8")]
    [TestCase("cz", "utf-8")]
    [TestCase("es", "utf-8")]
    [TestCase("fr", "utf-8")]
    [TestCase("hu", "utf-8")]
    [TestCase("pl", "utf-8")]
    [TestCase("ro", "iso-8859-2")]
    [TestCase("ru", "utf-8")]
    [TestCase("tr", "utf-8")]
    public async Task GetFileEncoding_Successfully(string language, string expected)
    {
        var encoding = await _fileSystem.GetFileEncoding(Path.Combine("Subtitles", $"Subtitle.{language}.txt"));
        Assert.That(expected, Is.EqualTo(encoding.WebName));
    }

    private static IEnumerable<object[]> ReadContentTests
    {
        get
        {
            yield return ["ar", new[] { "هناك من يتبعنا", "حمقى الآن يجب عليكم أن تسلموا أنفسكم", "هيا" }];
            yield return ["bg", new[] { "Ами... не съм ял от вчера.", "Не мога да се бия на празен стомах.", "Първо ме нахранете." }];
            yield return ["cz", new[] { "Ale aby dokonèili svùj plán,", "musí se se správcem brzy vidìt. Poèkejme.", "Neseïte a neèekejte. Správce je pryè." }];
            yield return ["es", new[] { "¿Quién es ese señor?", "Incluso nos salvó la vida.", "¿Sí?" }];
            yield return ["fr", new[] { "Il s'est mis à rire :", "J'en suis peut-être l'instigateur...", "Il l'a détruit ?", "C'est insensé !" }];
            yield return ["hu", new[] { "Legyőztek téged.", "Nem tudlak beajánlani.", "Bocsáss meg.", "VÉGE" }];
            yield return ["pl", new[] { "Uciekajcie tylnym wyjściem.", "Tędy!", "Obstawcie główną bramę!" }];
            yield return ["ro", new[] { "Înţeleg.", "Atunci să ne despărţim!", "Rămâneţi sănătoşi.", "SFÂRŞIT" }];
            yield return ["ru", new[] { "Бросайте оружие!", "Вы арестованы!", "Конец" }];
            yield return ["tr", new[] { "Üçü birlikte.", "Öyle mi?", "Bulduğum şeye bakın!", "Kopuk harf parçaları!", "Hoşça kalın!" }];
        }
    }

    [TestCaseSource(nameof(ReadContentTests))]
    public async Task ReadContent_Successfully(string language, string[] expected)
    {
        var result = await _fileSystem.ReadContent(Path.Combine("Subtitles", $"Subtitle.{language}.txt"));
        foreach (var line in expected)
        {
            Assert.That(result, Does.Contain(line), $"Expected to find '{line}' in content");
        }
    }

    [TestCaseSource(nameof(ReadContentTests))]
    public async Task ReadLines_Successfully(string language, string[] expected)
    {
        var results = await _fileSystem.ReadLines(Path.Combine("Subtitles", $"Subtitle.{language}.txt"));
        foreach (var line in expected)
        {
            Assert.That(results.Any(r => r.Contains(line)), Is.True, $"Expected to find '{line}' in the lines");
        }
    }

    [Test]
    public void EncodingFallbackMap_IsValid()
    {
        var encodingFallbackMap = _fileSystemPrivateType.GetStaticField("EncodingFallbackMap") as Dictionary<string, string>;
        Assert.That(encodingFallbackMap, Is.Not.Null);
        foreach (var name in encodingFallbackMap.Values)
        {
            Assert.DoesNotThrow(() => Encoding.GetEncoding(name), $"Encoding '{name}' does not exist or is not supported");
        }
    }

    [Test]
    public void GetSupportedEncoding_DetectionResultIsNull_ReturnsUtf8()
    {
        var result = _fileSystemPrivateType.InvokeStatic("GetSupportedEncoding", (DetectionResult?)null);
        Assert.That(result, Is.EqualTo(Encoding.UTF8));
    }

    [Test]
    public void GetSupportedEncoding_DetectedIsNull_ReturnsUtf8()
    {
        var detectionResult = new DetectionResult();
        var result = _fileSystemPrivateType.InvokeStatic("GetSupportedEncoding", detectionResult);
        Assert.That(result, Is.EqualTo(Encoding.UTF8));
    }

    [Test]
    public void GetSupportedEncoding_NoEncodingName_ReturnsUtf8()
    {
        var detectionResult = new DetectionResult([new DetectionDetail(string.Empty, 1)]);
        var result = (Encoding)_fileSystemPrivateType.InvokeStatic("GetSupportedEncoding", detectionResult);
        Assert.That(result, Is.EqualTo(Encoding.UTF8));
    }

    [Test]
    public void GetSupportedEncoding_NoFallback_ReturnsUtf8()
    {
        var detectionResult = new DetectionResult([new DetectionDetail("Unknown", 1)]);
        var result = (Encoding)_fileSystemPrivateType.InvokeStatic("GetSupportedEncoding", detectionResult);
        Assert.That(result, Is.EqualTo(Encoding.UTF8));
    }

    [Test]
    public void GetSupportedEncoding_ValidEncodingName_ReturnsCorrectEncoding()
    {
        var detectionResult = new DetectionResult([new DetectionDetail("windows-1250", 1)]);
        var result = (Encoding)_fileSystemPrivateType.InvokeStatic("GetSupportedEncoding", detectionResult);
        Assert.That(result.WebName, Is.EqualTo("windows-1250"));
    }

    [Test]
    public void GetSupportedEncoding_FallbackEncodingName_ReturnsCorrectEncoding()
    {
        var detectionResult = new DetectionResult([new DetectionDetail("iso-8859-16", 1)]);
        var result = (Encoding)_fileSystemPrivateType.InvokeStatic("GetSupportedEncoding", detectionResult);
        Assert.That(result.WebName, Is.EqualTo("iso-8859-2"));
    }
}
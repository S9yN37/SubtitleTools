using System.Text;

namespace SubtitleTools.Tests.Commands;

[Category("Functional")]
public class DetectEncodingCommandTests
{
    private FakeInMemoryConsole _console;
    private IFileSystem _fileSystem;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSubtitleToolsServices();
        services.AddSubtitleToolsFormatServices();
        services.AddSubtitleToolsCommands();

        // Replace IFileSystem
        _fileSystem = Substitute.For<IFileSystem>();
        services.AddScoped(_ => _fileSystem);

        services.BuildServiceProvider();

        _console = new FakeInMemoryConsole();
    }

    [TearDown]
    public void TearDown()
    {
        _console.Dispose();
    }

    [Test]
    public async Task Execute_Fail_FileNotFound()
    {
        // Arrange
        _fileSystem.FileExists("NotFound.srt").Returns(false);
        var command = new DetectEncodingCommand(_fileSystem) { FileName = "NotFound.srt" };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        await _fileSystem.DidNotReceive().WriteLines(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("File not found\n"));
    }

    [Test]
    public async Task Execute_Successfully()
    {
        // Arrange
        _fileSystem.FileExists("Subtitle.srt").Returns(true);
        _fileSystem.GetFileEncoding("Subtitle.srt").Returns(Encoding.UTF8);

        var command = new DetectEncodingCommand(_fileSystem) { FileName = "Subtitle.srt" };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Encoding: utf-8\n"));
    }
}
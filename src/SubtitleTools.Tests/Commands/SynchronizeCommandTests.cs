namespace SubtitleTools.Tests.Commands;

[Category("Functional")]
public class SynchronizeCommandTests
{
    private FakeInMemoryConsole _console;
    private IFileSystem _fileSystem;
    private ISubtitleProcessor _subtitleProcessor;

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

        // Settings
        var options = Substitute.For<IOptions<Settings>>();
        options.Value.Returns(new Settings { AutoCreateBackup = true });
        services.AddSingleton(options);

        var provider = services.BuildServiceProvider();
        _subtitleProcessor = provider.GetService<ISubtitleProcessor>()!;

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
        var command = new SynchronizeCommand(_subtitleProcessor, _fileSystem) { FileName = "NotFound.srt", OffsetSeconds = 1 };

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
        _fileSystem.ReadLines("Subtitle.srt").Returns([
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line", ""
        ]);
        var command = new SynchronizeCommand(_subtitleProcessor, _fileSystem) { FileName = "Subtitle.srt", OffsetSeconds = 1 };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        _fileSystem.Received(1).Copy(command.FileName, Arg.Is<string>(f => f.Contains(command.FileName)));
        var expectedLines = new[]
        {
            "1", "00:00:02,000 --> 00:00:03,000", "Subtitle Line", ""
        };
        await _fileSystem.Received(1).WriteLines(command.FileName, Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(expectedLines)));
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Subtitle synchronized successfully\n"));
    }
}
namespace SubtitleTools.Tests.Commands;

[Category("Functional")]
public class SynchronizePartialCommandTests
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
        var command = new SynchronizePartialCommand(_subtitleProcessor, _fileSystem) { FileName = "NotFound.srt", Adjustments = ["00:00:01+3"] };

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
        var command = new SynchronizePartialCommand(_subtitleProcessor, _fileSystem) { FileName = "Subtitle.srt", Adjustments = ["00:00:00+3"] };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        _fileSystem.Received(1).Copy(command.FileName, Arg.Is<string>(f => f.Contains(command.FileName)));
        var expectedLines = new[] { "1", "00:00:04,000 --> 00:00:05,000", "Subtitle Line", "" };
        await _fileSystem.Received(1).WriteLines(command.FileName, Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(expectedLines)));
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Subtitle synchronized successfully\n"));
    }

    [Test]
    public async Task Execute_Successfully_Multiple()
    {
        // Arrange
        _fileSystem.FileExists("SubtitleMultiple.srt").Returns(true);
        _fileSystem.ReadLines("SubtitleMultiple.srt").Returns([
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line 1", "",
            "2", "00:00:04,000 --> 00:00:05,000", "Subtitle Line 2", "",
            "3", "00:00:06,000 --> 00:00:07,000", "Subtitle Line 3", "",
            "4", "00:00:09,700 --> 00:00:10,700", "Subtitle Line 4", "",
            "5", "00:00:12,000 --> 00:00:13,000", "Subtitle Line 5", ""
        ]);
        var command = new SynchronizePartialCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "SubtitleMultiple.srt",
            Adjustments =
            [
                "00:00:03+0.3",
                "00:00:07-0.2",
                "00:00:11+0"
            ]
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        _fileSystem.Received(1).Copy(command.FileName, Arg.Is<string>(f => f.Contains(command.FileName)));
        var expectedLines = new[]
        {
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line 1", "",
            "2", "00:00:04,300 --> 00:00:05,300", "Subtitle Line 2", "",
            "3", "00:00:06,300 --> 00:00:07,300", "Subtitle Line 3", "",
            "4", "00:00:09,500 --> 00:00:10,500", "Subtitle Line 4", "",
            "5", "00:00:12,000 --> 00:00:13,000", "Subtitle Line 5", ""
        };
        await _fileSystem.Received(1).WriteLines(command.FileName, Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(expectedLines)));
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Subtitle synchronized successfully\n"));
    }

    [Test]
    public async Task Execute_Fail_Parse()
    {
        // Arrange
        _fileSystem.FileExists("SubtitleMultiple.srt").Returns(true);
        _fileSystem.ReadLines("SubtitleMultiple.srt").Returns([
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line 1", "",
            "2", "00:00:04,000 --> 00:00:05,000", "Subtitle Line 2", "",
            "3", "00:00:06,000 --> 00:00:07,000", "Subtitle Line 3", "",
            "4", "00:00:09,700 --> 00:00:10,700", "Subtitle Line 4", "",
            "5", "00:00:12,000 --> 00:00:13,000", "Subtitle Line 5", ""
        ]);
        var command = new SynchronizePartialCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "SubtitleMultiple.srt",
            Adjustments =
            [
                "00:00:03 +0.3",
                "00:00:07-o"
            ]
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        await _fileSystem.DidNotReceive().WriteLines(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Could not parse segment string '00:00:03 +0.3'\n" +
                                       "Could not parse offset value 'o' in '00:00:07-o'\n"));
    }
}
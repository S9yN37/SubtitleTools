namespace SubtitleTools.Tests.Commands;

[Category("Functional")]
public class VisualSyncCommandTests
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
        var command = new VisualSyncCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "NotFound.srt",
            FirstParagraph = 1,
            FirstTime = "00:00:01.000",
            LastParagraph = 3,
            LastTime = "00:00:03.000"
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        await _fileSystem.DidNotReceive().WriteLines(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("File not found\n"));
    }

    [TestCase(-1, 3)]
    [TestCase(0, 3)]
    [TestCase(1, -1)]
    [TestCase(1, 0)]
    [TestCase(1, 1)]
    public async Task Execute_Fail_ParagraphNotPositive(int firstParagraph, int lastParagraph)
    {
        // Arrange
        _fileSystem.FileExists("Subtitle.srt").Returns(true);
        var command = new VisualSyncCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "Subtitle.srt",
            FirstParagraph = firstParagraph,
            FirstTime = "00:00:01.000",
            LastParagraph = lastParagraph,
            LastTime = "00:00:03.000"
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        await _fileSystem.DidNotReceive().WriteLines(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo($"Invalid paragraphs {firstParagraph} and {lastParagraph}\n"));
    }

    [TestCase(5, true, 7, false)]
    [TestCase(5, true, 7, false)]
    [TestCase(3, false, 5, true)]
    [TestCase(3, false, 5, true)]
    public async Task Execute_Fail_ParagraphDoesNotExists(int firstParagraph, bool firstFail, int lastParagraph, bool lastFail)
    {
        // Arrange
        _fileSystem.FileExists("Subtitle.srt").Returns(true);
        _fileSystem.ReadLines("Subtitle.srt").Returns([
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line 1", "",
            "2", "00:00:03,000 --> 00:00:04,000", "Subtitle Line 2", "",
            "3", "00:00:05,000 --> 00:00:06,000", "Subtitle Line 3", ""
        ]);
        var command = new VisualSyncCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "Subtitle.srt",
            FirstParagraph = firstParagraph,
            FirstTime = "00:00:01.000",
            LastParagraph = lastParagraph,
            LastTime = "00:00:03.000"
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        await _fileSystem.DidNotReceive().WriteLines(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
        var output = _console.ReadOutputString();
        if (firstFail)
            Assert.That(output, Is.EqualTo($"First paragraph {firstParagraph} does not exist in subtitle\n"));
        if (lastFail)
            Assert.That(output, Is.EqualTo($"Last paragraph {lastParagraph} does not exist in subtitle\n"));
    }

    [TestCase("", "00:00:03.000")]
    [TestCase("00:00:01.000", "")]
    public async Task Execute_Fail_InvalidTime(string firstTime, string lastTime)
    {
        // Arrange
        _fileSystem.FileExists("Subtitle.srt").Returns(true);
        var command = new VisualSyncCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "Subtitle.srt",
            FirstParagraph = 1,
            FirstTime = firstTime,
            LastParagraph = 3,
            LastTime = lastTime
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        await _fileSystem.DidNotReceive().WriteLines(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
        var output = _console.ReadOutputString();
        if (string.IsNullOrEmpty(firstTime))
            Assert.That(output, Is.EqualTo("Invalid time format for first visual point: \n"));
        if (string.IsNullOrEmpty(lastTime))
            Assert.That(output, Is.EqualTo("Invalid time format for last visual point: \n"));
    }

    [Test]
    public async Task Execute_Successfully()
    {
        // Arrange
        _fileSystem.FileExists("Subtitle.srt").Returns(true);
        _fileSystem.ReadLines("Subtitle.srt").Returns([
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line 1", "",
            "2", "00:00:03,000 --> 00:00:04,000", "Subtitle Line 2", "",
            "3", "00:00:05,500 --> 00:00:06,500", "Subtitle Line 3", "",
            "4", "00:00:07,000 --> 00:00:08,000", "Subtitle Line 4", "",
            "5", "00:00:09,000 --> 00:00:10,000", "Subtitle Line 5", ""
        ]);

        var command = new VisualSyncCommand(_subtitleProcessor, _fileSystem)
        {
            FileName = "Subtitle.srt",
            FirstParagraph = 2,
            FirstTime = "03.300",
            LastParagraph = 4,
            LastTime = "00:00:07.300"
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        _fileSystem.Received(1).Copy(command.FileName, Arg.Is<string>(f => f.Contains(command.FileName)));
        var expectedLines = new[]
        {
            "1", "00:00:01,300 --> 00:00:02,300", "Subtitle Line 1", "",
            "2", "00:00:03,300 --> 00:00:04,300", "Subtitle Line 2", "",
            "3", "00:00:05,800 --> 00:00:06,800", "Subtitle Line 3", "",
            "4", "00:00:07,300 --> 00:00:08,300", "Subtitle Line 4", "",
            "5", "00:00:09,300 --> 00:00:10,300", "Subtitle Line 5", ""
        };
        await _fileSystem.Received(1).WriteLines(command.FileName, Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(expectedLines)));

        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Subtitle synchronized successfully\n"));
    }
}
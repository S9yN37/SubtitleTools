namespace SubtitleTools.Tests.Commands;

[Category("Functional")]
public class FixDiacriticsCommandTests
{
    private FakeInMemoryConsole _console;
    private IFileSystem _fileSystem;
    private IOptions<Settings> _options;

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
        _options = Substitute.For<IOptions<Settings>>();
        _options.Value.Returns(new Settings { AutoCreateBackup = true });
        services.AddSingleton(_options);

        var provider = services.BuildServiceProvider();

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
        var command = new FixDiacriticsCommand(_fileSystem, _options) { FileName = "NotFound.srt" };

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
        _fileSystem.ReadContent("Subtitle.srt").Returns("Ã ã ª Ş º ş Þ Ţ þ ţ");

        var command = new FixDiacriticsCommand(_fileSystem, _options) { FileName = "Subtitle.srt" };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        _fileSystem.Received(1).Copy(command.FileName, Arg.Is<string>(f => f.Contains(command.FileName)));
        const string expected = "Ă ă Ș Ș ș ș Ț Ț ț ț";
        await _fileSystem.Received(1).WriteContent(command.FileName, Arg.Is<string>(c => c == expected));
        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Diacritics fixed successfully\n"));
    }
}
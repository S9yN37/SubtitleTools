using System.Text;

namespace SubtitleTools.Tests.Commands;

[Category("Functional")]
public class ConvertCommandTests
{
    private FakeInMemoryConsole _console;
    private IFileSystem _fileSystem;
    private IOptions<Settings> _options;

    [SetUp]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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
        var command = new ConvertCommand(_fileSystem, _options) { FileName = "NotFound.srt" };

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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // Arrange
        const string content = "Ă ă Â â Î î Ș ș Ț ț";
        _fileSystem.FileExists("Subtitle.srt").Returns(true);
        _fileSystem.ReadContent("Subtitle.srt").Returns(content);

        var command = new ConvertCommand(_fileSystem, _options)
        {
            FileName = "Subtitle.srt"
        };

        // Act
        await command.ExecuteAsync(_console);

        // Assert
        _fileSystem.Received(1).Backup(command.FileName);

        const string expected = "Ă ă Â â Î î Ș ș Ț ț";
        await _fileSystem.Received(1)
            .WriteContent(
                command.FileName,
                Arg.Is<string>(c => c == expected)
            );

        var output = _console.ReadOutputString();
        Assert.That(output, Is.EqualTo("Successfully converted to UTF-8\n"));
    }
}
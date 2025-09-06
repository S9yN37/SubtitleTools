namespace SubtitleTools.Tests.Services;

[Category("Functional")]
public class SubtitleFormatFactoryTests
{
    private SubtitleFormatFactory _service;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSubtitleToolsFormatServices();
        var provider = services.BuildServiceProvider();
        _service = new SubtitleFormatFactory(provider);
    }

    [Test]
    public void GetParser_Successfully()
    {
        // Act
        var parser = _service.GetParser("Subtitle.srt");

        // Assert
        Assert.That(parser, Is.Not.Null);
        Assert.That(parser.Name, Is.EqualTo("SubRip"));
    }

    [Test]
    public void GetParser_Throws_Unsupported()
    {
        // Act
        var exception = Assert.Throws<NotSupportedException>(() => _service.GetParser("Subtitle.unknown"));

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.Message, Is.EqualTo("Subtitle format '.unknown' is not supported."));
    }
}
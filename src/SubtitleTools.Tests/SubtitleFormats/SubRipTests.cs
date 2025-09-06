namespace SubtitleTools.Tests.SubtitleFormats;

[Category("Unit")]
public class SubRipTests
{
    private SubRip _service;

    [SetUp]
    public void Setup()
    {
        _service = new SubRip();
    }

    [Test]
    public void Read_Successfully()
    {
        // Arrange
        var lines = new[]
        {
            "",
            "1", "00:02:54,768 --> 00:02:55,699", "Elisabeth, Elisabeth.", "",
            "2", "00:02:56,294 --> 00:02:57,094", "We love you.", "",
            ""
        };

        // Act
        var subtitle = _service.Read(".srt", lines);

        // Assert
        Assert.That(subtitle, Is.Not.Null);
        Assert.That(subtitle.Paragraphs.All(p => p.Valid()), Is.True, "All paragraphs should be valid");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Type, Is.EqualTo("SubRip"));
            Assert.That(subtitle.Extension, Is.EqualTo(".srt"));
            Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(2));
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(new TimeSpan(0, 0, 2, 54, 768)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(new TimeSpan(0, 0, 2, 55, 699)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines.SequenceEqual(["Elisabeth, Elisabeth."]), Is.True);
            Assert.That(subtitle.Paragraphs[1].Number, Is.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Start, Is.EqualTo(new TimeSpan(0, 0, 2, 56, 294)));
            Assert.That(subtitle.Paragraphs[1].End, Is.EqualTo(new TimeSpan(0, 0, 2, 57, 094)));
            Assert.That(subtitle.Paragraphs[1].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[1].Lines.SequenceEqual(["We love you."]), Is.True);
        }
    }

    [Test]
    public void Read_Successfully_ResetParagraph()
    {
        // Arrange
        var lines = new[]
        {
            "1", "00:17:39,897 --> 00:17:40,263", "Address?", "",
            "2", "00:17:43,330 --> 00:17:44,494", "1057", "Beverly Canyon.", "",
            "3", "00:17:47,096 --> 00:17:48,226", "Write this down.", ""
        };

        // Act
        var subtitle = _service.Read(".srt", lines);

        // Assert
        Assert.That(subtitle, Is.Not.Null);
        Assert.That(subtitle.Paragraphs.All(p => p.Valid()), Is.True, "All paragraphs should be valid");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Type, Is.EqualTo("SubRip"));
            Assert.That(subtitle.Extension, Is.EqualTo(".srt"));
            Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(3), "Paragraph count");

            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(new TimeSpan(0, 0, 17, 39, 897)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(new TimeSpan(0, 0, 17, 40, 263)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines.SequenceEqual(["Address?"]), Is.True);

            Assert.That(subtitle.Paragraphs[1].Number, Is.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Start, Is.EqualTo(new TimeSpan(0, 0, 17, 43, 330)));
            Assert.That(subtitle.Paragraphs[1].End, Is.EqualTo(new TimeSpan(0, 0, 17, 44, 494)));
            Assert.That(subtitle.Paragraphs[1].Lines, Has.Count.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Lines.SequenceEqual(["1057", "Beverly Canyon."]), Is.True);

            Assert.That(subtitle.Paragraphs[2].Number, Is.EqualTo(3));
            Assert.That(subtitle.Paragraphs[2].Start, Is.EqualTo(new TimeSpan(0, 0, 17, 47, 096)));
            Assert.That(subtitle.Paragraphs[2].End, Is.EqualTo(new TimeSpan(0, 0, 17, 48, 226)));
            Assert.That(subtitle.Paragraphs[2].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[2].Lines.SequenceEqual(["Write this down."]), Is.True);
        }
    }
}
namespace SubtitleTools.Tests.Extensions;

[Category("Unit")]
public class ParagraphExtensionsTests
{
    [Test]
    public void Sync_Offset_Successfully()
    {
        // Arrange
        var p = new Paragraph
        {
            Number = 1,
            Start = TimeSpan.FromSeconds(6),
            End = TimeSpan.FromSeconds(8),

            Lines = ["Text"]
        };

        // Act
        p.Sync(-1);

        // Assert
        Assert.That(p.Number, Is.EqualTo(1));
        Assert.That(p.Start, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(p.End, Is.EqualTo(TimeSpan.FromSeconds(7)));
        Assert.That(p.Lines, Is.Not.Null);
    }

    [Test]
    public void Sync_Offset_Successfully_NoNegative()
    {
        // Arrange
        var p = new Paragraph
        {
            Number = 1,
            Start = TimeSpan.FromSeconds(6),
            End = TimeSpan.FromSeconds(8),
            Lines = ["Text"]
        };

        // Act
        p.Sync(-7);

        // Assert
        Assert.That(p.Number, Is.EqualTo(1));
        Assert.That(p.Start, Is.EqualTo(TimeSpan.Zero));
        Assert.That(p.End, Is.EqualTo(TimeSpan.FromSeconds(1)));
        Assert.That(p.Lines, Is.Not.Null);
    }

    [Test]
    public void Sync_Offset_Fail()
    {
        // Arrange
        var p = new Paragraph
        {
            Number = 1,
            Start = TimeSpan.FromSeconds(6),
            End = TimeSpan.FromSeconds(8),
            Lines = ["Text"]
        };

        // Act
        var exception = Assert.Throws<ApplicationException>(() => p.Sync(-10));

        // Assert
        Assert.That(exception.Message, Is.EqualTo("Cannot adjust subtitle paragraph 1. Becomes zero."));
    }

    [TestCase(0, 0, 0, false)]
    [TestCase(1, 0, 1, false)]
    [TestCase(2, 1, 1, false)]
    [TestCase(1, 2, 1, true)]
    public void Valid(int start, int end, int lines, bool expected)
    {
        // Arrange
        var p = new Paragraph
        {
            Number = 1,
            Start = TimeSpan.FromSeconds(start),
            End = TimeSpan.FromSeconds(end),
            Lines = Enumerable.Range(1, lines).Select(l => $"Line {l}").ToList()
        };

        var valid = p.Valid();
        // Act

        // Assert
        Assert.That(expected, Is.EqualTo(valid));
    }

    private static IEnumerable<object[]> ParagraphInvalidTests
    {
        get
        {
            yield return [0, 0, new List<string>()];
            yield return [0, 1, new List<string>()];
            yield return [0, 0, new List<string> { "Text" }];
            yield return [1, 0, new List<string> { "Text" }];
        }
    }

    [TestCaseSource(nameof(ParagraphInvalidTests))]
    public void Paragraph_Invalid(double start, double end, List<string> lines)
    {
        // Arrange
        var p = new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(start), End = TimeSpan.FromSeconds(end), Lines = lines };

        // Act
        var valid = p.Valid();

        // Assert
        Assert.That(valid, Is.False, $"Paragraph should not be valid. Start: {start}, End: {end}, LinesCount: {lines.Count}");
    }
}
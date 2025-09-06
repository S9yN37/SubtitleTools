namespace SubtitleTools.Tests.Services;

[Category("Functional")]
public class SubtitleProcessorTests
{
    private IFileSystem _fileSystem;
    private SubtitleProcessor _service;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSubtitleToolsServices();
        services.AddSubtitleToolsFormatServices();

        // Replace IFileSystem
        _fileSystem = Substitute.For<IFileSystem>();
        services.AddScoped(_ => _fileSystem);

        var provider = services.BuildServiceProvider();

        var options = Substitute.For<IOptions<Settings>>();
        options.Value.Returns(new Settings { AutoCreateBackup = true });

        var readerFactory = provider.GetService<ISubtitleFormatFactory>();
        _service = new SubtitleProcessor(_fileSystem, readerFactory!, options);
    }

    [Test]
    public async Task Load_Successfully()
    {
        // Arrange
        _fileSystem.ReadLines("Subtitle.srt").Returns([
            "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line", ""
        ]);

        // Act
        var subtitle = await _service.Load("Subtitle.srt");

        // Assert
        Assert.That(subtitle, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Type, Is.EqualTo("SubRip"));
            Assert.That(subtitle.Extension, Is.EqualTo(".srt"));
            Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(1));
        }

        Assert.That(subtitle.Paragraphs.All(p => p.Valid()), Is.True, "All paragraphs should be valid");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines.SequenceEqual(["Subtitle Line"]), Is.True);
        }

        await _fileSystem.Received(1).ReadLines("Subtitle.srt");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("NewSubtitle.srt")]
    public async Task Save_Successfully(string? file)
    {
        // Arrange
        var subtitle = CreateDefaultSubtitle();

        // Act
        await _service.Save(subtitle, file);

        // Assert
        _fileSystem.Received(1).Copy(subtitle.OriginalFile, Arg.Is<string>(f => f.Contains(subtitle.OriginalFile)));
        var expectedFile = string.IsNullOrEmpty(file) ? subtitle.OriginalFile : file;
        var expectedLines = new[] { "1", "00:00:01,000 --> 00:00:02,000", "Subtitle Line", "" };
        await _fileSystem.Received(1).WriteLines(expectedFile, Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(expectedLines)));
    }

    [Test]
    public void Validate_Successfully()
    {
        // Arrange
        var subtitle = CreateDefaultSubtitle();

        // Act
        var valid = _service.Validate(subtitle);

        // Assert
        Assert.That(valid, Is.True);
    }

    [Test]
    public void Validate_Fail()
    {
        // Arrange
        var subtitle = CreateDefaultSubtitle();
        subtitle.Paragraphs = [new Paragraph { Number = 1 }];

        // Act
        var valid = _service.Validate(subtitle);

        // Assert
        Assert.That(valid, Is.False);
    }

    [Test]
    public void Sync_Offset_Successfully()
    {
        // Arrange
        var subtitle = CreateDefaultSubtitle();

        // Act
        _service.Sync(subtitle, 1);

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines.SequenceEqual(["Subtitle Line"]), Is.True);
        }
    }

    [Test]
    public void Sync_Offset_NoSync_0Offset()
    {
        // Arrange
        var subtitle = CreateDefaultSubtitle();

        // Act
        _service.Sync(subtitle, 0);

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines.SequenceEqual(["Subtitle Line"]), Is.True);
        }
    }

    [Test]
    public void Sync_Segments_Successfully_FromSegmentToEnd()
    {
        // Arrange
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 1"] },
                new Paragraph { Number = 2, Start = TimeSpan.FromSeconds(4), End = TimeSpan.FromSeconds(5), Lines = ["Subtitle Line 2"] },
                new Paragraph { Number = 3, Start = TimeSpan.FromSeconds(6), End = TimeSpan.FromSeconds(7), Lines = ["Subtitle Line 3"] },
                new Paragraph { Number = 4, Start = TimeSpan.FromSeconds(10), End = TimeSpan.FromSeconds(11), Lines = ["Subtitle Line 4"] },
                new Paragraph { Number = 5, Start = TimeSpan.FromSeconds(12), End = TimeSpan.FromSeconds(13), Lines = ["Subtitle Line 5"] }
            ]
        };

        // 0-3: no sync
        // 3-E: +1
        var segments = new List<Segment>
        {
            new() { From = TimeSpan.FromSeconds(3), Offset = +1 }
        };

        // Act
        _service.Sync(subtitle, segments);

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines[0], Is.EqualTo("Subtitle Line 1"));

            Assert.That(subtitle.Paragraphs[1].Number, Is.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Start, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(subtitle.Paragraphs[1].End, Is.EqualTo(TimeSpan.FromSeconds(6)));
            Assert.That(subtitle.Paragraphs[1].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[1].Lines[0], Is.EqualTo("Subtitle Line 2"));

            Assert.That(subtitle.Paragraphs[2].Number, Is.EqualTo(3));
            Assert.That(subtitle.Paragraphs[2].Start, Is.EqualTo(TimeSpan.FromSeconds(7)));
            Assert.That(subtitle.Paragraphs[2].End, Is.EqualTo(TimeSpan.FromSeconds(8)));
            Assert.That(subtitle.Paragraphs[2].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[2].Lines[0], Is.EqualTo("Subtitle Line 3"));

            Assert.That(subtitle.Paragraphs[3].Number, Is.EqualTo(4));
            Assert.That(subtitle.Paragraphs[3].Start, Is.EqualTo(TimeSpan.FromSeconds(11)));
            Assert.That(subtitle.Paragraphs[3].End, Is.EqualTo(TimeSpan.FromSeconds(12)));
            Assert.That(subtitle.Paragraphs[3].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[3].Lines[0], Is.EqualTo("Subtitle Line 4"));

            Assert.That(subtitle.Paragraphs[4].Number, Is.EqualTo(5));
            Assert.That(subtitle.Paragraphs[4].Start, Is.EqualTo(TimeSpan.FromSeconds(13)));
            Assert.That(subtitle.Paragraphs[4].End, Is.EqualTo(TimeSpan.FromSeconds(14)));
            Assert.That(subtitle.Paragraphs[4].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[4].Lines[0], Is.EqualTo("Subtitle Line 5"));
        }
    }

    [Test]
    public void Sync_Segments_Successfully_Segment1_Segment2End()
    {
        // Arrange
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 1"] },
                new Paragraph { Number = 2, Start = TimeSpan.FromSeconds(4), End = TimeSpan.FromSeconds(5), Lines = ["Subtitle Line 2"] },
                new Paragraph { Number = 3, Start = TimeSpan.FromSeconds(6), End = TimeSpan.FromSeconds(7), Lines = ["Subtitle Line 3"] },
                new Paragraph { Number = 4, Start = TimeSpan.FromSeconds(10), End = TimeSpan.FromSeconds(11), Lines = ["Subtitle Line 4"] },
                new Paragraph { Number = 5, Start = TimeSpan.FromSeconds(12), End = TimeSpan.FromSeconds(13), Lines = ["Subtitle Line 5"] }
            ]
        };

        // 0-3: no sync
        // 3-9: +1
        // 9-E: -1
        var segments = new List<Segment>
        {
            new() { From = TimeSpan.FromSeconds(3), Offset = +1 },
            new() { From = TimeSpan.FromSeconds(9), Offset = -1 }
        };

        // Act
        _service.Sync(subtitle, segments);

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines[0], Is.EqualTo("Subtitle Line 1"));

            Assert.That(subtitle.Paragraphs[1].Number, Is.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Start, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(subtitle.Paragraphs[1].End, Is.EqualTo(TimeSpan.FromSeconds(6)));
            Assert.That(subtitle.Paragraphs[1].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[1].Lines[0], Is.EqualTo("Subtitle Line 2"));

            Assert.That(subtitle.Paragraphs[2].Number, Is.EqualTo(3));
            Assert.That(subtitle.Paragraphs[2].Start, Is.EqualTo(TimeSpan.FromSeconds(7)));
            Assert.That(subtitle.Paragraphs[2].End, Is.EqualTo(TimeSpan.FromSeconds(8)));
            Assert.That(subtitle.Paragraphs[2].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[2].Lines[0], Is.EqualTo("Subtitle Line 3"));

            Assert.That(subtitle.Paragraphs[3].Number, Is.EqualTo(4));
            Assert.That(subtitle.Paragraphs[3].Start, Is.EqualTo(TimeSpan.FromSeconds(09)));
            Assert.That(subtitle.Paragraphs[3].End, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(subtitle.Paragraphs[3].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[3].Lines[0], Is.EqualTo("Subtitle Line 4"));

            Assert.That(subtitle.Paragraphs[4].Number, Is.EqualTo(5));
            Assert.That(subtitle.Paragraphs[4].Start, Is.EqualTo(TimeSpan.FromSeconds(11)));
            Assert.That(subtitle.Paragraphs[4].End, Is.EqualTo(TimeSpan.FromSeconds(12)));
            Assert.That(subtitle.Paragraphs[4].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[4].Lines[0], Is.EqualTo("Subtitle Line 5"));
        }
    }

    [Test]
    public void Sync_Segments_Successfully_Multiple()
    {
        // Arrange
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 1"] },
                new Paragraph { Number = 2, Start = TimeSpan.FromSeconds(4), End = TimeSpan.FromSeconds(5), Lines = ["Subtitle Line 2"] },
                new Paragraph { Number = 3, Start = TimeSpan.FromSeconds(6), End = TimeSpan.FromSeconds(7), Lines = ["Subtitle Line 3"] },
                new Paragraph { Number = 4, Start = TimeSpan.FromSeconds(10), End = TimeSpan.FromSeconds(11), Lines = ["Subtitle Line 4"] },
                new Paragraph { Number = 5, Start = TimeSpan.FromSeconds(12), End = TimeSpan.FromSeconds(13), Lines = ["Subtitle Line 5"] }
            ]
        };

        // 0-3: no sync
        // 3-7: +1
        // 7-11: -1
        // 11-E: no sync
        var segments = new List<Segment>
        {
            new() { From = TimeSpan.FromSeconds(3), Offset = +1 },
            new() { From = TimeSpan.FromSeconds(7), Offset = -1 },
            new() { From = TimeSpan.FromSeconds(11), Offset = 0 }
        };

        // Act
        _service.Sync(subtitle, segments);

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines[0], Is.EqualTo("Subtitle Line 1"));

            Assert.That(subtitle.Paragraphs[1].Number, Is.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Start, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(subtitle.Paragraphs[1].End, Is.EqualTo(TimeSpan.FromSeconds(6)));
            Assert.That(subtitle.Paragraphs[1].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[1].Lines[0], Is.EqualTo("Subtitle Line 2"));

            Assert.That(subtitle.Paragraphs[2].Number, Is.EqualTo(3));
            Assert.That(subtitle.Paragraphs[2].Start, Is.EqualTo(TimeSpan.FromSeconds(6)));
            Assert.That(subtitle.Paragraphs[2].End, Is.EqualTo(TimeSpan.FromSeconds(7)));
            Assert.That(subtitle.Paragraphs[2].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[2].Lines[0], Is.EqualTo("Subtitle Line 3"));

            Assert.That(subtitle.Paragraphs[3].Number, Is.EqualTo(4));
            Assert.That(subtitle.Paragraphs[3].Start, Is.EqualTo(TimeSpan.FromSeconds(9)));
            Assert.That(subtitle.Paragraphs[3].End, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(subtitle.Paragraphs[3].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[3].Lines[0], Is.EqualTo("Subtitle Line 4"));

            Assert.That(subtitle.Paragraphs[4].Number, Is.EqualTo(5));
            Assert.That(subtitle.Paragraphs[4].Start, Is.EqualTo(TimeSpan.FromSeconds(12)));
            Assert.That(subtitle.Paragraphs[4].End, Is.EqualTo(TimeSpan.FromSeconds(13)));
            Assert.That(subtitle.Paragraphs[4].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[4].Lines[0], Is.EqualTo("Subtitle Line 5"));
        }
    }

    [Test]
    public void Sync_Segments_NoSync()
    {
        // Arrange
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 1"] }
            ]
        };

        // Act
        _service.Sync(subtitle, new List<Segment>());

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines[0], Is.EqualTo("Subtitle Line 1"));
        }
    }

    [Test]
    public void Sync_VisualPoint_Successfully()
    {
        // Arrange
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 1"] },
                new Paragraph { Number = 2, Start = TimeSpan.FromSeconds(3), End = TimeSpan.FromSeconds(4), Lines = ["Subtitle Line 2"] },
                new Paragraph { Number = 3, Start = TimeSpan.FromSeconds(5.5), End = TimeSpan.FromSeconds(6.5), Lines = ["Subtitle Line 3"] },
                new Paragraph { Number = 4, Start = TimeSpan.FromSeconds(7), End = TimeSpan.FromSeconds(8), Lines = ["Subtitle Line 4"] },
                new Paragraph { Number = 5, Start = TimeSpan.FromSeconds(9), End = TimeSpan.FromSeconds(10), Lines = ["Subtitle Line 5"] }
            ]
        };

        var first = new VisualPoint { Paragraph = 2, NewStart = new TimeSpan(0, 0, 0, 3, 300) };
        var last = new VisualPoint { Paragraph = 4, NewStart = new TimeSpan(0, 0, 0, 7, 300) };

        // Act
        _service.Sync(subtitle, first, last);

        // Assert
        Assert.That(subtitle.Paragraphs, Has.Count.EqualTo(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(subtitle.Paragraphs[0].Number, Is.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Start, Is.EqualTo(TimeSpan.FromSeconds(1.3)));
            Assert.That(subtitle.Paragraphs[0].End, Is.EqualTo(TimeSpan.FromSeconds(2.3)));
            Assert.That(subtitle.Paragraphs[0].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[0].Lines[0], Is.EqualTo("Subtitle Line 1"));

            Assert.That(subtitle.Paragraphs[1].Number, Is.EqualTo(2));
            Assert.That(subtitle.Paragraphs[1].Start, Is.EqualTo(TimeSpan.FromSeconds(3.3)));
            Assert.That(subtitle.Paragraphs[1].End, Is.EqualTo(TimeSpan.FromSeconds(4.3)));
            Assert.That(subtitle.Paragraphs[1].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[1].Lines[0], Is.EqualTo("Subtitle Line 2"));

            Assert.That(subtitle.Paragraphs[2].Number, Is.EqualTo(3));
            Assert.That(subtitle.Paragraphs[2].Start, Is.EqualTo(TimeSpan.FromSeconds(5.8)));
            Assert.That(subtitle.Paragraphs[2].End, Is.EqualTo(TimeSpan.FromSeconds(6.8)));
            Assert.That(subtitle.Paragraphs[2].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[2].Lines[0], Is.EqualTo("Subtitle Line 3"));

            Assert.That(subtitle.Paragraphs[3].Number, Is.EqualTo(4));
            Assert.That(subtitle.Paragraphs[3].Start, Is.EqualTo(TimeSpan.FromSeconds(7.3)));
            Assert.That(subtitle.Paragraphs[3].End, Is.EqualTo(TimeSpan.FromSeconds(8.3)));
            Assert.That(subtitle.Paragraphs[3].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[3].Lines[0], Is.EqualTo("Subtitle Line 4"));

            Assert.That(subtitle.Paragraphs[4].Number, Is.EqualTo(5));
            Assert.That(subtitle.Paragraphs[4].Start, Is.EqualTo(TimeSpan.FromSeconds(9.3)));
            Assert.That(subtitle.Paragraphs[4].End, Is.EqualTo(TimeSpan.FromSeconds(10.3)));
            Assert.That(subtitle.Paragraphs[4].Lines, Has.Count.EqualTo(1));
            Assert.That(subtitle.Paragraphs[4].Lines[0], Is.EqualTo("Subtitle Line 5"));
        }
    }

    [Test]
    public void Sync_VisualPoint_Fail_InvalidReferencePoints()
    {
        // Arrange
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph { Number = 1, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 1"] },
                new Paragraph { Number = 2, Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Lines = ["Subtitle Line 2"] }
            ]
        };

        var first = new VisualPoint { Paragraph = 1, NewStart = new TimeSpan(0, 0, 0, 1, 1) };
        var last = new VisualPoint { Paragraph = 2, NewStart = new TimeSpan(0, 0, 0, 1, 300) };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => _service.Sync(subtitle, first, last));

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Is.EqualTo("Reference points cannot have the same original timestamp"));
    }

    private static Subtitle CreateDefaultSubtitle()
    {
        var subtitle = new Subtitle
        {
            Type = "SubRip",
            OriginalFile = "Subtitle.srt",
            Paragraphs =
            [
                new Paragraph
                {
                    Number = 1,
                    Start = TimeSpan.FromSeconds(1),
                    End = TimeSpan.FromSeconds(2),
                    Lines = ["Subtitle Line"]
                }
            ]
        };
        return subtitle;
    }
}
namespace SubtitleTools.Entities;

[ExcludeFromCodeCoverage]
public class SegmentRange
{
    public TimeSpan From { get; set; }
    public TimeSpan To { get; set; }
    public double Offset { get; set; }
}
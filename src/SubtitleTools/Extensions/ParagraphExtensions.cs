namespace SubtitleTools.Extensions;

public static class ParagraphExtensions
{
    public static void Sync(this Paragraph paragraph, double seconds)
    {
        var offset = TimeSpan.FromSeconds(seconds);

        var newStart = paragraph.Start + offset;
        var newEnd = paragraph.End + offset;

        // Ensure times don't go below zero
        paragraph.Start = newStart < TimeSpan.Zero ? TimeSpan.Zero : newStart;
        paragraph.End = newEnd < TimeSpan.Zero ? TimeSpan.Zero : newEnd;

        if (paragraph.Start == TimeSpan.Zero && paragraph.End == TimeSpan.Zero)
            throw new ApplicationException($"Cannot adjust subtitle paragraph {paragraph.Number}. Becomes zero.");
    }

    public static void Sync(this Paragraph paragraph, double scale, double offset)
    {
        // Formula: newTime = scale * originalTime + offset
        var newStartTime = TimeSpan.FromMilliseconds(scale * paragraph.Start.TotalMilliseconds + offset);
        var newEndTime = TimeSpan.FromMilliseconds(scale * paragraph.End.TotalMilliseconds + offset);
        paragraph.Start = newStartTime;
        paragraph.End = newEndTime;
    }

    public static bool Valid(this Paragraph paragraph)
    {
        return paragraph.Start >= TimeSpan.Zero &&
               paragraph.End > TimeSpan.Zero &&
               paragraph.Start < paragraph.End &&
               paragraph.Lines.Count > 0;
    }
}
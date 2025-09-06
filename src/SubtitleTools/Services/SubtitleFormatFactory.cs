namespace SubtitleTools.Services;

public class SubtitleFormatFactory(IServiceProvider serviceProvider) : ISubtitleFormatFactory
{
    public ISubtitleFormat GetParser(string file)
    {
        var extension = Path.GetExtension(file);
        try
        {
            return serviceProvider.GetRequiredKeyedService<ISubtitleFormat>(extension.ToLowerInvariant());
        }
        catch (InvalidOperationException)
        {
            throw new NotSupportedException($"Subtitle format '{extension}' is not supported.");
        }
    }
}
namespace SubtitleTools.Extensions;

public static class ServiceExtensions
{
    public static void AddSubtitleToolsServices(this IServiceCollection services)
    {
        services.AddScoped<IFileSystem, FileSystem>();
        services.AddScoped<ISubtitleProcessor, SubtitleProcessor>();
    }

    public static void AddSubtitleToolsFormatServices(this IServiceCollection services)
    {
        services.AddScoped<ISubtitleFormatFactory>(provider => new SubtitleFormatFactory(provider));
        services.AddKeyedScoped<ISubtitleFormat, SubRip>(".srt");
    }

    public static void AddSubtitleToolsCommands(this IServiceCollection services)
    {
        // Register command(s)
        services.AddTransient<DetectEncodingCommand>();
        services.AddTransient<FixDiacriticsCommand>();
        services.AddTransient<SynchronizeCommand>();
        services.AddTransient<SynchronizePartialCommand>();
        services.AddTransient<VisualSyncCommand>();
    }
}
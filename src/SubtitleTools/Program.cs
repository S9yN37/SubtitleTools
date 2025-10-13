namespace SubtitleTools;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        services.Configure<Settings>(configuration.GetSection("SubtitleTools"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Settings>>().Value);

        services.AddSubtitleToolsServices();
        services.AddSubtitleToolsFormatServices();
        services.AddSubtitleToolsCommands();

        var serviceProvider = services.BuildServiceProvider();

        var app = new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .UseTypeActivator(type => serviceProvider.GetRequiredService(type))
            .SetExecutableName("SubtitleTools")
            .Build();

        await app.RunAsync(args);
    }
}
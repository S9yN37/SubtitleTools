namespace SubtitleTools.Tests.Extensions;

[Category("Unit")]
public class ServiceExtensionsTests
{
    [Test]
    public void ServiceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        var options = Substitute.For<IOptions<Settings>>();
        options.Value.Returns(new Settings { AutoCreateBackup = true });
        services.AddSingleton(options);

        services.AddSubtitleToolsServices();
        services.AddSubtitleToolsFormatServices();
        services.AddSubtitleToolsCommands();

        // Act
        var provider = services.BuildServiceProvider(true);

        // Assert
        using var scope = provider.CreateScope();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(services, Has.Count.EqualTo(9), "Service count changed");

            Assert.That(scope.ServiceProvider.GetRequiredService<IOptions<Settings>>(), Is.InstanceOf<IOptions<Settings>>());

            // SubtitleToolsServices
            Assert.That(scope.ServiceProvider.GetRequiredService<IFileSystem>(), Is.InstanceOf<FileSystem>());
            Assert.That(scope.ServiceProvider.GetRequiredService<ISubtitleProcessor>(), Is.InstanceOf<SubtitleProcessor>());

            // SubtitleToolsFormatServices
            Assert.That(scope.ServiceProvider.GetRequiredService<ISubtitleFormatFactory>(), Is.InstanceOf<SubtitleFormatFactory>());
            Assert.That(scope.ServiceProvider.GetRequiredKeyedService<ISubtitleFormat>(".srt"), Is.InstanceOf<SubRip>());

            // SubtitleToolsCommands
            var commands = new[]
            {
                typeof(FixDiacriticsCommand),
                typeof(SynchronizeCommand),
                typeof(SynchronizePartialCommand),
                typeof(VisualSyncCommand)
            };

            foreach (var command in commands)
                Assert.That(scope.ServiceProvider.GetRequiredService(command), Is.InstanceOf(command));
        }
    }
}
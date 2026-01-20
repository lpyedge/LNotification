using System;
using System.IO;
using LNotification;
using LNotification.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LNotification.Tests;

public class NotificationServiceTests
{
    [Fact]
    public void AddLNotification_JsonPath_UsesLNotificationSection()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "LNotification", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var filePath = Path.Combine(tempDirectory, "lnotification.json");
        File.WriteAllText(filePath, "{\"LNotification\":{\"TimeoutSeconds\":12}}");

        try
        {
            var services = new ServiceCollection();

            NotificationService.AddLNotification(services, Path.GetFullPath(filePath));

            using var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<NotificationConfiguration>();

            Assert.Equal("12", configuration.Configuration.GetSection("LNotification")["TimeoutSeconds"]);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}

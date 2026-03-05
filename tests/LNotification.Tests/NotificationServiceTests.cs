using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using LNotification;
using LNotification.Internal;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public void AddLNotification_IConfiguration_RegistersServices()
    {
        var configData = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>(
                    "LNotification:TimeoutSeconds", "20")
            })
            .Build();

        var services = new ServiceCollection();

        NotificationService.AddLNotification(services, configData);

        using var provider = services.BuildServiceProvider();

        // NotificationConfiguration should be registered
        var notifConfig = provider.GetRequiredService<NotificationConfiguration>();
        Assert.NotNull(notifConfig);
        Assert.Equal("20", notifConfig.Configuration.GetSection("LNotification")["TimeoutSeconds"]);
    }

    [Fact]
    public void AddLNotification_NullServices_ThrowsArgumentNullException()
    {
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            NotificationService.AddLNotification(null!, config));
    }

    [Fact]
    public void AddLNotification_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            NotificationService.AddLNotification(services, (IConfiguration)null!));
    }

    [Fact]
    public void AddLNotification_EmptyJsonPath_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            NotificationService.AddLNotification(services, ""));
    }

    [Fact]
    public void AddLNotification_WhitespaceJsonPath_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            NotificationService.AddLNotification(services, "   "));
    }

    [Fact]
    public void NotificationService_PublicApi_DoesNotContainSendMarkdownAsync()
    {
        var hasSendMarkdownAsync = typeof(NotificationService)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Any(m => m.Name == "SendMarkdownAsync");

        Assert.False(hasSendMarkdownAsync);
    }

    [Fact]
    public void AddLNotification_ZeroTimeout_UsesDefaultTimeout()
    {
        var configData = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>(
                    "LNotification:TimeoutSeconds", "0")
            })
            .Build();

        var services = new ServiceCollection();
        NotificationService.AddLNotification(services, configData);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(NotificationProviderBase.NotificationHttpClient);

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }
}


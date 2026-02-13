using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LNotification.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class NotificationProviderBaseTests
{
    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class StubConfig : ProviderConfigBase { }

    /// <summary>
    /// A test provider whose type name is "StubProvider" so it looks for "StubConfig" automatically.
    /// </summary>
    private sealed class StubProvider : NotificationProviderBase
    {
        internal bool SendCalled { get; private set; }

        internal StubProvider(NotificationOptions options)
            : base(new TestHttpClientFactory(), NullLogger.Instance, options) { }

        protected override Task SendInternalAsync(
            ProviderConfigBase config,
            string message,
            NotificationService.NotifyLevel level)
        {
            SendCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SendAsync_NoMatchingConfigs_ReturnsFalse()
    {
        // Options with no providers at all
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };

        var provider = new StubProvider(options);

        var result = await provider.SendAsync("test", NotificationService.NotifyLevel.Info, null);

        Assert.False(result);
        Assert.False(provider.SendCalled);
    }

    [Fact]
    public async Task SendAsync_WrongAlias_ReturnsFalse()
    {
        var config = new StubConfig { Alias = "production", Enabled = true };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var provider = new StubProvider(options);

        // Request a non-existent alias
        var result = await provider.SendAsync("test", NotificationService.NotifyLevel.Info, "nonexistent");

        Assert.False(result);
        Assert.False(provider.SendCalled);
    }

    [Fact]
    public async Task SendAsync_MatchingConfig_ReturnsTrue()
    {
        var config = new StubConfig { Alias = "default", Enabled = true };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var provider = new StubProvider(options);

        var result = await provider.SendAsync("test", NotificationService.NotifyLevel.Info, null);

        Assert.True(result);
        Assert.True(provider.SendCalled);
    }

    [Fact]
    public async Task SendAsync_DisabledConfig_ReturnsFalse()
    {
        var config = new StubConfig { Alias = "default", Enabled = false };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var provider = new StubProvider(options);

        var result = await provider.SendAsync("test", NotificationService.NotifyLevel.Info, null);

        // Disabled configs are filtered out in constructor, so result should be false
        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_SpecificAlias_MatchesCorrectConfig()
    {
        var config1 = new StubConfig { Alias = "dev", Enabled = true };
        var config2 = new StubConfig { Alias = "prod", Enabled = true };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config1);
        options.Providers.Add(config2);

        var provider = new StubProvider(options);

        var result = await provider.SendAsync("test", NotificationService.NotifyLevel.Info, "prod");

        Assert.True(result);
        Assert.True(provider.SendCalled);
    }

    [Fact]
    public async Task SendMarkdownAsync_NoConfigs_ReturnsFalse()
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };

        var provider = new StubProvider(options);

        var result = await provider.SendMarkdownAsync("**bold**", NotificationService.NotifyLevel.Info, null);

        Assert.False(result);
    }
}

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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

    private sealed class StubSendOptions : SendOptions { }
    private sealed class OtherSendOptions : SendOptions { }

    private sealed class StubConfig : ProviderConfigBase, IProviderSendOptions<StubSendOptions>
    {
        public StubSendOptions SendOptions { get; set; } = new();
    }

    /// <summary>
    /// A test provider whose type name is "StubProvider" so it looks for "StubConfig" automatically.
    /// </summary>
    private sealed class StubProvider : NotificationProviderBase<StubConfig, StubSendOptions>
    {
        internal bool SendCalled { get; private set; }
        internal StubSendOptions? LastOptions { get; private set; }

        internal StubProvider(NotificationOptions options)
            : base(new TestHttpClientFactory(), NullLogger.Instance, options) { }

        protected override Task SendInternalAsync(
            StubConfig config,
            string message,
            NotificationService.NotifyLevel level,
            StubSendOptions options)
        {
            SendCalled = true;
            LastOptions = options;
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
    public async Task SendAsync_NoOptions_UsesConfigDefaultSendOptions()
    {
        var defaultOptions = new StubSendOptions
        {
            ContentFormat = MessageContentFormat.Markdown
        };
        var config = new StubConfig { Alias = "default", Enabled = true, SendOptions = defaultOptions };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var provider = new StubProvider(options);

        var result = await provider.SendAsync("test", NotificationService.NotifyLevel.Info, null);

        Assert.True(result);
        Assert.Same(defaultOptions, provider.LastOptions);
    }

    [Fact]
    public async Task SendAsync_DefaultSendOptions_ContentFormatIsPlainText()
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
        Assert.NotNull(provider.LastOptions);
        Assert.Equal(MessageContentFormat.PlainText, provider.LastOptions!.ContentFormat);
    }

    [Fact]
    public async Task SendAsync_WrongSendOptionsType_ThrowsArgumentException()
    {
        var config = new StubConfig { Alias = "default", Enabled = true };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var provider = new StubProvider(options);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.SendAsync("test", NotificationService.NotifyLevel.Info, null, new OtherSendOptions()));
    }

    [Fact]
    public async Task SendAsync_WithTypedOptions_UsesProvidedMarkdownContentFormat()
    {
        var config = new StubConfig { Alias = "default", Enabled = true };
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var provider = new StubProvider(options);
        var requestOptions = new StubSendOptions
        {
            ContentFormat = MessageContentFormat.Markdown
        };

        var result = await provider.SendAsync("**markdown**", NotificationService.NotifyLevel.Info, null, requestOptions);

        Assert.True(result);
        Assert.Same(requestOptions, provider.LastOptions);
        Assert.Equal(MessageContentFormat.Markdown, provider.LastOptions!.ContentFormat);
    }

    [Fact]
    public void NotificationProviderBase_NonPublicApi_DoesNotContainSendMarkdownInternalAsync()
    {
        var hasLegacyMethod = typeof(NotificationProviderBase)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(m => m.Name == "SendMarkdownInternalAsync");

        Assert.False(hasLegacyMethod);
    }

}

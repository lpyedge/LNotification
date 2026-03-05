using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LNotification;
using LNotification.Internal;
using LNotification.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class NtfyProviderTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        internal HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        internal TestHttpClientFactory(HttpMessageHandler handler)
        {
            _client = new HttpClient(handler, disposeHandler: false);
        }

        public HttpClient CreateClient(string name) => _client;
    }

    [Fact]
    public async Task SendAsync_DefaultContentFormat_DoesNotSetMarkdownHeader()
    {
        var handler = new CapturingHandler();
        var factory = new TestHttpClientFactory(handler);
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(new NtfyProvider.NtfyConfig
        {
            Alias = "default",
            ServerUrl = "https://ntfy.sh",
            Topic = "unit-test"
        });

        var provider = new NtfyProvider(factory, NullLogger<NtfyProvider>.Instance, options);

        var result = await provider.SendAsync(
            "plain text",
            "default");

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.False(handler.LastRequest!.Headers.Contains("Markdown"));
    }

    [Fact]
    public async Task SendAsync_MarkdownContentFormat_SetsMarkdownHeader()
    {
        var handler = new CapturingHandler();
        var factory = new TestHttpClientFactory(handler);
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(new NtfyProvider.NtfyConfig
        {
            Alias = "default",
            ServerUrl = "https://ntfy.sh",
            Topic = "unit-test"
        });

        var provider = new NtfyProvider(factory, NullLogger<NtfyProvider>.Instance, options);

        var result = await provider.SendAsync<NtfyProvider.NtfySendOptions>(
            "## markdown",
            "default",
            o => o.ContentFormat = MessageContentFormat.Markdown);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.Contains("Markdown"));
    }
}

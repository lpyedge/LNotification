using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LNotification.Internal;
using LNotification.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class GotifyProviderTests
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

    private static GotifyProvider CreateProvider(CapturingHandler handler)
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(new GotifyProvider.GotifyConfig
        {
            Alias = "default",
            ServerUrl = "https://gotify.example.com",
            Token = "token"
        });

        var factory = new TestHttpClientFactory(handler);
        return new GotifyProvider(factory, NullLogger<GotifyProvider>.Instance, options);
    }

    [Fact]
    public async Task SendAsync_PriorityOutOfRange_IsMappedToZeroToTen()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync<GotifyProvider.GotifySendOptions>(
            "plain",
            "default",
            o => o.Priority = 5);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"priority\":10", body);
    }

    [Fact]
    public async Task SendAsync_Markdown_SetsMarkdownContentTypeExtra()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync<GotifyProvider.GotifySendOptions>(
            "**markdown**",
            "default",
            o => o.ContentFormat = MessageContentFormat.Markdown);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"contentType\":\"text/markdown\"", body);
    }
}

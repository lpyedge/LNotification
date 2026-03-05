using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LNotification.Internal;
using LNotification.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class PushoverProviderTests
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

    private static PushoverProvider CreateProvider(CapturingHandler handler)
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(new PushoverProvider.PushoverConfig
        {
            Alias = "default",
            ApplicationToken = "app-token",
            UserKey = "user-key"
        });

        var factory = new TestHttpClientFactory(handler);
        return new PushoverProvider(factory, NullLogger<PushoverProvider>.Instance, options);
    }

    [Fact]
    public async Task SendAsync_Markdown_UsesHtmlAndPriorityMapping()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync<PushoverProvider.PushoverSendOptions>(
            "**ok**",
            "default",
            o =>
            {
                o.ContentFormat = MessageContentFormat.Markdown;
                o.Priority = 5;
                o.Format = PushoverMessageFormat.Monospace;
            });

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("priority=2", body);
        Assert.Contains("html=1", body);
        Assert.DoesNotContain("monospace=1", body);
    }

    [Fact]
    public async Task SendAsync_PlainTextMonospace_SetsMonospaceFlag()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync<PushoverProvider.PushoverSendOptions>(
            "plain",
            "default",
            o => o.Format = PushoverMessageFormat.Monospace);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("monospace=1", body);
        Assert.DoesNotContain("html=1", body);
    }
}

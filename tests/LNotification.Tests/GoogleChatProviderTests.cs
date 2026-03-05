using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LNotification.Internal;
using LNotification.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class GoogleChatProviderTests
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

    private static GoogleChatProvider CreateProvider(CapturingHandler handler, string webhookUrl)
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(new GoogleChatProvider.GoogleChatConfig
        {
            Alias = "default",
            WebhookUrl = webhookUrl
        });

        var factory = new TestHttpClientFactory(handler);
        return new GoogleChatProvider(factory, NullLogger<GoogleChatProvider>.Instance, options);
    }

    [Fact]
    public async Task SendAsync_FallbackToNewThread_AddsReplyOptionQuery()
    {
        const string webhookUrl = "https://chat.googleapis.com/v1/spaces/SPACE/messages?key=K&token=T";
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, webhookUrl);

        var result = await provider.SendAsync<GoogleChatProvider.GoogleChatSendOptions>(
            "deploy",
            "default",
            o =>
            {
                o.ThreadKey = "release";
                o.ReplyOption = GoogleChatReplyOption.FallbackToNewThread;
            });

        Assert.True(result);
        var url = Assert.IsType<string>(handler.LastRequest?.RequestUri?.ToString());
        Assert.Contains("threadKey=release", url);
        Assert.Contains("messageReplyOption=REPLY_MESSAGE_FALLBACK_TO_NEW_THREAD", url);
    }

    [Fact]
    public async Task SendAsync_ForceNewThread_UsesUniqueThreadKeyWithoutReplyOption()
    {
        const string webhookUrl = "https://chat.googleapis.com/v1/spaces/SPACE/messages?key=K&token=T";
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, webhookUrl);

        var result = await provider.SendAsync<GoogleChatProvider.GoogleChatSendOptions>(
            "deploy",
            "default",
            o =>
            {
                o.ThreadKey = "release";
                o.ReplyOption = GoogleChatReplyOption.ForceNewThread;
            });

        Assert.True(result);
        var url = Assert.IsType<string>(handler.LastRequest?.RequestUri?.ToString());
        Assert.Contains("threadKey=release-", url);
        Assert.DoesNotContain("messageReplyOption=", url);
    }

    [Fact]
    public async Task SendAsync_NoThreadKey_UsesRawWebhookUrl()
    {
        const string webhookUrl = "https://chat.googleapis.com/v1/spaces/SPACE/messages?key=K&token=T";
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, webhookUrl);

        var result = await provider.SendAsync("deploy", "default");

        Assert.True(result);
        var url = Assert.IsType<string>(handler.LastRequest?.RequestUri?.ToString());
        Assert.Equal(webhookUrl, url);
    }
}

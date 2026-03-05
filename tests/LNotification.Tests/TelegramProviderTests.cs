using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LNotification.Internal;
using LNotification.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class TelegramProviderTests
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

    private static TelegramProvider CreateProvider(CapturingHandler handler)
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(new TelegramProvider.TelegramConfig
        {
            Alias = "default",
            BotToken = "bot-token",
            ChatId = "123456"
        });

        var factory = new TestHttpClientFactory(handler);
        return new TelegramProvider(factory, NullLogger<TelegramProvider>.Instance, options);
    }

    [Fact]
    public async Task SendAsync_Markdown_UsesMarkdownV2AndEscapesText()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync<TelegramProvider.TelegramSendOptions>(
            "a_b",
            "default",
            o => o.ContentFormat = MessageContentFormat.Markdown);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"parse_mode\":\"MarkdownV2\"", body);
        Assert.Contains("\"text\":\"a\\\\_b\"", body);
    }

    [Fact]
    public async Task SendAsync_PlainTextHtmlParseMode_UsesHtmlParseMode()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync<TelegramProvider.TelegramSendOptions>(
            "<b>ok</b>",
            "default",
            o => o.ParseMode = TelegramParseMode.Html);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"parse_mode\":\"HTML\"", body);
    }
}
